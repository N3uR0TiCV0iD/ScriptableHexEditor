using System;
namespace ScriptableHexEditor
{
    public enum FieldType
    {
        List = 0,
        Struct = 1,
        Byte = 2,
        SByte = 3,
        Short = 4,
        UShort = 5,
        Int = 6,
        UInt = 7,
        Long = 8,
        ULong = 9,
        Float = 10,
        Double = 11,
        CString = 12,
        UTFString = 13,
        Bool = 14,
        Enum = 15
    }
    public class FieldInfo
    {
        protected int length;
        string name;
        int fileOffset;
        FieldType type;
        public FieldInfo(int fileOffset, FieldType type)
        {
            this.fileOffset = fileOffset;
            this.type = type;
        }
        public FieldInfo(string name, int fileOffset, FieldType type) : this(fileOffset, type)
        {
            this.name = name;
        }
        public int FileOffset
        {
            get
            {
                return fileOffset;
            }
        }
        public FieldType Type
        {
            get
            {
                return type;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        public int Length
        {
            get
            {
                return length;
            }
        }
    }
}
