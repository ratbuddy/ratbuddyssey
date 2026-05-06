using System;
using System.Collections.Generic;
using System.IO;
using Audyssey;
using Audyssey.MultEQAvr;
using Audyssey.MultEQAvrAdapter;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;

namespace Ratbuddyssey
{
    public partial class RatbuddysseyHome
    {
        private AudysseyMultEQAvr audysseyMultEQAvr = null;
        private AudysseyMultEQAvrTcp audysseyMultEQAvrTcp = null;
        private AudysseyMultEQAvrAdapter audysseyMultEQAvrAdapter = null;

        private void ParseFileToAudysseyMultEQAvr(string fileName)
        {
            if (File.Exists(fileName))
            {
                string serialized = File.ReadAllText(fileName);
                audysseyMultEQAvr = JsonConvert.DeserializeObject<AudysseyMultEQAvr>(serialized, new JsonSerializerSettings { });
                if (audysseyMultEQAvrAdapter == null)
                {
                    audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                }
            }
        }

        private void ParseAudysseyMultEQAvrToFile(string fileName)
        {
            if (audysseyMultEQAvr != null)
            {
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                if (!string.IsNullOrEmpty(serialized) && !string.IsNullOrEmpty(fileName))
                {
                    File.WriteAllText(fileName, serialized);
                }
            }
        }

        private async void openProjectFile_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Project",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Audyssey sniffer") { Patterns = new[] { "*.aud" } }
                }
            });
            if (files != null && files.Count > 0)
            {
                var path = files[0].TryGetLocalPath();
                if (string.IsNullOrEmpty(path)) return;
                ParseFileToAudysseyMultEQAvr(path);
                if (audysseyMultEQAvr != null)
                {
                    if (tabControl.SelectedIndex == 0) DataContext = audysseyMultEQAvrAdapter;
                    if (tabControl.SelectedIndex == 1) DataContext = audysseyMultEQAvr;
                }
            }
        }

        private async void saveProjectFile_Click(object sender, RoutedEventArgs e)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Project",
                SuggestedFileName = "AudysseySniffer.aud",
                DefaultExtension = "aud",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Audyssey sniffer") { Patterns = new[] { "*.aud" } }
                }
            });
            if (file != null)
            {
                var path = file.TryGetLocalPath();
                if (!string.IsNullOrEmpty(path)) ParseAudysseyMultEQAvrToFile(path);
            }
        }

        private async void ConnectReceiver_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender != connectReceiver) return;

            if (connectReceiver.IsChecked == true)
            {
                if (string.IsNullOrEmpty(cmbInterfaceClient.Text))
                {
                    await MessageBoxHelper.ShowAsync(this, "Ratbuddyssey", "Please enter receiver IP address.");
                    connectReceiver.IsChecked = false;
                    return;
                }

                if (audysseyMultEQAvrTcp == null)
                {
                    audysseyMultEQAvr ??= new AudysseyMultEQAvr();
                    audysseyMultEQAvrTcp = new AudysseyMultEQAvrTcp(audysseyMultEQAvr, cmbInterfaceClient.Text);
                    audysseyMultEQAvrAdapter ??= new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);

                    if (tabControl.SelectedIndex == 0 && audysseyMultEQApp == null)
                        DataContext = audysseyMultEQAvrAdapter;
                    if (tabControl.SelectedIndex == 1)
                        DataContext = audysseyMultEQAvr;
                }
                audysseyMultEQAvrTcp.Connect();
            }
            else
            {
                audysseyMultEQAvrAdapter = null;
                audysseyMultEQAvrTcp = null;
                audysseyMultEQAvr = null;
                DataContext = null;
            }
        }

        private void ChannelSetupView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (audysseyMultEQAvr != null && ChannelSetupView.SelectedItem is KeyValuePair<string, string> kvp)
            {
                audysseyMultEQAvr.SelectedItem = new Dictionary<string, string> { { kvp.Key, kvp.Value } };
            }
        }

        private void ConnectAudyssey_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender != connectAudyssey) return;

            if (connectAudyssey.IsChecked == true)
            {
                if (audysseyMultEQAvrTcp != null) audysseyMultEQAvrTcp.EnterAudysseyMode();
                else connectAudyssey.IsChecked = false;
            }
            else
            {
                audysseyMultEQAvrTcp?.ExitAudysseyMode();
            }
        }

        private void getReceiverInfo_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null && audysseyMultEQAvrTcp.GetAvrInfo())
            {
#if DEBUG
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                {
                    ContractResolver = new InterfaceContractResolver(typeof(IInfo))
                });
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "AvrInfo.json"), serialized);
#endif
            }
        }

        private void getReceiverStatus_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null && audysseyMultEQAvrTcp.GetAvrStatus())
            {
#if DEBUG
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                {
                    ContractResolver = new InterfaceContractResolver(typeof(IStatus))
                });
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "AvrStatus.json"), serialized);
#endif
            }
        }

        private void setAvrSetAmp_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null && audysseyMultEQAvrTcp.SetAvrSetAmp())
            {
#if DEBUG
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                {
                    ContractResolver = new InterfaceContractResolver(typeof(IAmp))
                });
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "AvrSetDataAmp.json"), serialized);
#endif
            }
        }

        private void setAvrSetAudy_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null && audysseyMultEQAvrTcp.SetAvrSetAudy())
            {
#if DEBUG
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr, new JsonSerializerSettings
                {
                    ContractResolver = new InterfaceContractResolver(typeof(IAudy))
                });
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "AvrSetDataAud.json"), serialized);
#endif
            }
        }

        private void setAvrSetDisFil_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null && audysseyMultEQAvrTcp.SetAvrSetDisFil())
            {
#if DEBUG
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr.DisFil, new JsonSerializerSettings { });
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "AvrDisFil.json"), serialized);
#endif
            }
        }

        private void setAvrInitCoefs_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null)
            {
                audysseyMultEQAvrTcp.SetAvrInitCoefs();
            }
        }

        private void setAvrSetCoefDt_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null && audysseyMultEQAvrTcp.SetAvrSetCoefDt())
            {
#if DEBUG
                string serialized = JsonConvert.SerializeObject(audysseyMultEQAvr.CoefData, new JsonSerializerSettings { });
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "AvrCoefDafa.json"), serialized);
#endif
            }
        }

        private void setAudysseyFinishedFlag_Click(object sender, RoutedEventArgs e)
        {
            if (audysseyMultEQAvrTcp != null && audysseyMultEQAvr != null)
            {
                audysseyMultEQAvrTcp.SetAudysseyFinishedFlag();
            }
        }
    }
}
