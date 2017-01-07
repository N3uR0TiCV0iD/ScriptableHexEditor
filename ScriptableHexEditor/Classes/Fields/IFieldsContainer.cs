using System;
namespace ScriptableHexEditor
{
    public interface IFieldsContainer
    {
        int FieldsCount { get; }
        int IndexOf(FieldInfo field);
        FieldInfo GetField(int index);
    }
}
