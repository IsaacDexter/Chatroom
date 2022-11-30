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
        PrivateMessage,
        ClientName,
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
    public class PublicKeyPacket : Packet
    {
        public RSAParameters m_publicKey;
        public PublicKeyPacket(RSAParameters publicKey)
        {
            m_publicKey = publicKey;
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
