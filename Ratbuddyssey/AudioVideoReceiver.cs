using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Buffers.Binary;

namespace Ratbuddyssey
{
    class TcpIP
    {
        #region Properties
        public string Address { get; set; }
        public int Port { get; set; }
        public int Timeout { get; set; }
        #endregion
        public void Init(string address, int port, int timeout)
        {
            Address = address;
            Port = port;
            Timeout = timeout;
        }
    }
    class CoefWaitTimeClass
    {
        #region Properties
        public decimal Init { get; set; }
        public decimal Final { get; set; }
        #endregion
    }
    class AvrInfo
    {
        //{"Ifver":"00.08","DType":"FixedA","CoefWaitTime":{"Init":3000,"Final":0},"ADC":2.11500,"SysDelay":261,"EQType":"MultEQXT32","SWLvlMatch":true,"LFC":true,"Auro":false,"Upgrade":"None"}
        #region Properties
        public string Ifver
        { get; set; }
        public string DType
        { get; set; }
        public CoefWaitTimeClass CoefWaitTime
        { get; set; }
        public decimal ADC
        { get; set; }
        public int SysDelay
        { get; set; }
        public string EQType
        { get; set; }
        public bool SWLvMatch
        { get; set; }
        public bool LFC
        { get; set; }
        public bool Auro
        { get; set; }
        public string Upgrade
        { get; set; }
        #endregion
    }
    class AvrStatus
    {
        //{"HPPlug":false,"Mic":false,"AmpAssign":"Zone2","AssignBin":"0102030200000000010002000200200001001000200010000100200002021002020200010203040600010000","ChSetup":[{"FL":"S"},{"C":"S"},{"FR":"S"},{"SLA":"S"},{"SRA":"S"},{"SWMIX1":"E"},{"SWMIX2":"N"}],"BTTXStatus":false}
        #region Properties
        public bool HPPlug
        { get; set; }
        public bool Mic
        { get; set; }
        public string AmpAssign
        { get; set; }
        public string AssignBin
        { get; set; }
        public List<Dictionary<string,string>> ChSetup
        { get; set; }
        public bool BTTXStatus
        { get; set; }
        public bool SpPreset
        { get; set; }
        #endregion
    }
    class AvrSetDataAmp
    {
        //{"AmpAssign":"FrontB","AssignBin":"0A02030200000000010002000200200001001000200010000100200002021002020200010203040600010001","SpConfig":[{"FL":"S"},{"SLA":"S"},{"FR":"S"},{"SW1":"S"},{"SRA":"S"},{"C":"S"}],"Distance":[{"FL":310},{"SLA":220},{"FR":310},{"SW1":380},{"SRA":220},{"C":310}],"ChLevel":[{"FL":-25},{"SLA":-25},{"FR":-25},{"SW1":0},{"SRA":-25},{"C":-65}],"Crossover":[{"FL":12},{"SLA":12},{"FR":12},{"SW1":"F"},{"SRA":12},{"C":12}],"AudyFinFlg":"NotFin","AudyDynEq":false,"AudyEqRef":0}
        #region Properties
        public string AmpAssign
        { get; set; } = "FrontB";
        public string AssignBin
        { get; set; } = "0A02030200000000010002000200200001001000200010000100200002021002020200010203040600010001";
        public List<Dictionary<string, string>> SpConfig
        { get; set; } // = [{"FL":"S"},{"SLA":"S"},{"FR":"S"},{"SW1":"S"},{"SRA":"S"},{"C":"S"}]
        public List<Dictionary<string, int>> Distance // unit: .01 m or cm
        { get; set; } //= [{"FL":310},{"SLA":220},{"FR":310},{"SW1":380},{"SRA":220},{"C":310}]
        public List<Dictionary<string, int>> ChLevel // unit: 0.1 dB
        { get; set; } //= [{"FL":-25},{"SLA":-25},{"FR":-25},{"SW1":0},{"SRA":-25},{"C":-65}]
        public List<Dictionary<string, object>> Crossover // unit: 10 Hz
        { get; set; } //= [{"FL":12},{"SLA":12},{"FR":12},{"SW1":"F"},{"SRA":12},{"C":12}]
        public string AudyFinFlg
        { get; set; } = "NotFin";
        public bool AudyDynEq
        { get; set; } = false;
        public int AudyEqRef
        { get; set; } = 0;
        #endregion
        public void AvrData()
        {
            SpConfig = new List<Dictionary<string, string>>() { };
            SpConfig.Add(new Dictionary<string, string>() { { "FL", "S" } });
            SpConfig.Add(new Dictionary<string, string>() { { "FL", "S" } });
            SpConfig.Add(new Dictionary<string, string>() { { "SLA", "S" } });
            SpConfig.Add(new Dictionary<string, string>() { { "FR", "S" } });
            SpConfig.Add(new Dictionary<string, string>() { { "SW1", "S" } });
            SpConfig.Add(new Dictionary<string, string>() { { "SRA", "S" } });
            SpConfig.Add(new Dictionary<string, string>() { { "C", "S" } });

            Distance = new List<Dictionary<string, int>>() { };
            Distance.Add(new Dictionary<string, int>() { { "FL", 310 } });
            Distance.Add(new Dictionary<string, int>() { { "SLA", 220 } });
            Distance.Add(new Dictionary<string, int>() { { "FR" , 310 } });
            Distance.Add(new Dictionary<string, int>() { { "SW1", 380 } });
            Distance.Add(new Dictionary<string, int>() { { "SRA", 220 } });
            Distance.Add(new Dictionary<string, int>() { { "C", 310 } });
            
            ChLevel = new List<Dictionary<string, int>>() { };
            Distance.Add(new Dictionary<string, int>() { { "FL", -25 } });
            Distance.Add(new Dictionary<string, int>() { { "SLA", -25 } });
            Distance.Add(new Dictionary<string, int>() { { "FR", -25 } });
            Distance.Add(new Dictionary<string, int>() { { "SW1", 0 } });
            Distance.Add(new Dictionary<string, int>() { { "SRA", -25 } });
            Distance.Add(new Dictionary<string, int>() { { "C", -65 } });
            
            Crossover = new List<Dictionary<string, object>>() { };
            Crossover.Add(new Dictionary<string, object>() { { "FL", 12 } });
            Crossover.Add(new Dictionary<string, object>() { { "SLA", 12 } });
            Crossover.Add(new Dictionary<string, object>() { { "FR", 12 } });
            Crossover.Add(new Dictionary<string, object>() { { "SW1", "F" } });
            Crossover.Add(new Dictionary<string, object>() { { "SRA", 12 } });
            Crossover.Add(new Dictionary<string, object>() { { "C", 12 } });
        }
    }
    class AvrSetDataAud
    {
        //{"AudyDynVol":false,"AudyDynSet":"M","AudyMultEq":true,"AudyEqSet":"Audy","AudyLfc":false,"AudyLfcLev":4}
        #region Properties
        public bool AudyDynVol
        { get; set; } = false;
        public string AudyDynSet
        { get; set; } = "M";
        public bool AudyMultEq
        { get; set; } = true;
        public string AudyEqSet
        { get; set; } = "Audy";
        public bool AudyLfc
        { get; set; } = false;
        public int AudyLfcLev
        { get; set; } = 4;
        #endregion
    }
    class AvrDisFil
    {
        //SET_DISFIL{"EqType":"Audy","ChData":"FL","FilData":[0,0,0,0,0,0,1,0,-10,-6,-10,0,-7,-8,-1,2,0,1,-2,0,-6,-2,-1,-1,-3,-2,-3,-1,2,0,0,0,0,0,0,0,-2,1,1,2,1,2,1,1,3,1,0,0,0,2,2,3,2,2,2,2,0,1,4,8,7],"DispData":[-6,-1,-3,-1,0,2,1,3,5]
        //SET_DISFIL{"EqType":"Audy","ChData":"C","FilData":[0,0,0,0,0,0,0,0,-2,0,-2,1,0,-1,4,2,0,-7,-3,-8,-10,-4,-2,-2,-5,-3,2,0,-4,1,0,0,0,1,0,2,3,6,4,3,1,3,4,3,3,1,2,1,0,2,2,1,0,0,0,-1,-1,2,1,2,1],"DispData":[-1,-2,-5,-1,2,4,2,1,2]}
        //SET_DISFIL{"EqType":"Audy","ChData":"FR","FilData":[0,0,0,-1,-1,-3,2,-1,4,3,-7,0,8,-1,0,0,0,0,-3,-2,-7,-1,0,0,-3,-5,-2,0,2,2,0,0,0,0,1,0,-1,1,1,2,0,1,1,1,3,0,-2,-2,-2,0,1,1,-1,-1,-1,-1,-4,-3,0,1,-2],"DispData":[1,-1,-3,0,0,1,-1,-1,-2]}
        //SET_DISFIL{"EqType":"Audy","ChData":"SLA","FilData":[0,0,0,0,1,1,-1,-7,3,-4,-9,-3,-6,-3,1,0,2,2,0,-5,-2,0,-1,-3,-3,-6,-3,-2,1,2,1,0,1,1,0,0,2,0,2,1,1,1,1,2,5,0,-3,-4,-4,-1,0,0,-1,-1,-2,-3,-4,-3,-1,1,0],"DispData":[-4,0,-3,-1,1,2,-1,-1,-2]}
        //SET_DISFIL{"EqType":"Audy","ChData":"SRA","FilData":[0,0,0,1,1,0,-4,1,0,-1,-11,3,5,0,-1,0,0,-1,0,-1,2,1,0,-3,-3,-5,-5,-2,0,1,0,0,0,1,0,0,1,1,2,1,2,2,2,3,3,-1,-4,-3,-3,-1,0,0,-1,0,-2,-2,-4,-3,-2,0,0],"DispData":[0,-1,-2,-2,0,2,-1,-1,-2]}
        //SET_DISFIL{"EqType":"Audy","ChData":"SW1","FilData":[-1,0,3,4,6,7,8,7,-4,0,-4,4,0,1,1,8,8,8,7,5,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0],"DispData":[1,6,1,0,0,0,0,0,0]}
        //SET_DISFIL{"EqType":"Flat","ChData":"FL","FilData":[0,0,0,0,0,0,1,0,-10,-6,-10,0,-7,-8,-1,2,0,1,-2,0,-6,-2,-1,-1,-3,-2,-3,-1,2,0,0,0,0,0,0,0,-2,1,1,2,1,2,1,1,3,1,0,0,0,3,4,5,5,5,5,6,4,6,8,9,9],"DispData":[-6,-1,-3,-1,0,2,1,5,8]}
        //SET_DISFIL{"EqType":"Flat","ChData":"C","FilData":[0,0,0,0,0,0,0,0,-2,0,-2,1,0,-1,4,2,0,-7,-3,-8,-10,-4,-2,-2,-5,-3,2,0,-4,1,0,0,0,1,0,2,3,6,4,3,1,3,4,3,3,1,2,2,1,3,3,3,3,2,3,2,3,7,6,7,7],"DispData":[-1,-2,-5,-1,2,4,2,3,6]}
        //SET_DISFIL{"EqType":"Flat","ChData":"FR","FilData":[0,0,0,-1,-1,-3,2,-1,4,3,-7,0,8,-1,0,0,0,0,-3,-2,-7,-1,0,0,-3,-5,-2,0,2,2,0,0,0,0,1,0,-1,1,1,2,0,1,1,1,3,0,-2,-2,-1,0,2,3,1,1,2,1,0,1,4,7,3],"DispData":[1,-1,-3,0,0,1,0,2,3]}
        //SET_DISFIL{"EqType":"Flat","ChData":"SLA","FilData":[0,0,0,0,1,1,-1,-7,3,-4,-9,-3,-6,-3,1,0,2,2,0,-5,-2,0,-1,-3,-3,-6,-3,-2,1,2,1,0,1,1,0,0,2,0,2,1,1,1,1,2,5,0,-3,-4,-3,0,1,2,0,2,0,0,0,1,3,7,5],"DispData":[-4,0,-3,-1,1,2,0,1,3]}
        //SET_DISFIL{"EqType":"Flat","ChData":"SRA","FilData":[0,0,0,1,1,0,-4,1,0,-1,-11,3,5,0,-1,0,0,-1,0,-1,2,1,0,-3,-3,-5,-5,-2,0,1,0,0,0,1,0,0,1,1,2,1,2,2,2,3,3,-1,-3,-3,-3,0,0,1,1,2,1,0,0,1,2,6,5],"DispData":[0,-1,-2,-2,0,2,-1,1,3]}
        //SET_DISFIL{"EqType":"Flat","ChData":"SW1","FilData":[-1,0,3,4,6,7,8,7,-4,0,-4,4,0,1,1,8,8,9,9,9,8,8,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9,9],"DispData":[1,7,9,9,9,9,9,9,9]}
        #region Properties
        public string EqType
        { get; set; } = "Audy";
        public string ChData
        { get; set; } = "FL";
        public List<int> FilData
        { get; set; } = new List<int>() { 0, 0, 0, 0, 0, 0, 1, 0, -10, -6, -10, 0, -7, -8, -1, 2, 0, 1, -2, 0, -6, -2, -1, -1, -3, -2, -3, -1, 2, 0, 0, 0, 0, 0, 0, 0, -2, 1, 1, 2, 1, 2, 1, 1, 3, 1, 0, 0, 0, 2, 2, 3, 2, 2, 2, 2, 0, 1, 4, 8, 7 };
        public List<int> DispData
        { get; set; } = new List<int>() { -6, -1, -3, -1, 0, 2, 1, 3, 5 };
        #endregion
    }
    class AvrCoefData
    {
        #region Properties
        public List<Int32> FilData
        { get; set; }
        public List<Int32> DispData
        { get; set; }
        #endregion
        public AvrCoefData()
        {
            Int32[] Array = { 0 };
            FilData = new List<Int32>(Array);
        }
    }
    class Avr : INotifyPropertyChanged
    {
        private const string NACK = "{\"Comm\":\"NACK\"}";
        private const string ACK = "{\"Comm\":\"ACK\"}";
        private const string INPROGRESS = "{\"Comm\":\"INPROGRESS\"}";

        private ObservableCollection<string> _enMultEQTypeList = new ObservableCollection<string>()
        { "MultEQ", "MultEQXT", "MultEQXT32" };

        private ObservableCollection<string> _enAmpAssignTypeList = new ObservableCollection<string>()
        { "FrontA", "FrontB", "Type3", "Type4",
          "Type5", "Type6", "Type7", "Type8",
          "Type9", "Type10", "Type11", "Type12",
          "Type13", "Type14", "Type15", "Type16",
          "Type17", "Type18", "Type19", "Type20"};

        private string ClientTcpIPFileName = "ClientTcpIP.json";

        private TcpClientWithTimeout TcpAudysseyStream = null;

        private TcpIP parsedClientTcpIP = null;
        private AvrInfo parsedAvrInfo = null;
        private AvrStatus parsedAvrStatus = null;
        private AvrSetDataAmp parsedAvrSetDataAmp = null;
        private AvrSetDataAud parsedAvrSetDataAud = null;
        private List<AvrDisFil> parsedAvrDisFil = null;
        private List<AvrCoefData> parsedAvrCoefData = null;

        TcpSniffer sniffer;

        private string CmdString;
        private string AvrString;

        private bool CheckSumChecked = false;

        private List<DetectedChannel> _detectedChannels = new List<DetectedChannel>();

        #region Properties
        // same
        public string InterfaceVersion
        {
            get
            {
                return parsedAvrInfo.Ifver;
            }
            set
            {
                parsedAvrInfo.Ifver = value;
            }
        }
        //new
        public string DType
        {
            get
            {
                return parsedAvrInfo.DType;
            }
            set
            {
                parsedAvrInfo.DType = value;
            }
        }
        // new
        public CoefWaitTimeClass coefWaitTime
        {
            get
            {
                return parsedAvrInfo.CoefWaitTime;
            }
            set
            {
                parsedAvrInfo.CoefWaitTime = value;
            }
        }
        // different: name
        public decimal AdcLineup
        {
            get
            {
                return parsedAvrInfo.ADC;
            }
            set
            {
                parsedAvrInfo.ADC = value;
            }
        }
        // same
        public int SystemDelay
        {
            get
            {
                return parsedAvrInfo.SysDelay;
            }
            set
            {
                parsedAvrInfo.SysDelay = value;
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
                _enMultEQTypeList = value;
            }
        }
        // different: enum in file but string in eth
        public int EnMultEQType
        {
            get
            {
                return _enMultEQTypeList.IndexOf(parsedAvrInfo.EQType);
            }
            set
            {
                parsedAvrInfo.EQType = _enMultEQTypeList.ElementAt(value);
            }
        }
        // different (not sure if those are the same keys)
        public bool LfcSupport
        {
            get
            {
                return parsedAvrInfo.SWLvMatch;
            }
            set
            {
                parsedAvrInfo.SWLvMatch = value;
            }
        }
        // same (but capitals)
        public bool Lfc
        {
            get
            {
                return parsedAvrInfo.LFC;
            }
            set
            {
                parsedAvrInfo.LFC = value;
            }
        }
        // same
        public bool Auro
        {
            get
            {
                return parsedAvrInfo.Auro;
            }
            set
            {
                parsedAvrInfo.Auro = value;
            }
        }
        // different: name
        public string UpgradeInfo
        {
            get
            {
                return parsedAvrInfo.Upgrade;
            }
            set
            {
                parsedAvrInfo.Upgrade = value;
            }
        }
        // new
        public bool HpPlug
        {
            get
            {
                return parsedAvrStatus.HPPlug;
            }
            set
            {
                parsedAvrStatus.HPPlug = value;
            }
        }
        //  new
        public bool Mic
        {
            get
            {
                return parsedAvrStatus.Mic;
            }
            set
            {
                parsedAvrStatus.Mic = value;
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
                _enAmpAssignTypeList = value;
            }
        }
        // different: type in file but string in eth
        public int EnAmpAssignType
        {
            get
            {
                return _enAmpAssignTypeList.IndexOf(parsedAvrStatus.AmpAssign);
            }
            set
            {
                parsedAvrStatus.AmpAssign = _enAmpAssignTypeList.ElementAt(value);
            }
        }
        // different: name
        public string AmpAssignInfo
        {
            get
            {
                return parsedAvrStatus.AssignBin;
            }
            set
            {
                parsedAvrStatus.AssignBin = value;
            }
        }
        // different: !!!
        public List<DetectedChannel> DetectedChannels
        {
            get
            {
                foreach (var chsetup in parsedAvrStatus.ChSetup)
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
                return _detectedChannels;
            }
            set
            {
                _detectedChannels = value;
            }
        }
        // new
        public bool BTTXStatus
        {
            get
            {
                return parsedAvrStatus.BTTXStatus;
            }
            set
            {
                parsedAvrStatus.BTTXStatus = value;
            }
        }
        // new
        public bool SpPreset
        {
            get
            {
                return parsedAvrStatus.SpPreset;
            }
            set
            {
                parsedAvrStatus.SpPreset = value;
            }
        }
        #endregion
        public string GetTcpIpClient()
        {
            return parsedClientTcpIP.Address + "::"+ parsedClientTcpIP.Port.ToString();
        }
        public string GetTcpIpHost()
        {
            return (sniffer != null ? sniffer.GetTcpIpHost() : "");
        }
        public void AttachSniffer()
        {
            sniffer = new TcpSniffer(parsedClientTcpIP.Address, parsedClientTcpIP.Port);
        }
        public void DetachSniffer()
        {
            sniffer = null;
            // immediately clean up the object
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public Avr(bool bAttachSniffer = false)
        {
            parsedClientTcpIP = new TcpIP();
            parsedAvrInfo = new AvrInfo();
            parsedAvrStatus = new AvrStatus();
            parsedAvrSetDataAmp = new AvrSetDataAmp();
            parsedAvrSetDataAud = new AvrSetDataAud();
            parsedAvrDisFil = new List<AvrDisFil>();
            parsedAvrCoefData = new List<AvrCoefData>();

            ClientTcpIPFileName = Environment.CurrentDirectory + "\\" + ClientTcpIPFileName;
            var FileInfoTest = new FileInfo(ClientTcpIPFileName);
            if ((FileInfoTest.Exists) && FileInfoTest.Length > 0)
            {
                String ClientTcpIPFile = File.ReadAllText(ClientTcpIPFileName);
                if (ClientTcpIPFile.Length > 0)
                {
                    parsedClientTcpIP = JsonConvert.DeserializeObject<TcpIP>(ClientTcpIPFile,
                        new JsonSerializerSettings { });
                }
            }
            else
            {
                parsedClientTcpIP.Init("192.168.50.82", 1256, 5000);
            }

            if(bAttachSniffer)
            {
                AttachSniffer();
            }

            TcpAudysseyStream = new TcpClientWithTimeout(parsedClientTcpIP.Address, parsedClientTcpIP.Port, parsedClientTcpIP.Timeout);

            if (GetAvrInfo())
            {
                string AvrInfoFile = JsonConvert.SerializeObject(parsedAvrInfo, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrInfo.json", AvrInfoFile);
            }

            if (GetAvrStatus())
            {
                string AvrStatusFile = JsonConvert.SerializeObject(parsedAvrStatus, new JsonSerializerSettings{ });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrStatus.json", AvrStatusFile);
            }

            //EnterAudysseyMode();

            //if (SetAvrSetDataAmp())
            {
                string AvrSetDataAmpFile = JsonConvert.SerializeObject(parsedAvrSetDataAmp, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAmp.json", AvrSetDataAmpFile);
            }

            //if (SetAvrSetDataAud())
            {
                string AvrSetDataAudFile = JsonConvert.SerializeObject(parsedAvrSetDataAud, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAud.json", AvrSetDataAudFile);
            }

            //if (SetAvrDisFil())
            {
                string AvrDisFilFile = JsonConvert.SerializeObject(parsedAvrDisFil, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrDisFil.json", AvrDisFilFile);
            }

            //if (InitAudysseyCoef())

            //if (SetAudysseyCoefData())

            //ExitAudysseyMode();
        }
        ~Avr()
        {
            var FileInfoTest = new FileInfo(ClientTcpIPFileName);
            if ((!FileInfoTest.Exists) || FileInfoTest.Length == 0)
            {
                string TcpFile = JsonConvert.SerializeObject(parsedClientTcpIP, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(ClientTcpIPFileName, TcpFile);
            }
        }
        private string MakeQuery(string Serialized)
        {
            var SerializedJObject = JObject.Parse(Serialized);
            foreach (var prop in SerializedJObject.Properties()) { prop.Value = "?"; }
            return SerializedJObject.ToString(Formatting.None);
        }
        public bool GetAvrInfo()
        {
            CmdString = "GET_AVRINF";
            Console.Write(CmdString);
            // build JSON and replace values with "?"
            AvrString = MakeQuery(JsonConvert.SerializeObject(parsedAvrInfo, new JsonSerializerSettings { }));
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive response
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            // parse JSON to class member variables
            parsedAvrInfo = JsonConvert.DeserializeObject<AvrInfo>(AvrString,
                new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
            return (CmdString.Equals("GET_AVRINF") && !AvrString.Equals(NACK) && CheckSumChecked);
        }
        public bool GetAvrStatus()
        {
            CmdString = "GET_AVRSTS";
            Console.Write(CmdString);
            // build JSON and replace values with "?"
            AvrString = MakeQuery(JsonConvert.SerializeObject(parsedAvrStatus, new JsonSerializerSettings { }));
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            // parse JSON to class member variables
            parsedAvrStatus = JsonConvert.DeserializeObject<AvrStatus>(AvrString,
                new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
            return (CmdString.Equals("GET_AVRSTS") && !AvrString.Equals(NACK) && CheckSumChecked);
        }
        public bool EnterAudysseyMode()
        {
            CmdString = "ENTER_AUDY";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, "");
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("ENTER_AUDY") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool ExitAudysseyMode()
        {
            CmdString = "EXIT_AUDMD";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, "");
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("EXIT_AUDMD") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetAvrSetDataAmp()
        {
            CmdString = "SET_SETDAT";
            Console.Write(CmdString);
            // build JSON
            AvrString = JsonConvert.SerializeObject(parsedAvrSetDataAmp, new JsonSerializerSettings { });
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetAvrSetDataAud()
        {
            CmdString = "SET_SETDAT";
            Console.Write(CmdString);
            // build JSON
            AvrString = JsonConvert.SerializeObject(parsedAvrSetDataAud, new JsonSerializerSettings { });
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public void SetAvrDisFil()
        {
            foreach (var AvrDisFil in parsedAvrDisFil)
            {
                CmdString = "SET_DISFIL";
                Console.Write(CmdString);
                // build JSON
                AvrString = JsonConvert.SerializeObject(AvrDisFil, new JsonSerializerSettings { });
                Console.WriteLine(AvrString);
                // transmit request
                TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
                // receive rseponse
                TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                Console.Write(CmdString);
                Console.WriteLine(AvrString);
            }
        }
        public bool InitAudysseyCoef()
        {
            CmdString = "INIT_COEFS";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, "");
            // response may take some processing time for the receiver
            var TimeElapsed = Stopwatch.StartNew();
            do
            {   // receive reseponse
                TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                Console.Write(CmdString);
                Console.WriteLine(AvrString);
            } while ((TimeElapsed.ElapsedMilliseconds < 10000) && AvrString.Equals(INPROGRESS));
            return (CmdString.Equals("INIT_COEFS") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public void SetAvrCoefData()
        {
            foreach (var AvrCoefData in parsedAvrCoefData)
            {
                if (AvrCoefData.FilData.Count() > 0)
                {
                    int[] Data = AvrCoefData.FilData.GetRange(0,128).ToArray();
                    AvrCoefData.FilData.RemoveRange(0, 128);
                    CmdString = "SET_COEFDT";
                    Console.Write(CmdString);
                    Console.WriteLine(AvrCoefData.FilData);
                    // transmit request
                    // TcpAudysseyStream.TransmitTcpAvrStream(CmdString, BitConverter.GetBytes(Data));
                    // receive rseponse
                    TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                }
                if (AvrCoefData.DispData.Count > 0)
                {
                    CmdString = "SET_COEFDT";
                    Console.Write(CmdString);
                    Console.WriteLine(AvrCoefData.DispData);
                    // transmit request
                    TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
                    // receive rseponse
                    TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                    Console.Write(CmdString);
                    Console.WriteLine(AvrString);
                }
            }
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

/* 64 FilData and 5*4100 bytes => 5*4100*8 bits per element => 32-bit data => 5125 float => 64 elements of 80 float */
/*  9 DispData and 1*2820 bytes => 313

/* FL? */
//Following process repeats 5 times, each time with different data: (4100 bytes)
//T 02 13 00 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 05 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 06 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 07 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 00 17 08 08 SET_COEFDT 00 00 04 followed by 4 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//Following process repeats 1 time, data: 2820 (bytes)
//T 02 13 00 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 01 17 05 05 SET_COEFDT 00 01 04 followed by 260 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

/* C? */
//Following process repeats 5 times, each time with different data: (4100 bytes)
//T 02 13 00 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 05 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 06 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 07 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 00 17 08 08 SET_COEFDT 00 00 04 followed by 4 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//Following process repeats 1 time, data: 2820 (bytes)
//T 02 13 00 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 01 17 05 05 SET_COEFDT 00 01 04 followed by 260 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

/* FR? */
//Following process repeats 5 times, each time with different data: (4100 bytes)
//T 02 13 00 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 05 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 06 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 07 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 00 17 08 08 SET_COEFDT 00 00 04 followed by 4 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//Following process repeats 1 time, data: 2820 (bytes)
//T 02 13 00 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 01 17 05 05 SET_COEFDT 00 01 04 followed by 260 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

/* SLA? */
//Following process repeats 5 times, each time with different data: (4100 bytes)
//T 02 13 00 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 05 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 06 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 07 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 00 17 08 08 SET_COEFDT 00 00 04 followed by 4 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//Following process repeats 1 time, data: 2820 (bytes)
//T 02 13 00 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 01 17 05 05 SET_COEFDT 00 01 04 followed by 260 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

/* SRA? */
//Following process repeats 5 times, each time with different data: (4100 bytes)
//T 02 13 00 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 05 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 06 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 07 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 00 17 08 08 SET_COEFDT 00 00 04 followed by 4 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//Following process repeats 1 time, data: 2820 (bytes)
//T 02 13 00 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 01 17 05 05 SET_COEFDT 00 01 04 followed by 260 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

/* SW1? */
//Following process repeats 5 times, each time with different data: (4100 bytes)
//T 02 13 00 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 05 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 06 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 07 08 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 00 17 08 08 SET_COEFDT 00 00 04 followed by 4 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//Following process repeats 1 time, data: 2820 (bytes)
//T 02 13 00 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 01 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 02 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 03 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 02 13 04 05 SET_COEFDT 00 02 00 followed by 512 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96
//T 01 17 05 05 SET_COEFDT 00 01 04 followed by 260 bytes payload plus 1 byte checksum
//R 00 21 00 00 SET_COEFDT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  96

//T 00 27 00 00 SET_SETDAT 00 00 14 {  "  A  u  d  y  F  i  n  F  l  g  "  :  "  F  i  n  "  } 3f
//R 00 21 00 00 SET_SETDAT 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  a6

//T 00 27 00 00 EXIT_AUDMD 00 00 00 6b
//R 00 21 00 00 EXIT_AUDMD 00 00 0e {  "  C  o  m  m  "  :  "  A  C  K  "  }  9a
