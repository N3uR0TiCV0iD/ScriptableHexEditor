using System;
using System.IO;
using LuaInterface;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class HexEditorScript : IFieldsContainer
    {
        static readonly string[] LUA_METHODS = new string[] {
            "print", "registerEnum", "enumKey",
            "startList", "endList", "startStruct", "startStructEx", "endStruct",
            "byteField", "sbyteField", "shortField", "ushortField",
            "intField", "uintField", "longField", "ulongField",
            "halfField", "floatField", "doubleField", "cstringField", "utfField",
            "boolField", "enumField",
        };
        Lua luaConsole;
        string scriptPath;
        BinaryReader dataReader;
        List<FieldInfo> rootFields;
        Dictionary<string, LuaTable> enumerations;
        Stack<FieldContainerInfo> currFieldContainers;
        public HexEditorScript(string scriptPath, BinaryReader dataReader)
        {
            Type thisType = typeof(HexEditorScript);
            this.luaConsole = new Lua();
            this.dataReader = dataReader;
            this.scriptPath = scriptPath;
            this.rootFields = new List<FieldInfo>();
            this.enumerations = new Dictionary<string, LuaTable>();
            this.currFieldContainers = new Stack<FieldContainerInfo>();
            foreach (var currMethodName in LUA_METHODS)
            {
                this.luaConsole.RegisterFunction(currMethodName, this, thisType.GetMethod(currMethodName));
            }
        }
        public Dictionary<string, LuaTable>.KeyCollection EnumerationNames
        {
            get
            {
                return enumerations.Keys;
            }
        }
        public FieldContainerInfo FirstContainer
        {
            get
            {
                FieldInfo currField;
                for (int currFieldIndex = 0; currFieldIndex < rootFields.Count; currFieldIndex++)
                {
                    currField = rootFields[currFieldIndex];
                    if (currField.GetType() == typeof(FieldContainerInfo))
                    {
                        return (FieldContainerInfo)currField;
                    }
                }
                return null; //Unlikely unless the script made none...
            }
        }
        public int FieldsCount
        {
            get
            {
                return rootFields.Count;
            }
        }
        private IFieldsContainer CurrFieldParent
        {
            get
            {
                return currFieldContainers.Count != 0 ? (IFieldsContainer)currFieldContainers.Peek() : this;
            }
        }
        public FieldInfo GetField(int index)
        {
            return rootFields[index];
        }
        public int IndexOf(FieldInfo field)
        {
            for (int currFieldIndex = 0; currFieldIndex < rootFields.Count; currFieldIndex++)
            {
                if (rootFields[currFieldIndex] == field)
                {
                    return currFieldIndex;
                }
            }
            return -1;
        }
        public void Run()
        {
            enumerations.Clear();
            dataReader.BaseStream.Position = 0;
            luaConsole.DoFile(scriptPath);
        }
        public FieldInfo FindFieldAt(int fileOffset)
        {
            return FindFieldAt(this, fileOffset);
        }
        private FieldInfo FindFieldAt(IFieldsContainer fieldsContainer, int fileOffset)
        {
            FieldInfo currField;
            FieldInfo foundField;
            for (int currFieldIndex = 0; currFieldIndex < fieldsContainer.FieldsCount; currFieldIndex++)
            {
                currField = fieldsContainer.GetField(currFieldIndex);
                if ((currField.FileOffset + currField.Length) > fileOffset)
                {
                    if (currField.GetType() == typeof(FieldContainerInfo))
                    {
                        foundField = FindFieldAt((FieldContainerInfo)currField, fileOffset);
                        if (foundField != null) //Did our child have it?
                        {
                            return foundField;
                        }
                        //No, continue looking...
                    }
                    else
                    {
                        return currField;
                    }
                }
            }
            return null; //Welp, we didn't have it...
        }
        public FieldContainerInfo FindContainerAt(int fileOffset)
        {
            IFieldsContainer parentContainer = FindFieldAt(this, fileOffset).ParentContainer;
            return parentContainer.GetType() == typeof(FieldContainerInfo) ? (FieldContainerInfo)parentContainer : null;
        }
        public ICollection<string> GetEnumKeys(string enumName)
        {
            return new CollectionWrapper<string>(enumerations[enumName].Keys);
        }
        public string GetEnumKey(string enumName, int value)
        {
            foreach (string currEnumKey in enumerations[enumName].Keys)
            {
                if (GetEnumValue(enumName, currEnumKey) == value)
                {
                    return currEnumKey;
                }
            }
            return value.ToString();
        }
        public int GetEnumValue(string enumName, string enumKey)
        {
            return Convert.ToInt32(enumerations[enumName][enumKey]);
        }
        public void print(object message)
        {
            MessageBox.Show(message.ToString());
        }
        public void registerEnum(string enumName, LuaTable enumTable)
        {
            foreach (var currValueKey in enumTable.Keys)
            {
                //MessageBox.Show(currValueKey.GetType().ToString() + " => " + enumTable[currValueKey].GetType().ToString());
                if (currValueKey.GetType() != typeof(string) || !IsObjectPureInt(enumTable[currValueKey]))
                {
                    throw new Exception();
                }
            }
            enumerations.Add(enumName, enumTable);
        }
        public string enumKey(string enumName, object value)
        {
            LuaTable enumTable = enumerations[enumName];
            foreach (string currValueKey in enumTable.Keys)
            {
                if (enumTable[currValueKey].Equals(value))
                {
                    return currValueKey;
                }
            }
            throw new Exception();
        }
        public void startList()
        {
            AddNewContainer(false);
        }
        public void endList(string listName)
        {
            currFieldContainers.Pop().Name = listName;
        }
        public void startStruct()
        {
            AddNewContainer(true);
        }
        public void startStructEx(object data1, object data2)
        {
            if (IsObjectPureInt(data2))
            {
                if (data1.GetType() == typeof(bool))
                {
                    StartStructEx((bool)data1, Convert.ToUInt32(data2));
                }
                else if (IsObjectPureInt(data1))
                {
                    StartStructEx(Convert.ToUInt32(data1), Convert.ToUInt32(data2));
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                throw new Exception();
            }
        }
        private bool IsObjectPureInt(object data) //Keyword "Pure" :P
        {
            Type dataType = data.GetType();
            if (dataType == typeof(byte) || dataType == typeof(sbyte) ||
                dataType == typeof(short) || dataType == typeof(ushort) ||
                dataType == typeof(int) || dataType == typeof(uint))
            {
                return true;
            }
            else if (dataType == typeof(long) || dataType == typeof(ulong))
            {
                long longData = (long)data;
                return longData >= int.MinValue && longData <= int.MaxValue;
            }
            else if (dataType == typeof(float))
            {
                float floatData = (float)data;
                if (floatData % 1 == 0) //Does it have decimals?
                {
                    //No it doesn't, let's make sure it has the value range of an in
                    return floatData >= int.MinValue && floatData <= int.MaxValue;
                }
            }
            else if (dataType == typeof(double))
            {
                double doubleData = (double)data;
                if (doubleData % 1 == 0) //Does it have decimals?
                {
                    //No it doesn't, let's make sure it has the value range of an in
                    return doubleData >= int.MinValue && doubleData <= int.MaxValue;
                }
            }
            return false;
        }
        private void StartStructEx(bool isBackground, uint hexColor)
        {
            AddNewContainer( new FieldContainerInfo(this.CurrFieldParent, (int)dataReader.BaseStream.Position, isBackground, HexToColor(hexColor)) );
        }
        private void StartStructEx(uint bgHexColor, uint rectHexColor)
        {
            AddNewContainer( new FieldContainerInfo(this.CurrFieldParent, (int)dataReader.BaseStream.Position, HexToColor(bgHexColor), HexToColor(rectHexColor)) );
        }
        private Color HexToColor(uint hexColor)
        {
            return Color.FromArgb((int)(hexColor >> 16) & 0xFF, (int)(hexColor >> 8) & 0xFF, (int)hexColor & 0xFF);
        }
        public void endStruct(string fieldName)
        {
            currFieldContainers.Pop().Name = fieldName;
        }
        public byte byteField(string fieldName)
        {
            AddNewField(fieldName, 1, FieldType.Byte);
            return dataReader.ReadByte();
        }
        public sbyte sbyteField(string fieldName)
        {
            AddNewField(fieldName, 1, FieldType.SByte);
            return dataReader.ReadSByte();
        }
        public short shortField(string fieldName)
        {
            AddNewField(fieldName, 2, FieldType.Short);
            return dataReader.ReadInt16();
        }
        public ushort ushortField(string fieldName)
        {
            AddNewField(fieldName, 2, FieldType.UShort);
            return dataReader.ReadUInt16();
        }
        public int intField(string fieldName)
        {
            AddNewField(fieldName, 4, FieldType.Int);
            return dataReader.ReadInt32();
        }
        public uint uintField(string fieldName)
        {
            AddNewField(fieldName, 4, FieldType.UInt);
            return dataReader.ReadUInt32();
        }
        public long longField(string fieldName)
        {
            AddNewField(fieldName, 8, FieldType.Long);
            return dataReader.ReadInt64();
        }
        public ulong ulongField(string fieldName)
        {
            AddNewField(fieldName, 8, FieldType.ULong);
            return dataReader.ReadUInt64();
        }
        public float halfField(string fieldName)
        {
            AddNewField(fieldName, 2, FieldType.HalfFloat);
            return HalfHelper.HalfToSingle(Half.ToHalf(dataReader.ReadBytes(2), 0));
        }
        public float floatField(string fieldName)
        {
            AddNewField(fieldName, 4, FieldType.Float);
            return dataReader.ReadSingle();
        }
        public double doubleField(string fieldName)
        {
            AddNewField(fieldName, 8, FieldType.Double);
            return dataReader.ReadDouble();
        }
        public string cstringField(string fieldName)
        {
            string cString = Utils.ReadCString(dataReader);
            int fieldLength = cString.Length + 1;
            AddNewField(new FieldInfo(this.CurrFieldParent, fieldName, (int)dataReader.BaseStream.Position - fieldLength, fieldLength, FieldType.CString));
            return cString;
        }
        public string utfField(string fieldName)
        {
            int fieldOffset = (int)dataReader.BaseStream.Position;
            string readString = dataReader.ReadString();
            AddNewField(new FieldInfo(this.CurrFieldParent, fieldName, fieldOffset, readString.Length + 1, FieldType.UTFString));
            return readString;
        }
        public bool boolField(string fieldName)
        {
            AddNewField(fieldName, 1, FieldType.Bool);
            return dataReader.ReadBoolean();
        }
        public int enumField(string fieldName, string enumName)
        {
            AddNewField(new EnumFieldInfo(this.CurrFieldParent, fieldName, (int)dataReader.BaseStream.Position, enumName));
            return dataReader.ReadInt32();
        }
        private void AddNewField(string fieldName, int length, FieldType fieldType)
        {
            AddNewField(new FieldInfo(this.CurrFieldParent, fieldName, (int)dataReader.BaseStream.Position, length, fieldType));
        }
        public void AddNewField(FieldInfo field)
        {
            if (currFieldContainers.Count != 0)
            {
                currFieldContainers.Peek().AddField(field);
            }
            else
            {
                rootFields.Add(field);
            }
        }
        private void AddNewContainer(bool isStruct)
        {
            AddNewContainer(new FieldContainerInfo(this.CurrFieldParent, (int)dataReader.BaseStream.Position, isStruct));
        }
        private void AddNewContainer(FieldContainerInfo container)
        {
            AddNewField(container);
            currFieldContainers.Push(container);
        }
    }
}
