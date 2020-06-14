using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Audyssey
{
    namespace MultEQAvr
    {
        interface IAmp
        {
            #region Properties
            string AmpAssign { get; set; }
            string AssignBin { get; set; }
            ObservableCollection<Dictionary<string, int>> ChLevel { get; set; }
            ObservableCollection<Dictionary<string, object>> Crossover { get; set; }
            ObservableCollection<Dictionary<string, int>> Distance { get; set; }
            ObservableCollection<Dictionary<string, string>> SpConfig { get; set; }
            bool? AudyDynEq { get; set; }
            int? AudyEqRef { get; set; }
            string AudyFinFlg { get; set; }
            #endregion
        }

        interface IAudy
        {
            #region Properties
            bool? AudyDynVol { get; set; }
            string AudyDynSet { get; set; }
            bool? AudyMultEq { get; set; }
            string AudyEqSet { get; set; }
            bool? AudyLfc { get; set; }
            int? AudyLfcLev { get; set; }
            #endregion
        }

        public class AvrData : IAmp, IAudy, INotifyPropertyChanged
        {
            // IAmp
            static string _AmpAssign;
            static string _AssignBin;
            static ObservableCollection<Dictionary<string, string>> _SpConfig;
            static ObservableCollection<Dictionary<string, int>> _Distance;
            static ObservableCollection<Dictionary<string, int>> _ChLevel;
            static ObservableCollection<Dictionary<string, object>> _Crossover;
            static string _AudyFinFlg;
            static bool? _AudyDynEq;
            static int? _AudyEqRef;

            // IAudy
            static bool? _AudyDynVol = null;
            static string _AudyDynSet = null;
            static bool? _AudyMultEq = null;
            static string _AudyEqSet = null;
            static bool? _AudyLfc = null;
            static int? _AudyLfcLev = null;

            // Local
            private string _SelectedChannel;

            #region Properties
            [JsonIgnore]
            public string SelectedChannel
            {
                get
                {
                    return _SelectedChannel;
                }
                set
                {
                    _SelectedChannel = value;
                    RaisePropertyChanged("SelectedChannel");
                    RaisePropertyChanged("SelectedSpConfig");
                    RaisePropertyChanged("SelectedDistance");
                    RaisePropertyChanged("SelectedChLevel");
                    RaisePropertyChanged("SelectedCrossover");
                }
            }
            // IAmp
            public string AmpAssign
            {
                get
                {
                    return _AmpAssign;
                }
                set
                {
                    if (value != null) _AmpAssign = value;
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
            public ObservableCollection<Dictionary<string, string>> SpConfig
            {
                get
                {
                    return _SpConfig;
                }
                set
                {
                    _SpConfig = value;
                    RaisePropertyChanged("SpConfig");
                }
            }
            [JsonIgnore]
            public string SelectedSpConfig
            {
                get
                {
                    if (_SelectedChannel != null)
                    {
                        foreach (var spConfig in _SpConfig)
                        {
                            if (spConfig.ContainsKey(_SelectedChannel))
                            {
                                string _SelectedSpConfig;
                                spConfig.TryGetValue(_SelectedChannel, out _SelectedSpConfig);
                                return _SelectedSpConfig;
                            }
                        }
                    }
                    return null;
                }
                set
                {
                }
            }
            public ObservableCollection<Dictionary<string, int>> Distance
            {
                get
                {
                    return _Distance;
                }
                set
                {
                    _Distance = value;
                    RaisePropertyChanged("Distance");
                }
            }
            [JsonIgnore]
            public int? SelectedDistance
            {
                get
                {
                    if (_SelectedChannel != null)
                    {
                        foreach (var _distance in _Distance)
                        {
                            if (_distance.ContainsKey(_SelectedChannel))
                            {
                                int _SelectedDistance;
                                _distance.TryGetValue(_SelectedChannel, out _SelectedDistance);
                                return _SelectedDistance;
                            }
                        }
                    }
                    return null;
                }
                set
                {
                }
            }
            public ObservableCollection<Dictionary<string, int>> ChLevel
            {
                get
                {
                    return _ChLevel;
                }
                set
                {
                    _ChLevel = value;
                    RaisePropertyChanged("ChLevel");
                }
            }
            [JsonIgnore]
            public int? SelectedChLevel
            {
                get
                {
                    if (_SelectedChannel != null)
                    {
                        foreach (var _chLevel in _ChLevel)
                        {
                            if (_chLevel.ContainsKey(_SelectedChannel))
                            {
                                int _SelectedChLevel;
                                _chLevel.TryGetValue(_SelectedChannel, out _SelectedChLevel);
                                return _SelectedChLevel;
                            }
                        }
                    }
                    return null;
                }
                set
                {
                }
            }
            public ObservableCollection<Dictionary<string, object>> Crossover
            {
                get
                {
                    return _Crossover;
                }
                set
                {
                    _Crossover = value;
                    RaisePropertyChanged("Crossover");
                }
            }
            [JsonIgnore]
            public object SelectedCrossover
            {
                get
                {
                    if (_SelectedChannel != null)
                    {
                        foreach (var _crossover in _Crossover)
                        {
                            if (_crossover.ContainsKey(_SelectedChannel))
                            {
                                object _SelectedCrossover;
                                _crossover.TryGetValue(_SelectedChannel, out _SelectedCrossover);
                                return _SelectedCrossover;
                            }
                        }
                    }
                    return null;
                }
                set
                {
                }
            }
            public string AudyFinFlg
            {
                get
                {
                    return _AudyFinFlg;
                }
                set
                {
                    _AudyFinFlg = value;
                    RaisePropertyChanged("AudyFinFlg");
                }
            }
            public bool? AudyDynEq
            {
                get
                {
                    return _AudyDynEq;
                }
                set
                {
                    _AudyDynEq = value;
                    RaisePropertyChanged("AudyDynEq");
                }
            }
            public int? AudyEqRef
            {
                get
                {
                    return _AudyEqRef;
                }
                set
                {
                    _AudyEqRef = value;
                    RaisePropertyChanged("AudyEqRef");
                }
            }
            // IAudy
            public bool? AudyDynVol
            {
                get
                {
                    return _AudyDynVol;
                }
                set
                {
                    _AudyDynVol = value;
                    RaisePropertyChanged("AudyDynVol");
                }
            }
            public string AudyDynSet
            {
                get
                {
                    return _AudyDynSet;
                }
                set
                {
                    _AudyDynSet = value;
                    RaisePropertyChanged("AudyDynSet");
                }
            }
            public string AudyEqSet
            {
                get
                {
                    return _AudyEqSet;
                }
                set
                {
                    _AudyEqSet = value;
                    RaisePropertyChanged("AudyEqSet");
                }
            }
            public bool? AudyLfc
            {
                get
                {
                    return _AudyLfc;
                }
                set
                {
                    _AudyLfc = value;
                    RaisePropertyChanged("AudyLfc");
                }
            }
            public int? AudyLfcLev
            {
                get
                {
                    return _AudyLfcLev;
                }
                set
                {
                    _AudyLfcLev = value;
                    RaisePropertyChanged("AudyLfcLev");
                }
            }
            public bool? AudyMultEq
            {
                get
                {
                    return _AudyMultEq;
                }
                set
                {
                    _AudyMultEq = value;
                    RaisePropertyChanged("AudyMultEq");
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