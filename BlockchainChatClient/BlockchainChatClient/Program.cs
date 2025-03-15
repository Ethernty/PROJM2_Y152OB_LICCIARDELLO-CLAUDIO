using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace BlockchainChatClient
{

    class Program
    {
        

        static void Main(string[] args)
        {
            ChatClient chatClient = new ChatClient("127.0.0.1", 5000);
            chatClient.Start();
        }
    }
}
