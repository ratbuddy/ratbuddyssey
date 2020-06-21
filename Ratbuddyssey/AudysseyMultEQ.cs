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
            { " ", "High Frequency Roll Off 1", "High Frequency Roll Off 2"};

            private ObservableCollection<string> _MultEQTypeList = new ObservableCollection<string>()
            { "MultEQ", "MultEQXT", "MultEQXT32" };

            private ObservableCollection<string> _CrossoverList = new ObservableCollection<string>()
            { " ", "40", "60", "80", "90", "100", "110", "120", "150", "180", "200", "250", "F" };

            private ObservableCollection<string> _SpeakerTypeList = new ObservableCollection<string>()
            { " ", "S", "L" };

            private ObservableCollection<string> _ChannelSetupList = new ObservableCollection<string>()
            { "N", "S", "E" };

            private ObservableCollection<string> _AudyFinFlgList = new ObservableCollection<string>()
            { "Fin", "NotFin" };

            [JsonIgnore]
            public ObservableCollection<string> AmpAssignTypeList
            {
                get
                {
                    return _AmpAssignTypeList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> TargetCurveTypeList
            {
                get
                {
                    return _TargetCurveTypeList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> MultEQTypeList
            {
                get
                {
                    return _MultEQTypeList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> CrossoverList
            {
                get
                {
                    return _CrossoverList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> SpeakerTypeList
            {
                get
                {
                    return _SpeakerTypeList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> ChannelSetupList
            {
                get
                {
                    return _ChannelSetupList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> AudyFinFlgList
            {
                get
                {
                    return _AudyFinFlgList;
                }
            }
        }
    }
}