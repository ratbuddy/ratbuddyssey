using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Collections.ObjectModel;

namespace Ratbuddyssey
{
    class AvrTcp
    {
        #region Properties
        public string HostName { get; set; } = "192.168.50.82";
        public int Port { get; set; } = 1256;
        public int Timeout { get; set; } = 5000;
        #endregion
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
        //{"T" + 0x00 + 0x98 + 0x00 + 0x00 + "GET_AVRINF" + 0x00 + 0x00 + 0x85 + Encoding.Default.GetBytes(jsonData) + 0x0B };
        //private string transmitString = "{\"Ifver\":\"?\",\"DType\":\"?\",\"CoefWaitTime\":\"?\",\"ADC\":\"?\",\"SysDelay\":\"?\",\"EQType\":\"?\",\"SWLvMatch\":\"?\",\"LFC\":\"?\",\"Auro\":\"?\",\"Upgrade\":\"?\"}";
        //private byte[] transmitBytes = new byte[] {
        //    0x54, 0x00, 0x98, 0x00, 0x00, 0x47, 0x45, 0x54, 0x5f, 0x41, 0x56, 0x52, 0x49, 0x4e, 0x46, 0x00,
        //    0x00, 0x85, 0x7b, 0x22, 0x49, 0x66, 0x76, 0x65, 0x72, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22,
        //    0x44, 0x54, 0x79, 0x70, 0x65, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x43, 0x6f, 0x65, 0x66,
        //    0x57, 0x61, 0x69, 0x74, 0x54, 0x69, 0x6d, 0x65, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x41,
        //    0x44, 0x43, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x53, 0x79, 0x73, 0x44, 0x65, 0x6c, 0x61,
        //    0x79, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x45, 0x51, 0x54, 0x79, 0x70, 0x65, 0x22, 0x3a,
        //    0x22, 0x3f, 0x22, 0x2c, 0x22, 0x53, 0x57, 0x4c, 0x76, 0x4d, 0x61, 0x74, 0x63, 0x68, 0x22, 0x3a,
        //    0x22, 0x3f, 0x22, 0x2c, 0x22, 0x4c, 0x46, 0x43, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x41,
        //    0x75, 0x72, 0x6f, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x55, 0x70, 0x67, 0x72, 0x61, 0x64,
        //    0x65, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x7d, 0x42 };
        //public MemoryStream GetTransmitMemoryStream()
        //{
        //    MemoryStream memoryStream = new MemoryStream(transmitBytes);
        //    return memoryStream;
        //}
        //public string GetTransmitString()
        //{
        //    return transmitString;
        //}

        ////{"R" + 0x00 + 0xCA + 0x00 + 0x00 + "GET_AVRINF" + 0x00 + 0x00 + 0xB7 + Encoding.Default.GetBytes(jsonData) + 0xB
        //private string receiveString = "{\"Ifver\":\"00.08\",\"DType\":\"FixedA\",\"CoefWaitTime\":{\"Init\":3000,\"Final\":0},\"ADC\":2.11500,\"SysDelay\":261,\"EQType\":\"MultEQXT32\",\"SWLvlMatch\":true,\"LFC\":true,\"Auro\":false,\"Upgrade\":\"None\"}";
        //private byte[] receiveBytes = new byte[] {
        //                0x52, 0x00, 0xca, 0x00, 0x00, 0x47, 0x45, 0x54, 0x5f, 0x41, 0x56, 0x52, 0x49, 0x4e, 0x46, 0x00,
        //                0x00, 0xb7, 0x7b, 0x22, 0x49, 0x66, 0x76, 0x65, 0x72, 0x22, 0x3a, 0x22, 0x30, 0x30, 0x2e, 0x30,
        //                0x38, 0x22, 0x2c, 0x22, 0x44, 0x54, 0x79, 0x70, 0x65, 0x22, 0x3a, 0x22, 0x46, 0x69, 0x78, 0x65,
        //                0x64, 0x41, 0x22, 0x2c, 0x22, 0x43, 0x6f, 0x65, 0x66, 0x57, 0x61, 0x69, 0x74, 0x54, 0x69, 0x6d,
        //                0x65, 0x22, 0x3a, 0x7b, 0x22, 0x49, 0x6e, 0x69, 0x74, 0x22, 0x3a, 0x33, 0x30, 0x30, 0x30, 0x2c,
        //                0x22, 0x46, 0x69, 0x6e, 0x61, 0x6c, 0x22, 0x3a, 0x30, 0x7d, 0x2c, 0x22, 0x41, 0x44, 0x43, 0x22,
        //                0x3a, 0x32, 0x2e, 0x31, 0x31, 0x35, 0x30, 0x30, 0x2c, 0x22, 0x53, 0x79, 0x73, 0x44, 0x65, 0x6c,
        //                0x61, 0x79, 0x22, 0x3a, 0x32, 0x36, 0x31, 0x2c, 0x22, 0x45, 0x51, 0x54, 0x79, 0x70, 0x65, 0x22,
        //                0x3a, 0x22, 0x4d, 0x75, 0x6c, 0x74, 0x45, 0x51, 0x58, 0x54, 0x33, 0x32, 0x22, 0x2c, 0x22, 0x53,
        //                0x57, 0x4c, 0x76, 0x6c, 0x4d, 0x61, 0x74, 0x63, 0x68, 0x22, 0x3a, 0x74, 0x72, 0x75, 0x65, 0x2c,
        //                0x22, 0x4c, 0x46, 0x43, 0x22, 0x3a, 0x74, 0x72, 0x75, 0x65, 0x2c, 0x22, 0x41, 0x75, 0x72, 0x6f,
        //                0x22, 0x3a, 0x66, 0x61, 0x6c, 0x73, 0x65, 0x2c, 0x22, 0x55, 0x70, 0x67, 0x72, 0x61, 0x64, 0x65,
        //                0x22, 0x3a, 0x22, 0x4e, 0x6f, 0x6e, 0x65, 0x22, 0x7d, 0x0e};
        //public MemoryStream GetReceiveMemoryStream()
        //{
        //    MemoryStream memoryStream = new MemoryStream(receiveBytes);
        //    return memoryStream;
        //}
        //public string GetReceiveString()
        //{
        //    return receiveString;
        //}
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
        //{"T" + 0x00 + 0x79 + 0x00 + 0x00 + "GET_AVRSTS" + 0x00 + 0x00 + 0x66 + Encoding.Default.GetBytes(jsonData) + 0x5E}
        //private string transmitString = "{\"HPPlug\":\"?\",\"Mic\":\"?\",\"AmpAssign\":\"?\",\"AssignBin\":\"?\",\"ChSetup\":\"?\",\"BTTXStatus\":\"?\",\"SpPreset\":\"?\"}";
        //private byte[] transmitByte = new byte[] {
        //0x54, 0x00, 0x79, 0x00, 0x00, 0x47, 0x45, 0x54, 0x5f, 0x41, 0x56, 0x52, 0x53, 0x54, 0x53, 0x00,
        //0x00, 0x66, 0x7b, 0x22, 0x48, 0x50, 0x50, 0x6c, 0x75, 0x67, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c,
        //0x22, 0x4d, 0x69, 0x63, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x41, 0x6d, 0x70, 0x41, 0x73,
        //0x73, 0x69, 0x67, 0x6e, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x41, 0x73, 0x73, 0x69, 0x67,
        //0x6e, 0x42, 0x69, 0x6e, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x43, 0x68, 0x53, 0x65, 0x74,
        //0x75, 0x70, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x42, 0x54, 0x54, 0x58, 0x53, 0x74, 0x61,
        //0x74, 0x75, 0x73, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x2c, 0x22, 0x53, 0x70, 0x50, 0x72, 0x65, 0x73,
        //0x65, 0x74, 0x22, 0x3a, 0x22, 0x3f, 0x22, 0x7d, 0x5e};
        //public MemoryStream GetTransmitMemoryStream()
        //{
        //    MemoryStream memoryStream = new MemoryStream(transmitByte);
        //    return memoryStream;
        //}
        //public string GetTransmitString()
        //{
        //    return transmitString;
        //}

        ////{"R" + 0x01 + 0x20 + 0x00 + 0x00 + "GET_AVRSTS" + 0x00 + 0x01 + 0x0D + Encoding.Default.GetBytes(jsonData) + 0xBD}
        //private string receiveString = "{\"HPPlug\":false,\"Mic\":false,\"AmpAssign\":\"FrontB\",\"AssignBin\":\"0A02030200000000010002000200200001001000200010000100200002021002020200010203040600010001\",\"ChSetup\":[{\"FL\":\"S\"},{\"C\":\"S\"},{\"FR\":\"S\"},{\"SLA\":\"S\"},{\"SRA\":\"S\"},{\"SWMIX1\":\"E\"},{\"SWMIX2\":\"N\"}],\"BTTXStatus\":false}";
        //byte[] receiveByte = new byte[] {
        //0x52, 0x01, 0x20, 0x00, 0x00, 0x47, 0x45, 0x54, 0x5f, 0x41, 0x56, 0x52, 0x53, 0x54, 0x53, 0x00,
        //0x01, 0x0d, 0x7b, 0x22, 0x48, 0x50, 0x50, 0x6c, 0x75, 0x67, 0x22, 0x3a, 0x66, 0x61, 0x6c, 0x73,
        //0x65, 0x2c, 0x22, 0x4d, 0x69, 0x63, 0x22, 0x3a, 0x66, 0x61, 0x6c, 0x73, 0x65, 0x2c, 0x22, 0x41,
        //0x6d, 0x70, 0x41, 0x73, 0x73, 0x69, 0x67, 0x6e, 0x22, 0x3a, 0x22, 0x46, 0x72, 0x6f, 0x6e, 0x74,
        //0x42, 0x22, 0x2c, 0x22, 0x41, 0x73, 0x73, 0x69, 0x67, 0x6e, 0x42, 0x69, 0x6e, 0x22, 0x3a, 0x22,
        //0x30, 0x41, 0x30, 0x32, 0x30, 0x33, 0x30, 0x32, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30,
        //0x30, 0x31, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x32, 0x30, 0x30, 0x30,
        //0x30, 0x31, 0x30, 0x30, 0x31, 0x30, 0x30, 0x30, 0x32, 0x30, 0x30, 0x30, 0x31, 0x30, 0x30, 0x30,
        //0x30, 0x31, 0x30, 0x30, 0x32, 0x30, 0x30, 0x30, 0x30, 0x32, 0x30, 0x32, 0x31, 0x30, 0x30, 0x32,
        //0x30, 0x32, 0x30, 0x32, 0x30, 0x30, 0x30, 0x31, 0x30, 0x32, 0x30, 0x33, 0x30, 0x34, 0x30, 0x36,
        //0x30, 0x30, 0x30, 0x31, 0x30, 0x30, 0x30, 0x31, 0x22, 0x2c, 0x22, 0x43, 0x68, 0x53, 0x65, 0x74,
        //0x75, 0x70, 0x22, 0x3a, 0x5b, 0x7b, 0x22, 0x46, 0x4c, 0x22, 0x3a, 0x22, 0x53, 0x22, 0x7d, 0x2c,
        //0x7b, 0x22, 0x43, 0x22, 0x3a, 0x22, 0x53, 0x22, 0x7d, 0x2c, 0x7b, 0x22, 0x46, 0x52, 0x22, 0x3a,
        //0x22, 0x53, 0x22, 0x7d, 0x2c, 0x7b, 0x22, 0x53, 0x4c, 0x41, 0x22, 0x3a, 0x22, 0x53, 0x22, 0x7d,
        //0x2c, 0x7b, 0x22, 0x53, 0x52, 0x41, 0x22, 0x3a, 0x22, 0x53, 0x22, 0x7d, 0x2c, 0x7b, 0x22, 0x53,
        //0x57, 0x4d, 0x49, 0x58, 0x31, 0x22, 0x3a, 0x22, 0x45, 0x22, 0x7d, 0x2c, 0x7b, 0x22, 0x53, 0x57,
        //0x4d, 0x49, 0x58, 0x32, 0x22, 0x3a, 0x22, 0x4e, 0x22, 0x7d, 0x5d, 0x2c, 0x22, 0x42, 0x54, 0x54,
        //0x58, 0x53, 0x74, 0x61, 0x74, 0x75, 0x73, 0x22, 0x3a, 0x66, 0x61, 0x6c, 0x73, 0x65, 0x7d, 0xbd};
        //public MemoryStream GetReceiveMemoryStream()
        //{
        //    MemoryStream memoryStream = new MemoryStream(receiveByte);
        //    return memoryStream;
        //}
        //public string GetReceiveString()
        //{
        //    return receiveString;
        //}
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
    class Avr : INotifyPropertyChanged
    {
        private const string NACK = "{\"Comm\":\"NACK\"}";
        private const string ACK = "{\"Comm\":\"ACK\"}";

        private const string GET_AVRINF = "GET_AVRINF";
        private const string GET_AVRSTS = "GET_AVRSTS";

        public const string ENTER_AUDY = "ENTER_AUDY";
        public const string EXIT_AUDMD = "EXIT_AUDMD";

        private ObservableCollection<string> _enMultEQTypeList = new ObservableCollection<string>()
        { "MultEQ", "MultEQ XT", "MultEQ XT32" };

        private string TcpFileName = "tcpip.json";

        private TcpClientWithTimeout TcpAudysseyStream = null;

        private AvrInfo parsedAvrInfo = null;
        private AvrStatus parsedAvrStatus = null;
        private AvrTcp parsedAvrTcp = null;

        private string CmdString;
        private string AvrString;
        
        private bool CheckSumChecked = false;

        #region Properties
        public string InterfaceVersion
        {
            get
            {
                return parsedAvrInfo.Ifver;
            }
            set
            {
                parsedAvrInfo.Ifver = value;
                RaisePropertyChanged("InterfaceVersion");
            }
        }
        public decimal AdcLineup
        {
            get
            {
                return parsedAvrInfo.ADC;
            }
            set
            {
                parsedAvrInfo.ADC = value;
                RaisePropertyChanged("AdcLineup");
            }
        }
        public int SystemDelay
        {
            get
            {
                return parsedAvrInfo.SysDelay;
            }
            set
            {
                parsedAvrInfo.SysDelay = value;
                RaisePropertyChanged("SystemDelay");
            }
        }
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
        public int EnMultEQType
        {
            get
            {
                return _enMultEQTypeList.IndexOf(parsedAvrInfo.EQType);
            }
            set
            {
                parsedAvrInfo.EQType = _enMultEQTypeList.ElementAt(value);
                RaisePropertyChanged("EnMultEQType");
            }
        }
        #endregion
        public Avr()
        {
            parsedAvrInfo = new AvrInfo();
            parsedAvrStatus = new AvrStatus();
            parsedAvrTcp = new AvrTcp();

            TcpFileName = Environment.CurrentDirectory + "\\" + TcpFileName;
            var FileInfoTest = new FileInfo(TcpFileName);
            if ((FileInfoTest.Exists) && FileInfoTest.Length > 0)
            {
                String TcpFile = File.ReadAllText(TcpFileName);
                if (TcpFile.Length > 0)
                {
                    parsedAvrTcp = JsonConvert.DeserializeObject<AvrTcp>(TcpFile,
                        new JsonSerializerSettings { });
                }
            }

            TcpAudysseyStream = new TcpClientWithTimeout(parsedAvrTcp.HostName, parsedAvrTcp.Port, parsedAvrTcp.Timeout);

            if (GetAvrInfo())
            {
                string AvrInfoFile = JsonConvert.SerializeObject(parsedAvrInfo, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrInfo.json", AvrInfoFile);
            }

            if (GetAvrStatus())
            {
                string AvrStatusFile = JsonConvert.SerializeObject(parsedAvrStatus, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(Environment.CurrentDirectory + "\\AvrStatus.json", AvrStatusFile);
            }
            //EnterAudysseyMode();
            //ExitAudysseyMode();
        }
        ~Avr()
        {
            var FileInfoTest = new FileInfo(TcpFileName);
            if ((!FileInfoTest.Exists) || FileInfoTest.Length == 0)
            {
                string TcpFile = JsonConvert.SerializeObject(parsedAvrTcp, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(TcpFileName, TcpFile);
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
            CmdString = GET_AVRINF;
            Console.Write(CmdString);
            // build JSON and replace values with "?"
            AvrString = MakeQuery(JsonConvert.SerializeObject(parsedAvrInfo, new JsonSerializerSettings { }));
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive reeponse
            AvrString = TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            // parse JSON to class member variables
            parsedAvrInfo = JsonConvert.DeserializeObject<AvrInfo>(AvrString,
                new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
            return (CmdString.Equals(GET_AVRINF) && !AvrString.Equals(NACK) && CheckSumChecked);
        }
        public bool GetAvrStatus()
        {
            CmdString = GET_AVRSTS;
            Console.Write(CmdString);
            // build JSON and replace values with "?"
            AvrString = MakeQuery(JsonConvert.SerializeObject(parsedAvrStatus, new JsonSerializerSettings { }));
            Console.WriteLine(AvrString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, AvrString);
            // receive rseponse
            AvrString = TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            // parse JSON to class member variables
            parsedAvrStatus = JsonConvert.DeserializeObject<AvrStatus>(AvrString,
                new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal });
            return (CmdString.Equals(GET_AVRSTS) && !AvrString.Equals(NACK) && CheckSumChecked);
        }
        public bool EnterAudysseyMode()
        {
            CmdString = ENTER_AUDY;
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, null);
            // receive rseponse
            string AvrString = TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals(ENTER_AUDY) && AvrString.Equals(ACK) && CheckSumChecked);
        }
        public bool ExitAudysseyMode()
        {
            CmdString = EXIT_AUDMD;
            Console.WriteLine(CmdString);
            // transmit request
            TcpAudysseyStream.TransmitTcpAvrStream(CmdString, null);
            // receive rseponse
            string AvrString = TcpAudysseyStream.ReceiveTcpAvrStream(ref CmdString, out CheckSumChecked);
            Console.Write(CmdString);
            Console.WriteLine(AvrString);
            return (CmdString.Equals(EXIT_AUDMD) && AvrString.Equals(ACK) && CheckSumChecked);
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
