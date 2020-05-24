using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ratbuddyssey
{
    class TcpIP
    {
        #region Properties
        public string Address { get; set; }
        public int Port { get; set; }
        public int Timeout { get; set; }
        #endregion
        public TcpIP(string address = "127.0.0.1", int port = 0, int timeout = 0)
        {
            Address = address;
            Port = port;
            Timeout = timeout;
        }
    }
    public class TcpClientWithTimeout
    {
        // TCPIP
        private string _hostname;
        private int _port;
        private int _timeout_milliseconds;
        private TcpClient _connection;
        private bool _connected;
        private Exception _exception;
        private NetworkStream _stream = null;
        // CLIENT
        private byte TransmitReceive;
        private UInt16 TotalLength;
        private byte[] Command;
        private UInt16 CommandLength;
        private UInt16 DataLength;
        private const UInt16 HeaderLength = 9;
        private byte Check;
        private Int32[] ByteToInt32(byte[] Byte)
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
        private byte[] Int32ToByte(Int32[] Int32s)
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
        public void TransmitTcpAvrStream(string Cmd, byte[] Data, int current_packet = 0, int total_packets = 0)
        {
            TransmitReceive = (byte)'T';

            if (Cmd != null)
            {
                CommandLength = (UInt16)Encoding.ASCII.GetByteCount(Cmd);
                Command = Encoding.ASCII.GetBytes(Cmd);
            }
            else
            {
                CommandLength = 0;
                Command = null;
            }

            if (Data != null)
            {
                DataLength = (UInt16)Data.Length;
            }
            else
            {
                DataLength = 0;
            }

            TotalLength = (UInt16)(HeaderLength + CommandLength + DataLength);

            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(TransmitReceive);
            binaryWriter.Write(BinaryPrimitives.ReverseEndianness(TotalLength));
            binaryWriter.Write((byte)current_packet);
            binaryWriter.Write((byte)total_packets);
            binaryWriter.Write(Command);
            binaryWriter.Write((byte)0);
            binaryWriter.Write(BinaryPrimitives.ReverseEndianness(DataLength));
            if (DataLength > 0) binaryWriter.Write(Data);
            binaryWriter.Write(CalculateChecksum(memoryStream.GetBuffer()));

            _stream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        }
        public void TransmitTcpAvrStream(string Cmd, Int32[] Data)
        {
            // transmit packets in chunks of 256 bytes
            int total_packets = (128 + Data.Length) / 256;
            // the last packet has less than 256 bytes
            int final_packet_length = Data.Length - (total_packets * 256); 
            // transmit all the 
            for (int current_packet = 0; current_packet < total_packets; current_packet++)
            {
                Int32[] CopyData = null;
                Array.Copy(Data, current_packet * 256, CopyData, 0, current_packet < total_packets ? 256 : final_packet_length);
                TransmitTcpAvrStream(Cmd, Int32ToByte(CopyData), current_packet, total_packets);
            }
        }
        public void TransmitTcpAvrStream(string Cmd, string Data)
        {
            TransmitTcpAvrStream(Cmd, Encoding.ASCII.GetBytes(Data));
        }
        public void ReceiveTcpAvrStream(ref string Cmd, out byte[] Data, out bool ValidCheckSum)
        {
            TransmitReceive = (byte)'R';

            if (Cmd != null)
            {
                CommandLength = (UInt16)Encoding.ASCII.GetByteCount(Cmd);
                Command = Encoding.ASCII.GetBytes(Cmd);
            }
            else
            {
                CommandLength = 0;
                Command = null;
            }

            MemoryStream memoryStream = null;
            Data = null;

            int nBufSize = 2048;
            byte[] byBuffer = new byte[nBufSize];
            _stream.ReadTimeout = 10000;

            int nReceived = 0;
            try
            {
                nReceived = _stream.Read(byBuffer, 0, nBufSize);
            }
            catch
            {
                ;
            }

            ValidCheckSum = false;
            if (nReceived > 0)
            {
                memoryStream = new MemoryStream(byBuffer, 0, nReceived);

                byte[] array = memoryStream.ToArray();
                Array.Resize<byte>(ref array, array.Length - 1);
                byte CheckSum = CalculateChecksum(array);

                if (memoryStream.Length > 0)
                {
                    BinaryReader binaryReader = new BinaryReader(memoryStream);
                    if (TransmitReceive == binaryReader.ReadByte())
                    {
                        TotalLength = BinaryPrimitives.ReverseEndianness(binaryReader.ReadUInt16());
                        binaryReader.ReadUInt16();
                        Command = binaryReader.ReadBytes(CommandLength);
                        Cmd = Encoding.ASCII.GetString(Command);
                        binaryReader.ReadByte();
                        DataLength = BinaryPrimitives.ReverseEndianness(binaryReader.ReadUInt16());
                        Data = binaryReader.ReadBytes(DataLength);
                        Check = binaryReader.ReadByte();
                    }
                }

                if (CheckSum == Check)
                {
                    ValidCheckSum = true;
                }
            }
        }
        public void ReceiveTcpAvrStream(ref string Cmd, out Int32[] Data, out bool ValidCheckSum)
        {
            byte[] DataByte;
            ReceiveTcpAvrStream(ref Cmd, out DataByte, out ValidCheckSum);
            Data = ByteToInt32(DataByte);
        }
        public void ReceiveTcpAvrStream(ref string Cmd, out string Data, out bool ValidCheckSum)
        {
            byte[] DataByte;
            ReceiveTcpAvrStream(ref Cmd, out DataByte, out ValidCheckSum);
            Data = Encoding.ASCII.GetString(DataByte);
        }
        public TcpClientWithTimeout(string hostname, int port, int timeout_milliseconds)
        {
            _hostname = hostname;
            _port = port;
            _timeout_milliseconds = timeout_milliseconds;
            Connect();
        }
        ~TcpClientWithTimeout()
        {
            // workaround for a .net bug: http://support.microsoft.com/kb/821625
            if (_stream != null)
            {
                _stream.Close();
            }
            if (_connection != null)
            {
                _connection.Close();
            }
        }
        private void Connect()
        {
            // kick off the thread that tries to connect
            _connected = false;
            _exception = null;
            Thread thread = new Thread(new ThreadStart(BeginConnect));
            thread.IsBackground = true; // So that a failed connection attempt 
                                        // wont prevent the process from terminating while it does the long timeout
            thread.Start();

            // wait for either the timeout or the thread to finish
            thread.Join(_timeout_milliseconds);

            if (_connected == true)
            {
                // it succeeded, so abort the thread
                thread.Abort();
                // open the stream if we need the stream and do not return TcpClient
                _stream = _connection.GetStream();
                _stream.ReadTimeout = 2000;
                _stream.WriteTimeout = 2000;
                // return the connection if return type is TcpClient else return void
                return;
            }
            if (_exception != null)
            {
                // it crashed, so return the exception to the caller
                thread.Abort();
                throw _exception;
            }
            else
            {
                // if it gets here, it timed out, so abort the thread and throw an exception
                thread.Abort();
                string message = string.Format("TcpClient connection to {0}:{1} timed out",_hostname, _port);
                throw new TimeoutException(message);
            }
        }
        private void BeginConnect()
        {
            try
            {
                _connection = new TcpClient(_hostname, _port);
                // record that it succeeded, for the main thread to return to the caller
                _connected = true;
            }
            catch (Exception ex)
            {
                // record the exception for the main thread to re-throw back to the calling code
                _exception = ex;
            }
        }
    }
}
