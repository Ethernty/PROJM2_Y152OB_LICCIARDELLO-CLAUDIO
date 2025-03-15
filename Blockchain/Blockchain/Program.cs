using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockchainApp
{
    class Program
    {

        static void Main(string[] args)
        {
            ChatServer chatServer = new ChatServer("127.0.0.1", 5000);
            chatServer.Start();
        }
    }
}

