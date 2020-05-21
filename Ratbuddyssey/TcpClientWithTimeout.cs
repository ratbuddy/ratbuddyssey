using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ratbuddyssey
{
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
        private byte CalculateChecksum(byte[] dataToCalculate)
        {
            return dataToCalculate.Aggregate((r, n) => r += n);
        }
        public MemoryStream TransmitTcpAvrStream(string Cmd, byte[] Data, int current_packet = 0, int total_packets = 0)
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

            return memoryStream;
        }
        public MemoryStream TransmitTcpAvrStream(string Cmd, string Data)
        {
            return TransmitTcpAvrStream(Cmd, Encoding.ASCII.GetBytes(Data));
        }
        public MemoryStream ReceiveTcpAvrStream(ref string Cmd, out byte[] Data, out bool ValidCheckSum)
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

            return memoryStream;
        }
        public MemoryStream ReceiveTcpAvrStream(ref string Cmd, out string Data, out bool ValidCheckSum)
        {
            byte[] DataByte;
            MemoryStream memoryStream = ReceiveTcpAvrStream(ref Cmd, out DataByte, out ValidCheckSum);
            Data = Encoding.ASCII.GetString(DataByte);
            return memoryStream;
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
