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
        CHAT_MESSAGE,
        CHAT_MESSAGE_ENCRYPTED,
        DIRECT_MESSAGE,
        DIRECT_MESSAGE_ENCRYPTED,
        UPDATE_NICKNAME,
        CLIENT_JOIN,
        CLIENT_LEAVE,
        SERVER_KEY,
        PUBLIC_KEY,
    }

    [Serializable]
    public class Packet
    {
        public PacketType m_packetType { get; protected set; }
    }

    #region Messaging

    [Serializable]
    public class ChatMessagePacket : Packet
    {
        public string m_message;
        public string m_sender;
        /// <param name="message">The message to send. Sender is handled by the server</param>
        public ChatMessagePacket(string message)
        {
            m_message = message;
            m_packetType = PacketType.CHAT_MESSAGE;
        }
        /// <param name="message">What is being said</param>
        /// <param name="sender">Who is saying it. Leave blank clientside, handled by the server</param>
        public ChatMessagePacket(string message, string sender)
        {
            m_message = message;
            m_sender = sender;
            m_packetType = PacketType.CHAT_MESSAGE;
        }
    }

    [Serializable]
    public class EncryptedChatMessagePacket : Packet
    {
        public byte[] m_message;
        public byte[] m_sender;
        /// <param name="encryptedMessage">The message, encrypted with the the server's public key. Sender is handled by server</param>
        public EncryptedChatMessagePacket(byte[] encryptedMessage)
        {
            m_message = encryptedMessage;
            m_packetType = PacketType.CHAT_MESSAGE_ENCRYPTED;
        }
        /// <param name="encryptedMessage">The message, encrypted with the the server's public key</param>
        /// <param name="encryptedSender">Leave blank clientside. The sender, encrypted with the server's public key</param>
        public EncryptedChatMessagePacket(byte[] encryptedMessage, byte[] encryptedSender)
        {
            m_message = encryptedMessage;
            m_sender = encryptedSender;
            m_packetType = PacketType.CHAT_MESSAGE_ENCRYPTED;
        }
    }

    [Serializable]
    public class DirectMessagePacket : Packet
    {
        public string m_message;
        public string m_recipient;
        /// <param name="message">What is being said</param>
        /// <param name="recipient">If being sent by the client: who it's addressed to, if being send by the server, who it's from</param>
        public DirectMessagePacket(string message, string recipient)
        {
            m_message = message;
            m_recipient = recipient; 
            m_packetType = PacketType.DIRECT_MESSAGE;
        }
    }

    [Serializable]
    public class EncryptedDirectMessagePacket : Packet
    {
        public byte[] m_message;
        public byte[] m_recipient;
        /// <param name="encryptedMessage">What is being said, encrypted with the recipient's public key</param>
        /// <param name="encryptedRecipient">Who it is being said to, encrypted with the server's public key</param>
        public EncryptedDirectMessagePacket(byte[] encryptedMessage, byte[] encryptedRecipient)
        {
            m_message = encryptedMessage;
            m_recipient = encryptedRecipient;
            m_packetType = PacketType.DIRECT_MESSAGE_ENCRYPTED;
        }
    }

    #endregion

    #region Keys

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
            m_packetType = PacketType.SERVER_KEY;
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

            m_packetType = PacketType.PUBLIC_KEY;
        }
    }

    #endregion

    #region Naming

    [Serializable]
    public class UpdateNicknamePacket : Packet
    {
        public string m_name;

        /// <summary>When a nickname is changed, oldname will be used by the window to update connected clients. <br/>
        /// When a client joins, oldname will be their current name <br/>
        /// The requests to the server to update nickname do not need the oldname, only updates from the client.</summary>
        public string m_oldName;
        public UpdateNicknamePacket(string name)
        {
            m_name = name;
            m_packetType = PacketType.UPDATE_NICKNAME;
            m_oldName = null;
        }

        public UpdateNicknamePacket(string name, string oldName)
        {
            m_name = name;
            m_packetType = PacketType.UPDATE_NICKNAME;
            m_oldName = oldName;
        }
    }

    [Serializable]
    public class ClientJoinPacket : Packet
    {
        public string m_name;

        public ClientJoinPacket(string name)
        {
            m_name = name;
            m_packetType = PacketType.CLIENT_JOIN;
        }
    }

    [Serializable]
    public class ClientLeavePacket : Packet
    {
        public string m_name;

        public ClientLeavePacket(string name)
        {
            m_name = name;
            m_packetType = PacketType.CLIENT_LEAVE;
        }
    }

    #endregion
}
