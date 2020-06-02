using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Audyssey;
using Audyssey.MultEQApp;
using Audyssey.MultEQAvr;
using Audyssey.MultEQAvrAdapter;

namespace Ratbuddyssey
{
    /// <summary>
    /// Interaction logic for RatbuddysseyHome.xaml
    /// </summary>
    public partial class RatbuddysseyHome : Page
    {
        private MultEQReferenceFilterCurve MultEQReferenceFilterCurve = new MultEQReferenceFilterCurve();
        private MultEQApp MultEQApp = null;
        private MultEQAvrAdapter MultEQAvrAdapter = null;

        public RatbuddysseyHome()
        {
            InitializeComponent();
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;
        }
        ~RatbuddysseyHome()
        {
        }
        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey files (*.ady)|*.ady";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                currentFile.Content = filename;
                // Load document 
                String audysseyFile = File.ReadAllText(filename);
                // Parse JSON data
                MultEQApp = JsonConvert.DeserializeObject<MultEQApp>(audysseyFile,
                    new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
                // Data Binding
                if (MultEQApp != null)
                {
                    this.DataContext = MultEQApp;
                }
            }
        }
        private void ReloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will reload the .ady file and discard all changes since last save", "Are you sure?", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                // Reload document 
                String audysseyFile = File.ReadAllText(filename);
                // Parse JSON data
                MultEQApp = JsonConvert.DeserializeObject<MultEQApp>(audysseyFile,
                    new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
                // Data Binding
                if (MultEQApp != null)
                {
                    this.DataContext = MultEQApp;
                }
            }
        }
        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
            string reSerialized = JsonConvert.SerializeObject(MultEQApp, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
#if DEBUG
            filename = System.IO.Path.ChangeExtension(filename, ".json");
#endif
            if ((reSerialized != null) && (!string.IsNullOrEmpty(filename)))
            {
                File.WriteAllText(filename, reSerialized);
            }
        }
        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = filename;
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey calibration (.ady)|*.ady";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filename = dlg.FileName;
                string reSerialized = JsonConvert.SerializeObject(MultEQApp, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                if ((reSerialized != null) && (!string.IsNullOrEmpty(filename)))
                {
                    File.WriteAllText(filename, reSerialized);
                }
            }
        }
        private void ConnectEthernet_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectEthernet)
            {
                if (connectEthernet.IsChecked)
                {
                    // Establish ethernet connection with receiver
                    MultEQAvrAdapter = new MultEQAvrAdapter(connectSniffer.IsChecked);
                    if (MultEQAvrAdapter != null)
                    {
                        // Data Binding
                        this.DataContext = MultEQAvrAdapter;
                        // Display connection details
                        if (connectSniffer.IsChecked)
                        {
                            currentFile.Content = "Host: " + MultEQAvrAdapter.GetTcpHost() + " Client:" + MultEQAvrAdapter.GetTcpClient();
                        }
                        else
                        {
                            currentFile.Content = "Client:" + MultEQAvrAdapter.GetTcpClient();
                        }
                    }
                    // Check if binding and propertychanged work
                    MultEQAvrAdapter.AudysseyToAvr();
                }
                else
                {
                    this.DataContext = null;
                    MultEQAvrAdapter = null;
                    // immediately clean up the object
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    currentFile.Content = "";
                }
            }
        }
        private void ConnectSniffer_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectSniffer)
            {
                if (connectEthernet.IsChecked)
                {
                    if (MultEQAvrAdapter != null)
                    {
                        MultEQAvrAdapter.AttachSniffer();
                        currentFile.Content = "Host: " + MultEQAvrAdapter.GetTcpHost() + " Client:" + MultEQAvrAdapter.GetTcpClient();
                    }
                }
                else
                {
                    if (MultEQAvrAdapter != null)
                    {
                        MultEQAvrAdapter.DetachSniffer();
                        currentFile.Content = "Client:" + MultEQAvrAdapter.GetTcpClient();
                    }
                }
            }
        }
        private void ExitProgram_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Close();
        }
        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Shout out to AVS Forum, use at your own risk!");
        }
    }
}