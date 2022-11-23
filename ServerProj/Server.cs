using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using PacketsProj;

namespace ServerProj
{
    public class ConnectedClient
    {
        private Socket m_socket;
        private NetworkStream m_stream;
        private BinaryReader m_reader;
        private BinaryWriter m_writer;
        private BinaryFormatter m_formatter;
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
            // Set up the binary reader and writer using the stream and UTF8 encoding
            m_reader = new BinaryReader(m_stream, Encoding.UTF8);
            m_writer = new BinaryWriter(m_stream, Encoding.UTF8);
            // Give the user a blank name to be set
            m_name = "";
            // Initialise the binary formatter
            m_formatter = new BinaryFormatter();
        }

        public void Close()
        {
            m_stream.Close();
            m_reader.Close();
            m_writer.Close();
            m_socket.Close();
        }

        public Packet Read()
        {
            // Create a lock using m_readLock
            lock (m_readLock)
            {
                // Check the size of the array is not -1 and store it to an int
                int numberOfBytes;
                if ((numberOfBytes = m_reader.ReadInt32()) != -1)
                {
                    // Use the number of bytes to read the correct number of bytes and store in the buffer
                    byte[] buffer = m_reader.ReadBytes(numberOfBytes);
                    // Create a new memory stream and pass the byte array into the constructor
                    MemoryStream memoryStream = new MemoryStream(buffer);
                    // use the formatter to deserialise the data in the memory stream, cast it to a packet and return it.
                    return m_formatter.Deserialize(memoryStream) as Packet;
                }
            }
            return null;
        }

        /// <summary>Sends binary data that is generated from a serialised packet</summary>
        /// <param name="message">a serialised packet of a PacketType, determined in the enum</param>
        public void Send(Packet message)
        {
            // Create a lock using m_writeLock
            lock (m_writeLock)
            {
                // Create a new memory stream object used to store binary data.
                MemoryStream memoryStream = new MemoryStream();
                // Use the binary formatter to serialise message, and store this into the memory stream
                m_formatter.Serialize(memoryStream, message);
                // Get the byte array from the memory stream and store into buffer
                byte[] buffer = memoryStream.GetBuffer();
                // Write the length of this array to m_writer, so the size can be checked on the recieving end
                m_writer.Write(buffer.Length);
                // Write the buffer to m_writer
                m_writer.Write(buffer);
                // Flush the writer
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
            Packet recievedMessage;

            ConnectedClient client = m_clients[index];

            // Send a welcome message to the client.
            //client.Send("Connected.");

            // Create a read / write loop to allow the client and server to have a back and forth conversation
            // Thread will pause here until readline recieves data as it is a blocking call
            while((recievedMessage = client.Read()) != null)
            {
                // Switch on the type of packet (message type) recieved
                switch (recievedMessage.m_packetType)
                {
                    case PacketType.ChatMessage:
                        // Cast the chatMessagePacket to be the right type of packet class
                        ChatMessagePacket chatMessagePacket = (ChatMessagePacket)recievedMessage;
                        // Pass the recieved message into to GetReturnMessage() which will return a new string that shall be the servers repsonse
                        // Send the return message as a new chat message packet back to the client
                        m_clients[index].Send(new ChatMessagePacket(GetReturnMessage(chatMessagePacket.m_message)));
                        break;
                    case PacketType.PrivateMessage:
                        break;
                    case PacketType.ClientName:
                        break;
                    default:
                        break;
                }
                
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
                m_clients[i].Send(new ChatMessagePacket(message));
            }
        }

        private string GetReturnMessage(string message)
        {
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
