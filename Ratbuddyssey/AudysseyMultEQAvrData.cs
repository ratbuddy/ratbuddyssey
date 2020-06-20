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
            public string AmpAssign { get; set; }
            public string AssignBin { get; set; }
            public ObservableCollection<Dictionary<string, int>> ChLevel { get; set; }
            public ObservableCollection<Dictionary<string, object>> Crossover { get; set; }
            public ObservableCollection<Dictionary<string, int>> Distance { get; set; }
            public ObservableCollection<Dictionary<string, string>> SpConfig { get; set; }
            public bool? AudyDynEq { get; set; }
            public int? AudyEqRef { get; set; }
            public string AudyFinFlg { get; set; }
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

        public partial class AudysseyMultEQAvr : IAmp, IAudy, INotifyPropertyChanged
        {
            // IAmp
            private ObservableCollection<Dictionary<string, string>> _SpConfig = null;
            private ObservableCollection<Dictionary<string, int>> _Distance = null;
            private ObservableCollection<Dictionary<string, int>> _ChLevel = null;
            private ObservableCollection<Dictionary<string, object>> _Crossover = null;
            private string _AudyFinFlg = null;
            private bool? _AudyDynEq = null;
            private int? _AudyEqRef = null;

            // IAudy
            private bool? _AudyDynVol = null;
            private string _AudyDynSet = null;
            private bool? _AudyMultEq = null;
            private string _AudyEqSet = null;
            private bool? _AudyLfc = null;
            private int? _AudyLfcLev = null;

            #region Properties
            // IAmp
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
                    if ((_SelectedChannel != null) && (_SpConfig != null))
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
                    if ((_SelectedChannel != null) && (_Distance != null))
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
                    if ((_SelectedChannel != null) && (_ChLevel != null))
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
                    if ((_SelectedChannel != null) && (_Crossover != null))
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
        }
    }
}