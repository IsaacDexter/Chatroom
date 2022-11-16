using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ClientProj
{
    public class Client
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private StreamWriter writer;
        private StreamReader reader;
        public Client()
        {

        }

        public bool Connect(IPAddress ipAddress, int port)
        {
            return true;
        }

        public void Run()
        {

        }

        private void ProcessServerResponse()
        {

        }
    }
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("Client");
            Console.ReadLine();
        }
    }
}
