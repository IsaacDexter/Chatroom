using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class DataObject
    {
        public ObservableCollection<string> p_clients { get; set; }
        public ObservableCollection<string> p_chat { get; set; }
        public ObservableCollection<string> p_messages { get; set; }

        public DataObject()
        {
            p_clients = new ObservableCollection<string>();
            p_chat = new ObservableCollection<string>();
            p_messages = new ObservableCollection<string>();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] m_invalidNames = { "", "Invalid nickname", "Nickname in use"};

        private int m_port;
        private IPAddress m_ip;

        private Client m_client;

        private DataObject m_dataContext;

        #region Sending

        /// <summary>
        /// sends the message to the data context and refreshes the context
        /// </summary>
        /// <param name="message"></param>
        private void SendChatMessage(string message)
        {
            m_client.SendChatMessage(message);
        }

        private void SendPrivateMessage(string message, string recipient)
        {
            m_client.SendPrivateMessage(message, recipient);
        }

        /// <summary>
        /// Sends a challenge from challenger to challengee. The game is nonfunctional and this code is dummy
        /// </summary>
        private void SendChallenge(string challengee)
        {
            //Add game functionality later
            string challenge = challengee + ", I challenge you!";
            m_client.SendChatMessage(challenge);
        }

        #endregion

        #region Setting

        /// <summary>
        /// Checks the clients nickname is valid, and not in use, then Updates the clients nickname, and refreshes the context
        /// </summary>
        /// <param name="nickname"></param>
        private void SetNickname(string nickname)
        {
            // If the user has entered an invalid name...
            if (m_invalidNames.FirstOrDefault(s => s == nickname) != null)
            {
                // ...Set the box to red and output an error
                NicknameBox.SelectionBrush = Brushes.Red;
                NicknameBox.Text = "Invalid nickname";
                NicknameBox.SelectAll();
                return;
            }
            // If the user has entered a name thats in use...
            if (m_dataContext.p_clients.FirstOrDefault(c => c == nickname) != null)
            {
                // ...Set the box to red and output an appropriate error
                NicknameBox.SelectionBrush = Brushes.Red;
                NicknameBox.Text = "Nickname in use";
                NicknameBox.SelectAll();
                return;
            }
            NicknameBox.SelectionBrush = Brushes.Gray;
            m_client.SetNickname(nickname);
        }

        #endregion

        #region Displaying

        public void DisplayChat(string message)
        {
            //Invoke will prevent the reading thread calling a UI function, instead allowing the UI to call it when it is safe to do so.
            ChatList.Dispatcher.Invoke(() =>
            {
                m_dataContext.p_chat.Add(message);
            });
        }

        public void DisplayMessage(string message, string sender)
        {
            //Invoke will prevent the reading thread calling a UI function, instead allowing the UI to call it when it is safe to do so.
            MessageList.Dispatcher.Invoke(() =>
            {
                m_dataContext.p_messages.Add(sender + " says: " + message);
            });
        }

        #endregion

        #region Updating

        /// <summary>Calls when a certain client's name is updated, i.e. has their nickname changed</summary>
        public void UpdateClient(string name, string oldName)
        {
            // this is an already existing client updating their nickname
            // Invoke will prevent the reading thread calling a UI function, instead allowing the UI to call it when it is safe to do so.
            ClientList.Dispatcher.Invoke(() =>
            {
                // Add this new client to the clients list
                // Find the index where the clients old name was
                var item = m_dataContext.p_clients.FirstOrDefault(c => c == oldName);
                // If the old clients name was indeed in the list...
                if (item != null)
                {
                    int i = m_dataContext.p_clients.IndexOf(item);
                    // ...Set it to the new one! Otherwise, do nothing, as something is wrong.
                    m_dataContext.p_clients[i] = name;
                }
                //Refresh the data context so this is reflected in the display
                return;
            });
        }

        public void AddClient(string name)
        {
            // the client has not updated their nickname, they have joined
            // Invoke will prevent the reading thread calling a UI function, instead allowing the UI to call it when it is safe to do so.
            ClientList.Dispatcher.Invoke(() =>
            {
                // Add this new client to the clients list
                m_dataContext.p_clients.Add(name);
                //Refresh the data context so this is reflected in the display
                return;
            });
        }

        public void RemoveClient(string name)
        {
            // the client has not updated their nickname, they have left
            // Invoke will prevent the reading thread calling a UI function, instead allowing the UI to call it when it is safe to do so.
            ClientList.Dispatcher.Invoke(() =>
            {
                // remove this client from the clients list
                m_dataContext.p_clients.Remove(name);
                //Refresh the data context so this is reflected in the display
                return;
            });
        }

        #endregion

        public MainWindow(Client client, bool connected, string ip = "127.0.0.1", int port = 4444)
        {
            //Set up Client technical information
            m_ip = IPAddress.Parse(ip);
            m_port = port;

            //Instanciate the data context from the data object class. This is used in this file to update fields, and in the XAML file as DataContext
            m_dataContext = new DataObject();
            DataContext = m_dataContext;

            //Initialise the visual component
            InitializeComponent();

            if (!connected)
            {
                Background.Background = Brushes.Salmon;
                DisplayChat("Not currently connected. Press connect to attempt a connection");
            }

            m_client = client;
            IPBox.Text = ip;
            PortBox.Text = port.ToString();

            //define interaction events
            SendButton.Click += SendButton_Click;
            NicknameBox.KeyUp += NicknameBox_KeyUp;
            EncryptionBox.Click += EncryptionBox_Click;
            IPBox.KeyUp += IPBox_KeyUp;
            PortBox.KeyUp += PortBox_KeyUp;
            StartGameButton.Click += StartGameButton_Click;
            ConnectButton.Click += ConnectButton_Click;
        }

        #region Events

        private void OnClosing()
        {
            m_client.Close();
            Close();
        }
        private void NicknameBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetNickname(NicknameBox.Text);
            }
        }

        private void EncryptionBox_Click(object sender, RoutedEventArgs e)
        {
            m_client.ToggleEncryption();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            SendChallenge(OpponentBox.Text);
        }
        
        private void PortBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int port = -1;
                string string_port = PortBox.Text;
                bool validatePort = int.TryParse(string_port, out port);
                if (validatePort && port > 1024 && port < 65366)
                {
                    PortBox.Background = Brushes.LightGreen; //Inform the user the port was valid through a color change
                    m_port = port;  
                }
                else //port was invalid
                {
                    DisplayChat(string_port + " is not a valid port.");
                    PortBox.Background = Brushes.Salmon;  //Inform the user the port was invalid
                }
            }
            else
            {
                PortBox.Background = Brushes.LightSalmon; //Inform the user that the port is not currently set
            }
        }

        private void IPBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) //If they're entering a new ip....
            {
                IPAddress ip;
                string string_ip = IPBox.Text;
                bool validateIp = IPAddress.TryParse(string_ip, out ip);
                if (validateIp)
                {
                    IPBox.Background = Brushes.LightGreen;  //Show the user they've entered an ip
                    m_ip = ip; 
                }
                else //Ip was invalid
                {
                    DisplayChat(string_ip + " is not a valid IP.");
                    IPBox.Background = Brushes.Salmon; //Inform the user the IP was invalid
                }
            }
            else
            {
                IPBox.Background = Brushes.LightSalmon; //Inform the user through the user of color that the ip is not yet validated
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            //Attempt to reconnect to the new port
            if (m_client.Reconnect(m_ip, m_port))   //Connection was successful!
            {
                DisplayChat("Connection to " + m_ip + ":" + m_port + " was successful.");
            }
            else //Connection failed
            {
                DisplayChat("Connection to " + m_ip + ":" + m_port + " failed.");
            }
        }

        /// <summary>
        /// Send a message to the chat box when the player hits send, acording to the contents of recipient box
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

            //if the user has selected no recipient...
            if (RecipientBox.Text == "")
            {
                //...assume they selected all
                RecipientBox.Text = "All";
            }

            //store the contents of the message box into message
            string message = MessageBox.Text;
            // Store the recipient
            string recipient = RecipientBox.Text;

            //leave the message box blank
            MessageBox.Text = "";

            switch (recipient)
            {
                // If this is a public message
                case "All":
                    // Send the message to the server
                    SendChatMessage(message);
                    break;
                // Otherwise
                default:
                    // Send the private message to the server
                    SendPrivateMessage(message, recipient);
                    break;
            }
            
        }

        #endregion
    }
}
