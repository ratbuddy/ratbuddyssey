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
        public class AudysseyMultEQAvrTcp : INotifyPropertyChanged
        {
            private AudysseyMultEQAvr _audysseyMultEQAvr = null;

            private TcpIP TcpClient = null;
            private AudysseyMultEQAvrTcpClientWithTimeout audysseyMultEQAvrTcpClientWithTimeout = null;

            #region Properties
            #endregion

            private const string NACK = "{\"Comm\":\"NACK\"}";
            private const string ACK = "{\"Comm\":\"ACK\"}";
            private const string INPROGRESS = "{\"Comm\":\"INPROGRESS\"}";
            private const string AUDYFINFLG = "{\"AudyFinFlg\":\"Fin\"}";
            private const string AUDYNOTFINFLG = "{\"AudyFinFlg\":\"NotFin\"}";

            public string GetTcpClientAsString()
            {
                return TcpClient.Address;
            }

            public TcpIP GetTcpClient()
            {
                return TcpClient;
            }
            
            public AudysseyMultEQAvrTcp(AudysseyMultEQAvr audysseyMultEQAvr, string ClientAddress)
            {
                _audysseyMultEQAvr = audysseyMultEQAvr;
                TcpClient = new TcpIP(ClientAddress, 1256, 5000);
            }

            public void Connect()
            {
                audysseyMultEQAvrTcpClientWithTimeout = new AudysseyMultEQAvrTcpClientWithTimeout(TcpClient.Address, TcpClient.Port, TcpClient.Timeout);
            }
            
            public void AudysseyToAvr()
            {
                //if (SetAvrSetAmp())
                {
#if DEBUG
                    string AvrSetDataAmpFile = JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings {
                        ContractResolver = new InterfaceContractResolver(typeof(IAmp))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAmp.json", AvrSetDataAmpFile);
#endif
                }

                //if (SetAvrSetAudy())
                {
#if DEBUG
                    string AvrSetDataAudFile = JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings {
                        ContractResolver = new InterfaceContractResolver(typeof(IAudy))
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAud.json", AvrSetDataAudFile);
#endif
                }

                //if (SetAvrDisFil())
                {
#if DEBUG
                    string AvrDisFilFile = JsonConvert.SerializeObject(_audysseyMultEQAvr.DisFil, new JsonSerializerSettings { });
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
                    string AvrString = MakeQuery(JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings {
                        ContractResolver = new InterfaceContractResolver(typeof(IInfo))
                    }));
                    Console.WriteLine(AvrString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive response
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    // parse JSON to class member variables
                    JsonConvert.PopulateObject(AvrString, _audysseyMultEQAvr, new JsonSerializerSettings
                    {
                            ContractResolver = new InterfaceContractResolver(typeof(IInfo)),
                            FloatParseHandling = FloatParseHandling.Decimal 
                    });
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
                    string AvrString = MakeQuery(JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings {
                        ContractResolver = new InterfaceContractResolver(typeof(IStatus))
                    }));
                    Console.WriteLine(AvrString);
                    // transmit request
                    audysseyMultEQAvrTcpClientWithTimeout.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive rseponse
                    audysseyMultEQAvrTcpClientWithTimeout.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                    // parse JSON to class member variables
                    JsonConvert.PopulateObject(AvrString, _audysseyMultEQAvr, new JsonSerializerSettings
                    {
                        ContractResolver = new InterfaceContractResolver(typeof(IStatus)),
                        FloatParseHandling = FloatParseHandling.Decimal,
                    });
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
                    // clear finflag
                    _audysseyMultEQAvr.AudyFinFlg = "NotFin";  //TODO what does this flag do?
                    // build JSON for class Dat on interface Iamp
                    string AvrString = JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings
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
                    string AvrString = JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings
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
                    foreach (var AvrDisFil in _audysseyMultEQAvr.DisFil)
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
                    foreach (Int32[] Coef in _audysseyMultEQAvr.CoefData)
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
