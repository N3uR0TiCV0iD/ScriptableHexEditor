using System;
using System.IO;
using System.Text;
using LuaInterface;
using System.Windows.Forms;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class HexEditorScript : IFieldContainer
    {
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
            this.luaConsole.RegisterFunction("print", this, thisType.GetMethod("print"));
            this.luaConsole.RegisterFunction("registerEnum", this, thisType.GetMethod("registerEnum"));
            this.luaConsole.RegisterFunction("startList", this, thisType.GetMethod("startList"));
            this.luaConsole.RegisterFunction("endList", this, thisType.GetMethod("endList"));
            this.luaConsole.RegisterFunction("startStruct", this, thisType.GetMethod("startStruct"));
            this.luaConsole.RegisterFunction("endStruct", this, thisType.GetMethod("endStruct"));
            this.luaConsole.RegisterFunction("byteField", this, thisType.GetMethod("byteField"));
            this.luaConsole.RegisterFunction("sbyteField", this, thisType.GetMethod("sbyteField"));
            this.luaConsole.RegisterFunction("shortField", this, thisType.GetMethod("shortField"));
            this.luaConsole.RegisterFunction("ushortField", this, thisType.GetMethod("ushortField"));
            this.luaConsole.RegisterFunction("intField", this, thisType.GetMethod("intField"));
            this.luaConsole.RegisterFunction("uintField", this, thisType.GetMethod("uintField"));
            this.luaConsole.RegisterFunction("longField", this, thisType.GetMethod("longField"));
            this.luaConsole.RegisterFunction("ulongField", this, thisType.GetMethod("ulongField"));
            this.luaConsole.RegisterFunction("floatField", this, thisType.GetMethod("floatField"));
            this.luaConsole.RegisterFunction("doubleField", this, thisType.GetMethod("doubleField"));
            this.luaConsole.RegisterFunction("cstringField", this, thisType.GetMethod("cstringField"));
            this.luaConsole.RegisterFunction("utfField", this, thisType.GetMethod("utfField"));
            this.luaConsole.RegisterFunction("boolField", this, thisType.GetMethod("boolField"));
            this.luaConsole.RegisterFunction("enumField", this, thisType.GetMethod("enumField"));
        }
        public int FieldsCount
        {
            get
            {
                return rootFields.Count;
            }
        }
        public FieldInfo GetField(int index)
        {
            return rootFields[index];
        }
        public void Run()
        {
            enumerations.Clear();
            dataReader.BaseStream.Position = 0;
            luaConsole.DoFile(scriptPath);
        }
        public void print(object message)
        {
            MessageBox.Show(message.ToString());
        }
        public void registerEnum(string enumName, LuaTable enumTable)
        {
            enumerations.Add(enumName, enumTable);
            //MessageBox.Show("typeof(enumTable[" + currValueKey + "]) = " + enumTable[currValueKey].GetType().ToString());
            //TODO: Check enum for invalid data types!
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
        public void endStruct(string fieldName)
        {
            currFieldContainers.Pop().Name = fieldName;
        }
        public byte byteField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Byte);
            return dataReader.ReadByte();
        }
        public sbyte sbyteField(string fieldName)
        {
            AddNewField(fieldName, FieldType.SByte);
            return dataReader.ReadSByte();
        }
        public short shortField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Short);
            return dataReader.ReadInt16();
        }
        public ushort ushortField(string fieldName)
        {
            AddNewField(fieldName, FieldType.UShort);
            return dataReader.ReadUInt16();
        }
        public int intField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Int);
            return dataReader.ReadInt32();
        }
        public uint uintField(string fieldName)
        {
            AddNewField(fieldName, FieldType.UInt);
            return dataReader.ReadUInt32();
        }
        public long longField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Long);
            return dataReader.ReadInt64();
        }
        public ulong ulongField(string fieldName)
        {
            AddNewField(fieldName, FieldType.ULong);
            return dataReader.ReadUInt64();
        }
        public float floatField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Float);
            return dataReader.ReadSingle();
        }
        public double doubleField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Double);
            return dataReader.ReadDouble();
        }
        public string cstringField(string fieldName)
        {
            StringBuilder cString = new StringBuilder();
            char readChar = dataReader.ReadChar();
            while (readChar != '\0')
            {
                cString.Append(readChar);
                readChar = dataReader.ReadChar();
            }
            AddNewField(fieldName, FieldType.CString);
            return cString.ToString();
        }
        public string utfField(string fieldName)
        {
            AddNewField(fieldName, FieldType.UTFString);
            return dataReader.ReadString();
        }
        public bool boolField(string fieldName)
        {
            AddNewField(fieldName, FieldType.Bool);
            return dataReader.ReadBoolean();
        }
        public int enumField(string fieldName, string enumName)
        {
            /*
            LuaTable enumTable = enumerations[enumName];
            foreach (string currValueKey in enumTable.Keys)
            {
                if (DoubleToInt((double)enumTable[currValueKey]) == readEnumValue)
                {
                    return currValueKey;
                }
            }
            */
            AddNewField(fieldName, FieldType.Enum);
            return dataReader.ReadInt32();
        }
        private void AddNewField(string fieldName, FieldType fieldType)
        {
            AddNewField(new FieldInfo(fieldName, (int)dataReader.BaseStream.Position, fieldType));
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
            FieldContainerInfo container = new FieldContainerInfo((int)dataReader.BaseStream.Position, isStruct);
            AddNewField(container);
            currFieldContainers.Push(container);
        }
        private int DoubleToInt(double number)
        {
            if (number >= 0)
            {
                return (int)Math.Floor(number);
            }
            else
            {
                return (int)Math.Ceiling(number);
            }
        }
    }
}
