using Audyssey;
using Audyssey.MultEQAvr;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Audyssey.MultEQAvrAdapter;
using Audyssey.MultEQTcp;
using System.Collections.ObjectModel;

namespace Ratbuddyssey
{
    public partial class RatbuddysseyHome : Page
    {
        private AudysseyMultEQAvr audysseyMultEQAvr = null;
        private AudysseyMultEQAvrTcp audysseyMultEQAvrTcp = null;
        private AudysseyMultEQAvrAdapter audysseyMultEQAvrAdapter = null;
        private AudysseyMultEQTcpSniffer audysseyMultEQTcpSniffer = null;

        private void ParseFileToAudysseyMultEQAvr(string FileName)
        {
            if (File.Exists(FileName))
            {
                string Serialized = File.ReadAllText(FileName);
                audysseyMultEQAvr = JsonConvert.DeserializeObject<AudysseyMultEQAvr>(Serialized, new JsonSerializerSettings { });
                if (audysseyMultEQAvrAdapter == null)
                {
                    audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
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
                if (audysseyMultEQAvr != null)
                {
                    if (tabControl.SelectedIndex == 0)
                    {
                        this.DataContext = audysseyMultEQAvrAdapter;
                    }
                    if (tabControl.SelectedIndex == 1)
                    {
                        this.DataContext = audysseyMultEQAvr;
                    }
                }
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
                        // if there is no Tcp client
                        if (audysseyMultEQAvrTcp == null)
                        {
                            // create receiver instance
                            if (audysseyMultEQAvr == null)
                            {
                                audysseyMultEQAvr = new AudysseyMultEQAvr();
                            }
                            // create receiver tcp instance
                            audysseyMultEQAvrTcp = new AudysseyMultEQAvrTcp(audysseyMultEQAvr, cmbInterfaceClient.Text);
                            // create adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                            if (audysseyMultEQAvrAdapter == null)
                            {
                                audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                            }
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
                        audysseyMultEQAvrTcp.Connect();
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
                                    audysseyMultEQTcpSniffer = new AudysseyMultEQTcpSniffer(audysseyMultEQAvr, cmbInterfaceHost.SelectedItem.ToString(), cmbInterfaceClient.SelectedItem.ToString());
                                }
                            }
                        }
                    }
                }
                else
                {
                    audysseyMultEQAvrAdapter = null;
                    audysseyMultEQAvrTcp = null;
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
                        audysseyMultEQAvr = new AudysseyMultEQAvr();
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
                            audysseyMultEQTcpSniffer = new AudysseyMultEQTcpSniffer(audysseyMultEQAvr, cmbInterfaceHost.SelectedItem.ToString(), cmbInterfaceClient.SelectedItem.ToString());
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

        private void ChannelSetupView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (audysseyMultEQAvr != null)
            {
                if (ChannelSetupView.SelectedItem != null)
                {
                    audysseyMultEQAvr.SelectedItem = (Dictionary<string, string>)ChannelSetupView.SelectedItem;
                }
            }
        }

        private void ConnectAudyssey_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectAudyssey)
            {
                if (connectAudyssey.IsChecked)
                {
                    if (audysseyMultEQAvrTcp != null)
                    {
                        audysseyMultEQAvrTcp.EnterAudysseyMode();
                    }
                    else
                    {
                        connectAudyssey.IsChecked = false;
                    }
                }
                else
                {
                    if (audysseyMultEQAvrTcp != null)
                    {
                        audysseyMultEQAvrTcp.ExitAudysseyMode();
                    }
                    else
                    {
                        // if we end up here we have a problem
                    }
                }
            }
        }

        private void getReceiverInfo_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.GetAvrInfo())
                {
#if DEBUG
                    string AvrInfoFile = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IInfo))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrInfo.json", AvrInfoFile);
#endif            
                }
            }
        }

        private void getReceiverStatus_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.GetAvrStatus())
                {
#if DEBUG
                    string AvrStatusFile = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IStatus))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrStatus.json", AvrStatusFile);
#endif
                }
            }
        }

        private void setAvrSetAmp_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.SetAvrSetAmp())
                {
#if DEBUG
                    string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IAmp))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAmp.json", serialized);
#endif
                }
            }
        }

        private void setAvrSetAudy_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.SetAvrSetAudy())
                {
#if DEBUG
                    string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IAudy))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAud.json", serialized);
#endif
                }

            }
        }

        private void setAvrSetDisFil_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.SetAvrSetDisFil())
                {
#if DEBUG
                    string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr.DisFil, new JsonSerializerSettings { });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrDisFil.json", serialized);
#endif
                }
            }
        }

        private void setAvrInitCoefs_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.SetAvrInitCoefs())
                {
                }
            }
        }

        private void setAvrSetCoefDt_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.SetAvrSetCoefDt())
                {
#if DEBUG
                    string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr.CoefData, new JsonSerializerSettings { });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrCoefDafa.json", serialized);
#endif
                }
            }
        }

        private void setAudysseyFinishedFlag_Click(object sender, RoutedEventArgs e)
        {
            if ((audysseyMultEQAvrTcp != null) && (audysseyMultEQAvr != null))
            {
                if (audysseyMultEQAvrTcp.SetAudysseyFinishedFlag())
                {
                }
            }
        }
    }
}