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
using System.Security.Cryptography;

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

        public string m_name { get; set; }

        public ConnectedClient(Socket socket)
        {
            InitialiseEncryption();
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

        #region Disconnecting

        public void Close()
        {
            m_stream.Close();
            m_reader.Close();
            m_writer.Close();
            m_socket.Close();
        }

        #endregion

        #region Recieving

        public Packet Read()
        {
            // Create a lock using m_readLock
            lock (m_readLock)
            {
                try
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
                catch   //Handle on if the client has disconnected
                {
                    Console.WriteLine(m_name + " disconnected.");
                    return null;
                }
            }
            return null;
        }

        #endregion

        #region Sending

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

        #endregion

        #region Encryption

        private void InitialiseEncryption()
        {
            // Instanciate RSACryptoServiceProvider object. The int is the size of the key.
            m_rsaProvider = new RSACryptoServiceProvider(1024);
            // Use m_rsaProvider to generate a private key (true = private)
            m_privateKey = m_rsaProvider.ExportParameters(true);
            // Use m_rsaProvider to generate a public key (false = public)
            m_publicKey = m_rsaProvider.ExportParameters(false);
        }

        private RSACryptoServiceProvider m_rsaProvider;
        public RSAParameters m_publicKey { get; private set; }
        private RSAParameters m_privateKey;
        public RSAParameters m_clientKey { set; private get; }

        private byte[] Encrypt(byte[] data)
        {
            // Lock on service provider to prevent race conditions
            lock (m_rsaProvider)
            {
                // Set the service proider to use the server key
                m_rsaProvider.ImportParameters(m_clientKey);
                // Generate an encrypted byte array and return it
                return m_rsaProvider.Encrypt(data, true);
            }
        }

        private byte[] Decrypt(byte[] data)
        {
            // Lock on service provider to prevent race conditions
            lock (m_rsaProvider)
            {
                // Set the service proider to use the client key
                m_rsaProvider.ImportParameters(m_privateKey);
                // decrypt the byte array and return it
                return m_rsaProvider.Decrypt(data, true);
            }
        }

        /// <summary></summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] EncryptString(string message)
        {
            // Convert the parameter string into a byte array, and encrypt it, then return it
            return Encrypt(Encoding.UTF8.GetBytes(message));
        }

        public string DecryptString(byte[] message)
        {
            // Call the decrypt method to decrypt the byte array, then convert it into a string, then return it
            return Encoding.UTF8.GetString(Decrypt(message));
        }

        #endregion

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

        #region Searching

        /// <param name="name">The name, as a string, of the client to find</param>
        /// <returns>The client of that name. NULL if no client was found</returns>
        public ConnectedClient FindClient(string name)
        {
            return m_clients.FirstOrDefault(c => c.Value.m_name == name).Value;
        }

        /// <param name="client">The client to find</param>
        /// <returns>The ID of that client. NULL if no client was found</returns>
        public int FindClientID(ConnectedClient client)
        {
            return m_clients.FirstOrDefault(c => c.Value == client).Key;
        }

        #endregion

        #region Updating

        /// <summary>Check the clients nickname is unique, Update the clients nickname serverside and broadcast it to the clients so they can update it clientside</summary>
        /// <param name="name">The client's new name to go by</param>
        /// <param name="oldName">The client's original name they went by</param>
        private void UpdateNickname(string name, string oldName)
        {
            // Find the ConnectedClient who made this request
            ConnectedClient client = FindClient(oldName);
            // If we could not find the client who made the request, return.
            if (client != null)
            {
                // Check to see if client's new name is already in use...
                if (FindClient(name) != null)
                {
                    // If it somehow is, set the clients name to a default one
                    name = "Client " + FindClientID(client);
                }
                // Otherwise...
                // Send the return message as a client name packet back to each client, with the current name as oldName
                Broadcast(new UpdateNicknamePacket(name, oldName));
                // update this clients name on the server
                client.m_name = name;
            }
            
        }

        #endregion

        #region Connecting

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

                if(!MakeConnection(socket, ref clientIndex))
                {
                    break;
                }
            }
        }

        /// <summary>When The connection has been made in Start(), call this with the socket and index. It will handle initial transfer of information, handshakes, etc.</summary>
        /// <param name="socket">The socket to instanciate the new connected client from</param>
        /// <param name="clientIndex">A reference to the index of the client</param>
        /// <returns></returns>
        private bool MakeConnection(Socket socket, ref int clientIndex)
        {
            Console.WriteLine("Connection made.");

            // Once the socket has been accepted, create a new instance of the connectedClient class and pass in the socket
            ConnectedClient client = new ConnectedClient(socket);

            // Create a new int value set to be the value of the client index. THis has to be done because starting the threads requires passing by reference
            int index = clientIndex;
            // Set the clients default name
            client.m_name = "Client " + clientIndex;

            // Update each client's client lists to recognise this new client that has joined
            Broadcast(new ClientJoinPacket(client.m_name));

            // Increase the client index
            clientIndex++;
            // Add the newly connected client into the client dictionary
            if (!m_clients.TryAdd(index, client))
            {
                // If this fails, break the loop and shut down the server
                return false;
            }

            // Start the client method in a new thread, allowing the server to service multiple clients
            Thread clientThread = new Thread(() => { Listen(index); });
            clientThread.Start();

            // Send the client its personal public key, prompting the client to reciprocate the handshake.
            client.Send(new ServerKeyPacket(client.m_publicKey));
            // Send the client each of the pre-existing clients, including itself.
            foreach (var connectedClientPair in m_clients)
            {
                client.Send(new ClientJoinPacket(connectedClientPair.Value.m_name));
            }

            return true;
        }

        #endregion

        #region Disconnecting

        /// <summary>
        /// Stop the tcp listener to prevent it form accepting new connections
        /// </summary>
        public void Stop()
        {
            m_tcpListener.Stop();
        }

        #endregion

        #region Recieving

        /// <summary>
        /// Used to read and write to the client
        /// </summary>
        /// <param name="socket"></param>
        private void Listen(int index)
        {
            Packet recievedMessage;

            ConnectedClient client = m_clients[index];

            // Send a welcome message to the client.
            //client.Send("Connected.");

            // Create a read / write loop to allow the client and server to have a back and forth conversation
            // Thread will pause here until readline recieves data as it is a blocking call
            while((recievedMessage = client.Read()) != null)
            {
                HandleResponse(recievedMessage, client);
                
            }
            client.Close();
            m_clients.TryRemove(index, out client);
            // Inform the other clients that this client is leaving
            Broadcast(new ClientLeavePacket(client.m_name));
            // Close the client, and remove it from the dictionary
        }

        public void HandleResponse(Packet packet, ConnectedClient client)
        {
            // Switch on the type of packet (message type) recieved
            switch (packet.m_packetType)
            {
                case PacketType.CHAT_MESSAGE:
                    {
                        // Cast the recieved packet to be the right type of client name packet class
                        ChatMessagePacket chatMessage = (ChatMessagePacket)packet;
                        // Broadcast the return message as a new chat message packet back to all clients
                        Broadcast(new ChatMessagePacket(chatMessage.m_message, client.m_name));
                        break;
                    }
                case PacketType.CHAT_MESSAGE_ENCRYPTED:
                    {
                        // Cast the recieved packet to be the right type of client name packet class
                        EncryptedChatMessagePacket encryptedChatMessage = (EncryptedChatMessagePacket)packet;
                        // Broadcast the return message as a new encrypted chat message packet back to all clients
                        string message = client.DecryptString(encryptedChatMessage.m_message);
                        EncryptedBroadcast(new ChatMessagePacket(message, client.m_name));
                        break;
                    }
                case PacketType.DIRECT_MESSAGE:
                    {
                        // Cast the recieved packet to be a direct packet
                        DirectMessagePacket directMessage = (DirectMessagePacket)packet;
                        // Search m_clients for the client the message was addressed to
                        ConnectedClient recipient = FindClient(directMessage.m_recipient);
                        // So long as that client exists
                        if (recipient != null)
                        {
                            // Send the message on its way to that client, with a the recipient set to be the sendee, so the client knows who messaged them
                            recipient.Send(new DirectMessagePacket(directMessage.m_message, client.m_name));
                        }
                        break;
                    }
                case PacketType.DIRECT_MESSAGE_ENCRYPTED:
                    {
                        // Cast the recieved packet
                        EncryptedDirectMessagePacket encryptedDirectMessage = (EncryptedDirectMessagePacket)packet;
                        // Decrypt the recipient's name, which is encrypted via the server key
                        string name = client.DecryptString(encryptedDirectMessage.m_recipient);
                        // Search m_clients for the client the message was addressed to
                        ConnectedClient recipient = FindClient(name);
                        // So long as that client exists
                        if(recipient != null)
                        {
                            // Encrypt the sender's name using the server key, which the recipient will decrypt with the server key
                            byte[] sender = recipient.EncryptString(client.m_name);
                            // Send the message to intended client, with the sendees name, encrypted via the server, as the recipient, so the client knows who messaged them
                            recipient.Send(new EncryptedDirectMessagePacket(encryptedDirectMessage.m_message, sender));
                        }
                        break;
                    }
                case PacketType.UPDATE_NICKNAME:
                    {
                        // Cast the recieved packet to be the right type of client name packet class
                        UpdateNicknamePacket clientName = (UpdateNicknamePacket)packet;
                        // Check the clients nickname is unique, Update the clients nickname serverside and broadcast it to the clients so they can update it clientside
                        UpdateNickname(clientName.m_name, client.m_name);
                        break;
                    }
                case PacketType.SERVER_KEY:
                    {
                        // Cast the recieved packet to be the right type of client name packet class
                        ServerKeyPacket clientKey = (ServerKeyPacket)packet;
                        // Set that client's serverside client key to be equal to their public key
                        client.m_clientKey = clientKey.m_key;
                        break;
                    }
                case PacketType.PUBLIC_KEY:
                    {
                        // Cast the recieved packet into a public key packet
                        PublicKeyPacket publicKey = (PublicKeyPacket)packet;
                        ConnectedClient recipient = FindClient(publicKey.m_name);
                        // So long as that client exists
                        if (recipient != null)
                        {
                            // Send the key on its way to that client, with a the recipient set to be the sendee, so the client knows who sent the key to them
                            recipient.Send(new PublicKeyPacket(publicKey.m_key, client.m_name));
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion

        #region Sending

        /// <summary>
        /// Sends a packet to each and every client on the server
        /// </summary>
        /// <param name="packet">The packet to send</param>
        public void Broadcast(Packet packet)
        {
            foreach (var connectedClient in m_clients)
            {
                connectedClient.Value.Send(packet); 
            }
        }

        /// <summary>
        /// Sends an encrypted chat message to each client on the server
        /// </summary>
        /// <param name="packet">The chat message packet to send</param>
        public void EncryptedBroadcast(ChatMessagePacket packet)
        {
            // For each client...
            foreach (var connectedClientPair in m_clients)
            {
                ConnectedClient client = connectedClientPair.Value;

                // Encrypt the message using their public key and send it
                client.Send(new EncryptedChatMessagePacket(client.EncryptString(packet.m_message), client.EncryptString(packet.m_sender)));
            }
        }

        #endregion
    }



    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server");
            Console.WriteLine("Enter server port:");
            int port = -1;
            bool valid = false;
            while (!valid)
            {
                while (!valid)
                {
                    valid = int.TryParse(Console.ReadLine(), out port);
                    valid = port < 65536;
                    valid = port > 1024;
                    if (!valid)
                    {
                        Console.WriteLine("Invalid port. Ports range from 1024 - 65536");
                    }
                }
                try
                {
                    //create a new instance of the Server using the IP Address 127.0.0.1 (this is a loopback address so that we can run the connection on our local machines) and use the port 4444
                    Server server = new Server(IPAddress.Parse("127.0.0.1"), port);
                    valid = true;
                    //Start and stop the server
                    server.Start();
                    server.Stop();
                }
                catch (Exception)
                {
                    valid = false;
                    Console.WriteLine("Could not create server.");
                }
            }
            Console.ReadLine();
        }
    }
}
