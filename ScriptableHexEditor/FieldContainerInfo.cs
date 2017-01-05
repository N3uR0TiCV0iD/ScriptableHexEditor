using System;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class FieldContainerInfo : FieldInfo, IFieldContainer
    {
        List<FieldInfo> fields;
        public FieldContainerInfo(int fileOffset, bool isStruct) : base(fileOffset, isStruct ? FieldType.Struct : FieldType.List)
        {
            this.fields = new List<FieldInfo>();
        }
        public int FieldsCount
        {
            get
            {
                return fields.Count;
            }
        }
        public void AddField(FieldInfo field)
        {
            fields.Add(field);
        }
        public FieldInfo GetField(int index)
        {
            return fields[index];
        }
    }
}
