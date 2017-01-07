using System;
namespace ScriptableHexEditor
{
    public enum FieldType
    {
        Byte = 0,
        SByte = 1,
        Bool = 2,
        Short = 3,
        UShort = 4,
        HalfFloat = 5,
        Int = 6,
        UInt = 7,
        Float = 8,
        Long = 9,
        ULong = 10,
        Double = 11,
        Enum = 12,
        CString = 13,
        UTFString = 14,
        Struct = 15,
        List = 16,
    }
    public class FieldInfo
    {
        protected int length;
        string name;
        int fileOffset;
        FieldType fieldType;
        IFieldsContainer parentContainer;
        public FieldInfo(IFieldsContainer parentContainer, int fileOffset, int length, FieldType fieldType)
        {
            this.length = length;
            this.fieldType = fieldType;
            this.fileOffset = fileOffset;
            this.parentContainer = parentContainer;
        }
        public FieldInfo(IFieldsContainer parentContainer, string name, int fileOffset, int length, FieldType fieldType) : this(parentContainer, fileOffset, length, fieldType)
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
        public FieldType Type
        {
            get
            {
                return fieldType;
            }
        }
        public virtual int Length
        {
            get
            {
                return length;
            }
        }
        public IFieldsContainer ParentContainer
        {
            get
            {
                return parentContainer;
            }
        }
    }
}
