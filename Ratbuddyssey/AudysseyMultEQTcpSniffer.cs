using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Buffers.Binary;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Audyssey.MultEQAvr;

namespace Audyssey
{
    namespace MultEQTcp
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
        }

        public enum Protocol
        {
            TCP = 6,
            UDP = 17,
            Unknown = -1
        }

        class AudysseyMultEQTcpIp
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

        class AudysseyMultEQTcpSniffer
        {
            static readonly object _locker = new object();

            private string AudysseySnifferFileName = "AudysseySniffer.json";
            private string TcpHostFileName = "TcpHost.json";

            private TcpIP TcpHost = null;
            private TcpIP TcpClient = null;

            private Socket mainSocket = null;

            private byte[] packetData = new byte[65536];

            private byte[] ByteData = null;

            private AudysseyMultEQTcpIp audysseyMultEQTcpIp = new AudysseyMultEQTcpIp();
            private AudysseyMultEQAvr _audysseyMultEQAvr = null;

            public AudysseyMultEQTcpSniffer(AudysseyMultEQAvr audysseyMultEQAvr, string HostAddress)
            {
                _audysseyMultEQAvr = audysseyMultEQAvr;

                TcpHost = new TcpIP(HostAddress, 0, 0);
                TcpClient = _audysseyMultEQAvr.GetTcpClient();

                if (HostAddress.Equals(string.Empty))
                {
                    // try to read host ip address from file...
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
                        TcpHost = new TcpIP("127.0.0.1", 0, 0);
                        MessageBox.Show("File not found: " + TcpHostFileName, "AudysseyMultEQTcpSniffer::TcpSniffer", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(ex.Message, "AudysseyMultEQTcpSniffer::TcpSniffer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            ~AudysseyMultEQTcpSniffer()
            {
                ParseAvrFile();
            }

            public string GetTcpHostAsString()
            {
                return TcpHost.Address + "::" + TcpHost.Port.ToString();
            }

            public string GetTcpClientAsString()
            {
                return TcpClient.Address + "::" + TcpClient.Port.ToString();
            }

            private void OnReceive(IAsyncResult ar)
            {
                try
                {
                    int nReceived = mainSocket.EndReceive(ar);

                    //Analyze the bytes received...
                    ParseTcpIPData(packetData, nReceived);

                    packetData = new byte[65536];

                    //Another call to BeginReceive so that we continue to receive the incoming packets
                    mainSocket.BeginReceive(packetData, 0, packetData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "AudysseyMultEQTcpSniffer::OnReceive", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            private void ParseAvrData(byte[] packetData, ushort packetLength)
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
                        audysseyMultEQTcpIp.CheckSum = (byte)~CheckSum;

                        BinaryReader binaryReader = new BinaryReader(memoryStream);
                        audysseyMultEQTcpIp.TransmitReceive = binaryReader.ReadChar();
                        audysseyMultEQTcpIp.TotalLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16()); //BinaryPrimitives.ReverseEndianness(binaryReader.ReadUInt16());
                        audysseyMultEQTcpIp.CurrentPacket = binaryReader.ReadByte();
                        audysseyMultEQTcpIp.TotalPackets = binaryReader.ReadByte();
                        audysseyMultEQTcpIp.Command = Encoding.ASCII.GetString(binaryReader.ReadBytes(audysseyMultEQTcpIp.CommandLength));
                        audysseyMultEQTcpIp.Reserved = binaryReader.ReadByte();
                        audysseyMultEQTcpIp.DataLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16()); //BinaryPrimitives.ReverseEndianness(binaryReader.ReadUInt16());
                        if (audysseyMultEQTcpIp.DataLength == 0)
                        {
                            //This is a packet without data
                            //Dispose irrelevant data
                            audysseyMultEQTcpIp.ByteData = null;
                            audysseyMultEQTcpIp.CharData = null;
                            audysseyMultEQTcpIp.Int32Data = null;
                        }
                        else
                        {
                            //Read the Data
                            int ByteToRead = audysseyMultEQTcpIp.DataLength;
                            audysseyMultEQTcpIp.ByteData = binaryReader.ReadBytes(ByteToRead);
                            //Data can be single packet transfer of multiple packet transfer
                            if (audysseyMultEQTcpIp.TotalPackets == 0)
                            {
                                //This is a single packet transfer
                                audysseyMultEQTcpIp.CharData = System.Text.Encoding.UTF8.GetString(audysseyMultEQTcpIp.ByteData).ToString();
                                //Keep the growing array for multi-packet transfers as we will have ACK packets in between
                                //Dispose irrelevant data
                                audysseyMultEQTcpIp.Int32Data = null;
                            }
                            else
                            {
                                //This is a multiple packet transfer
                                audysseyMultEQTcpIp.CharData = null;
                                //First packet: create array and fill with data
                                if (audysseyMultEQTcpIp.CurrentPacket == 0)
                                {
                                    ByteData = audysseyMultEQTcpIp.ByteData;
                                }
                                //Other packets: resize array and append data
                                else
                                {
                                    //Store length of current array
                                    Int32 PreviousBytaDataLength = ByteData.Length;
                                    //Resize the current array to add the new array
                                    Array.Resize(ref ByteData, ByteData.Length + audysseyMultEQTcpIp.ByteData.Length);
                                    //Append the new array to the current array
                                    Array.Copy(audysseyMultEQTcpIp.ByteData, 0, ByteData, PreviousBytaDataLength, audysseyMultEQTcpIp.ByteData.Length);
                                }
                                //Last packet: store to class
                                if (audysseyMultEQTcpIp.CurrentPacket == audysseyMultEQTcpIp.TotalPackets)
                                {
                                    //TotalPackets Data as int?
                                    //Reverse endianness of byte data and store
                                    audysseyMultEQTcpIp.Int32Data = ByteToInt32Array(ByteData);
                                }
                            }
                        }
                        audysseyMultEQTcpIp.CheckSum = binaryReader.ReadByte();
                        if (CheckSum == audysseyMultEQTcpIp.CheckSum)
                        {
                            string AvrString = JsonConvert.SerializeObject(audysseyMultEQTcpIp, new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                            // we are running asynchronous
                            lock (_locker)
                            {
                                //Dump to file for learning
                                File.AppendAllText(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName, AvrString + "\n");
                            }
                            //Parse to parent to display? modify? re-transmit?
                            ParseAvrObject(audysseyMultEQTcpIp.TransmitReceive, audysseyMultEQTcpIp.Command, audysseyMultEQTcpIp.CharData, audysseyMultEQTcpIp.Int32Data);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "AudysseyMultEQTcpSniffer::ParseAvrData", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void Invoke(Action action)
            {
                throw new NotImplementedException();
            }

            private void ParseAvrFile()
            {
                if (File.Exists(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName))
                {
                    string[] AvrStrings = File.ReadAllLines(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName);
                    foreach (string _AvrString in AvrStrings)
                    {
                        audysseyMultEQTcpIp = JsonConvert.DeserializeObject<AudysseyMultEQTcpIp>(_AvrString, new JsonSerializerSettings { });
                        ParseAvrObject(audysseyMultEQTcpIp.TransmitReceive, audysseyMultEQTcpIp.Command, audysseyMultEQTcpIp.CharData, audysseyMultEQTcpIp.Int32Data);
                    }
                    string AvrString = JsonConvert.SerializeObject(_audysseyMultEQAvr, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    File.WriteAllText(Environment.CurrentDirectory + "\\" + System.IO.Path.ChangeExtension(AudysseySnifferFileName, ".aud"), AvrString);
                }
            }

            private void ParseAvrObject(char TransmitReceive, string CmdString, string AvrString, Int32[] AvrData)
            {
                switch (CmdString)
                {
                    case "GET_AVRINF":
                        if (TransmitReceive == 'R')
                            _audysseyMultEQAvr.Info = JsonConvert.DeserializeObject<AvrInfo>(AvrString, new JsonSerializerSettings { });
                        break;
                    case "GET_AVRSTS":
                        if (TransmitReceive == 'R')
                            _audysseyMultEQAvr.Status = JsonConvert.DeserializeObject<AvrStatus>(AvrString, new JsonSerializerSettings { });
                        break;
                    case "ENTER_AUDY":
                        break;
                    case "EXIT_AUDMD":
                        break;
                    case "SET_SETDAT":
                        if (TransmitReceive == 'T')
                            _audysseyMultEQAvr.Data = JsonConvert.DeserializeObject<AvrData>(AvrString, new JsonSerializerSettings { });
                        break;
                    case "SET_DISFIL":
                        if (TransmitReceive == 'T')
                            _audysseyMultEQAvr.DisFil.Add(JsonConvert.DeserializeObject<AvrDisFil>(AvrString, new JsonSerializerSettings { }));
                        break;
                    case "INIT_COEFS":
                        break;
                    case "SET_COEFDT":
                        if (TransmitReceive == 'T')
                            if (AvrData != null)
                                _audysseyMultEQAvr.CoefData.Add(AvrData);
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
                byte[] Byte = new byte[4 * Int32s.Length];
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
}