using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using RTPAudio.Utils;
using System.Text;
using System.Linq;

namespace RTPAudio.AudioUtils
{
    public class MP3Utils
    {
        //This Class Contains Functions and tools to determine information from MP3 files such as Bit Rate, Sampling, ect..... This should ONLY be used for the mp3 standard up to Version 2.5.
        public class Mp3Data
        {
            static readonly int[,] samplingratetable = new int[4, 4] {
                { 11025 , 12000, 8000, 0 }, // MPEGIIV
                { 0, 0, 0, 0 }, // Reserved
                { 44100, 48000, 32000, 0 }, // MPEGI
                { 22050, 24000, 16000, 0 } }; // MPEGII
            enum Version
            {
                Reserved = 1, // Not Valid.
                MPEGIIdotV = 0, //Version 2.5
                MPEGI = 2,
                MPEGII = 3,
                

            }
            enum Layer
            {

                Reserved = 0, // Not Valid.
                LayerI = 1,
                LayerII = 2,
                LayerIII = 3,
            }


            public string file;
            Version version;
            Layer layer;
            bool protection;
            bool padding;

            public int BitRate;
            public int SampleRate;
            public int Padding;

            public bool hasID3;
            public byte[] ID3ver;
            public BitArray ID3Flags;
            uint ID3lengthnum;
            
            BitVector32 ID3size;

            BitVector32.Section[] ID3size_sections = new BitVector32.Section[4];

            public Mp3Data()
            {
                ID3ver = new byte[2];
                ID3size = new BitVector32(0);
                ID3size_sections[0] = BitVector32.CreateSection(127);
                for (int x = 1; x < ID3size_sections.GetLength(0); x++)
                {
                    ID3size_sections[x] = BitVector32.CreateSection(127, ID3size_sections[x - 1]);



                }

            }
            public uint GetID3Size(byte[] data)
            {

                ID3lengthnum = (uint)ID3size.Data;
                return (uint)ID3size.Data;
            }

            internal void GetHeaderData(byte[] header)
            {
                
            }
        }


        public MP3Utils()
        {

        }

        public Mp3Data Open(string path)
        {
            if(String.IsNullOrEmpty(path) || String.IsNullOrWhiteSpace(path))
            {
                throw new InvalidDataException(path);
            }
            Mp3Data Mp3info = new Mp3Data();
            Mp3info.file = path;
            using (FileStream file = File.OpenRead(path))
            {
                byte[] isID3 = new byte[3];
                file.Read(isID3, 0, 3);


                if (Encoding.ASCII.GetString(isID3) == "ID3")
                {
                    byte flags;
                    byte[] size = new byte[4];
                    uint id3size;

                    Mp3info.hasID3 = true;
                    file.Read(Mp3info.ID3ver, 0, 2);

                    Mp3info.ID3Flags = new BitArray((byte)file.ReadByte());

                    file.Read(size, 0, 4);
                    id3size = Mp3info.GetID3Size(size);
                    file.Position += id3size;
                    byte[] header = new byte[4];
                    file.Read(header, 0, 4);
                    if (header[0] == 255)
                    {
                        if(header[1] >> 5 == 7)
                        {
                            Mp3info.GetHeaderData(header);
                        }
                    }
                    else
                    {
                       
                        FindHeader(header,file);
                    }


                    



                }
                else
                {
                    file.Position = 0;
                }
            }
            return Mp3info;

        }

        private void FindHeader(byte[] header, FileStream file)
        {
            
        }
    }
}
