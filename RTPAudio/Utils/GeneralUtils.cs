using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RTPAudio.Utils
{
    class GeneralUtils
    {
        // all byte ot int conversion are based on Big Endian. For Little Endian use normal C# functions.
        public static byte[] UInt32ToByte(uint input)
        {

            byte[] output = new byte[4];

            output[0] = (byte)(input >> 24);
            output[1] = (byte)(input >> 16);
            output[2] = (byte)(input >> 8);
            output[3] = (byte)(input >> 0);

            return output;
        }
        public static uint ByteToUInt32(byte[] array)
        {
            uint output = (uint)(array[0] << 24 | array[1] << 16 | array[2] << 8 | array[3]);

            return output;
        }

        public static ushort ByteToUInt16(byte[] array)
        {
            ushort output = (ushort)(array[0] << 8 | array[1]);

            return output;
        }
        public static byte[] UInt16ToByte(ushort input)
        {

            byte[] output = new byte[2];

            output[0] = (byte)(input >> 8);
            output[1] = (byte)(input >> 0);


            return output;
        }
        public static string ReverseByteToString(byte[] array)
        {
            Array.Reverse(array);
            string output = String.Join("", array.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
            return output;
        }
        public static string ByteToString(byte[] array)
        {
            string output = String.Join("", array.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
            return output;
        }
        public GeneralUtils()
        {

        }
    }
}
