using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace RTPAudio.AudioUtils
{
    public class MP3Utils
    {
        //This Class Contains Functions and tools to determine information from MP3 files such as Bit Rate, Sampling, ect..... This should ONLY be used for the mp3 standard up to Version 2.5.
        public class Mp3Data
        {
            enum Version
            {
                MPEGI = 1,
                MPEGII = 2,
                MPEGIIdotV = 3, //Version 2.5

            }
            enum Layer
            {
                LayerI = 1,
                LayerII = 2,
                LayerIII = 3,
            }
            public string file;
            Version version;
            Layer layer;

            public int BitRate;
            public int SampleRate;
            public int Padding;

            public bool hasID3;
            public byte[] ID3ver;
            public BitArray ID3Flags;
            public BitVector32 ID3size;
            public BitVector32.Section[] ID3size_sections = new BitVector32.Section[4];

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
                

                if(Encoding.ASCII.GetString(isID3) == "ID3")
                {
                    byte flags;
                    byte[] size = new byte[4];
                    uint id3size;

                    Mp3info.hasID3 = true;
                    file.Read(Mp3info.ID3ver, 0, 2);
                    Mp3info.ID3Flags = new BitArray((byte)file.ReadByte());
                    file.Read(size, 0, 4);
                    for(int x = size.Length - 1; x >= 0; x--)
                    {
                        Mp3info.ID3size[Mp3info.ID3size_sections[(Mp3info.ID3size_sections.GetLength(0) - 1) - x]] = size[x];

                        

                        

                    }
                    id3size = (uint)Mp3info.ID3size.Data;
                    file.Position += id3size;
                    byte[] header = new byte[3];
                    file.Read(header, 0, 3);
                    Console.WriteLine("It Just Works -Todd Howard");



                }
                else
                {
                    file.Position = 0;
                }
            }
            return Mp3info;

        }






    }
}
