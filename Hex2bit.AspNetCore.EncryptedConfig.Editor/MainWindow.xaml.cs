using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private string OriginalFileText = "";
        private SettingsManager settings = new SettingsManager();
        private SecurityManager securityManager = new SecurityManager();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            // set filename text box if output file is defined and it exists
            if (!string.IsNullOrWhiteSpace(App.OutputFile) && File.Exists(App.OutputFile))
            {
                FileNameTextBox.Text = new FileInfo(App.OutputFile).FullName;
                LoadSettings();
            }

            // set defaults for dropdowns if needed
            if (string.IsNullOrWhiteSpace((string)storeLocation.SelectedValue))
            {
                storeLocation.SelectedValue = storeLocation.Items.Cast<string>().Where(x => string.Compare(x, App.StoreLocation, true) == 0).DefaultIfEmpty("LocalMachine").FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace((string)storeName.SelectedValue))
            {
                storeName.SelectedValue = storeName.Items.Cast<string>().Where(x => string.Compare(x, App.StoreName, true) == 0).DefaultIfEmpty("My").FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace((string)certificateName.SelectedValue))
            {
                RefreshCertList();
                certificateName.SelectedValue = certificateName.Items.Cast<string>().Where(x => x.StartsWith(App.Thumbprint + " (", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }

            // try opening file if there's a default file to open
            if (!string.IsNullOrWhiteSpace(App.OutputFile) && File.Exists(App.OutputFile))
            {
                OpenFile();
            }
            else
            {
                UpdateSaveButtons();
            }
        }

        private void LoadSettings()
        {
            // check if file exists
            FileInfo file = new FileInfo(FileNameTextBox.Text);
            if (file.Exists)
            {
                // try to find best match
                EncryptedFileDetails details = settings.FindBestMatch(file.FullName);
                if (details != null) // match found
                {
                    // initialize dropdowns
                    storeLocation.SelectedValue = storeLocation.Items.Cast<string>().Where(x => string.Compare(x, details.CertificateStoreLocation, true) == 0).DefaultIfEmpty("LocalMachine").FirstOrDefault();
                    storeName.SelectedValue = storeName.Items.Cast<string>().Where(x => string.Compare(x, details.CertificateStoreName, true) == 0).DefaultIfEmpty("My").FirstOrDefault();

                    RefreshCertList();
                    certificateName.SelectedValue = certificateName.Items.Cast<string>().Where(x => x.StartsWith(details.CertificateThumbprint + " (", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }
            }
        }

        

        private void RefreshCertList()
        {
            if (certificateName != null && storeName.SelectedValue != null && storeLocation.SelectedValue != null)
            {
                // save currently selected value
                string currentValue = (string)certificateName.SelectedValue;

                // update list
                certificateName.Items.Clear();
                certificateName.SelectedValue = "";
                foreach (string cert in securityManager.GetCertificates((string)storeName.SelectedValue, (string)storeLocation.SelectedValue))
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

                // try setting original value
                certificateName.SelectedValue = certificateName.Items.Cast<string>().Where(x => x == currentValue).FirstOrDefault();
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
            OpenFile();
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

        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBoxResult.Yes;

            if (FileContent.Text != OriginalFileText)
            {
                messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to open a different file and lose unsaved changes?  If not, click No and save your changes first", "Lose Changes?", System.Windows.MessageBoxButton.YesNo);
            }

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = "Encrypted JSON (*.ejson)|*.ejson|JSON (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == true)
                {
                    FileNameTextBox.Text = openFileDialog.FileName;
                    LoadSettings();
                    OpenFile();
                }
            }
        }

        private void OpenFile()
        {
            // try opening file
            if (certificateName.SelectedValue == null || certificateName.SelectedValue.ToString() == "")
            {
                OriginalFileText = "Cannot open file until certificate is selected";
                FileContent.Text = OriginalFileText;
                FileContent.IsEnabled = false;
            }
            else if (File.Exists(FileNameTextBox.Text))
            {
                byte[] fileBytes = File.ReadAllBytes(FileNameTextBox.Text);
                try
                {
                    using (EncryptionService encryptionService = GetNewEncryptionService())
                    {
                        OriginalFileText = encryptionService.Decrypt(fileBytes);
                        FileContent.Text = OriginalFileText;
                        FileContent.IsEnabled = true;
                        settings.UpdateSettings(FileNameTextBox.Text, storeLocation.SelectedValue.ToString().ToLower(), storeName.SelectedValue.ToString().ToLower(),
                            (certificateName.SelectedValue ?? "").ToString().Split(new string[] { " (" }, StringSplitOptions.None)[0]);
                    }
                }
                catch (Exception ex)
                {
                    OriginalFileText = "Failed to decrypt file with selected certificate, with error: " + ex.Message;
                    FileContent.Text = OriginalFileText;
                    FileContent.IsEnabled = false;
                }
            }

            UpdateSaveButtons();
        }

        // enables/disbale save buttons based on file exists and cert being selected.  Returns true of the file exists
        private void UpdateSaveButtons()
        {
            // enable Save As button if certificate is selected and file content area is enabled (not showing an error message)
            SaveAsButton.IsEnabled = certificateName.SelectedValue != null && certificateName.SelectedValue.ToString() != "" && FileContent.IsEnabled;

            // enable Save button if the above is true and the file selected exists
            SaveButton.IsEnabled = certificateName.SelectedValue != null && certificateName.SelectedValue.ToString() != "" && FileContent.IsEnabled && File.Exists(FileNameTextBox.Text);
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBoxResult.Yes;

            if (FileContent.Text != OriginalFileText)
            {
                messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to create a new file and lose unsaved changes?  If not, click No and save your changes first", "Lose Changes?", System.Windows.MessageBoxButton.YesNo);
            }

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                FileNameTextBox.Text = "";
                OriginalFileText = "";
                FileContent.Text = OriginalFileText;
                FileContent.IsEnabled = true;
                UpdateSaveButtons();
            }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                using(EncryptionService encryptionService = GetNewEncryptionService())
                {
                    FileNameTextBox.Text = saveFileDialog.FileName;
                    File.WriteAllBytes(saveFileDialog.FileName, encryptionService.Encrypt(FileContent.Text));
                    OriginalFileText = FileContent.Text;
                    FileContent.Background = Brushes.White;
                    settings.UpdateSettings(FileNameTextBox.Text, storeLocation.SelectedValue.ToString().ToLower(), storeName.SelectedValue.ToString().ToLower(),
                            (certificateName.SelectedValue ?? "").ToString().Split(new string[] { " (" }, StringSplitOptions.None)[0]);
                }
            }  
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // make sure file exists before proceeding
            if (File.Exists(FileNameTextBox.Text))
            {
                using (EncryptionService encryptionService = GetNewEncryptionService())
                {
                    File.WriteAllBytes(FileNameTextBox.Text, encryptionService.Encrypt(FileContent.Text));
                    OriginalFileText = FileContent.Text;
                    FileContent.Background = Brushes.White;
                    settings.UpdateSettings(FileNameTextBox.Text, storeLocation.SelectedValue.ToString().ToLower(), storeName.SelectedValue.ToString().ToLower(),
                            (certificateName.SelectedValue ?? "").ToString().Split(new string[] { " (" }, StringSplitOptions.None)[0]);
                }
            }
        }

        private EncryptionService GetNewEncryptionService()
        {
            return securityManager.GetNewEncryptionService(
                (string)storeName.SelectedValue,
                (string)storeLocation.SelectedValue, 
                (certificateName.SelectedValue ?? "").ToString().Split(new string[] { " (" }, StringSplitOptions.None)[0]);
        }

        private void FileContent_TextChanged(object sender, EventArgs e)
        {
            if (OriginalFileText != FileContent.Text)
            {
                FileContent.Background = Brushes.LightGoldenrodYellow;
            }
            else
            {
                FileContent.Background = Brushes.White;
            }
        }
    }
}
