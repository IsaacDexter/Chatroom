using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServerProj
{
    public class Message
    {
        private string m_sender;
        private string m_recipient;
        private string m_message;

        public Message(string sender, string recipient, string message)
        {
            m_sender = sender;
            m_recipient = recipient;
            m_message = message;
        }

        public string GetSender()
        {
            return m_sender;
        }

        public string GetRecipient()
        {
            return m_recipient;
        }

        public string GetMessage()
        {
            return m_message;
        }
    }

    public class ConnectedClient
    {
        private Socket m_socket;
        private NetworkStream m_stream;
        private StreamReader m_reader;
        private StreamWriter m_writer;
        private object m_readLock;
        private object m_writeLock;

        private string m_name;

        public ConnectedClient(Socket socket)
        {
            // Create new instances of the read and write locks
            m_readLock = new object();
            m_writeLock = new object();
            // Set m_socket to a reference of the server's socket
            m_socket = socket;
            // Set up the stream using the socket
            m_stream = new NetworkStream(socket, true);
            // Set up the reader and writer using the stream and UTF8 encoding
            m_reader = new StreamReader(m_stream, Encoding.UTF8);
            m_writer = new StreamWriter(m_stream, Encoding.UTF8);
            // Give the user a blank name to be set
            m_name = "";
        }

        public void Close()
        {
            m_stream.Close();
            m_reader.Close();
            m_writer.Close();
            m_socket.Close();
        }

        public Message Read()
        {
            // Create a lock using m_readLock
            lock (m_readLock)
            {
                string sender = m_reader.ReadLine();
                string recipient = m_reader.ReadLine();
                string message = m_reader.ReadLine();
                return new Message(sender, recipient, message);
            }
        }

        public void Send(string message)
        {
            // Create a lock using m_writeLock
            lock (m_writeLock)
            {
                // Write the message string to the writer and flush.
                m_writer.WriteLine(message);
                m_writer.Flush();
            }
        }

        public void UpdateName(string name)
        {
            m_name = name;
        }
    }
    public class Server
    {
        private TcpListener m_tcpListener;
        // A key value pair collection that is thread safe
        private ConcurrentDictionary<int, ConnectedClient> m_clients;

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
            // Initialise the client dictionary
            m_clients = new ConcurrentDictionary<int, ConnectedClient>();
            int clientIndex = 0;
            // 
            m_tcpListener.Start();

            //The max number of clients that can be connected to the server
            int maxClients = 10;
            
            // While there are less than maxClients clients...
            while (m_clients.Count() < maxClients)
            {
                // ... run Connection logic
                Console.WriteLine("Listening...");

                // Accept pending connection and save the returned socket into socket.
                // This is a blocking function, the program will wait here until a socket has been found
                Socket socket = m_tcpListener.AcceptSocket();
                Console.WriteLine("Connection made.");

                // Once the socket has been accepted, create a new instance of the connectedClient class and pass in the socket
                ConnectedClient client = new ConnectedClient(socket);
                //Create a new int value set to be the value of the client index. THis has to be done because starting the threads requires passing by reference
                int index = clientIndex;
                // Increase the client index
                clientIndex++;
                // Add the newly connected client into the client dictionary
                if(!m_clients.TryAdd(index, client))
                {
                    // If this fails, break the loop and shut down the server
                    break;
                }

                // Start the client method in a new thread, allowing the server to service multiple clients
                Thread clientThread = new Thread(() => { ClientMethod(index); });
                clientThread.Start();

                Broadcast("Client " + index + " has connected.");
            }
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
        private void ClientMethod(int index)
        {
            Message recievedMessage;

            ConnectedClient client = m_clients[index];

            // Send a welcome message to the client.
            client.Send("Connected.");

            // Create a read / write loop to allow the client and server to have a back and forth conversation
            // Thread will pause here until readline recieves data as it is a blocking call
            while((recievedMessage = client.Read()) != null)
            {
                // pass the recieved message into to GetReturnMessage() which will return a new string that shall be the servers repsonse.
                client.Send(GetReturnMessage(recievedMessage));
                Console.WriteLine("Message Recieved: " + recievedMessage.GetMessage());
            }

            // Close the client, and remove it from the dictionary
            client.Close();
            m_clients.TryRemove(index, out client);
        }

        /// <summary>
        /// Sends a message to every connected client
        /// </summary>
        /// <param name="message">The message to be broadcast</param>
        public void Broadcast(string message)
        {
            // For each client...
            for (int i = 0; i < m_clients.Count; i++)
            {
                m_clients[i].Send(message);
            }
        }

        private string GetReturnMessage(Message message)
        {
            //Handle server operations. In server operations, the sender is always the id.
            if (message.GetRecipient() == "Server")
            {
                // User has updated their nickname
                if (message.GetMessage().Contains("nick="))
                {
                    string nickname = message.GetMessage().Substring(message.GetMessage().IndexOf('=')+1);
                    m_clients[int.Parse(message.GetSender())].UpdateName(nickname);
                    return "Your nickname has been set to " + nickname;
                }
                return "Exiting...";
            }
            //handle messages
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
