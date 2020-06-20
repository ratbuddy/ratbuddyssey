using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Audyssey
{
    namespace MultEQ
    {
        public class MultEQList
        {
            private ObservableCollection<string> _AmpAssignTypeList = new ObservableCollection<string>()
            { "FrontA", "FrontB", "Type3", "Type4",
              "Type5", "Type6", "Type7", "Type8",
              "Type9", "Type10", "Type11", "Type12",
              "Type13", "Type14", "Type15", "Type16",
              "Type17", "Type18", "Type19", "Type20"};

            private ObservableCollection<string> _TargetCurveTypeList = new ObservableCollection<string>()
            { "Undefined", "High Frequency Roll Off 1", "High Frequency Roll Off 2"};

            private ObservableCollection<string> _MultEQTypeList = new ObservableCollection<string>()
            { "MultEQ", "MultEQXT", "MultEQXT32" };

            private ObservableCollection<string> _CrossoverList = new ObservableCollection<string>()
            { "U", "40", "60", "80", "90", "100", "110", "120", "150", "180", "200", "250" };
            
            private ObservableCollection<string> _SpeakerTypeList = new ObservableCollection<string>()
            { "U", "S", "M", "L" };

            [JsonIgnore]
            public ObservableCollection<string> TargetCurveTypeList
            {
                get
                {
                    return _TargetCurveTypeList;
                }
                set
                {
                    _TargetCurveTypeList = value;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> AmpAssignTypeList
            {
                get
                {
                    return _AmpAssignTypeList;
                }
                set
                {
                    _AmpAssignTypeList = value;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> MultEQTypeList
            {
                get
                {
                    return _MultEQTypeList;
                }
                set
                {
                    _MultEQTypeList = value;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> SpeakerTypeList
            {
                get
                {
                    return _SpeakerTypeList;
                }
                set
                {
                    _SpeakerTypeList = value;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> CrossoverList
            {
                get
                {
                    return _CrossoverList;
                }
                set
                {
                    _CrossoverList = value;
                }
            }
        }
    }
}