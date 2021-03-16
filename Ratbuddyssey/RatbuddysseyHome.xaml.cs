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
using System.Globalization;

namespace Ratbuddyssey
{
    /// <summary>
    /// Interaction logic for RatbuddysseyHome.xaml
    /// </summary>
    public partial class RatbuddysseyHome : Page
    {
        private AudysseyMultEQReferenceCurveFilter audysseyMultEQReferenceCurveFilter = new AudysseyMultEQReferenceCurveFilter();
        private AudysseyMultEQApp audysseyMultEQApp = null;

        private string TcpClientFileName = "TcpClient.json";

        public RatbuddysseyHome()
        {
            InitializeComponent();
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;

            System.Net.IPHostEntry HostEntry = System.Net.Dns.GetHostEntry((System.Net.Dns.GetHostName()));
            if (HostEntry.AddressList.Length > 0)
            {
                foreach (System.Net.IPAddress ip in HostEntry.AddressList)
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

            for(int x=0; x<61; x++)
            {
                var fcentre = Math.Pow(10.0, 3.0) * Math.Pow(2.0, ((float)x-34.0)/6.0);
                Console.Write(x); Console.Write(" ");
                Console.WriteLine("{0:N1}", fcentre);
            }
        }

        ~RatbuddysseyHome()
        {
        }

        private void HandleDroppedFile(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (files.Length > 0)
                    OpenFile(files[0]);
            }
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

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey files (*.ady)|*.ady";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                OpenFile(dlg.FileName);
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

        private void OpenFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                currentFile.Content = filePath;
                ParseFileToAudysseyMultEQApp(currentFile.Content.ToString());
                if ((audysseyMultEQApp != null) && (tabControl.SelectedIndex == 0))
                {
                    this.DataContext = audysseyMultEQApp;
                }
            }
        }
    }
}