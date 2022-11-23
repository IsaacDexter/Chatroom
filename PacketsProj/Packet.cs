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
        public String m_message;
        public ChatMessagePacket(string message)
        {
            m_message = message;
            m_packetType = PacketType.ChatMessage;
        }
    }
}
