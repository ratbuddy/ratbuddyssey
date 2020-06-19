using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Audyssey.MultEQAvr;

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
                audysseyMultEQAvr.SelectedChannelIndex = ChannelSetupView.SelectedIndex;
            }
        }
    }
}