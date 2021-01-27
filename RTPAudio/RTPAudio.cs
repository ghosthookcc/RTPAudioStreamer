using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace file_splitter
{
    /*
      The RTP header has the following format:

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |V=2|P|X|  CC   |M|     PT      |       sequence number         |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                           timestamp                           | <-- 80 works idk why
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |           synchronization source (SSRC) identifier            |
   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
   |            contributing source (CSRC) identifiers             |
   |                             ....                              |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

    */
    public class PacketBuilderParam
    {
        public UInt32 timestamp;
        public UInt16 sequence;
        public UInt16 frag_off;
        public int dataperpacket; //size of the payload. 
        public byte byte1;//has rtp_version, rtp_padding, rtp_extension, rtp_csources_count;
        public byte byte2; // has  rtp_marker, rtp_payload_type;
        public int rtp_identifier;
        public UInt32 rtp_csources;
        public UInt32 rtp_ssrc;
        public UInt32 rtp_csourcecount;

        public PacketBuilderParam()
        {
            Random rand = new Random();

            timestamp = Convert.ToUInt32(rand.Next(999, 43576));
            sequence = Convert.ToUInt16(rand.Next(1000, 22545));
            rtp_identifier = rand.Next(1001, 1000000);

            dataperpacket = 500;
            frag_off = 0;
        }
    }


    public class PacketBuilder
    {
        PacketBuilderParam packetinfo = new PacketBuilderParam();

        string selectedfile;
        public Queue<byte[]> splitfiledata;
        public Queue<byte[]> Headerqueue;
        public Queue<byte[]> packets;
        public byte[] DEBUG_header;
        public byte[] DEBUG_packet;

        int dataperpacket => packetinfo.dataperpacket;
        public PacketBuilder(string filepath)
        {
            selectedfile = filepath;
            splitfiledata = BuildPayload(selectedfile);
            packets = BuildPacket(splitfiledata);
        }
        Queue<byte[]> BuildPayload(string path)
        {
            Queue<byte[]> filepackets = new Queue<byte[]>();
            using (FileStream file = File.OpenRead(path))
            {
                long filelength = file.Length;
                long remainingbytes = filelength;

                while (remainingbytes != 0)
                {
                    byte[] temparray = new byte[dataperpacket];

                    if (remainingbytes < dataperpacket)
                    {
                        file.Read(temparray, 0, Convert.ToInt32(remainingbytes));
                        remainingbytes = remainingbytes - remainingbytes;

                    }
                    else
                    {
                        file.Read(temparray, 0, dataperpacket);

                        remainingbytes = remainingbytes - dataperpacket;

                    }

                    filepackets.Enqueue(temparray);
                }

            }

            return filepackets;
        }

        Queue<byte[]> BuildPacket(Queue<byte[]> payload) //This whole method needs to be refactored ASAP. Suggested: pointers instead of bitconversion, buffer usage instead of whatever kind of bullshit we used to manipulate the arrays.
        {
            int remainingpackets = payload.Count;

            byte[] sequencebytes = new byte[2];
            byte[] fragoffset = new byte[2];
            byte[] timestampbytes = new byte[4];

            Queue<byte[]> packetlist = new Queue<byte[]>();

            timestampbytes = BitConverter.GetBytes(packetinfo.timestamp);
            Array.Reverse(timestampbytes, 0, 4);

            packetinfo.byte1 = 0b_10_0_0_0001;
            packetinfo.byte2 = 0b_0_0_001110;

            byte[] header = new byte[20];
            header[0] = packetinfo.byte1;
            header[1] = packetinfo.byte2;

            Array.ConstrainedCopy(BitConverter.GetBytes(packetinfo.rtp_identifier), 0, header, 8, 4);
            for (int packetcount = 0; packetcount < remainingpackets; packetcount++)
            {
                byte[] packet = new byte[dataperpacket + 20];

                fragoffset = BitConverter.GetBytes(packetinfo.frag_off);
                Array.Reverse(fragoffset);

                sequencebytes = BitConverter.GetBytes(packetinfo.sequence);
                Array.Reverse(sequencebytes, 0, 2);

                timestampbytes = BitConverter.GetBytes(packetinfo.timestamp);
                Array.Reverse(timestampbytes, 0, 4);

                Array.ConstrainedCopy(sequencebytes, 0, header, 2, 2);
                Array.ConstrainedCopy(timestampbytes, 0, header, 4, 4);
                Array.ConstrainedCopy(fragoffset, 0, header, 18, 2);

                Array.ConstrainedCopy(header, 0, packet, 0, 20);

                Array.ConstrainedCopy(payload.Dequeue(), 0, packet, 20, dataperpacket);

                packetlist.Enqueue(packet);

                packetinfo.sequence++;
                packetinfo.timestamp += 80; // 80 works for some reason, I still to this day have no clue why
            }

            return packetlist;
        }
    }
    class Program
    {
        static Queue<byte[]>[] filestostream;
        static void Main()
        {
            //  PacketBuilder packets = new PacketBuilder(@"pathtofile\RTPAudio\UnlikePlutoEverythingBlack.mp3");
            PacketBuilder packets = new PacketBuilder(@"C:\Users\lasse\source\repos\RTPAudioStreamer-main\RTPAudio\UnlikePlutoEverythingBlack.mp3");

            EndPoint RemoteEP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8079);
            EndPoint SendtoEP = new IPEndPoint(IPAddress.Parse("192.168.1.107"), 8080);

            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            sender.Bind(RemoteEP);

            Queue<byte[]> DEBUG_packets = new Queue<byte[]>();
            DEBUG_packets = packets.packets;
            int remainingpackets = DEBUG_packets.Count;
            for (int x = 0; x < remainingpackets; x++)
            {
                // Console.WriteLine("Packet {0} sent.", x);
                sender.SendTo(DEBUG_packets.Dequeue(), SendtoEP);
            }
        }
    }
}