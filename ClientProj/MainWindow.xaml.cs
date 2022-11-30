﻿using System;
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

        private int m_port;
        private IPAddress m_ip;

        private Client m_client;

        private DataObject m_dataContext;

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
        /// Updates the clients nickname, and refreshes the context
        /// </summary>
        /// <param name="nickname"></param>
        private void SetNickname(string nickname)
        {
            m_client.SetNickname(nickname);
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

        /// <summary>
        /// Calls when a certain client is updated, i.e. has their nickname changed or joined
        /// </summary>
        /// <param name="id"></param>
        public void ClientUpdated(string name, string oldName)
        {
            // if the client has not updated their nickname, they have joined
            if (name == oldName)
            {
                // Invoke will prevent the reading thread calling a UI function, instead allowing the UI to call it when it is safe to do so.
                ClientList.Dispatcher.Invoke(() =>
                { 
                    // Add this new client to the clients list
                    m_dataContext.p_clients.Add(name);
                    //Refresh the data context so this is reflected in the display
                    return;
                });
            }
            else
            {
                // otherwise, this is an already existing client updating their nickname
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
            
        }


        public MainWindow(Client client)
        {
            //Set up Client technical information
            m_ip = IPAddress.Parse("127.0.0.1");
            m_port = 4444;

            //Instanciate the data context from the data object class. This is used in this file to update fields, and in the XAML file as DataContext
            m_dataContext = new DataObject();
            DataContext = m_dataContext;

            //Initialise the visual component
            InitializeComponent();
            m_client = client;

            //define interaction events
            SendButton.Click += SendButton_Click;
            NicknameBox.TextChanged += NicknameBox_TextChanged;
            EncryptionBox.Click += EncryptionBox_Click;
            IPBox.TextChanged += IPBox_TextChanged;
            PortBox.TextChanged += PortBox_TextChanged;
            StartGameButton.Click += StartGameButton_Click;
        }

        private void EncryptionBox_Click(object sender, RoutedEventArgs e)
        {
            m_client.ToggleEncryption();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            SendChallenge(OpponentBox.Text);
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
            }
        }



        /// <summary>
        /// When the nickname is changed, update the internal and displayed client lists to refelect this
        /// </summary>
        private void NicknameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetNickname(NicknameBox.Text);
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

    }
}
