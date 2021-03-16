using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Audyssey
{
    namespace MultEQAvr
    {
        interface IStatus
        {
            #region Properties
            public bool? HPPlug { get; set; }
            public bool? Mic { get; set; }
            public string AmpAssign { get; set; }
            public string AssignBin { get; set; }
            public UniqueObservableCollection<Dictionary<string, string>> ChSetup { get; set; }
            public bool? BTTXStatus { get; set; }
            public bool? SpPreset { get; set; }
            #endregion
        }

        public partial class AudysseyMultEQAvr : IStatus, INotifyPropertyChanged
        {
            private bool? _HPPlug = null;
            private bool? _Mic = null;
            private string _AmpAssign = null;
            private string _AssignBin = null;
            private UniqueObservableCollection<Dictionary<string, string>> _ChSetup = null;
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
            public UniqueObservableCollection<Dictionary<string, string>> ChSetup
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
            public string SelectedChSetup
            {
                get
                {
                    if ((_SelectedChannel != null) && (_ChSetup != null))
                    {
                        string selectedItem = _SelectedItem.Keys.ElementAt(0);
                        foreach (var _channel in _ChSetup)
                        {
                            if (_channel.ContainsKey(selectedItem))
                            {
                                return _channel[selectedItem];
                            }
                        }
                    }
                    return null;
                }
                set
                {
                    if ((_SelectedChannel != null) && (_Distance != null))
                    {
                        string selectedItem = _SelectedItem.Keys.ElementAt(0);
                        foreach (var _channel in _ChSetup)
                        {
                            if (_channel.ContainsKey(selectedItem))
                            {
                                _channel[selectedItem] = value;
                                RaisePropertyChanged("ChSetup");
                            }
                        }
                    }
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
        }
    }
}