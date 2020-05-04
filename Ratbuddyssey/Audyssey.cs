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
        // according to JSON .ady file
        private int _enAmpAssignType = 0;
        private bool _dynamicVolume = false;
        private int _enTargetCurveType = 0;
        private bool _lfcSupport = false;
        private List<DetectedChannel> _detectedChannels = null;
        private string _targetModelName = string.Empty;
        private string _title = string.Empty;
        private string _interfaceVersion = string.Empty;
        private bool _dynamicEq = false;
        private string _ampAssignInfo = string.Empty;
        private bool _lfc = false;
        private int _systemDelay = 0;
        private bool _auro = false;
        private string _upgradeInfo = string.Empty;
        private int _enMultEQType = 0;
        private decimal _adcLineup = 0;

        // some enums we understand
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
        public bool DynamicEq
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
        public bool DynamicVolume
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
        public bool LfcSupport
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
        public bool Lfc
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
        public int SystemDelay
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
        public decimal AdcLineup
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
        public int EnTargetCurveType
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
            }
        }
        public int EnAmpAssignType
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
            get { return _enAmpAssignTypeList; }
            set
            {
                _enAmpAssignTypeList = value;
            }
        }
        public int EnMultEQType
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
