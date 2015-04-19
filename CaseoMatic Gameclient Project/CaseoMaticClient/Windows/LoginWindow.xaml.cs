using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CaseoMaticClient.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private bool fullClose;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (fullClose)
                WindowManager.Instance.mainWindow.Close();
        }

        private void cmdLoginGo_Click(object sender, RoutedEventArgs e)
        {
            if(WindowManager.Instance.Login(txtLoginUsername.Text, pwtxtLoginPassword.Password))
            {
                Close();
            }
            else
            {
                lblLoginStatus.Content = "This didnt work!"; // TODO: Specify why the credentials are wrong
            }
        }

        private void cmdClose_Click(object sender, RoutedEventArgs e)
        {
            fullClose = true;
            Close();
        }

        private void cmdVisitTheWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.caseomatic.webs.com/");
        }
    }
}
