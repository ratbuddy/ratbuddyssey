using Audyssey;
using Audyssey.MultEQAvr;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Ratbuddyssey
{
    public partial class RatbuddysseyHome : Page
    {
        private void ChannelSetupView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (audysseyMultEQAvr != null)
            {
                if (ChannelSetupView.SelectedItem != null)
                {
                    audysseyMultEQAvr.SelectedChannel = ((Dictionary<string, string>)ChannelSetupView.SelectedItem).Keys.ElementAt(0);
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
    }
}