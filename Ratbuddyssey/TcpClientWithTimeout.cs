using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ratbuddyssey
{
    /// <summary>
    /// TcpClientWithTimeout is used to open a TcpClient connection, with a 
    /// user definable connection timeout in milliseconds (1000=1second)
    /// Use it like this:
    /// TcpClient connection = new TcpClientWithTimeout('127.0.0.1',80,1000).Connect();
    /// </summary>
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
        private byte[] Payload;
        private UInt16 PayloadLength;
        private byte Check;
        private UInt16 SwapUInt16(UInt16 DataWord)
        {
            return (UInt16)((DataWord >> 8) | (DataWord << 8));
        }
        private byte CalculateChecksum(byte[] dataToCalculate)
        {
            return dataToCalculate.Aggregate((r, n) => r += n);
        }
        public MemoryStream TransmitTcpAvrStream(string Cmd, string Data)
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
                PayloadLength = (UInt16)Encoding.ASCII.GetByteCount(Data);
                Payload = Encoding.ASCII.GetBytes(Data);
            }
            else
            {
                PayloadLength = 0;
                Payload = null;
            }

            TotalLength = (UInt16)(9 + CommandLength + PayloadLength);

            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            binaryWriter.Write(TransmitReceive);
            binaryWriter.Write(SwapUInt16(TotalLength));
            binaryWriter.Write((UInt16)0);
            binaryWriter.Write(Command);
            binaryWriter.Write((byte)0);
            binaryWriter.Write(SwapUInt16(PayloadLength));
            if ((PayloadLength > 0) && (Payload != null)) binaryWriter.Write(Payload);
            binaryWriter.Write(CalculateChecksum(memoryStream.GetBuffer()));

            _stream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

            return memoryStream;
        }
        public string ReceiveTcpAvrStream(ref string Cmd, out bool ValidCheckSum)
        {
            string Data = "";

            int nBufSize = 2048;
            byte[] byBuffer = new byte[nBufSize];
            _stream.ReadTimeout = 10000;
            int nReceived = _stream.Read(byBuffer, 0, nBufSize);

            MemoryStream memoryStream = new MemoryStream(byBuffer, 0, nReceived);

            // One would think that the returned checksum matches, but it does not..
            byte[] array = memoryStream.ToArray();
            Array.Resize<byte>(ref array, array.Length - 1);
            byte CheckSum = CalculateChecksum(array);
            
            if (memoryStream.Length > 0)
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                TransmitReceive = binaryReader.ReadByte();
                TotalLength = SwapUInt16(binaryReader.ReadUInt16());
                binaryReader.ReadUInt16();
                Command = binaryReader.ReadBytes(Cmd.Length);
                Cmd = Encoding.ASCII.GetString(Command);
                binaryReader.ReadByte();
                PayloadLength = SwapUInt16(binaryReader.ReadUInt16());
                Payload = binaryReader.ReadBytes(PayloadLength);
                Check = binaryReader.ReadByte();
                Data = Encoding.ASCII.GetString(Payload);
            }

            if (CheckSum == Check)
            {
                ValidCheckSum = true;
            }
            else
            {
                ValidCheckSum = false;
            }

            return Data;
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
