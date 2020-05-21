using System.Net;
using System.Text;
using System;
using System.IO;
using System.Windows.Forms;

namespace Ratbuddyssey
{
    public class IPHeader
    {
        //IP Header fields
        private byte byVersionAndHeaderLength;   //Eight bits for version and header length
        private byte byDifferentiatedServices;    //Eight bits for differentiated services (TOS)
        private ushort usTotalLength;              //Sixteen bits for total length of the datagram (header + message)
        private ushort usIdentification;           //Sixteen bits for identification
        private ushort usFlagsAndOffset;           //Eight bits for flags and fragmentation offset
        private byte byTTL;                      //Eight bits for TTL (Time To Live)
        private byte byProtocol;                 //Eight bits for the underlying protocol
        private short sChecksum;                  //Sixteen bits containing the checksum of the header
                                                  //(checksum can be negative so taken as short)
        private uint uiSourceIPAddress;          //Thirty two bit source IP Address
        private uint uiDestinationIPAddress;     //Thirty two bit destination IP Address
                                                 //End IP Header fields

        private byte byHeaderLength;             //Header length
        private byte[] byIPData;                   //Data carried by the datagram

        private ushort usMessageLength;           //Length of the data being carried

        public IPHeader(byte[] byBuffer, int nReceived)
        {
            try
            {
                //Create MemoryStream out of the received bytes
                MemoryStream memoryStream = new MemoryStream(byBuffer, 0, nReceived);
                //Next we create a BinaryReader out of the MemoryStream
                BinaryReader binaryReader = new BinaryReader(memoryStream);

                //The first eight bits of the IP header contain the version and
                //header length so we read them
                byVersionAndHeaderLength = binaryReader.ReadByte();

                //The next eight bits contain the Differentiated services
                byDifferentiatedServices = binaryReader.ReadByte();

                //Next eight bits hold the total length of the datagram
                usTotalLength = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                //Next sixteen have the identification bytes
                usIdentification = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                //Next sixteen bits contain the flags and fragmentation offset
                usFlagsAndOffset = (ushort)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                //Next eight bits have the TTL value
                byTTL = binaryReader.ReadByte();

                //Next eight represnts the protocol encapsulated in the datagram
                byProtocol = binaryReader.ReadByte();

                //Next sixteen bits contain the checksum of the header
                sChecksum = IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());

                //Next thirty two bits have the source IP address
                uiSourceIPAddress = (uint)(binaryReader.ReadInt32());

                //Next thirty two hold the destination IP address
                uiDestinationIPAddress = (uint)(binaryReader.ReadInt32());

                //Now we calculate the header length

                byHeaderLength = byVersionAndHeaderLength;
                //The last four bits of the version and header length field contain the
                //header length, we perform some simple binary airthmatic operations to
                //extract them
                byHeaderLength <<= 4;
                byHeaderLength >>= 4;
                //Multiply by four to get the exact header length
                byHeaderLength *= 4;

                if ((usTotalLength > 0) && (byHeaderLength > 0))
                {
                    usMessageLength = (ushort)(usTotalLength - byHeaderLength);
                    //Copy the data carried by the data gram into another array so that
                    //according to the protocol being carried in the IP datagram
                    byIPData = new byte[usTotalLength - byHeaderLength];
                    Array.Copy(byBuffer,
                               byHeaderLength,  //start copying from the end of the header
                               byIPData, 0,
                               usTotalLength - byHeaderLength);
                }
                else
                {
                    usMessageLength = 0;
                    byIPData = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IPHeader Received: " + nReceived + " TotalLength: " + usTotalLength + " HeaderLength: " + byHeaderLength, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public string Version
        {
            get
            {
                //Calculate the IP version

                //The four bits of the IP header contain the IP version
                if ((byVersionAndHeaderLength >> 4) == 4)
                {
                    return "IP v4";
                }
                else if ((byVersionAndHeaderLength >> 4) == 6)
                {
                    return "IP v6";
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public string HeaderLength
        {
            get
            {
                return byHeaderLength.ToString();
            }
        }

        public ushort MessageLength
        {
            get
            {
                //MessageLength = Total length of the datagram - Header length
                return usMessageLength;
            }
        }

        public string DifferentiatedServices
        {
            get
            {
                //Returns the differentiated services in hexadecimal format
                return string.Format("0x{0:x2} ({1})", byDifferentiatedServices,
                    byDifferentiatedServices);
            }
        }

        public string Flags
        {
            get
            {
                //The first three bits of the flags and fragmentation field 
                //represent the flags (which indicate whether the data is 
                //fragmented or not)
                int nFlags = usFlagsAndOffset >> 13;
                if (nFlags == 2)
                {
                    return "Don't fragment";
                }
                else if (nFlags == 1)
                {
                    return "More fragments to come";
                }
                else
                {
                    return nFlags.ToString();
                }
            }
        }

        public string FragmentationOffset
        {
            get
            {
                //The last thirteen bits of the flags and fragmentation field 
                //contain the fragmentation offset
                int nOffset = usFlagsAndOffset << 3;
                nOffset >>= 3;

                return nOffset.ToString();
            }
        }

        public string TTL
        {
            get
            {
                return byTTL.ToString();
            }
        }

        public Protocol ProtocolType
        {
            get
            {
                //The protocol field represents the protocol in the data portion
                //of the datagram
                if (byProtocol == 6)        //A value of six represents the TCP protocol
                {
                    return Protocol.TCP;
                }
                else if (byProtocol == 17)  //Seventeen for UDP
                {
                    return Protocol.UDP;
                }
                else
                {
                    return Protocol.Unknown;
                }
            }
        }

        public string Checksum
        {
            get
            {
                //Returns the checksum in hexadecimal format
                return string.Format("0x{0:x2}", sChecksum);
            }
        }

        public IPAddress SourceAddress
        {
            get
            {
                return new IPAddress(uiSourceIPAddress);
            }
        }

        public IPAddress DestinationAddress
        {
            get
            {
                return new IPAddress(uiDestinationIPAddress);
            }
        }

        public string TotalLength
        {
            get
            {
                return usTotalLength.ToString();
            }
        }

        public string Identification
        {
            get
            {
                return usIdentification.ToString();
            }
        }

        public byte[] Data
        {
            get
            {
                return byIPData;
            }
        }
    }
}
