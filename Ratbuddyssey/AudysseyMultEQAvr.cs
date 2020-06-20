using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace Audyssey
{
    namespace MultEQAvr
    {
        public partial class AudysseyMultEQAvr : INotifyPropertyChanged
        {
            /*local reference for selected channel from GUI*/
            private string _SelectedChannel = null;
            private string _SeletedEqType = "Audy";

            #region Properties
            public string SelectedChannel
            {
                set
                {
                    _SelectedChannel = value;
                    RaisePropertyChanged("SelectedDisFil");
                    RaisePropertyChanged("SelectedCoefData");
                    RaisePropertyChanged("SelectedSpConfig");
                    RaisePropertyChanged("SelectedDistance");
                    RaisePropertyChanged("SelectedChLevel");
                    RaisePropertyChanged("SelectedCrossover");
                }
            }
            [JsonIgnore]
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
