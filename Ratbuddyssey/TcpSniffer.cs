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
using System.Collections.Generic;

namespace Ratbuddyssey
{
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
    class AudysseyTcpIpClass
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
        private TcpIP parsedHostTcpIP = null;
        private TcpIP parsedClientTcpIP = null;
        private string HostTcpIpFileName = "HostTcpIP.json";
        private string AudysseySnifferFileName = "AudysseySniffer.json";
        private Socket mainSocket = null;
        private byte[] packetData = new byte[32768];
        private AudysseyTcpIpClass AudysseyTcpIP = new AudysseyTcpIpClass();
        private Int32[] Int32Data = null;
        public string GetTcpIpHost()
        {
            return parsedHostTcpIP.Address + "::" + parsedHostTcpIP.Port.ToString();
        }
        public string GetTcpIpClient()
        {
            return parsedClientTcpIP.Address + "::" + parsedClientTcpIP.Port.ToString();
        }
        ~TcpSniffer()
        {
            var FileInfoTest = new FileInfo(HostTcpIpFileName);
            if ((!FileInfoTest.Exists) || FileInfoTest.Length == 0)
            {
                string TcpIPFile = JsonConvert.SerializeObject(parsedHostTcpIP, new JsonSerializerSettings { });
                File.WriteAllText(HostTcpIpFileName, TcpIPFile);
            }
        }
        public TcpSniffer(string clientName, int clientPort)
        {
            parsedHostTcpIP = new TcpIP();
            parsedClientTcpIP = new TcpIP();
            parsedClientTcpIP.Init(clientName, clientPort, 0);

            if (IsUserAdministrator())
            {
                // read host ip address from file
                HostTcpIpFileName = Environment.CurrentDirectory + "\\" + HostTcpIpFileName;
                var FileInfoTest = new FileInfo(HostTcpIpFileName);
                if ((FileInfoTest.Exists) && FileInfoTest.Length > 0)
                {
                    String HostTcpIPFile = File.ReadAllText(HostTcpIpFileName);
                    if (HostTcpIPFile.Length > 0)
                    {
                        parsedHostTcpIP = JsonConvert.DeserializeObject<TcpIP>(HostTcpIPFile, new JsonSerializerSettings { });
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
                            parsedHostTcpIP.Address = ip.ToString();
                        }
                    }
                }

                //For sniffing the socket to capture the packets has to be a raw socket, with the
                //address family being of type internetwork, and protocol being IP
                try
                {
                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

                    mainSocket.ReceiveBufferSize = 32768;
                    
                    //Bind the socket to the selected IP address IPAddress.Parse("192.168.50.66")
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(parsedHostTcpIP.Address), parsedHostTcpIP.Port));

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
            if (ipHeader.SourceAddress.ToString().Equals(parsedClientTcpIP.Address) || 
                ipHeader.DestinationAddress.ToString().Equals(parsedClientTcpIP.Address))
            {
                //Now according to the protocol being carried by the IP datagram we parse 
                //the data field of the datagram if it carries TCP protocol
                if ((ipHeader.ProtocolType == Protocol.TCP) && (ipHeader.MessageLength > 0))
                {
                    TCPHeader tcpHeader = new TCPHeader(ipHeader.Data, ipHeader.MessageLength);
                    //Now filter only on our Denon (or Marantz) receiver
                    if (tcpHeader.SourcePort == parsedClientTcpIP.Port.ToString() ||
                        tcpHeader.DestinationPort == parsedClientTcpIP.Port.ToString())
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
                        AudysseyTcpIP.ByteData = binaryReader.ReadBytes(AudysseyTcpIP.DataLength);
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
                                Int32Data = ConvertByteArrayToInt32(AudysseyTcpIP.ByteData);
                            }
                            //Other packets: resize array and append data
                            else
                            {
                                Int32 PreviousInt32DataLength = Int32Data.Length;
                                Int32[] Int32NextData = ConvertByteArrayToInt32(AudysseyTcpIP.ByteData);
                                Array.Resize(ref Int32Data, PreviousInt32DataLength + Int32NextData.Length);
                                Array.Copy(Int32NextData, 0, Int32Data, PreviousInt32DataLength, Int32NextData.Length);
                            }
                            //Last packet: store to class
                            if (AudysseyTcpIP.CurrentPacket == AudysseyTcpIP.TotalPackets)
                            {
                                //TotalPackets Data as int 
                                AudysseyTcpIP.Int32Data = Int32Data;
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
                        Console.WriteLine(AvrString);
                        File.AppendAllText(Environment.CurrentDirectory + "\\" + AudysseySnifferFileName, AvrString + "\n");
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
        static Int32[] ConvertByteArrayToInt32(byte[] bytes)
        {
            Int32[] Ints = null;
            if (bytes.Length % 4 == 0)
            {
                Ints = new Int32[bytes.Length / 4];
                for (int i = 0; i < Ints.Length; i++)
                {
                    Ints[i] = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(bytes, i * 4));
                }
            }
            return Ints;
        }
        private byte CalculateChecksum(byte[] dataToCalculate)
        {
            return dataToCalculate.Aggregate((r, n) => r += n);
        }
    }
}