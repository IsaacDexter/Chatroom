using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServerProj
{
    public class Server
    {
        private TcpListener m_tcpListener;

        public Server(IPAddress ipAddress, int port)
        {
            m_tcpListener = new TcpListener(ipAddress, port);
        }

        /// <summary>
        /// Start listening for a connection. When found,
        /// Accept pending connection and save the returned socket into socket.
        /// This is a blocking function, the program will wait here until a socket has been found
        /// </summary>
        public void Start()
        {
            // 
            m_tcpListener.Start();
            Console.WriteLine("Listening...");

            // Accept pending connection and save the returned socket into socket.
            // This is a blocking function, the program will wait here until a socket has been found
            Socket socket = m_tcpListener.AcceptSocket();
            Console.WriteLine("Connection made.");
            ClientMethod(socket);
        }

        /// <summary>
        /// Stop the tcp listener to prevent it form accepting new connections
        /// </summary>
        public void Stop()
        {
            m_tcpListener.Stop();
        }

        /// <summary>
        /// Used to read and write to the client
        /// </summary>
        /// <param name="socket"></param>
        private void ClientMethod(Socket socket)
        {
            string recievedMessage;

            // Network stream is used to allow the data to be sent over the network
            NetworkStream stream = new NetworkStream(socket, true);

            // Create stream reader and writer using the networkStream, and utf8 encoding
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

            // Send a welcome message to the client. Flushing will push the data to the stream.
            writer.WriteLine("Connection to server made.");
            writer.Flush();

            // Create a read / write loop to allow the client and server to have a back and forth conversation
            // Thread will pause here until readline recieves data as it is a blocking call
            while((recievedMessage = reader.ReadLine()) != null)
            {
                // pass the recieved message into to GetReturnMEssage() which will return a new string that shall be the servers repsonse.
                writer.WriteLine(GetReturnMessage(recievedMessage));
                writer.Flush();
            }
            // Close the socket connection.
            socket.Close();
        }

        private string GetReturnMessage(string code)
        {
            if (code == "exit")
            {
                return "Exiting...";
            }
            return "thog dont caare";
        }
    }



    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server");

            //create a new instance of the Server using the IP Address 127.0.0.1 (this is a loopback address so that we can run the connection on our local machines) and use the port 4444
            Server server = new Server(IPAddress.Parse("127.0.0.1"), 4444);
            //Start and stop the server
            server.Start();
            server.Stop();

            Console.ReadLine();
        }
    }
}
