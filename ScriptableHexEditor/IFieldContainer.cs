using System;
namespace ScriptableHexEditor
{
    public interface IFieldContainer
    {
        int FieldsCount { get; }
        FieldInfo GetField(int index);
    }
}
