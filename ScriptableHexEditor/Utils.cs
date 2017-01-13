using System;
using System.IO;
using System.Text;
namespace ScriptableHexEditor
{
    public static class Utils
    {
        public static string ReadCString(BinaryReader dataReader)
        {
            StringBuilder cString = new StringBuilder();
            char readChar = dataReader.ReadChar();
            while (readChar != '\0')
            {
                cString.Append(readChar);
                if (dataReader.BaseStream.Position != dataReader.BaseStream.Length)
                {
                    readChar = dataReader.ReadChar();
                }
                else
                {
                    readChar = '\0';
                }
            }
            return cString.ToString();
        }
        public static byte[] HexStringToBytes(string hexString)
        {
            byte[] bytes;
            int currByteCharStart;
            if (hexString.Length % 2 != 0)
            {
                hexString = hexString + "0";
            }
            bytes = new byte[hexString.Length / 2];
            for (int currByteIndex = 0; currByteIndex < bytes.Length; currByteIndex++)
            {
                currByteCharStart = currByteIndex * 2;
                bytes[currByteIndex] = (byte)((GetHexCharValue(hexString[currByteCharStart]) << 4) | GetHexCharValue(hexString[currByteCharStart + 1]));
            }
            return bytes;
        }
        public static int GetHexCharValue(char hexChar)
        {
            hexChar = char.ToUpper(hexChar);
            switch (hexChar)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9': return hexChar - '0';

                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F': return hexChar - 'A' + 10;
                default: throw new Exception();
            }
        }
        public static bool IsPrintableASCIIChar(byte asciiByte)
        {
            return asciiByte >= 32 && asciiByte <= 126;
        }
    }
}
