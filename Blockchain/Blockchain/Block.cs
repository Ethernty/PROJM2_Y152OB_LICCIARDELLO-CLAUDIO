using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainApp
{
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string PreviousHash { get; set; }
        public string Hash { get; set; }

        public Block(int index, DateTime timestamp, string message, string previousHash = "")
        {
            Index = index;
            Timestamp = timestamp;
            Message = message;
            PreviousHash = previousHash;    
            Hash = CalculateHash();
        }

        public string CalculateHash()
        {
            SHA256 sha256 = SHA256.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes($"{Index}-{Timestamp}-{Message}-{PreviousHash}");
            byte[] outputBytes = sha256.ComputeHash(inputBytes);

            return Convert.ToBase64String(outputBytes);
        }
    }
}
