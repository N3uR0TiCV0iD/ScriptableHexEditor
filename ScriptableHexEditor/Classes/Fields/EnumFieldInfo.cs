using System;
namespace ScriptableHexEditor
{
    public class EnumFieldInfo : FieldInfo
    {
        string enumName;
        public EnumFieldInfo(IFieldsContainer parentContainer, int fileOffset, string enumName) : base(parentContainer, fileOffset, 4, FieldType.Enum)
        {
            this.enumName = enumName;
        }
        public EnumFieldInfo(IFieldsContainer parentContainer, string name, int fileOffset, string enumName) : base(parentContainer, name, fileOffset, 4, FieldType.Enum)
        {
            this.enumName = enumName;
        }
        public string EnumName
        {
            get
            {
                return enumName;
            }
        }
    }
}
