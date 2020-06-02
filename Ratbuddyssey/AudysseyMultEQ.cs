using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace Audyssey
{
    namespace MultEQApp
    {
        public class MultEQApp : INotifyPropertyChanged
        {
            // according to JSON .ady file
            private int? _enAmpAssignType = null;
            private bool? _dynamicVolume = null;
            private int? _enTargetCurveType = null;
            private bool? _lfcSupport = null;
            private List<DetectedChannel> _detectedChannels = null;
            private string _targetModelName = null;
            private string _title = null;
            private string _interfaceVersion = null;
            private bool? _dynamicEq = null;
            private string _ampAssignInfo = null;
            private bool? _lfc = null;
            private int? _systemDelay = null;
            private bool? _auro = null;
            private string _upgradeInfo = null;
            private int? _enMultEQType = null;
            private decimal? _adcLineup = null;

            private ObservableCollection<string> _enAmpAssignTypeList = new ObservableCollection<string>()
            { "Type1", "Type2", "Type3", "Type4",
              "Type5", "Type6", "Type7", "Type8",
              "Type9", "Type10", "Type11", "Type12",
              "Type13", "Type14", "Type15", "Type16",
              "Type17", "Type18", "Type19", "Type20"};

            private ObservableCollection<string> _enTargetCurveTypeList = new ObservableCollection<string>()
            { "Undefined", "High Frequency Roll Off 1", "High Frequency Roll Off 2"};

            private ObservableCollection<string> _enMultEQTypeList = new ObservableCollection<string>()
            { "MultEQ", "MultEQ XT", "MultEQ XT32" };

            #region Properties
            [JsonProperty(Order = 1)]
            public string Title
            {
                get
                {
                    return _title;
                }
                set
                {
                    _title = value;
                    RaisePropertyChanged("Title");
                }
            }
            [JsonProperty(Order = 2)]
            public string TargetModelName
            {
                get
                {
                    return _targetModelName;
                }
                set
                {
                    _targetModelName = value;
                    RaisePropertyChanged("TargetModelName");
                }
            }
            [JsonProperty(Order = 3)]
            public string InterfaceVersion
            {
                get
                {
                    return _interfaceVersion;
                }
                set
                {
                    _interfaceVersion = value;
                    RaisePropertyChanged("InterfaceVersion");
                }
            }
            [JsonProperty(Order = 4)]
            public bool? DynamicEq
            {
                get
                {
                    return _dynamicEq;
                }
                set
                {
                    _dynamicEq = value;
                    RaisePropertyChanged("DynamicEq");
                }
            }
            [JsonProperty(Order = 5)]
            public bool? DynamicVolume
            {
                get
                {
                    return _dynamicVolume;
                }
                set
                {
                    _dynamicVolume = value;
                    RaisePropertyChanged("DynamicVolume");
                }
            }
            [JsonProperty(Order = 6)]
            public bool? LfcSupport
            {
                get
                {
                    return _lfcSupport;
                }
                set
                {
                    _lfcSupport = value;
                    RaisePropertyChanged("LfcSupport");
                }
            }
            [JsonProperty(Order = 7)]
            public bool? Lfc
            {
                get
                {
                    return _lfc;
                }
                set
                {
                    _lfc = value;
                    RaisePropertyChanged("Lfc");
                }
            }
            [JsonProperty(Order = 8)]
            public int? SystemDelay
            {
                get
                {
                    return _systemDelay;
                }
                set
                {
                    _systemDelay = value;
                    RaisePropertyChanged("SystemDelay");
                }
            }
            [JsonProperty(Order = 9)]
            public decimal? AdcLineup
            {
                get
                {
                    return _adcLineup;
                }
                set
                {
                    _adcLineup = value;
                    RaisePropertyChanged("AdcLineup");
                }
            }
            [JsonProperty(Order = 10)]
            public int? EnTargetCurveType
            {
                get
                {
                    return _enTargetCurveType;
                }
                set
                {
                    _enTargetCurveType = value;
                    RaisePropertyChanged("EnTargetCurveType");
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> EnTargetCurveTypeList
            {
                get
                {
                    return _enTargetCurveTypeList;
                }
                set
                {
                    _enTargetCurveTypeList = value;
                    RaisePropertyChanged("EnTargetCurveTypeList");
                }
            }
            [JsonIgnore]
            public string TargetCurveType
            {
                get
                {
                    return EnTargetCurveTypeList[(int)EnTargetCurveType];
                }
                set
                {
                    EnTargetCurveType = EnTargetCurveTypeList.IndexOf(value);
                    RaisePropertyChanged("TargetCurveType");
                }
            }
            [JsonProperty(Order = 11)]
            public int? EnAmpAssignType
            {
                get
                {
                    return _enAmpAssignType;
                }
                set
                {
                    _enAmpAssignType = value;
                    RaisePropertyChanged("EnAmpAssignType");
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> EnAmpAssignTypeList
            {
                get
                {
                    return _enAmpAssignTypeList;
                }
                set
                {
                    _enAmpAssignTypeList = value;
                    RaisePropertyChanged("EnAmpAssignTypeList");
                }
            }
            [JsonIgnore]
            public string AmpAssignType
            {
                get
                {
                    return EnAmpAssignTypeList[(int)EnAmpAssignType];
                }
                set
                {
                    EnAmpAssignType = EnAmpAssignTypeList.IndexOf(value);
                    RaisePropertyChanged("AmpAssignType");
                }
            }
            [JsonProperty(Order = 12)]
            public int? EnMultEQType
            {
                get
                {
                    return _enMultEQType;
                }
                set
                {
                    _enMultEQType = value;
                    RaisePropertyChanged("EnMultEQType");
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> EnMultEQTypeList
            {
                get
                {
                    return _enMultEQTypeList;
                }
                set
                {
                    _enMultEQTypeList = value;
                }
            }
            [JsonIgnore]
            public string MultEQType
            {
                get
                {
                    return EnMultEQTypeList[(int)EnMultEQType];
                }
                set
                {
                    EnMultEQType = EnMultEQTypeList.IndexOf(value);
                    RaisePropertyChanged("MultEQType");
                }
            }
            [JsonProperty(Order = 13)]
            public string AmpAssignInfo
            {
                get
                {
                    return _ampAssignInfo;
                }
                set
                {
                    _ampAssignInfo = value;
                    RaisePropertyChanged("AmpAssignInfo");
                }
            }
            [JsonProperty(Order = 14)]
            public bool? Auro
            {
                get
                {
                    return _auro;
                }
                set
                {
                    _auro = value;
                    RaisePropertyChanged("Auro");
                }
            }
            [JsonProperty(Order = 15)]
            public string UpgradeInfo
            {
                get
                {
                    return _upgradeInfo;
                }
                set
                {
                    _upgradeInfo = value;
                    RaisePropertyChanged("UpgradeInfo");
                }
            }
            [JsonProperty(Order = 16)]
            public List<DetectedChannel> DetectedChannels
            {
                get
                {
                    return _detectedChannels;
                }
                set
                {
                    _detectedChannels = value;
                    RaisePropertyChanged("DetectedChannels");
                }
            }
            #endregion
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                foreach (var property in this.GetType().GetProperties())
                {
                    sb.Append(property + "=" + property.GetValue(this, null) + "\r\n");
                }

                foreach (var channel in this.DetectedChannels)
                {
                    sb.Append(channel.ToString());
                }

                return sb.ToString();
            }

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            protected void RaisePropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    Console.WriteLine(propertyName);
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }
    }
}