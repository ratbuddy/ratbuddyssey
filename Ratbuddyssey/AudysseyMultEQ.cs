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

            private ObservableCollection<string> _AudyDynSetList = new ObservableCollection<string>()
            { "H", "M", "L"};

            private ObservableCollection<string> _AudyEqSetList = new ObservableCollection<string>()
            { "Audy", "Flat"};

            private ObservableCollection<int> _AudyEqRefList = new ObservableCollection<int>()
            { 0, 5, 10, 15};

            private ObservableCollection<int> _AudyLfcLevList = new ObservableCollection<int>()
            { 1, 2, 3, 4, 5, 6, 7};

            private ObservableCollection<decimal> _SelectedChLevelList = new ObservableCollection<decimal>()
            { -12m, -11.5m, -11m, -10.5m, -10m, -9.5m, -9m, -8.5m, -8m, -7.5m, -7m, -6.5m, -6m, -5.5m, -5m, -4.5m, -4m, -3.5m, -3m, -2.5m, -2m, -1.5m, -1m, -0.5m, 0m,
            0.5m, 1.0m, 1.5m, 2.0m, 2.5m, 3m, 3.5m, 4m, 4.5m, 5m, 5.5m, 6m, 6.5m, 7m, 7.5m, 9m, 8.5m, 9m, 9.5m, 10m, 10.5m, 11m, 11.5m, 12m};

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
            public ObservableCollection<string> AudyDynSetList
            {
                get
                {
                    return _AudyDynSetList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<string> AudyEqSetList
            {
                get
                {
                    return _AudyEqSetList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<int> AudyEqRefList
            {
                get
                {
                    return _AudyEqRefList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<int> AudyLfcLevList
            {
                get
                {
                    return _AudyLfcLevList;
                }
            }
            [JsonIgnore]
            public ObservableCollection<decimal> SelectedChLevelList
            {
                get
                {
                    return _SelectedChLevelList;
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