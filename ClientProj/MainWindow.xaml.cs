using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientProj
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
        private int m_id;
        private string m_name;
        private IPAddress m_ip;

        public ConnectedClient(int id, string name, IPAddress ip)
        {
            m_id = id;
            m_name = name;
            m_ip = ip;
        }

        public int GetID()
        {
            return m_id;
        }

        public string GetName()
        {
            return m_name;
        }
        public void SetName(string name)
        {
            this.m_name = name;
        }

        public void SetIP(IPAddress ip)
        {
            this.m_ip = ip;
        }
    }

    public class DataObject
    {
        private List<ConnectedClient> m_clients;
        private List<Message> m_messages;

        public IList<string> p_clients { get; set; }
        //A list used in composite collections to give an all option with messages
        public IList<string> p_recipients { get; set; }
        public IList<string> p_chat { get; set; }
        public IList<string> p_messages { get; set; }

        public DataObject(List<ConnectedClient> clients, List<Message> messages)
        {
            m_clients = clients;
            m_messages = messages;

            p_clients = new List<string>();
            p_recipients = new List<string>();
            p_chat = new List<string>();
            p_messages = new List<string>();

            UpdateClients();
        }

        /// <summary>
        /// Updates the p_clientsList and p_allList
        /// </summary>
        public void UpdateClients()
        {
            p_recipients.Clear();
            p_clients.Clear();

            //Add the client's names to a list displayed in the UI
            for (int i = 0; i < m_clients.Count(); i++)
            {
                p_clients.Add(m_clients[i].GetName());
                p_recipients.Add(m_clients[i].GetName());
            }

            //Update the all list, a visual only list that is used in reciept box to also contain an all option
            p_recipients.Add("All");
        }

        public void UpdateMessages()
        {
            int currentlyPrintedMessagesCount = p_chat.Count() + p_messages.Count();
            //For each new message...
            for (int i = currentlyPrintedMessagesCount; i < m_messages.Count(); i++)
            {
                //...if the message was for someone specific...
                if (m_messages[i].GetRecipient() != "All")
                {
                    //...output it to that person
                    //dummy functionality until client/server set up
                    p_messages.Add(m_messages[i].GetSender() + " whispers to " + m_messages[i].GetRecipient() + ": " + m_messages[i].GetMessage());
                    return;
                }
                //...otherwise, output it to everyone
                p_chat.Add(m_messages[i].GetSender() + " says: " + m_messages[i].GetMessage());
            }
        }

        /// <param name="oneself">The client to remove from p_allList</param>
        public void RemoveSelf(ConnectedClient oneself)
        {
            p_recipients.RemoveAt(oneself.GetID() + 1);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConnectedClient m_client;
        private List<ConnectedClient> m_clients;
        private List<Message> m_messages;

        private int m_port;
        private IPAddress m_ip;
        private bool m_server;

        private Client client;

        private DataObject m_dataContext;





        private void RefreshDataContext()
        {
            DataContext = null;
            DataContext = m_dataContext;
        }

        /// <summary>
        /// sends the message to the data context and refreshes the context
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(Message message)
        {
            m_messages.Add(message);
            m_dataContext.UpdateMessages();
            RefreshDataContext();
        }

        /// <summary>
        /// Updates the clients nickname, and refreshes the context
        /// </summary>
        /// <param name="nickname"></param>
        private void SetNickname(string nickname)
        {
            //Prevents a blank nickname from being entered
            if (nickname == "")
            {
                nickname = "Client " + m_client.GetID();
            }
            //Prevents dupilcate nicknames from being entered
            foreach (ConnectedClient client in m_clients)
            {
                if (client.GetName() == nickname)
                {
                    nickname = "Client " + m_client.GetID();
                }
            }
            m_client.SetName(nickname);
            m_dataContext.UpdateClients();
            RefreshDataContext();
        }

        /// <summary>
        /// Sends a challenge from challenger to challengee. The game is nonfunctional and this code is dummy
        /// </summary>
        private void SendChallenge(ConnectedClient challenger, ConnectedClient challengee)
        {
            //Add game functionality later
            Message challenge = new Message(challenger.GetName(), challengee.GetName(), "I challenge you!");
            SendMessage(challenge);
            RefreshDataContext();
        }





        public MainWindow()
        {
            //Code is temporary and will be phased out with the introduction of the server
            m_clients = new List<ConnectedClient>();
            m_messages = new List<Message>();

            //Set up Client technical information
            m_server = false;
            m_ip = IPAddress.Parse("127.0.0.1");
            m_port = 4444;

            //Set up Client display information
            int id = m_clients.Count();
            m_clients.Add(new ConnectedClient(id, "Client " + id, m_ip));
            m_client = m_clients[id];

            //Pass the m_clients list to the displayed data class, which will use it to polpulate a list of strings of all clients names
            m_dataContext = new DataObject(m_clients, m_messages);
            DataContext = m_dataContext;

            //Initialise the visual component
            InitializeComponent();

            //Update the nickname box to display the default nickname
            NicknameBox.Text = m_client.GetName();

            //define interaction events
            SendButton.Click += SendButton_Click;
            NicknameBox.TextChanged += NicknameBox_TextChanged;
            EnableServerBox.Checked += EnableServerBox_Checked;
            EnableServerBox.Unchecked += EnableServerBox_Unchecked;
            IPBox.TextChanged += IPBox_TextChanged;
            PortBox.TextChanged += PortBox_TextChanged;
            StartGameButton.Click += StartGameButton_Click;

            client = new Client();

            // Check to see if the client can connect to the network. If so...
            if (client.Connect(m_ip, m_port))
            {
                // Run the client. Otherwise...
                client.Run();
            }
            else
            {
                Console.WriteLine("Failed to connect to the server");
            }
        }





        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            //Find the client selected in OpponentBox and send a challenge
            foreach (ConnectedClient client in m_clients)
            {
                if (client.GetName() == OpponentBox.Text)
                {
                    SendChallenge(m_client, client);
                    return;
                }
            }
            //If the opponent could not be found, output a challenge failed message.
            Message challenge = new Message(m_client.GetName(), "All", "Challenge failed!");
        }
        /// <summary>
        /// When the port box is updated, parse its content as an int and write it to m_port
        /// </summary>
        private void PortBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int port;
            string string_port = PortBox.Text;
            bool validatePort = int.TryParse(string_port, out port);
            if (validatePort)
            {
                m_port = port;
            }
        }

        /// <summary>
        /// When the ip box is updated, parse its content as an IP and write it to m_ip. Needs work, methinks.
        /// </summary>
        private void IPBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IPAddress ip;
            string ipAddress = IPBox.Text;
            bool validateIp = IPAddress.TryParse(ipAddress, out ip);
            if (validateIp)
            {
                m_ip = ip;
                m_client.SetIP(ip);
            }
        }

        /// <summary>
        /// Update m_server when the enable server checkbox is unchecked.
        /// </summary>
        private void EnableServerBox_Unchecked(object sender, RoutedEventArgs e)
        {
            m_server = false;
        }

        /// <summary>
        /// Update m_server when the enable server checkbox is checked.
        /// </summary>
        private void EnableServerBox_Checked(object sender, RoutedEventArgs e)
        {
            m_server = true;
        }

        /// <summary>
        /// When the nickname is changed, update the internal and displayed client lists to refelect this
        /// </summary>
        private void NicknameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetNickname(NicknameBox.Text);
        }

        /// <summary>
        /// Send a message to the chat box when the player hits send, acording to the contents of recepient box
        /// </summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //if the message is blank...
            if (MessageBox.Text == "")
            {
                //...put a warning into the chatlist
                //ChatList.Items.Add("No message in the text box!");
                return;
            }

            //if the user has no nickname...
            if (m_client.GetName() == "")
            {
                //...put a warning into the chatlist
                //ChatList.Items.Add("User has no nickname!");
                return;
            }

            //if the user has selected no recipient...
            if (RecipientBox.Text == "")
            {
                //...assume they selected all
                RecipientBox.Text = "All";
            }

            //store the contents of the message box into message
            Message message = new Message(m_client.GetName(), RecipientBox.Text, MessageBox.Text);

            //leave the message box blank
            MessageBox.Text = "";

            //Add the message to the list of messages, to be outputted to the screen
            SendMessage(message);
        }
    }
}
