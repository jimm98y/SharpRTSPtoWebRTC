using System.IO;

namespace SharpJaad.MP4.Boxes.Impl.FD
{
    /// <summary>
    /// A BASE64 character decoder.
    /// </summary>
    public static class Base64Decoder
    {
        /*private static final char[] CHAR_ARRAY = {
        //       0   1   2   3   4   5   6   7
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', // 0
        'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', // 1
        'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', // 2
        'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', // 3
        'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', // 4
        'o', 'p', 'q', 'r', 's', 't', 'u', 'v', // 5
        'w', 'x', 'y', 'z', '0', '1', '2', '3', // 6
        '4', '5', '6', '7', '8', '9', '+', '/' // 7
        };*/
        //CHAR_CONVERT_ARRAY[CHAR_ARRAY[i]] = i;
        private static readonly int[] CHAR_CONVERT_ARRAY = {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63, 52, 53, 54, 55, 56, 57,
            58, 59, 60, 61, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1,
            -1, -1, -1, -1, -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
            39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, 0
        };

        public static byte[] Decode(byte[] b)
        {
            using (MemoryStream input = new MemoryStream(b))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    int i;

                    try
                    {
                        while (true)
                        {
                            for (i = 0; (i + 4) < 72; i += 4)
                            {
                                DecodeAtom(input, output, 4);
                            }
                            if ((i + 4) == 72) DecodeAtom(input, output, 4);
                            else DecodeAtom(input, output, 72 - i);
                        }
                    }
                    catch (IOException)
                    {
                    }
                    return output.ToArray();
                }
            }
        }

        private static void DecodeAtom(Stream input, Stream output, int rem)
        {
            if (rem < 2) throw new IOException();

            int i;
            do 
            {
                i = input.ReadByte();
                if (i == -1) throw new IOException();
            }
            while (i == '\n' || i == '\r');

            byte[] buf = new byte[4];
            buf[0] = (byte)i;

            i = ReadFully(input, buf, 1, rem - 1);
            if (i == -1) throw new IOException();

            if (rem > 3 && buf[3] == '=') rem = 3;
            if (rem > 2 && buf[2] == '=') rem = 2;

            int a = -1, b = -1, c = -1, d = -1;
            switch (rem)
            {
                case 4:
                    d = CHAR_CONVERT_ARRAY[buf[3] & 0xff];
                    c = CHAR_CONVERT_ARRAY[buf[2] & 0xff];
                    b = CHAR_CONVERT_ARRAY[buf[1] & 0xff];
                    a = CHAR_CONVERT_ARRAY[buf[0] & 0xff];
                    break;

                case 3:
                    c = CHAR_CONVERT_ARRAY[buf[2] & 0xff];
                    b = CHAR_CONVERT_ARRAY[buf[1] & 0xff];
                    a = CHAR_CONVERT_ARRAY[buf[0] & 0xff];
                    break;

                case 2:
                    b = CHAR_CONVERT_ARRAY[buf[1] & 0xff];
                    a = CHAR_CONVERT_ARRAY[buf[0] & 0xff];
                    break;
            }

            switch (rem)
            {
                case 2:
                    output.WriteByte((byte)(((a << 2) & 0xfc) | ((int)((uint)b >> 4) & 3)));
                    break;
                case 3:
                    output.WriteByte((byte)(((a << 2) & 0xfc) | ((int)((uint)b >> 4) & 3)));
                    output.WriteByte((byte)(((b << 4) & 0xf0) | ((int)((uint)c >> 2) & 0xf)));
                    break;
                case 4:
                    output.WriteByte((byte)(((a << 2) & 0xfc) | ((int)((uint)b >> 4) & 3)));
                    output.WriteByte((byte)(((b << 4) & 0xf0) | ((int)((uint)c >> 2) & 0xf)));
                    output.WriteByte((byte)(((c << 6) & 0xc0) | (d & 0x3f)));
                    break;
            }
            return;
        }

        private static int ReadFully(Stream input, byte[] b, int off, int len)
        {
            for (int i = 0; i < len; i++) {
                int q = input.ReadByte();
                if (q == -1) return ((i == 0) ? -1 : i);
                b[i + off] = (byte)q;
            }
            return len;
        }
    }
}
