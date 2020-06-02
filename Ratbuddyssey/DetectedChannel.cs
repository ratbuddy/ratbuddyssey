using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ratbuddyssey
{
    class DetectedChannel : INotifyPropertyChanged
    {
        // according to JSON .ady file
        private string _customCrossover = null;
        private int _enChannelType = 0;
        private bool _isSkipMeasurement = false;
        private string _customLevel = null;
        private decimal? _customDistance = null;
        private decimal _frequencyRangeRolloff = 0;
        private string[] _customTargetCurvePoints;
        private string _commandId = string.Empty;
        private string _customSpeakerType = null;
        private string _delayAdjustment = string.Empty;
        private ChannelReport _channelReport;
        private Dictionary<string, string[]> _responseData = null;
        private string _trimAdjustment = string.Empty;
        private bool _midrangeCompensation = false;

        // local for data binding (shall not be serialised)
        private ObservableCollection<MyKeyValuePair> _customTargetCurvePointsDictionary = new ObservableCollection<MyKeyValuePair>();

        // local for data binding (not serialised)
        private ObservableCollection<string> _customCrossoverList = new ObservableCollection<string>() { "U", "40", "60", "80", "90", "100", "110", "120", "150", "180", "200", "250" };
        private int _customCrossoverIndex = 0;

        // local for data binding (not serialised)
        private ObservableCollection<string> _customSpeakerTypeList = new ObservableCollection<string>() { "U", "S", "M", "L" };
        private int _customSpeakerTypeIndex = 0;

        private bool _sticky;

        #region Properties
        [JsonIgnore]
        public bool Sticky
        {
            get
            {
                return _sticky;
            }
            set
            {
                _sticky = value;
                RaisePropertyChanged("Sticky");
            }
        }
        public int EnChannelType
        {
            get
            {
                return _enChannelType;
            }
            set
            {
                _enChannelType = value;
                RaisePropertyChanged("EnChannelType");
            }
        }
        public bool IsSkipMeasurement
        {
            get
            {
                return _isSkipMeasurement;
            }
            set
            {
                _isSkipMeasurement = value;
                RaisePropertyChanged("IsSkipMeasurement");
            }
        }
        public string DelayAdjustment
        {
            get { return _delayAdjustment; }
            set
            {
                _delayAdjustment = value;
                RaisePropertyChanged("DelayAdjustment");
            }
        }
        public string CommandId
        {
            get { return _commandId; }
            set
            {
                _commandId = value;
                RaisePropertyChanged("CommandId");
            }
        }
        public string TrimAdjustment
        {
            get { return _trimAdjustment; }
            set
            {
                _trimAdjustment = value;
                RaisePropertyChanged("TrimAdjustment");
            }
        }
        public ChannelReport ChannelReport
        {
            get { return _channelReport; }
            set
            {
                _channelReport = value;
                RaisePropertyChanged("ChannelReport");
            }
        }
        public Dictionary<string, string[]> ResponseData
        {
            get
            {
                return _responseData;
            }
            set
            {
                _responseData = value;
                RaisePropertyChanged("ResponseData");
            }
        }

        public string[] CustomTargetCurvePoints
        {
            get
            {
                _customTargetCurvePoints = ConvertDictionaryToStringArray(CustomTargetCurvePointsDictionary);
                return _customTargetCurvePoints;
            }
            set
            {
                _customTargetCurvePoints = (string[]) value.Clone();
                CustomTargetCurvePointsDictionary = ConvertStringArrayToDictionary(value);
                RaisePropertyChanged("CustomTargetCurvePoints");
            }
        }
        [JsonIgnore]
        public ObservableCollection<MyKeyValuePair> CustomTargetCurvePointsDictionary
        {
            get { return _customTargetCurvePointsDictionary; }
            set
            {
                _customTargetCurvePointsDictionary = value;
                RaisePropertyChanged("CustomTargetCurvePointsDictionary");
            }
        }
        public bool MidrangeCompensation
        {
            get
            {
                return _midrangeCompensation;
            }
            set
            {
                _midrangeCompensation = value;
                RaisePropertyChanged("MidrangeCompensation");
            }
        }
        public decimal FrequencyRangeRolloff
        {
            get { return _frequencyRangeRolloff; }
            set
            {
                _frequencyRangeRolloff = value;
                RaisePropertyChanged("FrequencyRangeRolloff");
            }
        }
        public string CustomLevel
        {
            get { return _customLevel; }
            set
            {
                _customLevel = value;
                RaisePropertyChanged("CustomLevel");
            }
        }
        public string CustomSpeakerType
        {
            get
            {
                return _customSpeakerType;
            }
            set
            {
                _customSpeakerType = value;
                _customSpeakerTypeIndex = CustomSpeakerTypeList.IndexOf(value);
                if (_customSpeakerTypeIndex == -1) _customSpeakerTypeIndex = CustomSpeakerTypeList.IndexOf("U");
                RaisePropertyChanged("CustomSpeakerType");
            }
        }
        [JsonIgnore]
        public ObservableCollection<string> CustomSpeakerTypeList
        {
            get
            {
                return _customSpeakerTypeList;
            }
            set
            {
                _customSpeakerTypeList = value;
                RaisePropertyChanged("CustomSpeakerTypeList");
            }
        }
        [JsonIgnore]
        public int CustomSpeakerTypeIndex
        {
            get
            {
                return _customSpeakerTypeIndex;
            }
            set
            {
                _customSpeakerTypeIndex = value;
                RaisePropertyChanged("CustomSpeakerTypeIndex");
                _customSpeakerType = CustomSpeakerTypeList[value];
                RaisePropertyChanged("CustomSpeakerType");
            }
        }
        public decimal? CustomDistance
        {
            get { return _customDistance; }
            set
            {
                _customDistance = value;
                RaisePropertyChanged("CustomDistance");
            }
        }
        public string CustomCrossover
        {
            get { return _customCrossover; }
            set
            {
                _customCrossover = value;
                _customCrossoverIndex = CustomCrossoverList.IndexOf(value + "0");
                if (_customCrossoverIndex == -1) _customCrossoverIndex = CustomCrossoverList.IndexOf("U");
                RaisePropertyChanged("CustomCrossover");
            }
        }
        [JsonIgnore]
        public ObservableCollection<string> CustomCrossoverList
        {
            get { return _customCrossoverList; }
            set
            {
                _customCrossoverList = value;
            }
        }
        [JsonIgnore]
        public int CustomCrossoverIndex
        {
            get { return _customCrossoverIndex; }
            set
            {
                _customCrossoverIndex = value;
                RaisePropertyChanged("CustomCrossoverIndex");
                _customCrossover = CustomCrossoverList[value].Remove(CustomCrossoverList[value].Length-1, 1);
                RaisePropertyChanged("CustomCrossover");
            }
        }
        #endregion
        public bool ShouldSerializeResponseData()
        {
            return true;
        }

        public bool ShouldSerializeCustomTargetCurvePointsDictionary()
        {
            return false;
        }

        public bool ShouldSerializeCustomTargetCurvePoints()
        {
            if (EnChannelType == 55)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ShouldSerializeCustomLevel()
        {
            return true;
        }

        public bool ShouldSerializeCustomSpeakerType()
        {
            return (CustomSpeakerType != null);
        }

        public bool ShouldSerializeCustomDistance()
        {
            return (CustomDistance.HasValue);
        }

        public bool ShouldSerializeCustomCrossover()
        {
            return (CustomCrossover != null);
        }

        private ObservableCollection<MyKeyValuePair> ConvertStringArrayToDictionary(string[] array)
        {
            ObservableCollection<MyKeyValuePair> result = new ObservableCollection<MyKeyValuePair>();
            foreach(string s in array)
            {
                string str = s.Substring(1, s.Length - 2);
                string[] arr = str.Split(',');
                result.Add(new MyKeyValuePair(arr[0], arr[1]));
            }
            return new ObservableCollection<MyKeyValuePair>(result.OrderBy(x => Double.Parse(x.Key)));
        }
        private string[] ConvertDictionaryToStringArray(ObservableCollection<MyKeyValuePair> dict)
        {
            string[] result = new string[dict.Count];
            for (int i = 0; i < dict.Count; i++)
            {
                result[i] = dict[i].ToString();
            }
            return result;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var property in this.GetType().GetProperties())
            {
                sb.Append(property + "=" + property.GetValue(this, null) + "\r\n");
            }

            if (ChannelReport != null) sb.Append(ChannelReport.ToString());
            if (ResponseData != null) sb.Append(ResponseData.ToString());

            return sb.ToString();
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

    public class MyKeyValuePair : INotifyPropertyChanged
    {
        private double KeyMin = 10; //10Hz Chris Kyriakakis
        private double KeyMax = 24000; //24000Hz Chris Kyriakakis
        private double ValueMin = -20; //-12dB AUDYSSEY MultiEQ app -> -20dB Chris Kyriakakis
        private double ValueMax = 9; //12dB AUDYSSEY MultiEQ app -> +9dB Chris Kyriakakis
        string _key;
        string _value;
        public string Key
        {
            get { return _key; }
            set
            {
                double dValue = Double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                if (dValue >= KeyMin && dValue <= KeyMax)
                {
                    _key = value;
                }                
                RaisePropertyChanged("Key");
            }
        }
        public string Value
        {
            get { return _value; }
            set
            {
                double dValue = Double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                if (dValue >= ValueMin && dValue <= ValueMax)
                {
                    _value = value;
                }                
                RaisePropertyChanged("Value");
            }
        }
        public MyKeyValuePair(string key, string value)
        {
            Key = key.Trim();
            Value = value.Trim();
        }
        public MyKeyValuePair(decimal key, decimal value)
        {
            Key = key.ToString();
            Value = value.ToString();
        }
        public override string ToString()
        {
            return "{" + Key + ", " + Value + "}";
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
