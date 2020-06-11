using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq;
using System.Windows.Controls;
using Audyssey.MultEQAvr;

namespace Ratbuddyssey
{

    public partial class RatbuddysseyHome : Page
    {
        public void InitializeTab2()
        {
            if (audysseyMultEQAvr == null) audysseyMultEQAvr = new AudysseyMultEQAvr();
            audysseyMultEQAvr.DisFil.Add(new AvrDisFil());
            audysseyMultEQAvr.DisFil[0].DispData = new ObservableCollection<sbyte> { 1, 5, 10, 15 };
            audysseyMultEQAvr.DisFil[0].FilData = new ObservableCollection<sbyte> { -6, -1, -3, -1, 0, 2, 1, 0, 0 };
        }

        private void ChannelSetupView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelSetupView.SelectedValue != null)
            {
                if (audysseyMultEQAvr.DisFil.Count > ChannelSetupView.SelectedIndex)
                    audysseyMultEQAvr.CurrentDisFil = audysseyMultEQAvr.DisFil[ChannelSetupView.SelectedIndex];
            }
        }
    }
}