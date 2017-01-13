using System;
using System.IO;
using System.Text;
using LuaInterface;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace ScriptableHexEditor
{
    //Lots and lots of magic numbers here... Be prepared :^)
    public delegate void DebugStringHandler(string message);
    public delegate void ScriptDoneHandler(object sender, IFieldsContainer rootFields);
    public delegate void DataChangedHandler(object sender, int fileOffset, int length);
    public partial class HexEditor : Control
    {
        //TMP
        public event DebugStringHandler DebugString;
        private void Log(string message)
        {
            DebugString?.Invoke(message);
        }
        //END TMP
        [DllImport("user32.dll")] private static extern bool CreateCaret(IntPtr hwnd, IntPtr hBMP, int width, int height);
        [DllImport("user32.dll")] private static extern bool SetCaretPos(int x, int y);
        [DllImport("user32.dll")] private static extern bool ShowCaret(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool HideCaret(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool DestroyCaret();
        const int CHARWIDTH = 10;
        const int CaretHexXPos0 = 90; //GetCaretHexXPos(0)
        const int CaretTextXPos0 = 572; //GetCaretTextXPos(0)
        const int HEXSPACING_HEIGHT = 20;
        const int RECTFILL_OFFSET = 7;
        const int RECTDRAW_OFFSET = 5;
        const int HEXBYTES_WIDTH = CHARWIDTH * 3;
        const int RECTDRAW_TEXTEND = CaretTextXPos16;
        const int RECTDRAW_HEXSTART = CaretHexXPos0 - 1;
        const int RECTDRAW_TEXTSTART = CaretTextXPos0 - 1;
        const int CaretTextXPos16 = 572 + (16 * CHARWIDTH);
        const int RECTDRAW_HEXEND = CaretHexXPos16 - RECTDRAW_OFFSET;
        const int CaretHexXPos16 = 90 + (16 * HEXBYTES_WIDTH) - (16 / 6);
        static readonly int TRIPLECLICK_TIME = SystemInformation.DoubleClickTime / 2;
        Stopwatch doubleClickStopwatch;
        HexEditorScript usingScript;
        bool updateStreamPosition;
        BinaryReader dataReader;
        MemoryStream dataStream;
        VScrollBar scrollBar;
        Control virtualCaret;
        Color selectionColor;
        Brush selectionBrush;
        bool mouseSelecting;
        int selectionStart;
        int selectionEnd;
        bool wroteNibble;
        bool caretHidden;
        string filePath;
        int firstNibble;
        bool selecting;
        bool inHexArea;
        bool isReading;
        public HexEditor()
        {
            int readBytes;
            byte[] buffer = new byte[4096];
            this.scrollBar = new VScrollBar();
            this.Controls.Add(this.scrollBar);
            this.virtualCaret = new Control();
            this.Controls.Add(this.virtualCaret);
            this.dataStream = new MemoryStream();
            this.scrollBar.Dock = DockStyle.Right;
            this.SelectionColor = SystemColors.Highlight;
            this.scrollBar.MouseEnter += ScrollBar_MouseEnter;
            this.doubleClickStopwatch = Stopwatch.StartNew();
            this.scrollBar.ValueChanged += ScrollBar_ValueChanged;
            this.Font = new Font("Courier New", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            //TMP
            //filePath = "C:\\Users\\Patrick\\AppData\\LocalLow\\DefaultCompany\\All-Mozart\\Sheets\\FRENCH SONG.msf";
            filePath = "C:\\Users\\Patrick\\AppData\\LocalLow\\DefaultCompany\\All-Mozart\\Sheets\\Contest_Entry.msf";
            try
            {
                using (FileStream editingFileStream = new FileStream(filePath, FileMode.Open))
                {
                    readBytes = editingFileStream.Read(buffer, 0, 4096);
                    while (readBytes != 0)
                    {
                        this.dataStream.Write(buffer, 0, readBytes);
                        readBytes = editingFileStream.Read(buffer, 0, 4096);
                    }
                    this.dataReader = new BinaryReader(dataStream);
                    //MessageBox.Show("Loaded " + editingFileStream.Length + " bytes");
                }
            }
            catch (Exception ex)
            {

            }
            //END TMP
            this.virtualCaret.BackColor = Color.Black;
            this.virtualCaret.Visible = false;
            this.scrollBar.LargeChange = 1;
            this.virtualCaret.Height = 2;
            this.caretHidden = true;
            this.inHexArea = true;
        }
        public HexEditor(string filePath) : this()
        {
            this.filePath = filePath;
        }
        public string FilePath
        {
            get
            {
                return filePath;
            }
        }
        public Color SelectionColor
        {
            get
            {
                return selectionColor;
            }
            set
            {
                selectionBrush = new SolidBrush(value);
                selectionColor = value;
            }
        }
        public FieldInfo SelectedField
        {
            get
            {
                return FindFieldAt(this.SelectionStart);
            }
        }
        public int NextFieldOffset
        {
            get
            {
                FieldInfo selectedField = FindFieldAt(selectionEnd);
                return selectedField.Length - (selectionEnd - selectedField.FileOffset);
            }
        }
        public int PreviousFieldOffset
        {
            get
            {
                FieldInfo previousField = FindFieldAt(selectionEnd - 1);
                return previousField.FileOffset - selectionEnd;
            }
        }
        public int NextContainerOffset
        {
            get
            {
                FieldContainerInfo selectedContainer = FindContainerAt(selectionEnd);
                if (selectedContainer != null)
                {
                    return selectedContainer.Length - (selectionEnd - selectedContainer.FileOffset);
                }
                else
                {
                    selectedContainer = usingScript.FirstContainer; //Reutilizing variable. It is technically nextContainer
                    if (selectedContainer != null)
                    {
                        return selectedContainer.FileOffset - selectionEnd;
                    }
                    else
                    {
                        return (int)dataStream.Position - selectionEnd;
                    }
                }
            }
        }
        public int PreviousContainerOffset
        {
            get
            {
                IFieldsContainer parentContainer;
                FieldContainerInfo selectedContainer = FindContainerAt(selectionEnd);
                if (selectedContainer != null)
                {
                    parentContainer = selectedContainer.ParentContainer;
                    int fieldIndex = parentContainer.IndexOf(selectedContainer);
                    if (fieldIndex != 0) //Is the selected container the first field of its parent?
                    {
                        return parentContainer.GetField(fieldIndex - 1).FileOffset - selectionEnd;
                    }
                    else if (parentContainer.GetType() == typeof(FieldContainerInfo))
                    {
                        FieldInfo currField = null;
                        var parent = (FieldContainerInfo)parentContainer;
                        selectedContainer = (FieldContainerInfo)parentContainer; //Reutilzing variable. It is technically currContainer
                        while (fieldIndex == 0)
                        {
                            //Keep going up
                            parentContainer = selectedContainer.ParentContainer;
                            fieldIndex = parentContainer.IndexOf(selectedContainer);
                        }
                        for (int currFieldIndex = fieldIndex - 1; currFieldIndex >= 0; currFieldIndex--)
                        {
                            //Let's look at the siblings (going up) until one of them is a field container
                            currField = parentContainer.GetField(currFieldIndex);
                            if (currField.GetType() == typeof(FieldContainerInfo))
                            {
                                parentContainer = (FieldContainerInfo)currField;
                                return parentContainer.GetField(parentContainer.FieldsCount - 1).FileOffset - selectionEnd;
                            }
                        }
                        //Welp, we didn't find any field container siblings. Let's go to the top sibling instead
                        return currField.FileOffset - selectionEnd;
                    }
                }
                return -selectionEnd;
            }
        }
        public int SelectionStart
        {
            get
            {
                if (selectionEnd >= selectionStart)
                {
                    //Forward selection
                    return selectionStart;
                }
                else
                {
                    //Backwards selection
                    return selectionEnd;
                }
            }
            set
            {
                if (value >= 0 && value <= dataStream.Length)
                {
                    selectionStart = value;
                    if (this.Focused && !caretHidden)
                    {
                        UpdateCaretPositions();
                    }
                    OnSelectionChanged(false, true);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public int SelectionLength
        {
            get
            {
                return Math.Abs(selectionEnd - selectionStart);
            }
            set
            {
                if (value >= 0)
                {
                    selectionEnd = selectionStart + value;
                    OnSelectionChanged(true, true);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public void RunScript(string scriptPath)
        {
            bool retry = true;
            bool success = false;
            usingScript = new HexEditorScript(scriptPath, dataReader);
            while (retry)
            {
                try
                {
                    usingScript.Run();
                    success = true;
                    retry = false;
                }
                catch (LuaException ex)
                {
                    retry = MessageBox.Show(ex.Message, "ERROR: LuaError", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry;
                }
                catch (EndOfStreamException ex)
                {
                    MessageBox.Show("WARNING: Something went wrong, are you sure this this is the correct file format?", "WARNING: Bad format?", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    retry = false;
                }
                catch (Exception ex)
                {
                    retry = false;
                    throw ex; //TODO: Display exception info
                }
            }
            if (success)
            {
                ScriptDone?.Invoke(this, usingScript);
                this.Refresh();
            }
        }
        public void Cut()
        {
            Cut(inHexArea);
        }
        public void Cut(bool isHexString)
        {
            Cut(this.SelectionStart, this.SelectionLength, isHexString);
        }
        public void Cut(int fileOffset, int length, bool isHexString)
        {
            Copy(fileOffset, length, isHexString);
            Delete(fileOffset, length);
        }
        public void Copy()
        {
            Copy(inHexArea);
        }
        public void Copy(bool isHexString)
        {
            Copy(this.SelectionStart, this.SelectionLength, isHexString);
        }
        public void Copy(int fileOffset, int length, bool isHexString)
        {
            StringBuilder clipboardText = new StringBuilder();
            byte[] bytes = GetBytes(fileOffset, length);
            if (isHexString)
            {
                foreach (var currByte in bytes)
                {
                    clipboardText.Append(currByte.ToString("X"));
                }
            }
            else
            {
                foreach (var currByte in bytes)
                {
                    if (!Utils.IsPrintableASCIIChar(currByte))
                    {
                        clipboardText.Append(".");
                    }
                    else
                    {
                        clipboardText.Append((char)currByte);
                    }
                }
            }
            Clipboard.SetText(clipboardText.ToString());
        }
        public void Paste()
        {
            Paste(inHexArea);
        }
        public void Paste(bool isHexString)
        {
            Paste(this.SelectionStart, isHexString);
        }
        public void Paste(int fileOffset, bool isHexString)
        {
            byte[] bytes;
            if (isHexString)
            {
                bytes = Utils.HexStringToBytes(Clipboard.GetText());
            }
            else
            {
                bytes = Encoding.ASCII.GetBytes(Clipboard.GetText());
            }
            Insert(fileOffset, bytes);
        }
        public void Insert(byte[] bytes)
        {
            Insert(this.SelectionStart, bytes);
        }
        public void Insert(int fileOffset, byte[] bytes)
        {

        }
        public void Delete()
        {
            Delete(this.SelectionStart, this.SelectionLength);
        }
        public void Delete(int fileOffset, int length)
        {

        }
        public byte[] GetBytes(int fileOffset, int length)
        {
            dataStream.Position = fileOffset;
            return dataReader.ReadBytes(length);
        }
        public FieldInfo FindFieldAt(int fileOffset)
        {
            return usingScript != null ? usingScript.FindFieldAt(fileOffset) : null;
        }
        public FieldContainerInfo FindContainerAt(int fileOffset)
        {
            return usingScript != null ? usingScript.FindContainerAt(fileOffset) : null;
        }
        public void ReplaceHexString(string hexString, string newHexString)
        {

        }
        public void FindHexString(string hexString)
        {

        }
        public void ReplaceString(string seachString, string newString)
        {

        }
        public void FindString(string searchString)
        {

        }
        public string GetEnumKey(EnumFieldInfo enumField, int value)
        {
            return GetEnumKey(enumField.EnumName, value);
        }
        public string GetEnumKey(string enumName, int value)
        {
            return usingScript.GetEnumKey(enumName, value);
        }
        public byte[] ReadField(FieldInfo field)
        {
            dataStream.Position = field.FileOffset;
            return dataReader.ReadBytes(field.Length);
        }
        public void StartReading(int fileOffset)
        {
            dataStream.Position = fileOffset;
            updateStreamPosition = false;
            isReading = true;
        }
        public void DoneReading()
        {
            updateStreamPosition = true;
            isReading = false;
        }
        public byte[] BulkRead()
        {
            return BulkRead(8);
        }
        public byte[] BulkRead(int max) //Reads up to MAX bytes
        {
            StreamPositionUpdateCheck();
            for (int currTryBytes = max; currTryBytes >= 1; currTryBytes /= 2)
            {
                if (CanReadBytes(currTryBytes))
                {
                    return dataReader.ReadBytes(currTryBytes);
                }
            }
            return null;
        }
        public bool PeekData(FieldType type, out object result)
        {
            if (ReadData(type, false, out result))
            {
                dataStream.Position -= GetFieldTypeLength(type);
                return true;
            }
            return false;
        }
        public bool ReadData(FieldType type, out object result)
        {
            return ReadData(type, true, out result);
        }
        public bool ReadData(FieldType type, bool moveCaret, out object result)
        {
            StreamPositionUpdateCheck();
            if ( CanReadBytes(GetFieldTypeLength(type)) )
            {
                switch (type)
                {
                    case FieldType.Byte:
                        result = dataReader.ReadByte();
                    break;
                    case FieldType.SByte:
                        result = dataReader.ReadSByte();
                    break;
                    case FieldType.Bool:
                        result = dataReader.ReadBoolean();
                    break;
                    case FieldType.Short:
                        result = dataReader.ReadInt16();
                    break;
                    case FieldType.UShort:
                        result = dataReader.ReadUInt16();
                    break;
                    case FieldType.HalfFloat:
                        result = Half.ToHalf(dataReader.ReadBytes(2), 0);
                    break;
                    case FieldType.Int:
                        result = dataReader.ReadInt32();
                    break;
                    case FieldType.UInt:
                        result = dataReader.ReadUInt32();
                    break;
                    case FieldType.Float:
                        result = dataReader.ReadSingle();
                    break;
                    case FieldType.Long:
                        result = dataReader.ReadInt64();
                    break;
                    case FieldType.ULong:
                        result = dataReader.ReadUInt64();
                    break;
                    case FieldType.Double:
                        result = dataReader.ReadDouble();
                    break;
                    default: throw new Exception();
                }
                if (moveCaret)
                {
                    selectionStart = (int)dataStream.Position;
                    OnSelectionChanged(false, true);
                }
                return true;
            }
            result = null;
            return false;
        }
        private int GetFieldTypeLength(FieldType type)
        {
            switch (type)
            {
                case FieldType.Byte:
                case FieldType.SByte:
                case FieldType.Bool: return 1;

                case FieldType.Short:
                case FieldType.UShort:
                case FieldType.HalfFloat: return 2;

                case FieldType.Int:
                case FieldType.UInt:
                case FieldType.Float:
                case FieldType.Enum: return 4;

                case FieldType.Long:
                case FieldType.ULong:
                case FieldType.Double: return 8;

                default: throw new Exception();
            }
        }
        public bool PeekReadEnum(string enumName, out string result)
        {
            object objectResult;
            if (PeekReadEnum(enumName, out objectResult))
            {
                result = objectResult.ToString();
                return true;
            }
            result = null;
            return false;
        }
        public bool PeekReadEnum(string enumName, out object result)
        {
            if (ReadEnum(enumName, false, out result))
            {
                dataStream.Position -= 4;
                return true;
            }
            result = null;
            return false;
        }
        public bool ReadEnum(string enumName, out string result)
        {
            return ReadEnum(enumName, true, out result);
        }
        public bool ReadEnum(string enumName, out object result)
        {
            return ReadEnum(enumName, true, out result);
        }
        public bool ReadEnum(string enumName, bool moveCaret, out string result)
        {
            object objectResult;
            if (ReadEnum(enumName, moveCaret, out objectResult))
            {
                result = objectResult.ToString();
                return true;
            }
            result = null;
            return false;
        }
        public bool ReadEnum(string enumName, bool moveCaret, out object result)
        {
            StreamPositionUpdateCheck();
            if (usingScript != null && CanReadBytes(4))
            {
                int readValue;
                readValue = dataReader.ReadInt32();
                result = GetEnumKey(enumName, readValue);
                if (moveCaret)
                {
                    selectionStart = (int)dataStream.Position;
                    OnSelectionChanged(false, true);
                }
                return result.ToString() != readValue.ToString(); //Was it a valid enum?
            }
            result = null;
            return false;
        }
        public bool PeekReadUTFString(out string result)
        {
            object objectResult;
            if (PeekReadUTFString(out objectResult))
            {
                result = objectResult.ToString();
                return true;
            }
            result = null;
            return false;
        }
        public bool PeekReadUTFString(out object result)
        {
            if (ReadUTFString(false, out result))
            {
                dataStream.Position -= result.ToString().Length + 1;
                return true;
            }
            result = null;
            return false;
        }
        public bool ReadUTFString(out string result)
        {
            return ReadUTFString(true, out result);
        }
        public bool ReadUTFString(out object result)
        {
            return ReadUTFString(true, out result);
        }
        public bool ReadUTFString(bool moveCaret, out string result)
        {
            object objectResult;
            if (ReadUTFString(moveCaret, out objectResult))
            {
                result = objectResult.ToString();
                return true;
            }
            result = null;
            return false;
        }
        public bool ReadUTFString(bool moveCaret, out object result)
        {
            StreamPositionUpdateCheck();
            try
            {
                result = dataReader.ReadString();
                if (moveCaret)
                {
                    selectionStart = (int)dataStream.Position;
                    OnSelectionChanged(false, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            result = null;
            return false;
        }
        public bool PeekReadCString(out string result)
        {
            object objectResult;
            if (PeekReadCString(out objectResult))
            {
                result = objectResult.ToString();
                return true;
            }
            result = null;
            return false;
        }
        public bool PeekReadCString(out object result)
        {
            if (ReadCString(false, out result))
            {
                dataStream.Position -= result.ToString().Length + 1;
                return true;
            }
            result = null;
            return false;
        }
        public bool ReadCString(out string result)
        {
            return ReadCString(true, out result);
        }
        public bool ReadCString(out object result)
        {
            return ReadCString(true, out result);
        }
        public bool ReadCString(bool moveCaret, out string result)
        {
            object objectResult;
            if (ReadCString(moveCaret, out objectResult))
            {
                result = objectResult.ToString();
                return true;
            }
            result = null;
            return false;
        }
        public bool ReadCString(bool moveCaret, out object result)
        {
            StreamPositionUpdateCheck();
            if (dataStream.Position != dataStream.Length)
            {
                try
                {
                    result = Utils.ReadCString(dataReader);
                    if (moveCaret)
                    {
                        selectionStart = (int)dataStream.Position;
                        OnSelectionChanged(false, true);
                    }
                    return true;
                }
                catch (Exception ex)
                {

                }
            }
            result = null;
            return false;
        }
        private bool CanReadBytes(int length)
        {
            return (dataStream.Position + length - 1) < dataStream.Length;
        }
        public void ScrollUp()
        {
            ScrollUp(1);
        }
        public void ScrollUp(int amount)
        {
            int newValue = scrollBar.Value - amount;
            if (newValue < scrollBar.Minimum)
            {
                newValue = scrollBar.Minimum;
            }
            scrollBar.Value = newValue;
        }
        public void ScrollDown()
        {
            ScrollDown(1);
        }
        public void ScrollDown(int amount)
        {
            int newValue = scrollBar.Value + amount;
            if (newValue > scrollBar.Maximum)
            {
                newValue = scrollBar.Maximum;
            }
            scrollBar.Value = newValue;
        }
        private void StreamPositionUpdateCheck()
        {
            if (updateStreamPosition)
            {
                //MessageBox.Show("Updating stream position...");
                dataStream.Position = this.SelectionStart;
                updateStreamPosition = !isReading;
            }
        }
        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            RefreshCaretState();
            this.Refresh();
        }
        private void RefreshCaretState()
        {
            int newYPos = GetCaretYPos(selectionStart / 16);
            if (newYPos < 20 || newYPos >= this.Height)
            {
                SafeHideCaret();
            }
            else if (!selecting)
            {
                UpdateCaretPositions();
                SafeShowCaret();
            }
        }
        private void UpdateCaretPositions()
        {
            const int VIRTUALCARET_XOFFSET = 0;
            int selectionStartColumn = selectionStart % 16;
            int newYPos = GetCaretYPos(selectionStart / 16);
            if (inHexArea)
            {
                const int VIRTUAL_TEXTCARET_YOFFSET = HEXSPACING_HEIGHT - 2;
                int xOffset;
                if (selectionStartColumn < 2)
                {
                    xOffset = 1;
                }
                else if (selectionStartColumn >= 12)
                {
                    xOffset = -1;
                }
                else
                {
                    xOffset = 0;
                }
                virtualCaret.Location = new Point(GetCaretTextXPos(selectionStartColumn) + xOffset + VIRTUALCARET_XOFFSET, newYPos + VIRTUAL_TEXTCARET_YOFFSET); //Put the virtual caret on the HexText area
                SetCaretPos(GetCaretHexXPos(selectionStartColumn), newYPos);
                virtualCaret.Width = CHARWIDTH;
            }
            else
            {
                const int VIRTUAL_HEXCARET_YOFFSET = HEXSPACING_HEIGHT - 4;
                const int VIRTUAL_HEXCARET_WIDTH = CHARWIDTH * 2;
                virtualCaret.Location = new Point(GetCaretHexXPos(selectionStartColumn) + VIRTUALCARET_XOFFSET, newYPos + VIRTUAL_HEXCARET_YOFFSET); //Put the virtual caret on the Hex area
                SetCaretPos(GetCaretTextXPos(selectionStartColumn), newYPos);
                virtualCaret.Width = VIRTUAL_HEXCARET_WIDTH;
            }
        }
        private int GetCaretHexXPos(int caretColumn)
        {
            return CaretHexXPos0 + (caretColumn * HEXBYTES_WIDTH) - (caretColumn / 6);
        }
        private int GetCaretTextXPos(int caretColumn)
        {
            return CaretTextXPos0 + (caretColumn * CHARWIDTH);
        }
        private int GetCaretYPos(int caretRow)
        {
            return 25 + ((caretRow - scrollBar.Value) * HEXSPACING_HEIGHT);
        }
        private void ScrollBar_MouseEnter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Y >= 20 && e.X >= 85)
            {
                this.Cursor = Cursors.IBeam;
                if (mouseSelecting)
                {
                    selectionEnd = ComputeCaretPosition(e.X, e.Y);
                    OnSelectionChanged(selectionEnd != selectionStart);
                }
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Y >= 20)
            {
                if (e.X >= 85)
                {
                    //In Hex | HexText Area
                    if (e.Button == MouseButtons.Left)
                    {
                        if (e.Clicks == 1)
                        {
                            if (doubleClickStopwatch.ElapsedMilliseconds > TRIPLECLICK_TIME) //Did he actually do a triple-click?
                            {
                                //No he didn't. Let's just deselect and move the caret
                                selectionStart = ComputeCaretPosition(e.X, e.Y);
                                OnSelectionChanged(false);
                                mouseSelecting = true;
                            }
                            else
                            {
                                //Yes he did! Let's select the entire row
                                SelectYPosRow(e.Y);
                            }
                        }
                        else
                        {
                            selectionStart = ComputeCaretPosition(e.X, e.Y);
                            selectionEnd = selectionStart + 1;
                            doubleClickStopwatch.Reset();
                            doubleClickStopwatch.Start();
                            OnSelectionChanged(true);
                        }
                    }
                    else
                    {
                        bool clickedOnSelection;
                        int newCaretPos = ComputeCaretPosition(e.X, e.Y);
                        if (selectionEnd >= selectionStart) //Is it forward selection?
                        {
                            //Yes it is!
                            if (newCaretPos >= selectionStart && newCaretPos <= selectionEnd) //Did the user right click in the selected area?
                            {
                                clickedOnSelection = true;
                            }
                            else
                            {
                                clickedOnSelection = false;
                            }
                        }
                        else if (newCaretPos >= selectionEnd && newCaretPos <= selectionStart)
                        {
                            //No it isn't BUT the user did click in the selected area
                            clickedOnSelection = true;
                        }
                        else
                        {
                            //No it isn't AND the user did NOT click in the selected area
                            clickedOnSelection = false;
                        }
                        if (!clickedOnSelection)
                        {
                            selectionStart = newCaretPos;
                        }
                        OnSelectionChanged(clickedOnSelection);
                        mouseSelecting = false;
                    }
                }
                else if (e.Clicks == 2 && e.Button == MouseButtons.Left)
                {
                    //In Offset Area
                    SelectYPosRow(e.Y);
                }
            }
            this.Select();
        }
        private void SelectYPosRow(int yPos)
        {
            selectionStart = ( ((yPos - 30) / HEXSPACING_HEIGHT) + scrollBar.Value) * 16;
            selectionEnd = selectionStart + 16;
            OnSelectionChanged(true);
        }
        private void OnSelectionChanged(bool selecting)
        {
            OnSelectionChanged(selecting, false);
        }
        protected virtual void OnSelectionChanged(bool selecting, bool changedOutside)
        {
            int focusRow;
            int displayableRows = GetDisplayableRows();
            int lastVisibleRow = scrollBar.Value + displayableRows;
            this.selecting = selecting;
            if (!selecting)
            {
                SafeShowCaret();
                selectionEnd = selectionStart;
                UpdateCaretPositions();
            }
            else
            {
                SafeHideCaret();
            }
            if (!changedOutside)
            {
                focusRow = selectionEnd / 16;
                if (focusRow < scrollBar.Value)
                {
                    scrollBar.Value = focusRow;
                }
                else if (focusRow > (lastVisibleRow - 1))
                {
                    scrollBar.Value = focusRow - displayableRows + 1;
                }
            }
            else
            {
                focusRow = selectionStart / 16;
                if (focusRow < scrollBar.Value)
                {
                    int newRow = focusRow - (displayableRows / 3) - 1;
                    if (newRow < scrollBar.Minimum)
                    {
                        newRow = scrollBar.Minimum;
                    }
                    scrollBar.Value = newRow;
                }
                else
                {
                    displayableRows /= 3;
                    lastVisibleRow = scrollBar.Value + displayableRows;
                    if (focusRow > (lastVisibleRow - 1))
                    {
                        scrollBar.Value = focusRow - displayableRows + 1;
                    }
                }
            }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            updateStreamPosition = true;
            wroteNibble = false;
            this.Refresh();
        }
        private void SafeShowCaret()
        {
            if (caretHidden)
            {
                virtualCaret.Visible = true;
                ShowCaret(this.Handle);
                caretHidden = false;
            }
        }
        private void SafeHideCaret()
        {
            if (!caretHidden)
            {
                virtualCaret.Visible = false;
                HideCaret(this.Handle);
                caretHidden = true;
            }
        }
        private int ComputeCaretPosition(int xPos, int yPos)
        {
            int caretPosition = ( ((yPos - 30) / HEXSPACING_HEIGHT) + scrollBar.Value ) * 16;
            if (xPos < 566)
            {
                //In Hex Area
                caretPosition += (xPos - 80) / HEXBYTES_WIDTH;
                inHexArea = true;
            }
            else
            {
                //In HexText Area
                const int MAX_XPOS = CHARWIDTH * 16;
                int textAreaXPos = (xPos - 565);
                if (textAreaXPos > MAX_XPOS)
                {
                    textAreaXPos = MAX_XPOS;
                }
                caretPosition += textAreaXPos / CHARWIDTH;
                inHexArea = false;
            }
            if (caretPosition >= dataStream.Length)
            {
                caretPosition = (int)dataStream.Length;
            }
            return caretPosition;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseSelecting = false;
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                ScrollUp();
            }
            else
            {
                ScrollDown();
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            int newValue;
            int moveAmount;
            switch (e.KeyCode)
            {
                case Keys.Right:
                    if (!e.Control || usingScript == null)
                    {
                        moveAmount = 1;
                    }
                    else
                    {
                        moveAmount = this.NextFieldOffset;
                    }
                    if (!e.Shift)
                    {
                        if (!selecting)
                        {
                            if (selectionStart != dataStream.Length)
                            {
                                newValue = selectionStart + moveAmount;
                                if (newValue > dataStream.Length)
                                {
                                    newValue = (int)dataStream.Length;
                                }
                                selectionStart = newValue;
                                OnSelectionChanged(false);
                            }
                        }
                        else
                        {
                            //Cancel the selection
                            if (selectionEnd >= selectionStart) //Is it forward selection?
                            {
                                //Yes it is! Move the caret to the bottom right
                                selectionStart = selectionEnd;
                            }
                            else
                            {
                                //No it isn't. Move the to the top left + 1
                                selectionStart = selectionEnd + 1;
                            }
                            OnSelectionChanged(false);
                        }
                    }
                    else if (selectionEnd != dataStream.Length)
                    {
                        newValue = selectionEnd + moveAmount;
                        if (newValue > dataStream.Length)
                        {
                            newValue = (int)dataStream.Length;
                        }
                        selectionEnd = newValue;
                        OnSelectionChanged(selectionStart != selectionEnd);
                    }
                break;
                case Keys.Left:
                    if (!e.Control || usingScript == null)
                    {
                        moveAmount = -1;
                    }
                    else
                    {
                        moveAmount = this.PreviousFieldOffset;
                    }
                    if (!e.Shift)
                    {
                        if (selectionEnd >= selectionStart)
                        {
                            if (selectionEnd != 0)
                            {
                                newValue = selectionEnd + moveAmount;
                                if (newValue < 0)
                                {
                                    newValue = 0;
                                }
                                selectionStart = newValue;
                                OnSelectionChanged(false);
                            }
                        }
                        else
                        {
                            //It has backward selection. Cancel the selection and move the caret to the top left
                            selectionStart = selectionEnd;
                            OnSelectionChanged(false);
                        }
                    }
                    else if (selectionEnd != 0)
                    {
                        newValue = selectionEnd + moveAmount;
                        if (newValue < 0)
                        {
                            newValue = 0;
                        }
                        selectionEnd = newValue;
                        OnSelectionChanged(selectionStart != selectionEnd);
                    }
                break;
                case Keys.Up:
                    if (!e.Control || usingScript == null)
                    {
                        moveAmount = -16;
                    }
                    else
                    {
                        moveAmount = this.PreviousContainerOffset;
                    }
                    if (!e.Shift)
                    {
                        if (selectionEnd != 0)
                        {
                            newValue = selectionEnd + moveAmount;
                            if (newValue < 0)
                            {
                                newValue = 0;
                            }
                            selectionStart = newValue;
                            OnSelectionChanged(false);
                        }
                    }
                    else if (selectionEnd != 0)
                    {
                        newValue = selectionEnd + moveAmount;
                        if (newValue < 0)
                        {
                            newValue = 0;
                        }
                        selectionEnd = newValue;
                        OnSelectionChanged(selectionStart != selectionEnd);
                    }
                break;
                case Keys.Down:
                    if (!e.Control || usingScript == null)
                    {
                        moveAmount = 16;
                    }
                    else
                    {
                        moveAmount = this.NextContainerOffset;
                    }
                    if (!e.Shift)
                    {
                        if (selectionEnd <= (dataStream.Length - moveAmount))
                        {
                            selectionStart = selectionEnd + moveAmount;
                        }
                        else
                        {
                            selectionStart = (int)dataStream.Length;
                            OnSelectionChanged(false);
                            ScrollDown();
                        }
                        OnSelectionChanged(false);
                    }
                    else
                    {
                        if (selectionEnd <= (dataStream.Length - moveAmount))
                        {
                            selectionEnd += moveAmount;
                        }
                        else
                        {
                            selectionEnd = (int)dataStream.Length;
                        }
                        OnSelectionChanged(selectionStart != selectionEnd);
                    }
                break;
                case Keys.Home:
                    if (!e.Shift)
                    {
                        selectionStart = 0;
                        OnSelectionChanged(false);
                    }
                    else
                    {
                        selectionEnd = 0;
                        OnSelectionChanged(selectionStart != selectionEnd);
                    }
                break;
                case Keys.PageUp:

                break;
                case Keys.PageDown:

                break;
                case Keys.End:
                    if (!e.Shift)
                    {
                        selectionStart = (int)dataStream.Length;
                        OnSelectionChanged(false);
                    }
                    else
                    {
                        selectionEnd = (int)dataStream.Length;
                        OnSelectionChanged(selectionStart != selectionEnd);
                    }
                    ScrollDown();
                break;
                case Keys.D0:
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9:
                    if (inHexArea)
                    {
                        if (!wroteNibble)
                        {
                            UpdateFirstNibble((int)e.KeyCode - 48);
                        }
                        else
                        {
                            UpdateLastNibble((int)e.KeyCode - 48);
                        }
                    }
                break;
                case Keys.NumPad0:
                case Keys.NumPad1:
                case Keys.NumPad2:
                case Keys.NumPad3:
                case Keys.NumPad4:
                case Keys.NumPad5:
                case Keys.NumPad6:
                case Keys.NumPad7:
                case Keys.NumPad8:
                case Keys.NumPad9:
                    if (inHexArea)
                    {
                        if (!wroteNibble)
                        {
                            UpdateFirstNibble((int)e.KeyCode - 96);
                        }
                        else
                        {
                            UpdateLastNibble((int)e.KeyCode - 96);
                        }
                    }
                break;
                case Keys.A:
                case Keys.B:
                case Keys.C:
                case Keys.D:
                case Keys.E:
                case Keys.F:
                    if (inHexArea)
                    {
                        if (!wroteNibble)
                        {
                            UpdateFirstNibble((int)e.KeyCode - 65 + 10);
                        }
                        else
                        {
                            UpdateLastNibble((int)e.KeyCode - 65 + 10);
                        }
                    }
                break;
                case Keys.Insert:

                break;
                case Keys.Delete:

                break;
            }
        }
        private void UpdateFirstNibble(int nibbleValue)
        {
            int selectionStart = this.SelectionStart;
            int newByteValue;
            firstNibble = nibbleValue << 4;
            StreamPositionUpdateCheck();
            if (selectionStart != dataStream.Length)
            {
                newByteValue = firstNibble | (dataStream.ReadByte() & 0x0F);
                dataStream.Position = selectionStart;
            }
            else
            {
                MessageBox.Show("Test2");
                newByteValue = firstNibble;
            }
            SetCaretPos(GetCaretHexXPos(selectionStart % 16) + CHARWIDTH, GetCaretYPos(selectionStart / 16)); //Moves caret to the next nibble
            dataStream.WriteByte((byte)newByteValue);
            OnDataChanged(selectionStart, 1);
            wroteNibble = true;
            this.Refresh();
        }
        private void UpdateLastNibble(int nibbleValue)
        {
            int selectionStart = this.SelectionStart;
            dataStream.Position = selectionStart;
            dataStream.WriteByte((byte)(firstNibble | nibbleValue));
            OnDataChanged(selectionStart, 1);
            this.selectionStart = (int)dataStream.Position;
            OnSelectionChanged(false);
        }
        protected virtual void OnDataChanged(int fileOffset, int length)
        {
            DataChanged?.Invoke(this, fileOffset, length);
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!inHexArea)
            {
                dataStream.Position = selectionStart;
                dataStream.WriteByte((byte)e.KeyChar);
                selectionStart++;
                OnSelectionChanged(false);
            }
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateScrollBar();
        }
        private void UpdateScrollBar()
        {
            int rows = (int)Math.Ceiling(dataStream.Length / 16F) + 1; //+1 to give an extra row at the very bottom
            int nonDisplayableRows = rows - GetDisplayableRows();
            if (nonDisplayableRows > 0)
            {
                this.scrollBar.Maximum = nonDisplayableRows;
                this.scrollBar.Enabled = true;
            }
            else
            {
                this.scrollBar.Enabled = false;
                this.scrollBar.Maximum = 0;
            }
        }
        private int GetDisplayableRows()
        {
            return (this.Height - 20 + (HEXSPACING_HEIGHT / 8)) / HEXSPACING_HEIGHT;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            List<FieldContainerInfo> paintingFieldContainers = new List<FieldContainerInfo>();
            int selectionLength = selectionEnd - selectionStart;
            StringBuilder bytesString = new StringBuilder();
            StringBuilder asciiString = new StringBuilder();
            FieldContainerInfo currFieldContainer;
            int caretColumn = selectionStart % 16;
            int caretRow = selectionStart / 16;
            RegionPaintInfo drawPaintInfo;
            RegionPaintInfo fillPaintInfo;
            bool[] visibleRegions;
            string fileOffset;
            int yPos = 25;
            byte readByte;
            int times;
            int diff;
            e.Graphics.Clear(SystemColors.Control);
            e.Graphics.FillRectangle(Brushes.White, 85, 20, this.Width - 85, this.Height - 20);
            if (usingScript != null)
            {
                FieldInfo currField;
                for (int currFieldIndex = 0; currFieldIndex < usingScript.FieldsCount; currFieldIndex++)
                {
                    currField = usingScript.GetField(currFieldIndex);
                    if (currField.GetType() == typeof(FieldContainerInfo))
                    {
                        RememberFieldContainer((FieldContainerInfo)currField, paintingFieldContainers, e.Graphics);
                    }
                }
                visibleRegions = new bool[paintingFieldContainers.Count];
            }
            else
            {
                visibleRegions = null;
            }
            for (int currContainerIndex = 0; currContainerIndex < paintingFieldContainers.Count; currContainerIndex++) //Draws FieldContainers that have a background
            {
                currFieldContainer = paintingFieldContainers[currContainerIndex];
                if (currFieldContainer.BackgroundBrush != null)
                {
                    fillPaintInfo = PreparePaintRegion(currFieldContainer.FileOffset, currFieldContainer.Length, false);
                    if (fillPaintInfo != null)
                    {
                        FillRectangleRegion(fillPaintInfo, currFieldContainer.BackgroundBrush, e.Graphics);
                        visibleRegions[currContainerIndex] = true;
                    }
                }
            }
            if (selectionLength != 0)
            {
                RegionPaintInfo paintInfo;
                paintInfo = PreparePaintRegion(this.SelectionStart, Math.Abs(selectionLength), false);
                if (paintInfo != null)
                {
                    FillRectangleRegion(paintInfo, selectionBrush, e.Graphics);
                }
            }
            for (int currContainerIndex = 0; currContainerIndex < paintingFieldContainers.Count; currContainerIndex++)
            {
                currFieldContainer = paintingFieldContainers[currContainerIndex];
                if (currFieldContainer.RectanglePen != null)
                {
                    drawPaintInfo = PreparePaintRegion(currFieldContainer.FileOffset, currFieldContainer.Length, true);
                    if (visibleRegions[currContainerIndex] || drawPaintInfo != null)
                    {
                        //TODO: Make use of the LEVEL argument
                        DrawRectangleRegion(drawPaintInfo, 0, currFieldContainer.RectanglePen, e.Graphics);
                    }
                }
            }
            e.Graphics.DrawLine(Pens.Black, 0, 20, this.Width, 20);
            e.Graphics.DrawLine(Pens.Black, 85, 0, 85, this.Height);
            e.Graphics.DrawLine(Pens.Black, 567, 0, 567, this.Height);
            e.Graphics.DrawString("0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  0123456789ABCDEF", this.Font, Brushes.Gray, 95, 0);
            dataStream.Position = scrollBar.Value * 16;
            while (yPos < this.Height && dataStream.Position != dataStream.Length)
            {
                fileOffset = dataStream.Position.ToString("X");
                diff = (int)(dataStream.Length - dataStream.Position);
                times = diff >= 16 ? 16 : diff;
                for (int currColumn = 0; currColumn < times; currColumn++)
                {
                    readByte = dataReader.ReadByte();
                    if (readByte >= 16)
                    {
                        if (!Utils.IsPrintableASCIIChar(readByte))
                        {
                            asciiString.Append(".");
                        }
                        else
                        {
                            asciiString.Append((char)readByte);
                        }
                        bytesString.Append(readByte.ToString("X") + " ");
                    }
                    else
                    {
                        bytesString.Append("0" + readByte.ToString("X") + " ");
                        asciiString.Append(".");
                    }
                }
                e.Graphics.DrawString(bytesString.ToString(), this.Font, Brushes.Black, 90, yPos);
                e.Graphics.DrawString(asciiString.ToString(), this.Font, Brushes.Black, 570, yPos);
                e.Graphics.DrawString(new String('0', 8 - fileOffset.Length) + fileOffset, this.Font, Brushes.Gray, 0, yPos);
                bytesString.Length = 0;
                asciiString.Length = 0;
                yPos += 20;
            }
        }
        private void RememberFieldContainer(FieldContainerInfo fieldContainer, List<FieldContainerInfo> paintingFieldContainers, Graphics graphics)
        {
            FieldInfo currField;
            paintingFieldContainers.Add(fieldContainer);
            for (int currFieldIndex = 0; currFieldIndex < fieldContainer.FieldsCount; currFieldIndex++)
            {
                currField = fieldContainer.GetField(currFieldIndex);
                if (currField.GetType() == typeof(FieldContainerInfo))
                {
                    RememberFieldContainer((FieldContainerInfo)currField, paintingFieldContainers, graphics);
                }
            }
        }
        private RegionPaintInfo PreparePaintRegion(int regionStart, int regionLength, bool drawHidden)
        {
            int regionEnd = regionStart + regionLength;
            int lastRowRow = regionEnd / 16;
            if (lastRowRow >= scrollBar.Value) //Is the last row hidden?
            {
                //No it isn't, therefore we have to draw at least 1 row
                int firstRowColumn = regionStart % 16;
                int firstRowRow = regionStart / 16;
                if (!drawHidden && firstRowRow < scrollBar.Value) //Is the first row hidden?
                {
                    //Yes it is, this means there is a possibility of other rows being hidden as well. Let's change the region so that it is drawn in the visible area
                    int hiddenRows = scrollBar.Value - firstRowRow;
                    regionLength = regionLength - (16 - firstRowColumn) - ((hiddenRows - 1) * 16);
                    firstRowRow += hiddenRows;
                    firstRowColumn = 0;
                }
                int lastRowColumn = regionEnd % 16;
                int yStart = 25 + ((firstRowRow - scrollBar.Value) * HEXSPACING_HEIGHT);
                return new RegionPaintInfo(new RegionRow(firstRowRow, firstRowColumn), new RegionRow(lastRowRow, lastRowColumn), regionLength, yStart);
            }
            return null;
        }
        private void FillRectangleRegion(RegionPaintInfo paintInfo, Brush brush, Graphics graphics)
        {
            switch (paintInfo.DrawingRows)
            {
                default: //region.DrawingRows > 2
                    if (paintInfo.DrawEntireFirstRow && paintInfo.DrawEntireLastRow)
                    {
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - RECTFILL_OFFSET, HEXSPACING_HEIGHT * paintInfo.DrawingRows);
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * paintInfo.DrawingRows);
                    }
                    else if (paintInfo.DrawEntireFirstRow)
                    {
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - RECTFILL_OFFSET, HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 1));
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart + ((paintInfo.DrawingRows - 1) * HEXSPACING_HEIGHT), (HEXBYTES_WIDTH * paintInfo.LastRowColumn) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);

                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 1));
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart + ((paintInfo.DrawingRows - 1) * HEXSPACING_HEIGHT), CHARWIDTH * paintInfo.LastRowColumn, HEXSPACING_HEIGHT);
                    }
                    else if (paintInfo.DrawEntireLastRow)
                    {
                        graphics.FillRectangle(brush, GetCaretHexXPos(paintInfo.FirstRowColumn), paintInfo.YStart, (HEXBYTES_WIDTH * (16 - paintInfo.FirstRowColumn)) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart + HEXSPACING_HEIGHT, (HEXBYTES_WIDTH * 16) - RECTFILL_OFFSET, HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 1));

                        graphics.FillRectangle(brush, GetCaretTextXPos(paintInfo.FirstRowColumn), paintInfo.YStart, CHARWIDTH * (16 - paintInfo.FirstRowColumn), HEXSPACING_HEIGHT);
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart + HEXSPACING_HEIGHT, CHARWIDTH * 16, HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 1));
                    }
                    else
                    {
                        //Cannot be batched
                        graphics.FillRectangle(brush, GetCaretHexXPos(paintInfo.FirstRowColumn), paintInfo.YStart, (HEXBYTES_WIDTH * (16 - paintInfo.FirstRowColumn)) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart + HEXSPACING_HEIGHT, (HEXBYTES_WIDTH * 16) - RECTFILL_OFFSET, HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 2));
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart + ((paintInfo.DrawingRows - 1) * HEXSPACING_HEIGHT), (HEXBYTES_WIDTH * paintInfo.LastRowColumn) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);

                        graphics.FillRectangle(brush, GetCaretTextXPos(paintInfo.FirstRowColumn), paintInfo.YStart, CHARWIDTH * (16 - paintInfo.FirstRowColumn), HEXSPACING_HEIGHT);
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart + HEXSPACING_HEIGHT, CHARWIDTH * 16, HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 2));
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart + ((paintInfo.DrawingRows - 1) * HEXSPACING_HEIGHT), CHARWIDTH * paintInfo.LastRowColumn, HEXSPACING_HEIGHT);
                    }
                break;
                case 1:
                    graphics.FillRectangle(brush, GetCaretHexXPos(paintInfo.FirstRowColumn), paintInfo.YStart, (HEXBYTES_WIDTH * paintInfo.RegionLength) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);
                    graphics.FillRectangle(brush, GetCaretTextXPos(paintInfo.FirstRowColumn), paintInfo.YStart, CHARWIDTH * paintInfo.RegionLength, HEXSPACING_HEIGHT);
                break;
                case 2:
                    if (paintInfo.DrawEntireFirstRow && paintInfo.DrawEntireLastRow)
                    {
                        //Can be batched
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - RECTFILL_OFFSET, HEXSPACING_HEIGHT * 2);
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * 2);
                    }
                    else
                    {
                        int lastRowColumn;
                        if (!paintInfo.DrawEntireLastRow)
                        {
                            lastRowColumn = paintInfo.LastRowColumn;
                        }
                        else
                        {
                            lastRowColumn = 16;
                        }
                        graphics.FillRectangle(brush, GetCaretHexXPos(paintInfo.FirstRowColumn), paintInfo.YStart, (HEXBYTES_WIDTH * (16 - paintInfo.FirstRowColumn)) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);
                        graphics.FillRectangle(brush, CaretHexXPos0, paintInfo.YStart + HEXSPACING_HEIGHT, (HEXBYTES_WIDTH * lastRowColumn) - RECTFILL_OFFSET, HEXSPACING_HEIGHT);

                        graphics.FillRectangle(brush, GetCaretTextXPos(paintInfo.FirstRowColumn), paintInfo.YStart, CHARWIDTH * (16 - paintInfo.FirstRowColumn), HEXSPACING_HEIGHT);
                        graphics.FillRectangle(brush, CaretTextXPos0, paintInfo.YStart + HEXSPACING_HEIGHT, CHARWIDTH * lastRowColumn, HEXSPACING_HEIGHT);
                    }
                break;
            }
        }
        private void DrawRectangleRegion(RegionPaintInfo paintInfo, int level, Pen pen, Graphics graphics)
        {
            switch (paintInfo.DrawingRows)
            {
                default: //region.DrawingRows > 2
                    DrawFirstRow(pen, paintInfo, graphics);
                    DrawMiddleRows(pen, paintInfo, graphics);
                    DrawLastRow(pen, paintInfo, graphics);
                    /*
                    if (paintInfo.DrawEntireFirstRow && paintInfo.DrawEntireLastRow)
                    {
                        graphics.DrawRectangle(pen, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - 0, HEXSPACING_HEIGHT * paintInfo.DrawingRows);
                        graphics.DrawRectangle(pen, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * paintInfo.DrawingRows);
                    }
                    else
                    {
                    }
                    */
                break;
                case 1:
                    graphics.DrawRectangle(pen, GetCaretHexXPos(paintInfo.FirstRowColumn), paintInfo.YStart, (HEXBYTES_WIDTH * paintInfo.RegionLength), HEXSPACING_HEIGHT);
                    graphics.DrawRectangle(pen, GetCaretTextXPos(paintInfo.FirstRowColumn), paintInfo.YStart, CHARWIDTH * paintInfo.RegionLength, HEXSPACING_HEIGHT);
                break;
                case 2:
                    DrawFirstRow(pen, paintInfo, graphics);
                    if (paintInfo.LastRowColumn <= paintInfo.FirstRowColumn) //Do they meet each other? ie: Do they merge?
                    {
                        //No they don't. We need to draw a line to separate them
                        int yPos = paintInfo.YStart + HEXSPACING_HEIGHT;
                        graphics.DrawLine(pen, RECTDRAW_HEXSTART, yPos, RECTDRAW_HEXEND, yPos);
                        graphics.DrawLine(pen, RECTDRAW_TEXTSTART, yPos, RECTDRAW_TEXTEND, yPos);
                    }
                    DrawLastRow(pen, paintInfo, graphics);
                break;
            }
        }
        private void DrawFirstRow(Pen pen, RegionPaintInfo paintInfo, Graphics graphics)
        {
            int hexEndXPos;
            int textEndXPos;
            int hexStartXPos;
            int textStartXPos;
            int yPos = paintInfo.YStart - 1;
            if (yPos > 0) //Will this row be visible at all?
            {
                int endYPos = yPos + HEXSPACING_HEIGHT;
                if (yPos < 20) //Does it start in the visible area?
                {
                    //No it doesn't lets clamp the start
                    yPos = 20;
                }
                if (!paintInfo.DrawEntireFirstRow)
                {
                    int length = (16 - paintInfo.FirstRowColumn);
                    hexStartXPos = GetCaretHexXPos(paintInfo.FirstRowColumn) - 1;
                    textStartXPos = GetCaretTextXPos(paintInfo.FirstRowColumn) - 1;
                    hexEndXPos = hexStartXPos + (length * HEXBYTES_WIDTH) - RECTDRAW_OFFSET + 1;
                    textEndXPos = textStartXPos + (length * CHARWIDTH) + 1;
                }
                else
                {
                    textStartXPos = CaretTextXPos0 - 1;
                    hexStartXPos = CaretHexXPos0 - 1;
                    textEndXPos = CaretTextXPos16;
                    hexEndXPos = CaretHexXPos16;
                }
                graphics.DrawLine(pen, hexStartXPos, yPos, hexEndXPos, yPos); //Top line
                graphics.DrawLine(pen, hexStartXPos, yPos, hexStartXPos, endYPos); //Left line
                graphics.DrawLine(pen, hexEndXPos, yPos, hexEndXPos, endYPos); //Right line

                graphics.DrawLine(pen, textStartXPos, yPos, textEndXPos, yPos); //Top line
                graphics.DrawLine(pen, textStartXPos, yPos, textStartXPos, endYPos); //Left line
                graphics.DrawLine(pen, textEndXPos, yPos, textEndXPos, endYPos); //Right line
            }
        }
        private void DrawMiddleRows(Pen pen, RegionPaintInfo paintInfo, Graphics graphics)
        {
            int yStart = paintInfo.YStart + HEXSPACING_HEIGHT - 1;
            if (yStart > 0) //Will this row be visible at all?
            {
                int endYPos = yStart + ((paintInfo.DrawingRows - 2) * HEXSPACING_HEIGHT) + 1;
                if (yStart < 20) //Does it start in the visible area?
                {
                    //No it doesn't lets clamp the start
                    yStart = 20;
                }
                if (paintInfo.FirstRowColumn != 0)
                {
                    graphics.DrawLine(pen, CaretHexXPos0, yStart, CaretHexXPos0 + (paintInfo.FirstRowColumn * HEXBYTES_WIDTH) - RECTDRAW_OFFSET + 1, yStart); //Top line
                    graphics.DrawLine(pen, CaretTextXPos0, yStart, CaretTextXPos0 + (paintInfo.FirstRowColumn * CHARWIDTH) - 2, yStart); //Top line
                }
                if (paintInfo.LastRowColumn != 0)
                {
                    graphics.DrawLine(pen, GetCaretHexXPos(paintInfo.LastRowColumn) - 1, endYPos, CaretHexXPos16 - RECTDRAW_OFFSET, endYPos); //Bottom line
                    graphics.DrawLine(pen, GetCaretTextXPos(paintInfo.LastRowColumn), endYPos, RECTDRAW_TEXTEND, endYPos); //Bottom line
                }
                graphics.DrawLine(pen, RECTDRAW_HEXSTART, yStart, RECTDRAW_HEXSTART, endYPos); //Left line
                graphics.DrawLine(pen, RECTDRAW_HEXEND, yStart, RECTDRAW_HEXEND, endYPos); //Right line

                graphics.DrawLine(pen, RECTDRAW_TEXTSTART, yStart, RECTDRAW_TEXTSTART, endYPos); //Left line
                graphics.DrawLine(pen, RECTDRAW_TEXTEND, yStart, RECTDRAW_TEXTEND, endYPos); //Right line
            }
        }
        private void DrawLastRow(Pen pen, RegionPaintInfo paintInfo, Graphics graphics)
        {
            int hexEndXPos = RECTDRAW_HEXSTART + (HEXBYTES_WIDTH * paintInfo.LastRowColumn) - 2;
            int textEndXPos = RECTDRAW_TEXTSTART + (CHARWIDTH * paintInfo.LastRowColumn) + 1;
            int yPos = paintInfo.YStart + (HEXSPACING_HEIGHT * (paintInfo.DrawingRows - 1));
            int endYPos = yPos + HEXSPACING_HEIGHT;
            graphics.DrawLine(pen, RECTDRAW_HEXSTART, endYPos, hexEndXPos, endYPos); //Bottom line
            graphics.DrawLine(pen, RECTDRAW_HEXSTART, yPos, RECTDRAW_HEXSTART, endYPos); //Left line
            graphics.DrawLine(pen, hexEndXPos, yPos, hexEndXPos, endYPos); //Right line

            graphics.DrawLine(pen, RECTDRAW_TEXTSTART, endYPos, textEndXPos, endYPos); //Bottom line
            graphics.DrawLine(pen, RECTDRAW_TEXTSTART, yPos, RECTDRAW_TEXTSTART, endYPos); //Left line
            graphics.DrawLine(pen, textEndXPos, yPos, textEndXPos, endYPos); //Right line
        }
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
                case Keys.Control | Keys.Right:
                case Keys.Control | Keys.Left:
                case Keys.Control | Keys.Up:
                case Keys.Control | Keys.Down:
                    return true;
                case Keys.Control | Keys.Shift | Keys.Right:
                case Keys.Control | Keys.Shift | Keys.Left:
                case Keys.Control | Keys.Shift | Keys.Up:
                case Keys.Control | Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }
        protected override void OnGotFocus(EventArgs e)
        {
            CreateCaret(this.Handle, IntPtr.Zero, 2, HEXSPACING_HEIGHT);
            virtualCaret.Visible = true;
            ShowCaret(this.Handle);
            UpdateCaretPositions();
            caretHidden = false;
            base.OnGotFocus(e);
        }
        protected override void OnLostFocus(EventArgs e)
        {
            virtualCaret.Visible = false;
            DestroyCaret();
            caretHidden = true;
            base.OnLostFocus(e);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            dataStream.Dispose();
            dataReader.Close();
        }
        public event DataChangedHandler DataChanged;
        public event EventHandler SelectionChanged;
        public event ScriptDoneHandler ScriptDone;
    }
}
