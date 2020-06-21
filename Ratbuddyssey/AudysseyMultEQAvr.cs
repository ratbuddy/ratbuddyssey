using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Audyssey
{
    namespace MultEQAvr
    {
        public partial class AudysseyMultEQAvr : INotifyPropertyChanged
        {
            /*local reference for selected channel from GUI*/
            private Dictionary<string, string> _SelectedItem;
            private string _SelectedChannel = null;
            private string _SeletedEqType = "Audy";

            #region Properties
            public Dictionary<string,string> SelectedItem
            {
                set
                {
                    _SelectedItem = value;
                    SelectedChannel = _SelectedItem.Keys.ElementAt(0);
                    RaisePropertyChanged("SelectedChSetup");
                }
            }
            public string SelectedChannel
            {
                set
                {
                    _SelectedChannel = value.Replace("MIX", "");
                    RaisePropertyChanged("SelectedDisFil");
                    RaisePropertyChanged("SelectedCoefData");
                    RaisePropertyChanged("SelectedSpConfig");
                    RaisePropertyChanged("SelectedDistance");
                    RaisePropertyChanged("SelectedChLevel");
                    RaisePropertyChanged("SelectedCrossover");
                }
            }
            public string SelectedEqType
            {
                set
                {
                    _SeletedEqType = value;
                    RaisePropertyChanged("SelectedDisFil");
                    RaisePropertyChanged("SelectedCoefData");
                }
            }
            #endregion

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            private void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }       
    }
}
