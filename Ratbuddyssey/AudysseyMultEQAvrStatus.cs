using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Audyssey
{
    namespace MultEQAvr
    {
        interface Status
        {
            #region Properties
            public bool? HPPlug { get; set; }
            public bool? Mic { get; set; }
            public string AmpAssign { get; set; }
            public string AssignBin { get; set; }
            public ObservableCollection<Dictionary<string, string>> ChSetup { get; set; }
            public bool? BTTXStatus { get; set; }
            public bool? SpPreset { get; set; }
            #endregion
        }

        public class AvrStatus : Status, INotifyPropertyChanged
        {
            private bool? _HPPlug = null;
            private bool? _Mic = null;
            private string _AmpAssign = null;
            private string _AssignBin = null;
            private ObservableCollection<Dictionary<string, string>> _ChSetup = new ObservableCollection<Dictionary<string, string>>();
            private bool? _BTTXStatus = null;
            private bool? _SpPreset = null;

            #region Properties
            public bool? HPPlug
            { 
                get
                {
                    return _HPPlug;
                }
                set
                {
                    _HPPlug = value;
                    RaisePropertyChanged("HPPlug");
                }
            }
            public bool? Mic
            {
                get
                {
                    return _Mic;
                }
                set
                {
                    _Mic = value;
                    RaisePropertyChanged("Mic");
                }
            }
            public string AmpAssign
            {
                get
                {
                    return _AmpAssign;
                }
                set
                {
                    _AmpAssign = value;
                    RaisePropertyChanged("AmpAssign");
                }
            }
            public string AssignBin
            {
                get
                {
                    return _AssignBin;
                }
                set
                {
                    _AssignBin = value;
                    RaisePropertyChanged("AssignBin");
                }
            }
            public ObservableCollection<Dictionary<string, string>> ChSetup
            {
                get
                {
                    return _ChSetup;
                }
                set
                {
                    _ChSetup = value;
                    RaisePropertyChanged("ChSetup");
                }
            }
            public bool? BTTXStatus
            {
                get
                {
                    return _BTTXStatus;
                }
                set
                {
                    _BTTXStatus = value;
                    RaisePropertyChanged("BTTXStatus");
                }
            }
            public bool? SpPreset
            {
                get
                {
                    return _SpPreset;
                }
                set
                {
                    _SpPreset = value;
                    RaisePropertyChanged("SpPreset");
                }
            }
            #endregion

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            #endregion

            #region methods
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