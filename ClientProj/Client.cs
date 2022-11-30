using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using PacketsProj;
using System.Security.Cryptography;

namespace ClientProj
{
    /// <summary>
    /// Client will connect to the Server class and will be capable of sending and receiving a string.
    /// </summary>
    public class Client
    {
        #region Initialisation

        private TcpClient m_tcpClient;
        private NetworkStream m_stream;
        private BinaryReader m_reader;
        private BinaryWriter m_writer;
        private BinaryFormatter m_formatter;

        private MainWindow m_mainWindow;
        public Client()
        {
            // Create a new instance of TcpClient
            m_tcpClient = new TcpClient();

            InitialiseEncryption();
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
                m_reader = new BinaryReader(m_stream, Encoding.UTF8);
                m_writer = new BinaryWriter(m_stream, Encoding.UTF8);
                m_formatter = new BinaryFormatter();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                return false;
            }
        }

        public void Run()
        {
            // Create the instance of main window
            m_mainWindow = new MainWindow(this);

            // Create a thread that will process server response and start it
            Thread readThread = new Thread(() => { Listen(); });
            readThread.Start();

            // Call show dialogue on the form class
            m_mainWindow.ShowDialog();

            // Close the TcpClient
            m_tcpClient.Close();
        }

        #endregion

        #region Listening

        /// <summary>
        /// Print out the stream readers value to the console. 
        /// </summary>
        private void Listen()
        {
            // While the client is connected...
            while (m_tcpClient.Connected)
            {
                // Write the messages to the console
                // Don't forget that readline is a blocking method, the client could get stuck here if nothing is sent from the server.
                // Check the size of the array is not -1 and store it to an int
                int numberOfBytes;
                if ((numberOfBytes = m_reader.ReadInt32()) != -1)
                {
                    // Use the number of bytes to read the correct number of bytes and store in the buffer
                    byte[] buffer = m_reader.ReadBytes(numberOfBytes);
                    ProcessServerResponse(buffer);
                }
            }
        }

        private void ProcessServerResponse(byte[] buffer)
        {
            // Create a new memory stream and pass the byte array into the constructor
            MemoryStream memoryStream = new MemoryStream(buffer);
            // use the formatter to deserialise the data in the memory stream, cast it to a packet and return it.
            Packet packet = m_formatter.Deserialize(memoryStream) as Packet;
            // Switch on the type of packet (message type) recieved
            switch (packet.m_packetType)
            {
                case PacketType.ChatMessage:
                    {
                        // Cast the chatMessagePacket to be the right type of packet class
                        ChatMessagePacket chatMessage = (ChatMessagePacket)packet;
                        // Output the recieved message to the UI, after having cast it
                        m_mainWindow.DisplayChat(chatMessage.m_message);
                        break;
                    }
                case PacketType.PrivateMessage:
                    {
                        break;
                    }
                case PacketType.ClientName:
                    {
                        // Cast the ClientNamePacket to be the right type of packet class
                        ClientNamePacket clientName = (ClientNamePacket)packet;
                        m_mainWindow.ClientUpdated(clientName.m_name, clientName.m_oldName);
                        break;
                    }
                case PacketType.EncryptedChatMessage:
                    {
                        // Cast the recieved packet to be the right type of client name packet class
                        EncryptedChatMessagePacket encryptedChatMessage = (EncryptedChatMessagePacket)packet;
                        // Decrypt the recieved packet's message
                        string message = DecryptString(encryptedChatMessage.m_message);
                        // Output the recieved message to the UI, after having cast it
                        m_mainWindow.DisplayChat(message);
                        break;
                    }
                case PacketType.PublicKey:
                    {
                        // Server key was recieved
                        Handshake(packet);
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

        public void SetNickname(string nickname)
        {
            // Instanciate a new ChatMessagePacket from the string sent from the UI
            ClientNamePacket clientName = new ClientNamePacket(nickname);
            Send(clientName);
        }

        public void Send(Packet packet)
        {
            // Create a new memory stream object used to store binary data.
            MemoryStream memoryStream = new MemoryStream();
            // Use the binary formatter to serialise message, and store this into the memory stream
            m_formatter.Serialize(memoryStream, packet);
            // Get the byte array from the memory stream and store into buffer
            byte[] buffer = memoryStream.GetBuffer();
            // Write the length of this array to m_writer, so the size can be checked on the recieving end
            m_writer.Write(buffer.Length);
            // Write the buffer to m_writer
            m_writer.Write(buffer);
            // Flush the writer
            m_writer.Flush();
        }

        /// <summary>Builds a chat message packet or encrypted chat message packet and calls the send function to send it across the server</summary>
        /// <param name="message">The message to send, will be encrypted for you if you so desire</param>
        /// <param name="encrypted">Whether or not to encyrpted the data. True = encrypted. Defaults to true</param>
        public void SendChatMessage(string message)
        {
            // Send an encrypted message
            if (m_encrypted)
            {
                byte[] encryptedMessage = EncryptString(message);
                // Pass this encrypted byte array into an encryptedChatMessagePacket
                EncryptedChatMessagePacket encryptedChatMessage = new EncryptedChatMessagePacket(encryptedMessage);
                Send(encryptedChatMessage);
            }
            // Send an unencrypted message
            else
            {
                // Instanciate a new ChatMessagePacket from the string sent from the UI
                ChatMessagePacket chatMessagePacket = new ChatMessagePacket(message);
                Send(chatMessagePacket);
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
            m_encrypted = true;
        }

        private RSACryptoServiceProvider m_rsaProvider;
        private RSAParameters m_publicKey;
        private RSAParameters m_privateKey;
        private RSAParameters m_serverKey;
        /// <summary>Whether or not to use encryption when sending messages.</summary>
        private bool m_encrypted;

        public void ToggleEncryption()
        {
            m_encrypted = !m_encrypted;
        }

        private byte[] Encrypt(byte[] data)
        {
            // Lock on service provider to prevent race conditions
            lock (m_rsaProvider)
            {
                // Set the service proider to use the server key
                m_rsaProvider.ImportParameters(m_serverKey);
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

        private byte[] EncryptString(string message)
        {
            // Convert the parameter string into a byte array, and encrypt it, then return it
            return Encrypt(Encoding.UTF8.GetBytes(message));
        }

        private string DecryptString(byte[] message)
        {
            // Call the decrypt method to decrypt the byte array, then convert it into a string, then return it
            return Encoding.UTF8.GetString(Decrypt(message));
        }

        //Recieves the server's public key and sends back this' public key
        private void Handshake(Packet packet)
        {
            // cast the packet to the right type
            PublicKeyPacket serverKey = (PublicKeyPacket)packet;
            // Set the server key to be equal to the server's public key
            m_serverKey = serverKey.m_key;
            // send our public key back to them
            Send(new PublicKeyPacket(m_publicKey));
        }

        #endregion
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