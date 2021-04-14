using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using RTPAudio.AudioUtils;
using RTPAudio.Utils;

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

        public float PacketizationTime, FrameSizeInMS;
        public float SampleRate;

        public PacketBuilderParam(MP3Utils.Mp3Data mp3info)
        {
            Random rand = new Random();

            timestamp = Convert.ToUInt32(rand.Next(999, 47645)); // A random value that increments by a dynamically calculated value (increment value is consant)
            sequence = Convert.ToUInt16(rand.Next(1000, 22745)); // A random value that increments by 1 for each following packet
            rtp_identifier = rand.Next(1001, 200000845); // Unique identifer to distinguish between rtp streams

            int SamplesPerFrame = (mp3info.layer == MP3Utils.Mp3Data.Layer.LayerI) ? 384 : 1152; //= 1152;
            int BitRate = mp3info.BitRate; //= 125 * 192; // Multiply 1kb with the bitrate (bitrate for mpeg 1 layer 3 is 192 or 128)

            SampleRate = mp3info.SampleRate;
            FrameSizeInMS = (float)(SamplesPerFrame / SampleRate) * 1000;

            dataperpacket = mp3info.framelength; // If padding is set it would be added to the end here but it will never be set(ARE YOU SURE ABOUT THAT?!)
            PacketizationTime = 1000 / FrameSizeInMS;  // This signifies the amount of frames in a second (fps)
            frag_off = 0; // This value never changes for audio and is therefore 0
        }
    }

    public class PacketBuilder
    {
        string selectedfile;

        public Queue<byte[]> splitfiledata;
        public Queue<byte[]> Headerqueue;
        public Queue<byte[]> packets;
        
        public byte[] DEBUG_header;
        public byte[] DEBUG_packet;

        MP3Utils mp3parser = new MP3Utils();
        public PacketBuilderParam packetinfo;
        public PacketBuilder(string filepath)
        {
            MP3Utils.Mp3Data mp3dat = mp3parser.Open(filepath);
            packetinfo = new PacketBuilderParam(mp3dat);
            selectedfile = filepath;
            splitfiledata = BuildPayload(selectedfile);
            packets = BuildPacket(ref splitfiledata);
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
                    byte[] temparray = new byte[packetinfo.dataperpacket];

                    if (remainingbytes < packetinfo.dataperpacket)
                    {
                        file.Read(temparray, 0, Convert.ToInt32(remainingbytes));
     
                        remainingbytes = remainingbytes - remainingbytes;
                    }
                    else
                    {
                        file.Read(temparray, 0, packetinfo.dataperpacket);

                        remainingbytes = remainingbytes - packetinfo.dataperpacket;
                    }

                    filepackets.Enqueue(temparray);
                }
            }

            return filepackets;
        }

        unsafe Queue<byte[]> BuildPacket(ref Queue<byte[]> payload) 
        {
            
            int remainingpackets = payload.Count;

            Queue<byte[]> packetlist = new Queue<byte[]>();

            ushort sequence = packetinfo.sequence;
            uint timestamp = packetinfo.timestamp;
            int identifier = packetinfo.rtp_identifier;

            packetinfo.byte1 = 0b_10_0_0_0001;
            packetinfo.byte2 = 0b_0_0001110;

            byte[] header = new byte[20];
            header[0] = packetinfo.byte1;
            header[1] = packetinfo.byte2;

            fixed (byte* headerptr = &header[2])
            {
                ushort* sequenceptr = &sequence;
                uint* timestampptr = &timestamp;
                int* identifierptr = &identifier;
                byte* headeroffsetptr = headerptr;

                Buffer.MemoryCopy(sequenceptr, headeroffsetptr, 3, sizeof(ushort));
                ReverseByteOrder(headeroffsetptr, 2);
                Stopwatch memes = Stopwatch.StartNew();
                byte[] ayyylmao = GeneralUtils.UInt16ToByte(*sequenceptr);
                memes.Stop();
                TimeSpan ts = memes.Elapsed;
                Console.WriteLine(ts.TotalMilliseconds);

                headeroffsetptr += 2;

                Buffer.MemoryCopy(timestampptr, headeroffsetptr, 5, sizeof(uint));
                ReverseByteOrder(headeroffsetptr, 4);

                headeroffsetptr += 4;

                Buffer.MemoryCopy(identifierptr, headeroffsetptr, 5, sizeof(int));
                ReverseByteOrder(headeroffsetptr, 4);
                

                for (int packetcount = 0; packetcount < remainingpackets; packetcount++)
                {
                    byte[] packet = new byte[packetinfo.dataperpacket + header.Length];

                    headeroffsetptr = headerptr;

                    Buffer.MemoryCopy(sequenceptr, headeroffsetptr, 5, sizeof(ushort));
                    ReverseByteOrder(headeroffsetptr, sizeof(ushort));

                    headeroffsetptr += 2;

                    Buffer.MemoryCopy(timestampptr, headeroffsetptr, 5, sizeof(uint));
                    ReverseByteOrder(headeroffsetptr, sizeof(uint));

                    Buffer.BlockCopy(header, 0, packet, 0, header.Length);
                    Buffer.BlockCopy(payload.Dequeue(), 0, packet, header.Length, packetinfo.dataperpacket);

                    packetlist.Enqueue(packet);

                    ++*sequenceptr;
                    *timestampptr += (uint)(packetinfo.SampleRate / packetinfo.PacketizationTime);
                }
            }

            return packetlist;
        }

        unsafe void ReverseByteOrder(byte* indptr, int length)
        {
            byte tmp;
            byte* ptr1 = indptr;
            byte* ptr2 = indptr;
            ptr2 = ptr2 + length - 1;

            for (int x = 0; x < Math.Round((decimal)length / 2, 2); x++)
            {
                tmp = *ptr1;
                *ptr1 = *ptr2;
                *ptr2 = tmp;
                ptr1++;
                ptr2--;
            }
        }
    }

    class Program
    {
        static Queue<byte[]>[] filestostream;

        static void Main()
        {
            PacketBuilder packets = new PacketBuilder(@"C:\Users\19erlind\Desktop\SuperNova Website\RTPAudioStreamer\RTPAudio\UnlikePlutoEverythingBlack.mp3");

            EndPoint RemoteEP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8079);
            EndPoint SendtoEP = new IPEndPoint(IPAddress.Parse("192.168.10.160"), 8080);

            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            sender.Bind(RemoteEP);

            Queue<byte[]> DEBUG_packets = new Queue<byte[]>();
            DEBUG_packets = packets.packets;
            int remainingpackets = DEBUG_packets.Count;

            for (int x = 0; x < remainingpackets; x++)
            {
                Thread.Sleep((int)packets.packetinfo.FrameSizeInMS / 2); // 2 is wtf
                sender.SendTo(DEBUG_packets.Dequeue(), SendtoEP);
            }
        }
    }
}