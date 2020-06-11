using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Audyssey.MultEQApp;
using Audyssey.MultEQAvr;

namespace Audyssey
{
    namespace MultEQAvrAdapter
    {
        // Adapter class needed as long as ethernet traffic uses the file GUI
        // TODO: design GUI TAB dedicated to ethernet traffic which makes this
        // adapter redundant -> directly access avr class and sniffer class!
        class AudysseyMultEQAvrAdapter : INotifyPropertyChanged
        {
            private AudysseyMultEQAvr _audysseyMultEQAvr;

            private ObservableCollection<string> _enMultEQTypeList = new ObservableCollection<string>()
            { "MultEQ", "MultEQXT", "MultEQXT32" };

            private ObservableCollection<string> _enAmpAssignTypeList = new ObservableCollection<string>()
            { "FrontA", "FrontB", "Type3", "Type4",
              "Type5", "Type6", "Type7", "Type8",
              "Type9", "Type10", "Type11", "Type12",
              "Type13", "Type14", "Type15", "Type16",
              "Type17", "Type18", "Type19", "Type20"};

            private ObservableCollection<DetectedChannel> _detectedChannels = new ObservableCollection<DetectedChannel>();

            #region Properties
            // same
            public string InterfaceVersion
            {
                get
                {
                    return _audysseyMultEQAvr.Info.Ifver;
                }
                set
                {
                    _audysseyMultEQAvr.Info.Ifver = value;
                    RaisePropertyChanged("InterfaceVersion");
                }
            }
            // different: name
            public decimal? AdcLineup
            {
                get
                {
                    return _audysseyMultEQAvr.Info.ADC;
                }
                set
                {
                    RaisePropertyChanged("AdcLineup");
                }
            }
            // same
            public int? SystemDelay
            {
                get
                {
                    return _audysseyMultEQAvr.Info.SysDelay;
                }
                set
                {
                    RaisePropertyChanged("SystemDelay");
                }
            }
            // local var for backwards compatibility with enum in file
            public ObservableCollection<string> EnMultEQTypeList
            {
                get
                {
                    return _enMultEQTypeList;
                }
                set
                {
                    RaisePropertyChanged("EnMultEQTypeList");
                }
            }
            // different: enum in file but string in eth
            public int EnMultEQType
            {
                get
                {
                    return _enMultEQTypeList.IndexOf(_audysseyMultEQAvr.Info.EQType);
                }
                set
                {
                    RaisePropertyChanged("EnMultEQType");
                }
            }
            // same (but capitals)
            public bool? Lfc
            {
                get
                {
                    return _audysseyMultEQAvr.Info.LFC;
                }
                set
                {
                    RaisePropertyChanged("Lfc");
                }
            }
            // same
            public bool? Auro
            {
                get
                {
                    return _audysseyMultEQAvr.Info.Auro;
                }
                set
                {
                    RaisePropertyChanged("Auro");
                }
            }
            // different: name
            public string UpgradeInfo
            {
                get
                {
                    return _audysseyMultEQAvr.Info.Upgrade;
                }
                set
                {
                    RaisePropertyChanged("UpgradeInfo");
                }
            }
            // local var for backwards compatibility with enum in file
            public ObservableCollection<string> EnAmpAssignTypeList
            {
                get
                {
                    return _enAmpAssignTypeList;
                }
                set
                {
                    RaisePropertyChanged("EnAmpAssignTypeList");
                }
            }
            // different: type in file but string in eth
            public int EnAmpAssignType
            {
                get
                {
                    return _enAmpAssignTypeList.IndexOf(_audysseyMultEQAvr.Status.AmpAssign);
                }
                set
                {
                    _audysseyMultEQAvr.Status.AmpAssign = _enAmpAssignTypeList.ElementAt(value);
                    RaisePropertyChanged("EnAmpAssignType");
                }
            }
            // different: name
            public string AmpAssignInfo
            {
                get
                {
                    return _audysseyMultEQAvr.Status.AssignBin;
                }
                set
                {
                    RaisePropertyChanged("AmpAssignInfo");
                }
            }
            // different: !!!
            public ObservableCollection<DetectedChannel> DetectedChannels
            {
                get
                {
                    if (_audysseyMultEQAvr.Status.ChSetup != null)
                    {
                        _detectedChannels = new ObservableCollection<DetectedChannel>();
                        foreach (var chsetup in _audysseyMultEQAvr.Status.ChSetup)
                        {
                            foreach (var ch in chsetup)
                            {
                                if (_detectedChannels != null)
                                {
                                    foreach (var channel in _detectedChannels)
                                    {
                                        if (channel.CommandId == ch.Key.ToUpper())
                                            break;
                                    }
                                    _detectedChannels.Add(new DetectedChannel());
                                    _detectedChannels.Last().CommandId = ch.Key;
                                    _detectedChannels.Last().CustomSpeakerType = ch.Value;
                                }
                                else
                                {
                                    _detectedChannels.Add(new DetectedChannel());
                                    _detectedChannels.Last().CommandId = ch.Key;
                                    _detectedChannels.Last().CustomSpeakerType = ch.Value;
                                }
                            }
                        }
                    }
                    return _detectedChannels;
                }
                set
                {
                    RaisePropertyChanged("DetectedChannels");
                }
            }
            #endregion

            public AudysseyMultEQAvrAdapter(AudysseyMultEQAvr audysseyMultEQAvr)
            {
                _audysseyMultEQAvr = audysseyMultEQAvr;
                // bind parent to nofify property changed
                audysseyMultEQAvr.PropertyChanged += _PropertyChanged;
            }

            ~AudysseyMultEQAvrAdapter()
            {
            }

            #region INotifyPropertyChanged members
            public event PropertyChangedEventHandler PropertyChanged = delegate { };

            protected void RaisePropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    Console.WriteLine("Changed: " + propertyName);
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public void _PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                // simple adapter forward notification of class 
                Console.WriteLine("Changed: e." + e.PropertyName);
                switch (e.PropertyName)
                {
                    case "Info":
                        RaisePropertyChanged("InterfaceVersion");
                        RaisePropertyChanged("AdcLineup");
                        RaisePropertyChanged("SystemDelay");
                        RaisePropertyChanged("EnMultEQType");
                        RaisePropertyChanged("Lfc");
                        RaisePropertyChanged("Auro");
                        RaisePropertyChanged("UpgradeInfo");
                        break;
                    case "Status":
                        RaisePropertyChanged("DetectedChannels");
                        RaisePropertyChanged("EnAmpAssignType");
                        RaisePropertyChanged("AmpAssignInfo");
                        break;
                }
            }
            #endregion
        }
    }
}
