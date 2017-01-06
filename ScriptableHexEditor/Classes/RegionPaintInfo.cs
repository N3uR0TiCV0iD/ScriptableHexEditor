using System;
namespace ScriptableHexEditor
{
    public class RegionPaintInfo
    {
        int yStart;
        int drawingRows;
        int regionLength;
        RegionRow lastRow;
        RegionRow firstRow;
        public RegionPaintInfo(RegionRow firstRow, RegionRow lastRow, int regionLength, int yStart)
        {
            this.yStart = yStart;
            this.lastRow = lastRow;
            this.firstRow = firstRow;
            this.regionLength = regionLength;
            this.drawingRows = (int)Math.Ceiling((firstRow.ColumnIndex + regionLength) / 16F);
        }
        public int FirstRowColumn
        {
            get
            {
                return firstRow.ColumnIndex;
            }
        }
        public int LastRowColumn
        {
            get
            {
                return lastRow.ColumnIndex;
            }
        }
        public int RegionLength
        {
            get
            {
                return regionLength;
            }
        }
        public int DrawingRows
        {
            get
            {
                return drawingRows;
            }
        }
        public int YStart
        {
            get
            {
                return yStart;
            }
        }
        public bool DrawEntireFirstRow
        {
            get
            {
                return firstRow.ColumnIndex == 0 && regionLength >= 16;
            }
        }
        public bool DrawEntireLastRow
        {
            get
            {
                return lastRow.ColumnIndex == 0; //Since selection would end on the first byte of the next row :)
            }
        }
    }
    public class RegionRow
    {
        int rowIndex;
        int columnIndex;
        public RegionRow(int rowIndex, int columnIndex)
        {
            this.rowIndex = rowIndex;
            this.columnIndex = columnIndex;
        }
        public int RowIndex
        {
            get
            {
                return rowIndex;
            }
        }
        public int ColumnIndex
        {
            get
            {
                return columnIndex;
            }
        }
    }
}
