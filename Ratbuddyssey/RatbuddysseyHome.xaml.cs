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
                if (File.Exists(dlg.FileName))
                {
                    // Open document 
                    currentFile.Content = dlg.FileName;
                    // Load document 
                    String audysseyFile = File.ReadAllText(currentFile.Content.ToString());
                    // Parse JSON data
                    audysseyMultEQApp = JsonConvert.DeserializeObject<AudysseyMultEQApp>(audysseyFile, new JsonSerializerSettings
                    {
                        FloatParseHandling = FloatParseHandling.Decimal
                    });
                    // Data Binding
                    if (audysseyMultEQApp != null)
                    {
                        // cleanup: do not leave dangling
                        if (audysseyMultEQAvr != null)
                        {
                            audysseyMultEQAvr = null;
                        }
                        if (audysseyMultEQAvrAdapter != null)
                        {
                            audysseyMultEQAvrAdapter = null;
                        }
                        if (audysseyMultEQTcpSniffer != null)
                        {
                            audysseyMultEQTcpSniffer = null;
                        }
                        // update checkboxes
                        if (connectReceiver.IsChecked)
                        {
                            connectReceiver.IsChecked = false;
                        }
                        if (connectSniffer.IsChecked)
                        {
                            connectSniffer.IsChecked = false;
                        }
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
                    // Reload document 
                    String audysseyFile = File.ReadAllText(currentFile.Content.ToString());
                    // Parse JSON data
                    audysseyMultEQApp = JsonConvert.DeserializeObject<AudysseyMultEQApp>(audysseyFile, new JsonSerializerSettings
                    {
                        FloatParseHandling = FloatParseHandling.Decimal
                    });
                    // Data Binding
                    if (audysseyMultEQApp != null)
                    {
                        this.DataContext = audysseyMultEQApp;
                    }
                }
            }
        }

        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
            string reSerialized = JsonConvert.SerializeObject(audysseyMultEQApp, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
#if DEBUG
            currentFile.Content = System.IO.Path.ChangeExtension(currentFile.Content.ToString(), ".json");
#endif
            if ((reSerialized != null) && (!string.IsNullOrEmpty(currentFile.Content.ToString())))
            {
                File.WriteAllText(currentFile.Content.ToString(), reSerialized);
            }
        }

        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = currentFile.Content.ToString();
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey calibration (.ady)|*.ady";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                currentFile.Content = dlg.FileName;
                string reSerialized = JsonConvert.SerializeObject(audysseyMultEQApp, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                if ((reSerialized != null) && (!string.IsNullOrEmpty(currentFile.Content.ToString())))
                {
                    File.WriteAllText(currentFile.Content.ToString(), reSerialized);
                }
            }
        }

        private void ConnectReceiver_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectReceiver)
            {
                if (connectReceiver.IsChecked)
                {
                    if (audysseyMultEQAvr == null)
                    {
                        // create receiver instance
                        audysseyMultEQAvr = new AudysseyMultEQAvr(true);
                        // adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                        audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                        // data Binding to adapter
                        this.DataContext = audysseyMultEQAvrAdapter;
                    }
                    else
                    {
                        // object exists but not sure if we connected ethernet
                        audysseyMultEQAvr.Connect();
                    }
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
                    // check if binding and propertychanged work
                    audysseyMultEQAvr.AudysseyToAvr(); //TODO
                }
                else
                {
                    this.DataContext = null;
                    audysseyMultEQAvrAdapter = null;
                    audysseyMultEQAvr = null;
                    // immediately clean up the object
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                // display connection details
                currentFile.Content = (audysseyMultEQTcpSniffer != null ? "Host: " + audysseyMultEQTcpSniffer.GetTcpHostAsString() : "") + (audysseyMultEQAvr != null ? " Client:" + audysseyMultEQAvr.GetTcpClientAsString() : "");
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
                        // receiver instance
                        audysseyMultEQAvr = new AudysseyMultEQAvr(false);
                        // create adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                        audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                        // data Binding to adapter
                        this.DataContext = audysseyMultEQAvrAdapter;
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
                // Display connection details
                currentFile.Content = (audysseyMultEQTcpSniffer != null ? "Host: " + audysseyMultEQTcpSniffer.GetTcpHostAsString() : "") + (audysseyMultEQAvr != null ? " Client:" + audysseyMultEQAvr.GetTcpClientAsString() : "");
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
                    if ((connectReceiver.IsChecked) || (connectSniffer.IsChecked))
                        this.DataContext = audysseyMultEQAvrAdapter;
                    else
                        this.DataContext = audysseyMultEQApp;
                    break;
                case 1:
                    this.DataContext = audysseyMultEQAvr;
                    InitializeTab2();
                    break;
            }
        }
    }
}