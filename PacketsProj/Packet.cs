using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace PacketsProj
{
    public enum PacketType
    {
        ChatMessage,
        EncryptedChatMessage,
        DirectMessage,
        EncryptedDirectMessage,
        ClientName,
        ServerKey,
        PublicKey,
    }

    [Serializable]
    public class Packet
    {
        public PacketType m_packetType { get; protected set; }
    }

    [Serializable]
    public class ChatMessagePacket : Packet
    {
        public string m_message;
        public ChatMessagePacket(string message)
        {
            m_message = message;
            m_packetType = PacketType.ChatMessage;
        }
    }

    [Serializable]
    public class EncryptedChatMessagePacket : Packet
    {
        public byte[] m_message;
        public EncryptedChatMessagePacket(byte[] encryptedMessage)
        {
            m_message = encryptedMessage;
            m_packetType = PacketType.EncryptedChatMessage;
        }
    }

    [Serializable]
    public class DirectMessagePacket : Packet
    {
        public string m_message;
        public string m_recipient;
        public DirectMessagePacket(string message, string recipient)
        {
            m_message = message;
            m_recipient = recipient; 
            m_packetType = PacketType.DirectMessage;
        }
    }

    [Serializable]
    public class EncryptedDirectMessagePacket : Packet
    {
        public byte[] m_message;
        public byte[] m_recipient;
        public EncryptedDirectMessagePacket(byte[] encryptedMessage, byte[] encryptedRecipient)
        {
            m_message = encryptedMessage;
            m_recipient = encryptedRecipient;
            m_packetType = PacketType.EncryptedDirectMessage;
        }
    }

    /// <summary>
    /// A packet that contains the public key of a server or client, to be sent to the server or sent to the client during their initial handshake
    /// </summary>
    [Serializable]
    public class ServerKeyPacket : Packet
    {
        public RSAParameters m_key;
        public ServerKeyPacket(RSAParameters publicKey)
        {
            m_key = publicKey;
            m_packetType = PacketType.ServerKey;
        }
    }

    /// <summary>
    /// A packet containing the public key of a client, to be sent to another client for their handshake before exchanging encrypted messages that the server cannot decrypt
    /// </summary>
    [Serializable]
    public class PublicKeyPacket : Packet
    {
        public RSAParameters m_key;
        public string m_name;
        public PublicKeyPacket(RSAParameters publicKey, string name)
        {
            m_key = publicKey;
            m_name = name;

            m_packetType = PacketType.PublicKey;
        }
    }

    [Serializable]
    public class ClientNamePacket : Packet
    {
        public string m_name;

        /// <summary>When a nickname is changed, oldname will be used by the window to update connected clients. <br/>
        /// When a client joins, oldname will be their current name <br/>
        /// The requests to the server to update nickname do not need the oldname, only updates from the client.</summary>
        public string m_oldName;
        public ClientNamePacket(string name)
        {
            m_name = name;
            m_packetType = PacketType.ClientName;
            m_oldName = name;
        }

        public ClientNamePacket(string name, string oldName)
        {
            m_name = name;
            m_packetType = PacketType.ClientName;
            m_oldName = oldName;
        }
    }
}
