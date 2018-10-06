using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Ratbuddyssey
{
    class Audyssey : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _targetModelName = string.Empty;
        private string _interfaceVersion = string.Empty;
        private bool _dynamicEq = false;
        private bool _dynamicVolume = false;
        private bool _lfcSupport = false;
        private bool _lfc = false;
        private int _systemDelay = 0;
        private decimal _adcLineup = 0;
        private int _enTargetCurveType = 0;
        private bool _enTargetCurveType0 = false;
        private bool _enTargetCurveType1 = false;
        private int _enAmpAssignType = 0;
        private ObservableCollection<string> _enAmpAssignTypeList = new ObservableCollection<string>()
        { "Type1", "Type2", "Type3", "Type4",
            "Type5", "Type6", "Type7", "Type8", "Type9", "Type10", "Type11" ,
            "Type12" , "Type13" , "Type14" , "Type15" , "Type16" , "Type17" , "Type18" , "Type19" , "Type20"  };
        private int _enMultEQType = 0;
        private bool _enMultEQType0 = false;
        private bool _enMultEQType1 = false;
        private bool _enMultEQType2 = false;
        private string _ampAssignInfo = string.Empty;
        private bool _auro = false;
        private string _upgradeInfo = string.Empty;

        private List<DetectedChannel> _detectedChannels = new List<DetectedChannel>();

        #region Properties
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged("Title");
            }
        }
        public string TargetModelName
        {
            get { return _targetModelName; }
            set
            {
                _targetModelName = value;
                RaisePropertyChanged("TargetModelName");
            }
        }
        public string InterfaceVersion
        {
            get { return _interfaceVersion; }
            set
            {
                _interfaceVersion = value;
                RaisePropertyChanged("InterfaceVersion");
            }
        }
        public bool DynamicEq
        {   get => _dynamicEq;
            set
            {
                _dynamicEq = value;
                RaisePropertyChanged("DynamicEq");
            }
        }
        public bool DynamicVolume
        {
            get => _dynamicVolume;
            set
            {
                _dynamicVolume = value;
                RaisePropertyChanged("DynamicVolume");
            }
        }
        public bool LfcSupport
        {
            get => _lfcSupport;
            set
            {
                _lfcSupport = value;
                RaisePropertyChanged("LfcSupport");
            }
        }
        public bool Lfc
        {
            get => _lfc;
            set
            {
                _lfc = value;
                RaisePropertyChanged("Lfc");
            }
        }
        public int SystemDelay
        {
            get => _systemDelay;
            set
            {
                _systemDelay = value;
                RaisePropertyChanged("SystemDelay");
            }
        }
        public decimal AdcLineup
        {
            get => _adcLineup;
            set
            {
                _adcLineup = value;
                RaisePropertyChanged("AdcLineup");
            }
        }
        public int EnTargetCurveType
        {
            get => _enTargetCurveType;
            set
            {
                if(_enTargetCurveType != value)
                {
                    _enTargetCurveType = value;
                    switch (value)
                    {
                        case 0:
                            EnTargetCurveType0 = true;
                            break;
                        case 1:
                            EnTargetCurveType1 = true;
                            break;
                        default:
                            break;
                    }
                }                                
                RaisePropertyChanged("EnTargetCurveType");
            }
        }
        [JsonIgnore]
        public bool EnTargetCurveType0
        {
            get => _enTargetCurveType0;
            set
            {
                if(_enTargetCurveType0 != value)
                {
                    _enTargetCurveType0 = value;
                }                
                if (value) EnTargetCurveType = 0;
                RaisePropertyChanged("EnTargetCurveType0");
            }
        }
        [JsonIgnore]
        public bool EnTargetCurveType1
        {
            get => _enTargetCurveType1;
            set
            {
                _enTargetCurveType1 = value;
                if (value) EnTargetCurveType = 1;
                RaisePropertyChanged("EnTargetCurveType1");
            }
        }
        public int EnAmpAssignType
        {
            get => _enAmpAssignType;
            set
            {
                _enAmpAssignType = value;                
                RaisePropertyChanged("EnAmpAssignType");
            }
        }
        [JsonIgnore]
        public ObservableCollection<string> EnAmpAssignTypeList
        {
            get { return _enAmpAssignTypeList; }
            set
            {
                _enAmpAssignTypeList = value;
            }
        }
        public int EnMultEQType
        {
            get => _enMultEQType;
            set
            {
                if(_enMultEQType!=value)
                {
                    _enMultEQType = value;
                    switch (value)
                    {
                        case 0:
                            EnMultEQType0 = true;
                            break;
                        case 1:
                            EnMultEQType1 = true;
                            break;
                        case 2:
                            EnMultEQType2 = true;
                            break;
                        default:
                            break;
                    }
                }                               
                RaisePropertyChanged("EnMultEQType");
            }
        }
        [JsonIgnore]
        public bool EnMultEQType0
        {
            get => _enMultEQType0;
            set
            {
                if (_enMultEQType0 != value)
                {
                    _enMultEQType0 = value;
                    if (value) EnMultEQType = 0;
                }                
                RaisePropertyChanged("EnMultEQType0");
            }
        }
        [JsonIgnore]
        public bool EnMultEQType1
        {
            get => _enMultEQType1;
            set
            {
                if (_enMultEQType1 != value)
                {
                    _enMultEQType1 = value;
                    if (value) EnMultEQType = 1;
                }
                RaisePropertyChanged("EnMultEQType1");
            }
        }
        [JsonIgnore]
        public bool EnMultEQType2
        {
            get => _enMultEQType2;
            set
            {
                if (_enMultEQType2 != value)
                {
                    _enMultEQType2 = value;
                    if (value) EnMultEQType = 2;
                }
                RaisePropertyChanged("EnMultEQType2");
            }
        }
        public string AmpAssignInfo
        {
            get => _ampAssignInfo;
            set
            {
                _ampAssignInfo = value;
                RaisePropertyChanged("AmpAssignInfo");
            }
        }
        public bool Auro
        {
            get => _auro;
            set
            {
                _auro = value;
                RaisePropertyChanged("Auro");
            }
        }
        public string UpgradeInfo
        {
            get => _upgradeInfo;
            set
            {
                _upgradeInfo = value;
                RaisePropertyChanged("UpgradeInfo");
            }
        }
        public List<DetectedChannel> DetectedChannels
        {
            get { return _detectedChannels; }
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
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}
