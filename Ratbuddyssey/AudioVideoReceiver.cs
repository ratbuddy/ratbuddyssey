using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using System.ComponentModel;

namespace Ratbuddyssey
{
    class CoefWaitTime : INotifyPropertyChanged
    {   // TODO: add local var and RaisePropertyChanged
        #region Properties
        public decimal Init { get; set; }
        public decimal Final { get; set; }
        #endregion
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
    class AVRINF : INotifyPropertyChanged
    {   // TODO: add local var and RaisePropertyChanged
        private string _Ifver;
        #region Properties
        public string Ifver
        { 
            get
            {
                return _Ifver;
            }
            set 
            {
                _Ifver = value;
                RaisePropertyChanged("Ifver");
            }
        }
        public string DType
        { get; set; }
        public CoefWaitTime CoefWaitTime
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
        #region INotifyPropertyChanged members
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        #region methods
        protected void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
    class AVRSTS : INotifyPropertyChanged
    {   // TODO: add local var and RaisePropertyChanged
        #region Properties
        public bool HPPlug
        { get; set; }
        public bool Mic
        { get; set; }
        public string AmpAssign
        { get; set; }
        public string AssignBin
        { get; set; }
        public List<Dictionary<string, string>> ChSetup
        { get; set; }
        public bool BTTXStatus
        { get; set; }
        public bool SpPreset
        { get; set; }
        #endregion
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
    interface IAmp
    {
        #region Properties
        string AmpAssign { get; set; }
        string AssignBin { get; set; }
        bool AudyDynEq { get; set; }
        int AudyEqRef { get; set; }
        string AudyFinFlg { get; set; }
        List<Dictionary<string, int>> ChLevel { get; set; }
        List<Dictionary<string, object>> Crossover { get; set; }
        List<Dictionary<string, int>> Distance { get; set; }
        List<Dictionary<string, string>> SpConfig { get; set; }
        #endregion
    }
    interface IAudy
    {
        #region Properties
        bool AudyDynVol { get; set; }
        string AudyDynSet { get; set; }
        bool AudyMultEq { get; set; }
        string AudyEqSet { get; set; }
        bool AudyLfc { get; set; }
        int AudyLfcLev { get; set; }
        #endregion
    }
    class SETDAT : IAmp, IAudy, INotifyPropertyChanged
    {   // TODO: add RaisePropertyChanged
        // IAmp
        static string _AmpAssign;
        static string _AssignBin;
        static List<Dictionary<string, string>> _SpConfig;
        static List<Dictionary<string, int>> _Distance;
        static List<Dictionary<string, int>> _ChLevel;
        static List<Dictionary<string, object>> _Crossover;
        static string _AudyFinFlg;
        static bool _AudyDynEq;
        static int _AudyEqRef;
        // IAudy
        static bool _AudyDynVol;
        static string _AudyDynSet;
        static string _AudyEqSet;
        static bool _AudyLfc;
        static int _AudyLfcLev;
        static bool _AudyMultEq;
        #region Properties
        // IAmp
        public string AmpAssign
        {
            get
            {
                return _AmpAssign;
            }
            set
            {
                if (value != null) _AmpAssign = value;
            }
        }
        public string AssignBin
        {
            get
            {
                return _AssignBin;
            }
            set
            {
                _AssignBin = value;
            }
        }
        public List<Dictionary<string, string>> SpConfig
        {
            get
            {
                return _SpConfig;
            }
            set
            {
                _SpConfig = value;
            }
        }
        public List<Dictionary<string, int>> Distance
        {
            get
            {
                return _Distance;
            }
            set
            {
                _Distance = value;
            }
        }
        public List<Dictionary<string, int>> ChLevel
        {
            get
            {
                return _ChLevel;
            }
            set
            {
                _ChLevel = value;
            }
        }
        public List<Dictionary<string, object>> Crossover
        {
            get
            {
                return _Crossover;
            }
            set
            {
                _Crossover = value;
            }
        }
        public string AudyFinFlg
        {
            get
            {
                return _AudyFinFlg;
            }
            set
            {
                _AudyFinFlg = value;
            }
        }
        public bool AudyDynEq
        {
            get
            {
                return _AudyDynEq;
            }
            set
            {
                _AudyDynEq = value;
            }
        }
        public int AudyEqRef
        {
            get
            {
                return _AudyEqRef;
            }
            set
            {
                _AudyEqRef = value;
            }
        }
        // IAudy
        public bool AudyDynVol
        {
            get
            {
                return _AudyDynVol;
            }
            set
            {
                _AudyDynVol = value;
            }
        }
        public string AudyDynSet
        {
            get
            {
                return _AudyDynSet;
            }
            set
            {
                _AudyDynSet = value;
            }
        }
        public string AudyEqSet
        {
            get
            {
                return _AudyEqSet;
            }
            set
            {
                _AudyEqSet = value;
            }
        }
        public bool AudyLfc
        {
            get
            {
                return _AudyLfc;
            }
            set
            {
                _AudyLfc = value;
            }
        }
        public int AudyLfcLev
        {
            get
            {
                return _AudyLfcLev;
            }
            set
            {
                _AudyLfcLev = value;
            }
        }
        public bool AudyMultEq
        {
            get
            {
                return _AudyMultEq;
            }
            set
            {
                _AudyMultEq = value;
            }
        }
        #endregion
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
    class DISFIL : INotifyPropertyChanged
    {   // TODO: add local var and RaisePropertyChanged
        #region Properties
        public string EqType
        { get; set; }
        public string ChData
        { get; set; }
        public sbyte[] FilData
        { get; set; }
        public sbyte[] DispData
        { get; set; }
        #endregion
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
    class AudioVideoReceiver : INotifyPropertyChanged
    {
        private AVRINF _AVRINF = null;
        private AVRSTS _AVRSTS = null;
        private SETDAT _SETDAT = null;
        private List<DISFIL> _DISFIL = null;
        private List<Int32[]> _COEFDT = null;
        #region Properties
        public AVRINF AVRINF
        {
            get
            {
                return _AVRINF;
            }
            set
            {
                _AVRINF = value;
                RaisePropertyChanged("AVRINF");
            }
        }
        public AVRSTS AVRSTS
        {
            get
            {
                return _AVRSTS;
            }
            set
            {
                _AVRSTS = value;
                RaisePropertyChanged("AVRSTS");
            }
        }
        public SETDAT SETDAT
        {
            get
            {
                return _SETDAT;
            }
            set
            {
                _SETDAT = value;
                RaisePropertyChanged("SETDAT");
            }
        }
        public List<DISFIL> DISFIL
        {
            get
            {
                return _DISFIL;
            }
            set
            {
                _DISFIL = value;
                RaisePropertyChanged("DISFIL");
            }
        }
        public List<Int32[]> COEFDT
        {
            get
            {
                return _COEFDT;
            }
            set
            {
                _COEFDT = value;
                RaisePropertyChanged("COEFDT");
            }
        }
        #endregion

        private const string NACK = "{\"Comm\":\"NACK\"}";
        private const string ACK = "{\"Comm\":\"ACK\"}";
        private const string INPROGRESS = "{\"Comm\":\"INPROGRESS\"}";
        private const string AUDYFINFLG = "{\"AudyFinFlg\":\"Fin\"}";
        private string TcpClientFileName = "TcpClient.json";

        [NonSerialized]
        public TcpIP TcpClient = null;

        private TcpClientWithTimeout TcpAudysseyStream = null;

        private TcpSniffer sniffer;

        private bool CheckSumChecked = false;
        public string GetTcpClient()
        {
            return TcpClient.Address + "::" + TcpClient.Port.ToString();
        }
        public string GetTcpHost()
        {
            return (sniffer != null ? sniffer.GetTcpHost() : "");
        }
        public void AttachSniffer()
        {
            if (sniffer == null) sniffer = new TcpSniffer(this);
        }
        public void DetachSniffer()
        {
            sniffer = null;
            // immediately clean up the object
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        ~AudioVideoReceiver()
        {
            var FileInfoTest = new FileInfo(TcpClientFileName);
            if ((!FileInfoTest.Exists) || FileInfoTest.Length == 0)
            {
                string TcpFile = JsonConvert.SerializeObject(TcpClient, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(TcpClientFileName, TcpFile);
            }
        }
        public AudioVideoReceiver(AvrAudysseyAdapter AvrAudysseyAdapter, bool bAttachSniffer)
        {
            this.PropertyChanged += AvrAudysseyAdapter._PropertyChanged; // bind parent to nofify property changed (it's a bitch)

            _AVRINF = new AVRINF();
            _AVRSTS = new AVRSTS();
            _SETDAT = new SETDAT();
            _DISFIL = new List<DISFIL>();
            _COEFDT = new List<Int32[]>();

            TcpClient = new TcpIP("192.168.50.82", 1256, 5000);

            TcpClientFileName = Environment.CurrentDirectory + "\\" + TcpClientFileName;
            var FileInfoTest = new FileInfo(TcpClientFileName);
            if ((FileInfoTest.Exists) && FileInfoTest.Length > 0)
            {
                String ClientTcpIPFile = File.ReadAllText(TcpClientFileName);
                if (ClientTcpIPFile.Length > 0)
                {
                    TcpClient = JsonConvert.DeserializeObject<TcpIP>(ClientTcpIPFile,
                        new JsonSerializerSettings { });
                }
            }

            TcpAudysseyStream = new TcpClientWithTimeout(TcpClient.Address, TcpClient.Port, TcpClient.Timeout);

            if (bAttachSniffer)
            {
                AttachSniffer();
            }
        }
        public AudioVideoReceiver(bool bAttachSniffer)
        {
            TcpClient = new TcpIP("192.168.50.82", 1256, 5000);

            TcpClientFileName = Environment.CurrentDirectory + "\\" + TcpClientFileName;
            var FileInfoTest = new FileInfo(TcpClientFileName);
            if ((FileInfoTest.Exists) && FileInfoTest.Length > 0)
            {
                String ClientTcpIPFile = File.ReadAllText(TcpClientFileName);
                if (ClientTcpIPFile.Length > 0)
                {
                    TcpClient = JsonConvert.DeserializeObject<TcpIP>(ClientTcpIPFile,
                        new JsonSerializerSettings { });
                }
            }

            TcpAudysseyStream = new TcpClientWithTimeout(TcpClient.Address, TcpClient.Port, TcpClient.Timeout);

            if (bAttachSniffer)
            {
                AttachSniffer();
            }

        }
        public void AudysseyToAvr()
        {
            if (GetAvrInfo())
            {
                string AvrInfoFile = JsonConvert.SerializeObject(AVRINF, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrInfo.json", AvrInfoFile);
            }

            if (GetAvrStatus())
            {
                string AvrStatusFile = JsonConvert.SerializeObject(AVRSTS, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrStatus.json", AvrStatusFile);
            }

            //EnterAudysseyMode();

            //if (SetAvrSetAmp())
            {
                string AvrSetDataAmpFile = JsonConvert.SerializeObject(SETDAT, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAmp.json", AvrSetDataAmpFile);
            }

            //if (SetAvrSetAudy())
            {
                string AvrSetDataAudFile = JsonConvert.SerializeObject(SETDAT, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrSetDataAud.json", AvrSetDataAudFile);
            }

            //if (SetAvrDisFil())
            {
                string AvrDisFilFile = JsonConvert.SerializeObject(DISFIL, new JsonSerializerSettings { });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrDisFil.json", AvrDisFilFile);
            }

            //if (InitAudysseyCoef())

            //if (SetAudysseyCoefData())

            //if (SetAudysseyFinishedFlag())

            //ExitAudysseyMode();
        }
        private string MakeQuery(string Serialized)
        {
            var SerializedJObject = JObject.Parse(Serialized);
            foreach (var prop in SerializedJObject.Properties()) { prop.Value = "?"; }
            return SerializedJObject.ToString(Formatting.None);
        }
        public bool GetAvrInfo()
        {
            string CmdString = "GET_AVRINF";
            Console.Write(CmdString);
            // build JSON and replace values with "?"
            string AvrString = MakeQuery(JsonConvert.SerializeObject(AVRINF, new JsonSerializerSettings { }));
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive response
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            // parse JSON to class member variables
            AVRINF = JsonConvert.DeserializeObject<AVRINF>(AvrString,
                new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
            return (CmdString.Equals("GET_AVRINF") && !AvrString.Equals(NACK) && CheckSumChecked);
        }
        public bool GetAvrStatus()
        {
            string CmdString = "GET_AVRSTS";
            Console.Write(CmdString);
            // build JSON and replace values with "?"
            string AvrString = MakeQuery(JsonConvert.SerializeObject(AVRSTS, new JsonSerializerSettings { }));
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            // parse JSON to class member variables
            AVRSTS = JsonConvert.DeserializeObject<AVRSTS>(AvrString,
                new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
            return (CmdString.Equals("GET_AVRSTS") && !AvrString.Equals(NACK) && CheckSumChecked);
        }
        public bool EnterAudysseyMode()
        {
            string CmdString = "ENTER_AUDY";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, "");
            // receive rseponse
            string AvrString;
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("ENTER_AUDY") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool ExitAudysseyMode()
        {
            string CmdString = "EXIT_AUDMD";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, "");
            // receive rseponse
            string AvrString;
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("EXIT_AUDMD") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetAudysseyFinishedFlag()
        {
            string CmdString = "SET_SETDAT";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AUDYFINFLG);
            // receive rseponse
            string AvrString;
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetAvrSetAmp()
        {
            string CmdString = "SET_SETDAT";
            Console.Write(CmdString);
            // build JSON for class Dat on interface Iamp
            string AvrString = JsonConvert.SerializeObject(SETDAT, new JsonSerializerSettings {
                ContractResolver = new InterfaceContractResolver(typeof(IAmp))
            });
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetAvrSetAudy()
        {
            string CmdString = "SET_SETDAT";
            Console.Write(CmdString);
            // build JSON for class Dat on interface IAudy
            string AvrString = JsonConvert.SerializeObject(SETDAT, new JsonSerializerSettings
            {
                ContractResolver = new InterfaceContractResolver(typeof(IAudy))
            });
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetDisFil()
        {
            // there are multiple speaker?
            foreach (var AvrDisFil in DISFIL)
            {
                string CmdString = "SET_DISFIL";
                Console.Write(CmdString);
                // build JSON
                string AvrString = JsonConvert.SerializeObject(AvrDisFil, new JsonSerializerSettings { });
                Console.WriteLine(AvrString);
                // transmit request
                TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
                // receive rseponse
                TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                Console.Write(CmdString);
                Console.WriteLine(AvrString);
                // check every transmission
                if (!(CmdString.Equals("SET_SETDAT") && AvrString.Equals(ACK) && CheckSumChecked)) return false;
            }
            return true;
        }
        public bool InitAvrCoefs()
        {
            string CmdString = "INIT_COEFS";
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, "");
            // response may take some processing time for the receiver
            var TimeElapsed = Stopwatch.StartNew();
            string AvrString;
            do
            {   // receive reseponse
                TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                Console.Write(CmdString);
                Console.WriteLine(AvrString);
            } while ((TimeElapsed.ElapsedMilliseconds < 10000) && AvrString.Equals(INPROGRESS));
            return (CmdString.Equals("INIT_COEFS") && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool SetAvrCoefDt()
        {
            bool Success = true;
            // data for each speaker... this is a very dumb binary data pump
            foreach (Int32[] Coef in COEFDT)
            {
                string CmdString = "SET_COEFDT";
                Console.Write(CmdString);
                Console.WriteLine(Coef.ToString());
                // transmit request
                TcpAudysseyStream.TransmitTcpAvrStream(CmdString, Coef);
                string AvrString;
                // receive rseponse
                TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out AvrString, out CheckSumChecked);
                Console.Write(CmdString);
                Console.WriteLine(AvrString);
                // success if all succeed
                Success &= (CmdString.Equals("SET_COEFDT") && AvrString.Equals(ACK) && CheckSumChecked);
            }
            return Success;
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
