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
    /// <summary>
    /// Client will connect to the Server class and will be capable of sending and receiving a string.
    /// </summary>
    public class Client
    {
        private TcpClient m_tcpClient;
        private NetworkStream m_stream;
        private StreamReader m_reader;
        private StreamWriter m_writer;

        private MainWindow m_mainWindow;
        public Client()
        {
            //Create a new instance of TcpClient
            m_tcpClient = new TcpClient();
        }

        /// <summary>
        /// This method will try and set up a connection to the server using a try/catch to check for errors
        /// </summary>
        /// <param name="ipAddress">The IP of the server to connect to, already parsed</param>
        /// <param name="port">The port to connect to, as a valid int from 1-65535</param>
        /// <returns>Whether a </returns>
        public bool Connect(IPAddress ipAddress, int port)
        {
            try
            {
                // Try and connect TcpClient to the remote server
                m_tcpClient.Connect(ipAddress, port);
                // Set the network stream
                m_stream = m_tcpClient.GetStream();
                // Create stream reader and writer using the networkStream, and utf8 encoding
                m_reader = new StreamReader(m_stream, Encoding.UTF8);
                m_writer = new StreamWriter(m_stream, Encoding.UTF8);
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                return false;
            }
        }

        public void Run()
        {
            // Create the instance of main window
            m_mainWindow = new MainWindow();

            string userInput;
            // Create a thread that will process server response and start it
            Thread readThread = new Thread(() => { ProcessServerResponse(); });
            readThread.Start();

            // Call show dialogue on the form class
            m_mainWindow.ShowDialog();

            while ((userInput = Console.ReadLine()) != null)
            {
                SendMessage(userInput);
                // Check to see if the user input is equal to the exit condition used in the server...
                if (userInput == "exit")
                {
                    // ...If it is, break out of the while loop
                    break;
                }
            }
            // Close the TcpClient
            m_tcpClient.Close();
        }

        /// <summary>
        /// Print out the stream readers value to the console. 
        /// </summary>
        private void ProcessServerResponse()
        {
            // While the client is connected...
            while (m_tcpClient.Connected)
            {
                try
                {
                    // Write the messages to the console
                    // Don't forget that readline is a blocking method, the client could get stuck here if nothing is sent from the server.
                    Console.WriteLine("Server says: " + m_reader.ReadLine());
                }
                catch (Exception)
                {
                    //Avoids disrupting a blocking call whten the connection is closed down
                    break;
                }
            }
        }

        public void SendMessage(string message)
        {
            // Write message to the server and flush it
            m_writer.WriteLine(message);
            m_writer.Flush();
        }
    }

    internal class Program
    {
        [STAThread]
        /// <summary>
        /// Create a new instance of the client and connect it to the network
        /// </summary>
        static void Main()
        {
            Console.WriteLine("Client");
            Client client = new Client();
            // Check to see if the client can connect to the network. If so...
            if (client.Connect(IPAddress.Parse("127.0.0.1"), 4444))
            {
                // Run the client. Otherwise...
                client.Run();
            }
            else
            {
                Console.WriteLine("Failed to connect to the server");
            }

            Console.ReadLine();
        }
    }
}
