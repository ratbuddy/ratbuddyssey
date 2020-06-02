using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System;
using Audyssey.MultEQApp;
using Audyssey.MultEQAvr;

namespace Audyssey
{
    namespace MultEQAvrAdapter
    {
        // Adapter class needed as long as ethernet traffic uses the file GUI
        // TODO: design GUI TAB dedicated to ethernet traffic which makes this
        // adapter redundant -> directly access avr class and sniffer class!
        class MultEQAvrAdapter : INotifyPropertyChanged
        {
            private AudioVideoReceiver AudioVideoReceiver = null;

            private ObservableCollection<string> _enMultEQTypeList = new ObservableCollection<string>()
            { "MultEQ", "MultEQXT", "MultEQXT32" };

            private ObservableCollection<string> _enAmpAssignTypeList = new ObservableCollection<string>()
            { "FrontA", "FrontB", "Type3", "Type4",
              "Type5", "Type6", "Type7", "Type8",
              "Type9", "Type10", "Type11", "Type12",
              "Type13", "Type14", "Type15", "Type16",
              "Type17", "Type18", "Type19", "Type20"};

            private List<DetectedChannel> _detectedChannels = new List<DetectedChannel>();

            #region Properties
            // same
            public string InterfaceVersion
            {
                get
                {
                    return AudioVideoReceiver.AVRINF.Ifver;
                }
                set
                {
                    AudioVideoReceiver.AVRINF.Ifver = value;
                    RaisePropertyChanged("InterfaceVersion");
                }
            }
            // different: name
            public decimal AdcLineup
            {
                get
                {
                    return AudioVideoReceiver.AVRINF.ADC;
                }
                set
                {
                    RaisePropertyChanged("AdcLineup");
                }
            }
            // same
            public int SystemDelay
            {
                get
                {
                    return AudioVideoReceiver.AVRINF.SysDelay;
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
                    return _enMultEQTypeList.IndexOf(AudioVideoReceiver.AVRINF.EQType);
                }
                set
                {
                    RaisePropertyChanged("EnMultEQType");
                }
            }
            // same (but capitals)
            public bool Lfc
            {
                get
                {
                    return AudioVideoReceiver.AVRINF.LFC;
                }
                set
                {
                    RaisePropertyChanged("Lfc");
                }
            }
            // same
            public bool Auro
            {
                get
                {
                    return AudioVideoReceiver.AVRINF.Auro;
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
                    return AudioVideoReceiver.AVRINF.Upgrade;
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
                    return _enAmpAssignTypeList.IndexOf(AudioVideoReceiver.AVRSTS.AmpAssign);
                }
                set
                {
                    AudioVideoReceiver.AVRSTS.AmpAssign = _enAmpAssignTypeList.ElementAt(value);
                    RaisePropertyChanged("EnAmpAssignType");
                }
            }
            // different: name
            public string AmpAssignInfo
            {
                get
                {
                    return AudioVideoReceiver.AVRSTS.AssignBin;
                }
                set
                {
                    RaisePropertyChanged("AmpAssignInfo");
                }
            }
            // different: !!!
            public List<DetectedChannel> DetectedChannels
            {
                get
                {
                    if (AudioVideoReceiver.AVRSTS.ChSetup != null)
                    {
                        foreach (var chsetup in AudioVideoReceiver.AVRSTS.ChSetup)
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
            public MultEQAvrAdapter(bool bAttachSniffer = false)
            {
                AudioVideoReceiver = new AudioVideoReceiver(bAttachSniffer);
                AudioVideoReceiver.PropertyChanged += _PropertyChanged; // bind parent to nofify property changed (adapter's a bitch)
            }
            ~MultEQAvrAdapter()
            {
                AudioVideoReceiver = null;
            }
            public void AttachSniffer()
            {
                AudioVideoReceiver.AttachSniffer();
            }
            public void DetachSniffer()
            {
                AudioVideoReceiver.DetachSniffer();
            }
            public string GetTcpClient()
            {
                return AudioVideoReceiver.GetTcpClient();
            }
            public string GetTcpHost()
            {
                return AudioVideoReceiver.GetTcpHost();
            }
            public void AudysseyToAvr()
            {
                AudioVideoReceiver.AudysseyToAvr();
            }

            #region INotifyPropertyChanged members
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            #endregion
            #region methods
            protected void RaisePropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            public void _PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                // simple adapter forward notification of class 
                Console.WriteLine("Changed: " + e.PropertyName);
                switch (e.PropertyName)
                {
                    case "AVRINF":
                        RaisePropertyChanged("InterfaceVersion");
                        RaisePropertyChanged("AdcLineup");
                        RaisePropertyChanged("SystemDelay");
                        RaisePropertyChanged("EnMultEQType");
                        RaisePropertyChanged("Lfc");
                        RaisePropertyChanged("Auro");
                        RaisePropertyChanged("UpgradeInfo");
                        break;
                    case "AVRSTS":
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
