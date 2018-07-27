using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using System.Xml.Linq;

namespace Hex2bit.AspNetCore.EncryptedConfig.Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            // initialize dropdowns
            storeLocation.SelectedValue = storeLocation.Items.Cast<string>().Where(x => x.ToLower() == App.StoreLocation).DefaultIfEmpty("LocalMachine").FirstOrDefault();
            storeName.SelectedValue = storeName.Items.Cast<string>().Where(x => x.ToLower() == App.StoreName).DefaultIfEmpty("My").FirstOrDefault();

            RefreshCertList();
            certificateName.SelectedValue = certificateName.Items.Cast<string>().Where(x => x.ToLower().StartsWith(App.Thumbprint + " (")).FirstOrDefault();
        }

        private string[] GetCertificates()
        {
            List<string> certList = new List<string>();

            using (var store = new X509Store((StoreName)Enum.Parse(typeof(StoreName), storeName.SelectedValue.ToString()),
                (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocation.SelectedValue.ToString())))
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (var cert in store.Certificates)
                {
                    certList.Add(cert.Thumbprint + " (" + (!string.IsNullOrWhiteSpace(cert.FriendlyName) ? cert.FriendlyName : cert.Subject) + ")");
                }
            }

            return certList.ToArray();
        }

        private void RefreshCertList()
        {
            if (certificateName != null && storeName.SelectedValue != null && storeLocation.SelectedValue != null)
            {
                certificateName.Items.Clear();
                certificateName.SelectedValue = "";
                foreach (string cert in GetCertificates())
                {
                    certificateName.Items.Add(cert);
                }

                certificateHint.Visibility = Visibility.Visible;
                if (certificateName.Items.Count > 0)
                {
                    certificateHint.Text = "--- Select a Certificate ---";
                }
                else
                {
                    certificateHint.Text = "--- No certificates found ---";
                }
            }
        }

        private void storeLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshCertList();
        }

        private void storeName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshCertList();
        }

        private void certificateName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            certificateHint.Visibility = Visibility.Hidden;
        }

        private void storeLocation_Initialized(object sender, EventArgs e)
        {
            storeLocation.Items.Clear();
            foreach (string name in Enum.GetNames(typeof(StoreLocation)))
            {
                storeLocation.Items.Add(name);
            }
        }

        private void storeName_Initialized(object sender, EventArgs e)
        {
            storeName.Items.Clear();
            foreach (string name in Enum.GetNames(typeof(StoreName)))
            {
                storeName.Items.Add(name);
            }
        }
    }
}
