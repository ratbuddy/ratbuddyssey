using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace Audyssey
{
    namespace MultEQAvr
    {
        //TODO: merge status <=> data AmpAssign, ssignBin, etc?
        public class AudysseyMultEQAvr : INotifyPropertyChanged
        {
            private AvrInfo _AvrInfo = null;
            private AvrStatus _AvrStatus = null;
            private AvrData _AvrData = null;
            private ObservableCollection<AvrDisFil> _AvrDisFil = null;
            private ObservableCollection<Int32[]> _AvrCoefData = null;
            /*local reference for selected channel from GUI*/
            private int _SelectedChannelIndex = 0;
            private string _SelectedChannel = null;
            private string _SeletedEqType = "Audy";

            #region Properties
            [JsonIgnore]
            public int SelectedChannelIndex
            {
                set
                {
                    _SelectedChannelIndex = value;
                    RaisePropertyChanged("CurrentCoefData");
                }
            }
            [JsonIgnore]
            public string SelectedChannel
            {
                set
                {
                    _SelectedChannel = value;
                    _AvrData.SelectedChannel = _SelectedChannel;
                    RaisePropertyChanged("CurrentDisFil");
                }
            }
            [JsonIgnore]
            public string SelectedEqType
            {
                set
                {
                    _SeletedEqType = value;
                    RaisePropertyChanged("CurrentDisFil");
                }
            }
            public AvrInfo Info
            {
                get
                {
                    return _AvrInfo;
                }
                set
                {
                    _AvrInfo = value;
                    RaisePropertyChanged("Info");
                }
            }
            public AvrStatus Status
            {
                get
                {
                    return _AvrStatus;
                }
                set
                {
                    _AvrStatus = value;
                    RaisePropertyChanged("Status");
                }
            }
            public AvrData Data
            {
                get
                {
                    return _AvrData;
                }
                set
                {
                    _AvrData = value;
                    RaisePropertyChanged("Data");
                }
            }
            public ObservableCollection<AvrDisFil> DisFil
            {
                get
                {
                    return _AvrDisFil;
                }
                set
                {
                    _AvrDisFil = value;
                    RaisePropertyChanged("DisFil");
                }
            }
            [JsonIgnore]
            public AvrDisFil CurrentDisFil
            {
                get
                {
                    if (_SelectedChannel != null)
                    {
                        foreach (var avrDisFil in _AvrDisFil)
                        {
                            if ((avrDisFil.ChData.Equals(_SelectedChannel)) &&
                                (avrDisFil.EqType.Equals(_SeletedEqType)))
                            {
                                CurrentCoefData = CoefData[_AvrDisFil.IndexOf(avrDisFil)];
                                RaisePropertyChanged("CurrentCoefData");
                                return avrDisFil;
                            }
                        }
                    }
                    return null;
                }
                set
                {
                }
            }
            public ObservableCollection<Int32[]> CoefData
            {
                get
                {
                    return _AvrCoefData;
                }
                set
                {
                    _AvrCoefData = value;
                    RaisePropertyChanged("CoefData");
                }
            }
            [JsonIgnore]
            public Int32[] CurrentCoefData //TODO add to the GUI
            {
                get
                {
                    return _AvrCoefData.ElementAt(_SelectedChannelIndex);
                }
                set
                {
                }
            }
            #endregion

            private const string NACK = "{\"Comm\":\"NACK\"}";
            private const string ACK = "{\"Comm\":\"ACK\"}";
            private const string INPROGRESS = "{\"Comm\":\"INPROGRESS\"}";
            private const string AUDYFINFLG = "{\"AudyFinFlg\":\"Fin\"}";

            private TcpIP TcpClient = null;

            private AudysseyMultEQAvrTcpClientWithTimeout audysseyMultEQAvrTcpClientWithTimeout = null;

            public string GetTcpClientAsString()
            {
                return TcpClient.Address;
            }

            public TcpIP GetTcpClient()
            {
                return TcpClient;
            }
            
            ~AudysseyMultEQAvr()
            {
            }

            public AudysseyMultEQAvr(string TcpIPAddress)
            {
                _AvrInfo = new AvrInfo();
                _AvrStatus = new AvrStatus();
                _AvrData = new AvrData();
                _AvrDisFil = new ObservableCollection<AvrDisFil>();
                _AvrCoefData = new ObservableCollection<Int32[]>();
                TcpClient = new TcpIP(TcpIPAddress, 1256, 5000);
            }

            public void Connect()
            {
                audysseyMultEQAvrTcpClientWithTimeout = new AudysseyMultEQAvrTcpClientWithTimeout(TcpClient.Address, TcpClient.Port, TcpClient.Timeout);
            }
            
            public void QueryAudyssey()
            {
                if (GetAvrInfo())
                {
#if DEBUG
                    string AvrInfoFile = JsonConvert.SerializeObject(Info, new JsonSerializerSettings { });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrInfo.json", AvrInfoFile);
#endif
                }

                if (GetAvrStatus())
                {
#if DEBUG
                    string AvrStatusFile = JsonConvert.SerializeObject(Status, new JsonSerializerSettings { });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrStatus.json", AvrStatusFile);
#endif
                }
            }

            public void AudysseyToAvr()
            {
                //if (SetAvrSetAmp())
                {
#if DEBUG
                    string AvrSetDataAmpFile = JsonConvert.SerializeObject(Data, new JsonSerializerSettings {
                        ContractResolver = new InterfaceContractResolver(typeof(IAmp))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAmp.json", AvrSetDataAmpFile);
#endif
                }

                //if (SetAvrSetAudy())
                {
#if DEBUG
                    string AvrSetDataAudFile = JsonConvert.SerializeObject(Data, new JsonSerializerSettings {
                        ContractResolver = new InterfaceContractResolver(typeof(IAudy))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAud.json", AvrSetDataAudFile);
#endif
                }

                //if (SetAvrDisFil())
                {
#if DEBUG
                    string AvrDisFilFile = JsonConvert.SerializeObject(DisFil, new JsonSerializerSettings { });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrDisFil.json", AvrDisFilFile);
#endif
                }

                //if (InitAudysseyCoef())

                //if (SetAudysseyCoefData())

                //if (SetAudysseyFinishedFlag())

            }
            
            private string MakeQuery(string Serialized)
            {
                var SerializedJObject = JObject.Parse(Serialized);
                foreach (var prop in SerializedJObject.Properties()) { prop.Value = "?"; }
                return SerializedJObject.ToString(Formatting.None);
            }
            
            public bool GetAvrInfo()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "GET_AVRINF";
                    Console.Write(CmdString);
                    // build JSON and replace values with "?"
                    string AvrString = MakeQuery(JsonConvert.SerializeObject(Info, new JsonSerializerSettings { }));
                    Console.WriteLine(AvrString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive response
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    // parse JSON to class member variables
                    Info = JsonConvert.DeserializeObject<AvrInfo>(AvrString,
                        new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
                    return (CmdString.Equals("GET_AVRINF") && !AvrString.Equals(NACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool GetAvrStatus()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "GET_AVRSTS";
                    Console.Write(CmdString);
                    // build JSON and replace values with "?"
                    string AvrString = MakeQuery(JsonConvert.SerializeObject(Status, new JsonSerializerSettings { }));
                    Console.WriteLine(AvrString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive rseponse
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    // parse JSON to class member variables
                    Status = JsonConvert.DeserializeObject<AvrStatus>(AvrString,
                        new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
                    return (CmdString.Equals("GET_AVRSTS") && !AvrString.Equals(NACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool EnterAudysseyMode()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "ENTER_AUDY";
                    Console.WriteLine(CmdString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, "");
                    // receive rseponse
                    string AvrString;
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    return (CmdString.Equals("ENTER_AUDY") && AvrString.Equals(ACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool ExitAudysseyMode()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "EXIT_AUDMD";
                    Console.WriteLine(CmdString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, "");
                    // receive rseponse
                    string AvrString;
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    return (CmdString.Equals("EXIT_AUDMD") && AvrString.Equals(ACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool SetAudysseyFinishedFlag()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "SET_SETDAT";
                    Console.WriteLine(CmdString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AUDYFINFLG);
                    // receive rseponse
                    string AvrString;
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool SetAvrSetAmp()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "SET_SETDAT";
                    Console.Write(CmdString);
                    // build JSON for class Dat on interface Iamp
                    string AvrString = JsonConvert.SerializeObject(Data, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IAmp))
                    });
                    Console.WriteLine(AvrString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive rseponse
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool SetAvrSetAudy()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "SET_SETDAT";
                    Console.Write(CmdString);
                    // build JSON for class Dat on interface IAudy
                    string AvrString = JsonConvert.SerializeObject(Data, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IAudy))
                    });
                    Console.WriteLine(AvrString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive rseponse
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool SetDisFil()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    // there are multiple speaker?
                    foreach (var AvrDisFil in DisFil)
                    {
                        bool CheckSumChecked = false;
                        string CmdString = "SET_DISFIL";
                        Console.Write(CmdString);
                        // build JSON
                        string AvrString = JsonConvert.SerializeObject(AvrDisFil, new JsonSerializerSettings { });
                        Console.WriteLine(AvrString);
                        // transmit request
                        audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                        // receive rseponse
                        audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                        Console.Write(CmdString);
                        Console.WriteLine(AvrString);
                        // check every transmission
                        if (!(CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked)) return false;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
            public bool InitAvrCoefs()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool CheckSumChecked = false;
                    string CmdString = "INIT_COEFS";
                    Console.WriteLine(CmdString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, "");
                    // response may take some processing time for the receiver
                    var TimeElapsed = Stopwatch.StartNew();
                    string AvrString;
                    do
                    {   // receive reseponse
                        audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                        Console.Write(CmdString);
                        Console.WriteLine(AvrString);
                    } while ((TimeElapsed.ElapsedMilliseconds < 10000) && AvrString.Equals(INPROGRESS));
                    return (CmdString.Equals("INIT_COEFS") && AvrString.Equals(ACK) && CheckSumChecked);
                }
                else
                {
                    return false;
                }
            }
            
            public bool SetAvrCoefDt()
            {
                if (audysseyMultEQAvrTcpClientWithTimeout != null)
                {
                    bool Success = true;
                    // data for each speaker... this is a very dumb binary data pump
                    foreach (Int32[] Coef in CoefData)
                    {
                        bool CheckSumChecked = false;
                        string CmdString = "SET_COEFDT";
                        Console.Write(CmdString);
                        Console.WriteLine(Coef.ToString());
                        // transmit request
                        audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, Coef);
                        string AvrString;
                        // receive rseponse
                        audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                        Console.Write(CmdString);
                        Console.WriteLine(AvrString);
                        // success if all succeed
                        Success &= (CmdString.Equals("SET_COEFDT") && AvrString.Equals(ACK) && CheckSumChecked);
                    }
                    return Success;
                }
                else
                {
                    return false;
                }
            }

            #region INotifyPropertyChanged implementation
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            private void RaisePropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }       
    }
}
