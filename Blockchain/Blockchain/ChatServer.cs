using BlockchainApp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainApp
{
    public class ChatServer
    {
        private TcpListener server;
        private Blockchain chatBlockchain;
        private Dictionary<string, TcpClient> clients;
        private readonly object lockObject;

        public ChatServer(string ipAddress, int port)
        {
            server = new TcpListener(IPAddress.Parse(ipAddress), port);
            chatBlockchain = new Blockchain();
            clients = new Dictionary<string, TcpClient>();
            lockObject = new object();
        }

        public void Start()
        {
            server.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Task.Run(() => HandleClient(client));
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount;

            // Get the username from the client
            byteCount = stream.Read(buffer, 0, buffer.Length);
            string username = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim();

            lock (lockObject)
            {
                clients[username] = client;
            }
            Console.WriteLine($"Client {username} connected...");

            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.ASCII.GetString(buffer, 0, byteCount).Trim();
                Console.WriteLine($"Received from {username}: {message}");

                lock (lockObject)
                {
                    if (message.StartsWith("INVITE"))
                    {
                        HandleInvite(message, username);
                    }
                    else if (message.StartsWith("ACCEPT") || message.StartsWith("DECLINE"))
                    {
                        ForwardMessage(message);
                    }
                    else
                    {
                        Block newBlock = new Block(chatBlockchain.Chain.Count, DateTime.Now, message);
                        chatBlockchain.AddBlock(newBlock);
                        BroadcastBlock(newBlock);
                    }
                }
            }

            lock (lockObject)
            {
                clients.Remove(username);
            }
            client.Close();
            Console.WriteLine($"Client {username} disconnected...");
        }

        private void HandleInvite(string message, string senderUsername)
        {
            string[] parts = message.Split(' ');
            if (parts.Length != 2) return;

            string targetUsername = parts[1];
            if (clients.ContainsKey(targetUsername))
            {
                TcpClient targetClient = clients[targetUsername];
                NetworkStream targetStream = targetClient.GetStream();
                string inviteMessage = $"INVITE {senderUsername}";
                byte[] buffer = Encoding.ASCII.GetBytes(inviteMessage);
                targetStream.Write(buffer, 0, buffer.Length);
            }
        }

        private void ForwardMessage(string message)
        {
            string[] parts = message.Split(' ');
            if (parts.Length != 2) return;

            string targetUsername = parts[1];
            if (clients.ContainsKey(targetUsername))
            {
                TcpClient targetClient = clients[targetUsername];
                NetworkStream targetStream = targetClient.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                targetStream.Write(buffer, 0, buffer.Length);
            }
        }

        private void BroadcastBlock(Block block)
        {
            string blockData = JsonConvert.SerializeObject(block);
            byte[] buffer = Encoding.ASCII.GetBytes(blockData);
            foreach (var client in clients.Values)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
