using System;
using System.Drawing;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class FieldContainerInfo : FieldInfo, IFieldsContainer
    {
        Pen rectanglePen;
        List<FieldInfo> fields;
        SolidBrush backgroundBrush;
        public FieldContainerInfo(IFieldsContainer parentContainer, int fileOffset, bool isStruct) : base(parentContainer, fileOffset, -1, isStruct ? FieldType.Struct : FieldType.List)
        {
            this.fields = new List<FieldInfo>();
        }
        public FieldContainerInfo(IFieldsContainer parentContainer, int fileOffset, bool colorIsBackground, Color color) : this(parentContainer, fileOffset, true)
        {
            if (colorIsBackground)
            {
                this.backgroundBrush = new SolidBrush(color);
            }
            else
            {
                this.rectanglePen = new Pen(color);
            }
        }
        public FieldContainerInfo(IFieldsContainer parentContainer, int fileOffset, Color backgroundColor, Color rectangleColor) : this(parentContainer, fileOffset, true)
        {
            this.backgroundBrush = new SolidBrush(backgroundColor);
            this.rectanglePen = new Pen(rectangleColor);
        }
        public int IndexOf(FieldInfo field)
        {
            for (int currFieldIndex = 0; currFieldIndex < fields.Count; currFieldIndex++)
            {
                if (fields[currFieldIndex] == field)
                {
                    return currFieldIndex;
                }
            }
            return -1;
        }
        public override int Length
        {
            get
            {
                if (base.length == -1)
                {
                    base.length = 0;
                    foreach (var currField in fields)
                    {
                        base.length += currField.Length;
                    }
                }
                return base.length;
            }
        }
        public SolidBrush BackgroundBrush
        {
            get
            {
                return backgroundBrush;
            }
        }
        public Pen RectanglePen
        {
            get
            {
                return rectanglePen;
            }
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
