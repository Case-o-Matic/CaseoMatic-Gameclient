using CaseoMaticClient.Windows;
using CaseoMaticCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CaseoMaticClient
{
    public class WindowManager
    {
        public const string version = "1.01";
        public static WindowManager Instance;

        public MainWindow mainWindow;
        public ClientSocket clientSocket;

        private LoginInfo currentLoginInfo;
        private LoginWindow loginWindow;

        internal WindowManager(MainWindow mainwnd)
        {
            Instance = this;
            mainWindow = mainwnd;

            string clientPort = Interaction.InputBox("Client port", "Case-o-Matic Client");
            string serverEndPoint = Interaction.InputBox("Server-endpoint", "Case-o-Matic Client");

            clientSocket = new ClientSocket(int.Parse(clientPort));
            clientSocket.Connect(new IPEndPoint(IPAddress.Parse(serverEndPoint.Split(':')[0]), int.Parse(serverEndPoint.Split(':')[1])));
        }

        public void Start()
        {
            mainWindow.Visibility = Visibility.Hidden;

            loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            mainWindow.lblUsername.Content = currentLoginInfo.username;
            mainWindow.lblEmail.Content = currentLoginInfo.email;

            mainWindow.Visibility = Visibility.Visible;
        }

        public bool Login(string username, string password)
        {
            clientSocket.Send(new SocketMessage("login", username, password));
            SocketMessage loginStateMsg = clientSocket.Receive();
            if (loginStateMsg.type == "loginsuccess")
            {
                currentLoginInfo = new LoginInfo(username, loginStateMsg.data[0], loginStateMsg.data[1]);
                return true;
            }
            else if (loginStateMsg.type == "loginfail")
            {
                return false;
            }
            else if (loginStateMsg.type == "exception")
            {
                MessageBox.Show("An internal exception occured:\n" + loginStateMsg.data[0]);
                return false;
            }
            else return false;
        }

        public void Stop()
        {
            clientSocket.Stop();

            // Is this really needed?
            //Application.Current.Shutdown();
        }
    }
}
