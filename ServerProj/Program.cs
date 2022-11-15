using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServerProj
{
    public class Server
    {
        private TcpListener tcpListener;

        public Server(string ipAddress, int port)
        {

        }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        private void ClientMethod(Socket socket)
        {

        }

        private string GetReturnMessage(string code)
        {
            return "";
        }
    }



    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server");
            Console.ReadLine();
        }
    }
}
