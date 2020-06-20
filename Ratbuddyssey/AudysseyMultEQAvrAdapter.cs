using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Audyssey.MultEQ;
using Audyssey.MultEQApp;
using Audyssey.MultEQAvr;

namespace Audyssey
{
    namespace MultEQAvrAdapter
    {
        // Adapter class needed as long as ethernet traffic uses the file GUI
        // TODO: design GUI TAB dedicated to ethernet traffic which makes this
        // adapter redundant -> directly access avr class and sniffer class!
        class AudysseyMultEQAvrAdapter : MultEQList, INotifyPropertyChanged
        {
            private AudysseyMultEQAvr _audysseyMultEQAvr;

            private ObservableCollection<DetectedChannel> _detectedChannels = new ObservableCollection<DetectedChannel>();

            #region Properties
            // same
            public string InterfaceVersion
            {
                get
                {
                    return _audysseyMultEQAvr.Ifver;
                }
                set
                {
                    _audysseyMultEQAvr.Ifver = value;
                    RaisePropertyChanged("InterfaceVersion");
                }
            }
            // different: name
            public decimal? AdcLineup
            {
                get
                {
                    return _audysseyMultEQAvr.ADC;
                }
                set
                {
                    _audysseyMultEQAvr.ADC = value;
                    RaisePropertyChanged("AdcLineup");
                }
            }
            // same
            public int? SystemDelay
            {
                get
                {
                    return _audysseyMultEQAvr.SysDelay;
                }
                set
                {
                    _audysseyMultEQAvr.SysDelay = value;
                    RaisePropertyChanged("SystemDelay");
                }
            }
            // different: enum in file but string in eth
            public int EnMultEQType
            {
                get
                {
                    return MultEQTypeList.IndexOf(_audysseyMultEQAvr.EQType);
                }
                set
                {
                    _audysseyMultEQAvr.EQType = MultEQTypeList.ElementAt(value);
                    RaisePropertyChanged("EnMultEQType");
                }
            }
            // same (but capitals)
            public bool? Lfc
            {
                get
                {
                    return _audysseyMultEQAvr.LFC;
                }
                set
                {
                    _audysseyMultEQAvr.LFC = value;
                    RaisePropertyChanged("Lfc");
                }
            }
            // same
            public bool? Auro
            {
                get
                {
                    return _audysseyMultEQAvr.Auro;
                }
                set
                {
                    _audysseyMultEQAvr.Auro = value;
                    RaisePropertyChanged("Auro");
                }
            }
            // different: name
            public string UpgradeInfo
            {
                get
                {
                    return _audysseyMultEQAvr.Upgrade;
                }
                set
                {
                    _audysseyMultEQAvr.Upgrade = value;
                    RaisePropertyChanged("UpgradeInfo");
                }
            }
            // different: type in file but string in eth
            public int EnAmpAssignType
            {
                get
                {
                    return AmpAssignTypeList.IndexOf(_audysseyMultEQAvr.AmpAssign);
                }
                set
                {
                    _audysseyMultEQAvr.AmpAssign = AmpAssignTypeList.ElementAt(value);
                    RaisePropertyChanged("EnAmpAssignType");
                }
            }
            // different: name
            public string AmpAssignInfo
            {
                get
                {
                    return _audysseyMultEQAvr.AssignBin;
                }
                set
                {
                    _audysseyMultEQAvr.AssignBin = value;
                    RaisePropertyChanged("AmpAssignInfo");
                }
            }
            // different: !!!
            public ObservableCollection<DetectedChannel> DetectedChannels
            {
                get
                {
                    if (_audysseyMultEQAvr.ChSetup != null)
                    {
                        _detectedChannels = new ObservableCollection<DetectedChannel>();
                        foreach (var chsetup in _audysseyMultEQAvr.ChSetup)
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
