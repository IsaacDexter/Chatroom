using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketsProj
{
    public enum PacketType
    {
        ChatMessage,
        PrivateMessage,
        ClientName,
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
