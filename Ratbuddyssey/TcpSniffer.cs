using Newtonsoft.Json;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Ratbuddyssey
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    struct FloatInt32
    {
        [FieldOffset(0)] private float Float;
        [FieldOffset(0)] private int Int32;

        private static FloatInt32 inst = new FloatInt32();
        public static int FloatToInt32(float value)
        {
            inst.Float = value;
            return inst.Int32;
        }
        public static float Int32ToFloat(int value)
        {
            inst.Int32 = value;
            return inst.Float;
        }
    }
    public enum ReceiveAll
    {
        RCVALL_OFF = 0,
        RCVALL_ON = 1,
        RCVALL_SOCKETLEVELONLY = 2,
        RCVALL_IPLEVEL = 3,
    };
    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    };
    class AudysseyTcpIp
    {
        #region Properties
        public char TransmitReceive { get; set; }
        public byte CurrentPacket { get; set; }
        public byte TotalPackets { get; set; }
        public UInt16 TotalLength { get; set; }
        public UInt16 CommandLength { get; } = 10;
        public string Command { get; set; }
        public byte Reserved { get; set; }
        public UInt16 DataLength { get; set; }
        [JsonIgnore]
        public byte[] ByteData { get; set; }
        public string CharData { get; set; }
        public Int32[] Int32Data { get; set; }
        public byte CheckSum { get; set; } = 0;
        #endregion
    }
    class TcpSniffer
    {
        private AudioVideoReceiver _Avr;

        private TcpIP TcpHost = null;
        private TcpIP TcpClient = null;
        private string TcpHostFileName = "TcpHost.json";

        private string AudysseySnifferFileName = "AudysseySniffer.json";

        private Socket mainSocket = null;

        private byte[] packetData = new byte[32768];

        private AudysseyTcpIp AudysseyTcpIP = new AudysseyTcpIp();

        private byte[] ByteData = null;
        public string GetTcpHost()
        {
            return TcpHost.Address + "::" + TcpHost.Port.ToString();
        }
        public string GetTcpClient()
        {
            return TcpClient.Address + "::" + TcpClient.Port.ToString();
        }
        public TcpSniffer(AudioVideoReceiver Avr)
        {
            _Avr = Avr;

            TcpHost = new TcpIP("192.168.50.66", 0, 0);
            TcpClient = new TcpIP(_Avr.TcpClient.Address, _Avr.TcpClient.Port, _Avr.TcpClient.Timeout);

            if (IsUserAdministrator())
            {
                // read host ip address from file
                TcpHostFileName = Environment.CurrentDirectory + "\\" + TcpHostFileName;
                var FileInfoTest = new FileInfo(TcpHostFileName);
                if ((FileInfoTest.Exists) && FileInfoTest.Length > 0)
                {
                    String HostTcpIPFile = File.ReadAllText(TcpHostFileName);
                    if (HostTcpIPFile.Length > 0)
                    {
                        TcpHost = JsonConvert.DeserializeObject<TcpIP>(HostTcpIPFile, new JsonSerializerSettings { });
                    }
                }
                else
                {
                    IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));
                    if (HosyEntry.AddressList.Length > 0)
                    {
                        IPAddress ipAddress = new IPAddress(0);
                        foreach (IPAddress ip in HosyEntry.AddressList)
                        {
                            ipAddress = ip; // fortunately for me the last one is the right one :)
                            TcpHost.Address = ip.ToString();
                        }
                        // write last host ip address to file
                        string TcpIPFile = JsonConvert.SerializeObject(TcpHost, new JsonSerializerSettings { });
                        File.WriteAllText(TcpHostFileName, TcpIPFile);
                    }
                }

                //For sniffing the socket to capture the packets has to be a raw socket, with the
                //address family being of type internetwork, and protocol being IP
                try
                {
                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

                    mainSocket.ReceiveBufferSize = 32768;
                    
                    //Bind the socket to the selected IP address IPAddress.Parse("192.168.50.66")
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(TcpHost.Address), TcpHost.Port));

                    //Set the socket  options
                    mainSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

                    mainSocket.IOControl(IOControlCode.ReceiveAll, new byte[] { (byte)ReceiveAll.RCVALL_ON, 0, 0, 0 }, new byte[] { (byte)ReceiveAll.RCVALL_ON, 0, 0, 0 });
                    
                    //Start receiving the packets asynchronously
                    mainSocket.BeginReceive(packetData, 0, packetData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "TcpSniffer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        ~TcpSniffer()
        {
            ParseAvrFile();
            //Trace.Close();
        }
        public bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int nReceived = mainSocket.EndReceive(ar);

                //Analyze the bytes received...
                ParseTcpIPData(packetData, nReceived);

                packetData = new byte[32768];

                //Another call to BeginReceive so that we continue to receive the incoming packets
                mainSocket.BeginReceive(packetData, 0, packetData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), null);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OnReceive", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ParseTcpIPData(byte[] byteData, int nReceived)
        {
            //Since all protocol packets are encapsulated in the IP datagram
            //so we start by parsing the IP header and see what protocol data
            //is being carried by it and filter source and destination address.
            IPHeader ipHeader = new IPHeader(byteData, nReceived);
            if (ipHeader.SourceAddress.ToString().Equals(TcpClient.Address) || 
                ipHeader.DestinationAddress.ToString().Equals(TcpClient.Address))
            {
                //Now according to the protocol being carried by the IP datagram we parse 
                //the data field of the datagram if it carries TCP protocol
                if ((ipHeader.ProtocolType == Protocol.TCP) && (ipHeader.MessageLength > 0))
                {
                    TCPHeader tcpHeader = new TCPHeader(ipHeader.Data, ipHeader.MessageLength);
                    //Now filter only on our Denon (or Marantz) receiver
                    if (tcpHeader.SourcePort == TcpClient.Port.ToString() ||
                        tcpHeader.DestinationPort == TcpClient.Port.ToString())
                    {
                        if (tcpHeader.MessageLength > 1)
                        {
                            ParseAvrData(tcpHeader.Data, tcpHeader.MessageLength);
                        }
                    }
                }
            }
        }
        public void ParseAvrData(byte[] packetData, ushort packetLength)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream(packetData, 0, packetLength);

                // If we want to filter only packets which we can decode the minimum
                // length of a packet with no data is header + checksum = 19 bytes.
                if (memoryStream.Length >= 19)
                {
                    byte[] array = packetData;
                    Array.Resize<byte>(ref array, array.Length - 1);
                    byte CheckSum = CalculateChecksum(array);
                    AudysseyTcpIP.CheckSum = (byte)~CheckSum;

                    BinaryReader binaryReader = new BinaryReader(memoryStream);
                    AudysseyTcpIP.TransmitReceive = binaryReader.ReadChar();
                    AudysseyTcpIP.TotalLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16()); //BinaryPrimitives.ReverseEndianness(binaryReader.ReadUInt16());
                    AudysseyTcpIP.CurrentPacket = binaryReader.ReadByte();
                    AudysseyTcpIP.TotalPackets = binaryReader.ReadByte();
                    AudysseyTcpIP.Command = Encoding.ASCII.GetString(binaryReader.ReadBytes(AudysseyTcpIP.CommandLength));
                    AudysseyTcpIP.Reserved = binaryReader.ReadByte();
                    AudysseyTcpIP.DataLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16()); //BinaryPrimitives.ReverseEndianness(binaryReader.ReadUInt16());
                    if (AudysseyTcpIP.DataLength == 0)
                    {
                        //This is a packet without data
                        //Dispose irrelevant data
                        AudysseyTcpIP.ByteData = null;
                        AudysseyTcpIP.CharData = null;
                        AudysseyTcpIP.Int32Data = null;
                    }
                    else
                    {
                        //Read the Data
                        int ByteToRead = AudysseyTcpIP.DataLength;
                        AudysseyTcpIP.ByteData = binaryReader.ReadBytes(ByteToRead);
                        //Data can be single packet transfer of multiple packet transfer
                        if (AudysseyTcpIP.TotalPackets == 0)
                        {
                            //This is a single packet transfer
                            AudysseyTcpIP.CharData = System.Text.Encoding.UTF8.GetString(AudysseyTcpIP.ByteData).ToString();
                            //Keep the growing array for multi-packet transfers as we will have ACK packets in between
                            //Dispose irrelevant data
                            AudysseyTcpIP.Int32Data = null;
                        }
                        else
                        {
                            //This is a multiple packet transfer
                            AudysseyTcpIP.CharData = null;
                            //First packet: create array and fill with data
                            if (AudysseyTcpIP.CurrentPacket == 0)
                            {
                                ByteData = AudysseyTcpIP.ByteData;
                            }
                            //Other packets: resize array and append data
                            else
                            {
                                //Store length of current array
                                Int32 PreviousBytaDataLength = ByteData.Length;
                                //Resize the current array to add the new array
                                Array.Resize(ref ByteData, ByteData.Length + AudysseyTcpIP.ByteData.Length);
                                //Append the new array to the current array
                                Array.Copy(AudysseyTcpIP.ByteData, 0, ByteData, PreviousBytaDataLength, AudysseyTcpIP.ByteData.Length);
                            }
                            //Last packet: store to class
                            if (AudysseyTcpIP.CurrentPacket == AudysseyTcpIP.TotalPackets)
                            {
                                //TotalPackets Data as int?
                                //Reverse endianness of byte data and store
                                AudysseyTcpIP.Int32Data = ByteToInt32Array(ByteData); 
                            }
                        }
                    }
                    AudysseyTcpIP.CheckSum = binaryReader.ReadByte();
                    if (CheckSum == AudysseyTcpIP.CheckSum)
                    {
                        string AvrString = JsonConvert.SerializeObject(AudysseyTcpIP, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                        //Dump to file for leaning
                        //Trace.Flush();
                        //Trace.WriteLine(AvrString);
                        File.AppendAllText(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName, AvrString + "\n");
                        //Console.WriteLine(AvrString);
                        //Parse to parent to display? modify? re-transmit?
                        ParseAvrObject(AudysseyTcpIP.TransmitReceive, AudysseyTcpIP.Command, AudysseyTcpIP.CharData, AudysseyTcpIP.Int32Data);
                    }
                }
             }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ParseAVR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void ParseAvrFile()
        {
            if (File.Exists(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName))
            {
                string[] AvrStrings = File.ReadAllLines(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName);
                foreach (string _AvrString in AvrStrings)
                {
                    AudysseyTcpIP = JsonConvert.DeserializeObject<AudysseyTcpIp>(_AvrString, new JsonSerializerSettings { });
                    ParseAvrObject(AudysseyTcpIP.TransmitReceive, AudysseyTcpIP.Command, AudysseyTcpIP.CharData, AudysseyTcpIP.Int32Data);
                }
                string AvrString = JsonConvert.SerializeObject(_Avr, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                File.WriteAllText(Environment.CurrentDirectory + "\\" + System.IO.Path.ChangeExtension(AudysseySnifferFileName, ".aud"), AvrString);
            }
        }
        public void ParseAvrObject(char TransmitReceive, string CmdString, string AvrString, Int32[] AvrData)
        {
            switch (CmdString)
            {
                case "GET_AVRINF":
                    if (TransmitReceive == 'R')
                        _Avr.AVRINF = JsonConvert.DeserializeObject<AVRINF>(AvrString, new JsonSerializerSettings { });
                    break;
                case "GET_AVRSTS":
                    if (TransmitReceive == 'R')
                        _Avr.AVRSTS = JsonConvert.DeserializeObject<AVRSTS>(AvrString, new JsonSerializerSettings { });
                    break;
                case "ENTER_AUDY":
                    break;
                case "EXIT_AUDMD":
                    break;
                case "SET_SETDAT":
                    if (TransmitReceive == 'T')
                        _Avr.SETDAT = JsonConvert.DeserializeObject<SETDAT>(AvrString, new JsonSerializerSettings { });
                    break;
                case "SET_DISFIL":
                    if (TransmitReceive == 'T')
                        _Avr.DISFIL.Add(JsonConvert.DeserializeObject<DISFIL>(AvrString, new JsonSerializerSettings { }));
                    break;
                case "INIT_COEFS":
                    break;
                case "SET_COEFDT":
                    if (TransmitReceive == 'T')
                        if (AvrData != null)
                            _Avr.COEFDT.Add(AvrData);
                    break;
            }
        }
        private Int32[] ByteToInt32Array(byte[] Byte)
        {
            Int32[] Int32s = null;
            if (Byte.Length % 4 == 0)
            {
                Int32s = new Int32[Byte.Length / 4];
                for (int i = 0; i < Byte.Length / 4; i++)
                {
                    Int32s[i] = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(Byte, i * 4));
                }
            }
            return Int32s;
        }
        private byte[] Int32ToByteArray(Int32[] Int32s)
        {
            byte[] Byte = new byte[4*Int32s.Length];
            for (int i = 0; i < Int32s.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(Int32s[i])), 0, Byte, i * 4, 4);
            }
            return Byte;
        }
        private byte CalculateChecksum(byte[] dataToCalculate)
        {
            return dataToCalculate.Aggregate((r, n) => r += n);
        }
    }
}