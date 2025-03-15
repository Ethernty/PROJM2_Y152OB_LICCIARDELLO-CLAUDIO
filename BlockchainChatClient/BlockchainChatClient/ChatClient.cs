using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainChatClient
{
    public class ChatClient
    {
        private TcpClient client;
        private NetworkStream stream;
        private List<string> pendingInvitations;
        private bool chatActive;
        private string chatPartner;
        private List<string> chatHistory;
        private string username;

        public ChatClient(string serverAddress, int port)
        {
            client = new TcpClient(serverAddress, port);
            stream = client.GetStream();
            pendingInvitations = new List<string>();
            chatHistory = new List<string>();
        }

        public void Start()
        {
            Console.Write("Enter your username: ");
            username = Console.ReadLine();

            SendMessage(username);

            Task.Run(() => ListenForMessages());

            while (true)
            {
                if (!chatActive)
                {
                    ShowMainMenu();
                }
                else
                {
                    StartChat();
                }
            }
        }

        private void ShowMainMenu()
        {
            Console.WriteLine("Menu:");
            Console.WriteLine("1. Chat with someone");
            Console.WriteLine("2. Exit");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ShowChatMenu();
                    break;
                case "2":
                    Exit();
                    break;
                default:
                    Console.WriteLine("Invalid choice, please try again.");
                    break;
            }
        }

        private void ShowChatMenu()
        {
            Console.WriteLine("Chat Menu:");
            Console.WriteLine("1. Invite chat");
            Console.WriteLine("2. Receive chat invitation");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    InviteChat();
                    break;
                case "2":
                    ReceiveChatInvitation();
                    break;
                default:
                    Console.WriteLine("Invalid choice, please try again.");
                    break;
            }
        }

        private void InviteChat()
        {
            Console.Write("Enter the username of the person you want to chat with: ");
            string otherUsername = Console.ReadLine();
            SendMessage($"INVITE {otherUsername}");

            Console.WriteLine("Waiting for the other user to accept the invitation...");
        }

        private void ReceiveChatInvitation()
        {
            Console.WriteLine("Pending Chat Invitations:");
            if (pendingInvitations.Count == 0)
            {
                Console.WriteLine("No pending invitations.");
                return;
            }

            foreach (var invitation in pendingInvitations)
            {
                Console.WriteLine(invitation);
            }

            Console.Write("Do you want to accept any invitation? (yes/no): ");
            string response = Console.ReadLine().ToLower();

            if (response == "yes")
            {
                Console.Write("Enter the username of the person whose invitation you want to accept: ");
                string inviter = Console.ReadLine();
                if (pendingInvitations.Contains(inviter))
                {
                    pendingInvitations.Remove(inviter);
                    SendMessage($"ACCEPT {inviter}");
                    Console.WriteLine("Invitation accepted.");
                    chatPartner = inviter;
                    chatActive = true;
                }
                else
                {
                    Console.WriteLine("No such invitation found.");
                }
            }
        }

        private void StartChat()
        {
            Console.Clear();
            Console.WriteLine($"You are now chatting with {chatPartner}. Type 'exit' to end the chat.");

            foreach (var message in chatHistory)
            {
                DisplayNewMessage(message);
            }

            while (chatActive)
            {
                string message = Console.ReadLine();
                if (message.ToLower() == "exit")
                {
                    SendMessage($"ENDCHAT {chatPartner}");
                    chatActive = false;
                }
                else
                {
                    string chatMessage = $"{username}: {message}";
                    SendMessage(chatMessage);
                }
            }
        }

        private void SendMessage(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }

        private void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            int byteCount;

            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.ASCII.GetString(buffer, 0, byteCount);

                if (message.StartsWith("{") && message.EndsWith("}"))
                {
                    try
                    {
                        var block = JsonConvert.DeserializeObject<Block>(message);
                        chatHistory.Add(block.Message);
                        DisplayChatHistory();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(message);
                    }
                }
                else
                {
                    HandleControlMessages(message);
                }
            }

            client.Close();
            Console.WriteLine("Disconnected from server...");
        }

        private void HandleControlMessages(string message)
        {
            if (message.StartsWith("INVITE"))
            {
                HandleChatInvite(message);
            }
            else if (message.StartsWith("ACCEPT"))
            {
                HandleAcceptMessage(message);
            }
            else if (message.StartsWith("DECLINE"))
            {
                HandleDeclineMessage(message);
            }
            else if (message.StartsWith("ENDCHAT"))
            {
                Console.WriteLine($"{chatPartner} has ended the chat.");
                chatActive = false;
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        private void DisplayChatHistory()
        {
            Console.Clear();
            Console.WriteLine($"You are now chatting with {chatPartner}. Type 'exit' to end the chat.");
            foreach (var message in chatHistory)
            {
                DisplayNewMessage(message);
            }
        }

        private void DisplayNewMessage(string message)
        {
            if (message.StartsWith($"{username}:"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void HandleAcceptMessage(string message)
        {
            string[] parts = message.Split(' ');
            if (parts.Length != 2) return;

            string targetUsername = parts[1];

            Console.WriteLine($"{targetUsername} accepted your chat request.");
            chatPartner = targetUsername;
            chatActive = true;
        }

        private void HandleDeclineMessage(string message)
        {
            string[] parts = message.Split(' ');
            if (parts.Length != 2) return;

            string targetUsername = parts[1];

            Console.WriteLine($"{targetUsername} declined your chat request.");
        }

        private void HandleChatInvite(string message)
        {
            string[] parts = message.Split(' ');
            string inviter = parts[1];

            pendingInvitations.Add(inviter);
            Console.WriteLine($"New chat invitation from {inviter}. You can view and accept it in the 'Receive chat invitation' menu.");
        }

        private void Exit()
        {
            Console.WriteLine("Exiting...");
            client.Close();
        }
    }
}
