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
    }
}
