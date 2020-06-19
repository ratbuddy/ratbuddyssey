using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Audyssey;
using Audyssey.MultEQApp;
using Audyssey.MultEQAvr;
using Audyssey.MultEQAvrAdapter;
using Audyssey.MultEQTcp;

namespace Ratbuddyssey
{
    /// <summary>
    /// Interaction logic for RatbuddysseyHome.xaml
    /// </summary>
    public partial class RatbuddysseyHome : Page
    {
        private AudysseyMultEQReferenceCurveFilter audysseyMultEQReferenceCurveFilter = new AudysseyMultEQReferenceCurveFilter();
        private AudysseyMultEQApp audysseyMultEQApp = null;
        private AudysseyMultEQAvr audysseyMultEQAvr = null;
        private AudysseyMultEQAvrAdapter audysseyMultEQAvrAdapter = null;
        private AudysseyMultEQTcpSniffer audysseyMultEQTcpSniffer = null;

        private string TcpClientFileName = "TcpClient.json";

        public RatbuddysseyHome()
        {
            InitializeComponent();
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;

            System.Net.IPHostEntry HosyEntry = System.Net.Dns.GetHostEntry((System.Net.Dns.GetHostName()));
            if (HosyEntry.AddressList.Length > 0)
            {
                foreach (System.Net.IPAddress ip in HosyEntry.AddressList)
                {
                    cmbInterfaceHost.Items.Add(ip.ToString());
                }
                cmbInterfaceHost.SelectedIndex = cmbInterfaceHost.Items.Count - 1;
            }

            if (File.Exists(Environment.CurrentDirectory + "\\" + TcpClientFileName))
            {
                String ClientTcpIPFile = File.ReadAllText(Environment.CurrentDirectory + "\\" + TcpClientFileName);
                if (ClientTcpIPFile.Length > 0)
                {
                    TcpIP TcpClient = JsonConvert.DeserializeObject<TcpIP>(ClientTcpIPFile,
                        new JsonSerializerSettings { });
                    cmbInterfaceClient.Items.Add(TcpClient.Address.ToString());
                    cmbInterfaceClient.SelectedIndex = cmbInterfaceClient.Items.Count - 1;
                }
            }
        }

        ~RatbuddysseyHome()
        {
        }

        private void ParseFileToAudysseyMultEQApp(string FileName)
        {
            if (File.Exists(FileName))
            {
                string Serialized = File.ReadAllText(FileName);
                audysseyMultEQApp = JsonConvert.DeserializeObject<AudysseyMultEQApp>(Serialized, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
            }
        }

        private void ParseAudysseyMultEQAppToFile(string FileName)
        {
            if(audysseyMultEQApp != null)
            {
                string Serialized = JsonConvert.SerializeObject(audysseyMultEQApp, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                if ((Serialized != null) && (!string.IsNullOrEmpty(FileName)))
                {
                    File.WriteAllText(FileName, Serialized);
                }
            }
        }

        private void ParseFileToAudysseyMultEQAvr(string FileName)
        {
            if (File.Exists(FileName))
            {
                string Serialized = File.ReadAllText(FileName);
                audysseyMultEQAvr = JsonConvert.DeserializeObject<AudysseyMultEQAvr>(Serialized, new JsonSerializerSettings
                {
                });
                if ((audysseyMultEQAvr != null) && (tabControl.SelectedIndex == 1))
                {
                    this.DataContext = audysseyMultEQAvr;
                }
            }
        }

        private void ParseAudysseyMultEQAvrToFile(string FileName)
        {
            if (audysseyMultEQAvr != null)
            {
                string Serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                if ((Serialized != null) && (!string.IsNullOrEmpty(FileName)))
                {
                    File.WriteAllText(FileName, Serialized);
                }
            }
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey files (*.ady)|*.ady";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                if (File.Exists(dlg.FileName))
                {
                    currentFile.Content = dlg.FileName;
                    ParseFileToAudysseyMultEQApp(currentFile.Content.ToString());
                    if ((audysseyMultEQApp != null) && (tabControl.SelectedIndex == 0))
                    {
                        this.DataContext = audysseyMultEQApp;
                    }
                }
            }
        }

        private void ReloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will reload the .ady file and discard all changes since last save", "Are you sure?", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                if (File.Exists(currentFile.Content.ToString()))
                {
                    ParseFileToAudysseyMultEQApp(currentFile.Content.ToString());
                    if ((audysseyMultEQApp != null) && (tabControl.SelectedIndex == 0))
                    {
                        this.DataContext = audysseyMultEQApp;
                    }
                }
            }
        }

        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
#if DEBUG
            currentFile.Content = System.IO.Path.ChangeExtension(currentFile.Content.ToString(), ".json");
#endif
            ParseAudysseyMultEQAppToFile(currentFile.Content.ToString());
        }

        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = currentFile.Content.ToString();
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey calibration (.ady)|*.ady";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                currentFile.Content = dlg.FileName;
                ParseAudysseyMultEQAppToFile(currentFile.Content.ToString());
            }
        }

        private void openProjectFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "AudysseySniffer.aud";
            dlg.DefaultExt = ".aud";
            dlg.Filter = "Audyssey sniffer (*.aud)|*.aud";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                ParseFileToAudysseyMultEQAvr(dlg.FileName);
            }
        }

        private void saveProjectFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            // Set filter for file extension and default file extension 
            dlg.FileName = "AudysseySniffer.aud";
            dlg.DefaultExt = ".aud";
            dlg.Filter = "Audyssey sniffer (.aud)|*.aud";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                ParseAudysseyMultEQAvrToFile(dlg.FileName);
            }
        }

        private void ConnectReceiver_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectReceiver)
            {
                if (connectReceiver.IsChecked)
                {
                    if (string.IsNullOrEmpty(cmbInterfaceClient.Text))
                    {
                        System.Windows.MessageBox.Show("Please enter receiver IP address.");
                    }
                    else
                    {
                        // if audysseyMultEQAvr was loaded from a projectfile there is no Tcp client address
                        if ((audysseyMultEQAvr == null) || (string.IsNullOrEmpty(audysseyMultEQAvr.GetTcpClientAsString())))
                        {
                            // create receiver instance
                            audysseyMultEQAvr = new AudysseyMultEQAvr(cmbInterfaceClient.Text);
                            // adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                            audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                            // data Binding to adapter
                            if ((tabControl.SelectedIndex == 0) && (audysseyMultEQApp == null))
                            {
                                this.DataContext = audysseyMultEQAvrAdapter;
                            }
                            if (tabControl.SelectedIndex == 1)
                            {
                                this.DataContext = audysseyMultEQAvr;
                            }
                        }
                        audysseyMultEQAvr.Connect();
                        // attach sniffer
                        if (connectSniffer.IsChecked)
                        {
                            // sniffer must be elevated to capture raw packets
                            if (!IsElevated())
                            {
                                // we cannot create the sniffer...
                                connectSniffer.IsChecked = false;
                                // but we can ask the user to elevate the program!
                                RunAsAdmin();
                            }
                            else
                            {
                                if (audysseyMultEQTcpSniffer == null)
                                {
                                    audysseyMultEQTcpSniffer = new AudysseyMultEQTcpSniffer(audysseyMultEQAvr, cmbInterfaceHost.SelectedItem.ToString());
                                }
                            }
                        }
                        // query info and status from the receiver
                        audysseyMultEQAvr.QueryAudyssey();
                        // from here it is possible to enable the audyssey remote app mode on the receiver
                        connectAudyssey.IsEnabled = true;
                    }
                }
                else
                {
                    connectAudyssey.IsChecked = false;
                    connectAudyssey.IsEnabled = false;
                    audysseyMultEQAvrAdapter = null;
                    audysseyMultEQAvr = null;
                    // immediately clean up the object
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    this.DataContext = null;
                }
            }
        }

        private void ConnectSniffer_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectSniffer)
            {
                if (connectSniffer.IsChecked)
                {
                    // can only attach sniffer to receiver if receiver object exists 
                    if (audysseyMultEQAvr == null)
                    {
                        // create receiver instance
                        audysseyMultEQAvr = new AudysseyMultEQAvr(cmbInterfaceClient.Text);
                        // create adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                        audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                        // data Binding to adapter
                        if ((tabControl.SelectedIndex == 0) && (audysseyMultEQApp == null))
                        {
                            this.DataContext = audysseyMultEQAvrAdapter;
                        }
                        if (tabControl.SelectedIndex == 1)
                        {
                            this.DataContext = audysseyMultEQAvr;
                        }
                    }
                    // sniffer must be elevated to capture raw packets
                    if (!IsElevated())
                    {
                        // we cannot create the sniffer...
                        connectSniffer.IsChecked = false;
                        // but we can ask the user to elevate the program!
                        RunAsAdmin();
                    }
                    else
                    {
                        // onyl create sniffer if it not already exists
                        if (audysseyMultEQTcpSniffer == null)
                        {
                            // create sniffer attached to receiver
                            audysseyMultEQTcpSniffer = new AudysseyMultEQTcpSniffer(audysseyMultEQAvr, cmbInterfaceHost.SelectedItem.ToString());
                        }
                    }
                }
                else
                {
                    if (audysseyMultEQTcpSniffer != null)
                    {
                        audysseyMultEQTcpSniffer = null;
                        // if not interested in receiver then close connection and delete objects
                        if (connectReceiver.IsChecked == false)
                        {
                            this.DataContext = null;
                            audysseyMultEQAvrAdapter = null;
                            audysseyMultEQAvr = null;
                        }
                        // immediately clean up the object
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }

        private void ConnectAudyssey_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectAudyssey)
            {
                if (connectAudyssey.IsChecked)
                {
                    if (audysseyMultEQAvr != null)
                    {
                        audysseyMultEQAvr.EnterAudysseyMode();
                    }
                }
                else
                {
                    if (audysseyMultEQAvr != null)
                    {
                        audysseyMultEQAvr.ExitAudysseyMode();
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

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        #endregion

        #region methods
        protected void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private static void RunAsAdmin()
        {
            try
            {
                var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path, "/run_elevated_action")
                {
                    Verb = "runas"
                }))
                {
                    process?.WaitForExit();
                }

            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    System.Windows.Forms.MessageBox.Show("Sniffer needs elevated rights for raw socket!", "Warning");
                }
                else
                {
                    throw;
                }
            }
        }

        private static bool IsElevated()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);

                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int currentTab = (sender as TabControl).SelectedIndex;

            switch (currentTab)
            {
                case 0:
                    if (audysseyMultEQApp == null)
                    {
                        if (audysseyMultEQAvrAdapter != null)
                        {
                            this.DataContext = audysseyMultEQAvrAdapter;
                        }
                    }
                    else
                    {
                        this.DataContext = audysseyMultEQApp;
                    }
                    break;
                case 1:
                    if (audysseyMultEQAvr != null)
                    {
                        this.DataContext = audysseyMultEQAvr;
                    }
                    break;
            }
        }
    }
}