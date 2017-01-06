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
    public delegate void ScriptDoneHandler(IFieldContainer rootFields);
    public partial class HexEditor : Control
    {
        public event DebugStringHandler DebugString; //TMP
        private void Debug(string message)
        {
            DebugString?.Invoke(message);
        }

        //TODO: Fields background/rectangle highlight (Can be disabled) | Fields tree view
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
        const int RECTDRAW_HEXSTART = CaretHexXPos0 - 1;
        const int RECTDRAW_TEXTSTART = CaretTextXPos0 - 1;
        const int CaretTextXPos16 = 572 + (16 * CHARWIDTH);
        const int RECTDRAW_HEXEND = CaretHexXPos16 - RECTDRAW_OFFSET;
        const int CaretHexXPos16 = 90 + (16 * HEXBYTES_WIDTH) - (16 / 6);
        static readonly int TRIPLECLICK_TIME = SystemInformation.DoubleClickTime / 2;
        Stopwatch doubleClickStopwatch;
        HexEditorScript usingScript;
        BinaryReader dataReader;
        MemoryStream dataStream;
        SimpleLine virtualCaret;
        VScrollBar scrollBar;
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
        public HexEditor()
        {
            int readBytes;
            byte[] buffer = new byte[4096];
            this.scrollBar = new VScrollBar();
            this.Controls.Add(this.scrollBar);
            this.virtualCaret = new SimpleLine();
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
            filePath = "C:\\Users\\Patrick\\AppData\\LocalLow\\DefaultCompany\\All-Mozart\\Sheets\\FRENCH SONG.msf";
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
        public int SelectionStart
        {
            get
            {
                return selectionStart;
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
                return selectionEnd - selectionStart;
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
                    retry = MessageBox.Show("LuaError: " + ex.Message, "ERROR: LuaError", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry;
                }
                catch (EndOfStreamException ex)
                {
                    MessageBox.Show("Warning: Something went wrong, are you sure this this is the correct file format?", "Warning: Bad format?", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                ScriptDone?.Invoke(usingScript);
                this.Refresh();
            }
        }
        public void FindHexBytes(string hexString)
        {

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
        public void OnSelectionChanged(bool selecting)
        {
            OnSelectionChanged(selecting, false);
        }
        protected virtual void OnSelectionChanged(bool selecting, bool outside)
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
            focusRow = (!outside ? selectionEnd : selectionStart) / 16;
            if (focusRow < scrollBar.Value)
            {
                scrollBar.Value = focusRow;
            }
            else if (focusRow > (lastVisibleRow - 1))
            {
                scrollBar.Value = focusRow - displayableRows + 1;
            }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
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
            int moveAmount;
            switch (e.KeyCode)
            {
                case Keys.Right:
                    if (!e.Control)
                    {
                        moveAmount = 1;
                    }
                    else
                    {
                        //TODO: Find the amount of bytes until the start of a next field and do moveAmount = ...
                    }
                    if (!e.Shift)
                    {
                        if (!selecting)
                        {
                            if (selectionStart != dataStream.Length)
                            {
                                selectionStart++;
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
                        selectionEnd++;
                        OnSelectionChanged(true);
                    }
                break;
                case Keys.Left:
                    if (!e.Control)
                    {
                        moveAmount = 1;
                    }
                    else
                    {
                        //TODO: Find the amount of bytes until the start of a previous field and do moveAmount = ...
                    }
                    if (!e.Shift)
                    {
                        if (selectionEnd >= selectionStart)
                        {
                            if (selectionEnd != 0)
                            {
                                selectionStart = selectionEnd - 1;
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
                        selectionEnd--;
                        OnSelectionChanged(true);
                    }
                break;
                case Keys.Up:
                    if (!e.Control)
                    {
                        moveAmount = 16;
                    }
                    else
                    {
                        //TODO: Find the amount of bytes until the start of a previous structure and do moveAmount = ...
                    }
                    if (!e.Shift)
                    {
                        if (selectionEnd >= 16)
                        {
                            selectionStart = selectionEnd - 16;
                        }
                        else
                        {
                            selectionStart = 0;
                        }
                        OnSelectionChanged(false);
                    }
                    else if (selectionEnd >= 16)
                    {
                        selectionEnd -= 16;
                        OnSelectionChanged(true);
                    }
                break;
                case Keys.Down:
                    if (!e.Control)
                    {
                        moveAmount = 16;
                    }
                    else
                    {
                        //TODO: Find the amount of bytes until the start of a next structure and do moveAmount = ...
                    }
                    if (!e.Shift)
                    {
                        if (selectionEnd <= (dataStream.Length - 16))
                        {
                            selectionStart = selectionEnd + 16;
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
                        if (selectionEnd <= (dataStream.Length - 16))
                        {
                            selectionEnd += 16;
                        }
                        else
                        {
                            selectionEnd = (int)dataStream.Length;
                        }
                        OnSelectionChanged(true);
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
                        OnSelectionChanged(true);
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
                        OnSelectionChanged(true);
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
            int newByteValue;
            firstNibble = nibbleValue << 4;
            if (selectionStart != dataStream.Length)
            {
                dataStream.Position = selectionStart;
                newByteValue = firstNibble | (dataStream.ReadByte() & 0x0F);
            }
            else
            {
                newByteValue = firstNibble;
            }
            dataStream.Position = selectionStart;
            SetCaretPos(GetCaretHexXPos(selectionStart % 16) + CHARWIDTH, GetCaretYPos(selectionStart / 16)); //Moves caret to the next nibble
            dataStream.WriteByte((byte)newByteValue);
            wroteNibble = true;
            this.Refresh();
        }
        private void UpdateLastNibble(int nibbleValue)
        {
            dataStream.Position = selectionStart;
            dataStream.WriteByte((byte)(firstNibble | nibbleValue));
            selectionStart++;
            OnSelectionChanged(false);
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
            int diff;
            int times;
            int yPos = 25;
            byte readByte;
            string fileOffset;
            int caretRow = selectionStart / 16;
            int caretColumn = selectionStart % 16;
            StringBuilder bytesString = new StringBuilder();
            StringBuilder asciiString = new StringBuilder();
            int selectionLength = selectionEnd - selectionStart;
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
                        PaintFieldContainer((FieldContainerInfo)currField, e.Graphics);
                    }
                }
            }
            if (selectionLength != 0)
            {
                int realSelectionStart;
                RegionPaintInfo paintInfo;
                if (selectionLength > 0)
                {
                    //Forward selection
                    realSelectionStart = selectionStart;
                }
                else
                {
                    //Backwards selection
                    selectionLength = -selectionLength;
                    realSelectionStart = selectionEnd;
                }
                paintInfo = PreparePaintRegion(realSelectionStart, selectionLength, false);
                if (paintInfo != null)
                {
                    FillRectangleRegion(paintInfo, selectionBrush, e.Graphics);
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
                        if (readByte < 32 || readByte > 126)
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
        private void PaintFieldContainer(FieldContainerInfo fieldContainer, Graphics graphics)
        {
            FieldInfo currField;
            bool regionVisible = false;
            if (fieldContainer.BackgroundBrush != null)
            {
                RegionPaintInfo fillPaintInfo = PreparePaintRegion(fieldContainer.FileOffset, fieldContainer.Length, false);
                if (fillPaintInfo != null)
                {
                    FillRectangleRegion(fillPaintInfo, fieldContainer.BackgroundBrush, graphics);
                    regionVisible = true;
                }
            }
            if (fieldContainer.RectanglePen != null)
            {
                RegionPaintInfo drawPaintInfo = PreparePaintRegion(fieldContainer.FileOffset, fieldContainer.Length, true);
                if (regionVisible || drawPaintInfo != null)
                {
                    DrawRectangleRegion(drawPaintInfo, 0, fieldContainer.RectanglePen, graphics);
                }
            }
            for (int currFieldIndex = 0; currFieldIndex < fieldContainer.FieldsCount; currFieldIndex++)
            {
                currField = fieldContainer.GetField(currFieldIndex);
                if (currField.GetType() == typeof(FieldContainerInfo))
                {
                    PaintFieldContainer((FieldContainerInfo)currField, graphics);
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
                    if (paintInfo.DrawEntireFirstRow && paintInfo.DrawEntireLastRow)
                    {
                        graphics.DrawRectangle(pen, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - 0, HEXSPACING_HEIGHT * paintInfo.DrawingRows);
                        graphics.DrawRectangle(pen, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * paintInfo.DrawingRows);
                    }
                    else
                    {
                        DrawFirstRow(pen, paintInfo, graphics);
                        DrawMiddleRows(pen, paintInfo, graphics);
                        DrawLastRow(pen, paintInfo, graphics);
                    }
                break;
                case 1:
                    graphics.DrawRectangle(pen, GetCaretHexXPos(paintInfo.FirstRowColumn), paintInfo.YStart, (HEXBYTES_WIDTH * paintInfo.RegionLength), HEXSPACING_HEIGHT);
                    graphics.DrawRectangle(pen, GetCaretTextXPos(paintInfo.FirstRowColumn), paintInfo.YStart, CHARWIDTH * paintInfo.RegionLength, HEXSPACING_HEIGHT);
                break;
                case 2:
                    if (paintInfo.DrawEntireFirstRow && paintInfo.DrawEntireLastRow)
                    {
                        //Can be batched
                        graphics.DrawRectangle(pen, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - 0, HEXSPACING_HEIGHT * 2);
                        graphics.DrawRectangle(pen, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * 2);
                    }
                    else if (paintInfo.LastRowColumn > paintInfo.FirstRowColumn) //Do they meet each other? ie: Do they merge?
                    {
                        //Yes they do!
                        DrawFirstRow(pen, paintInfo, graphics);
                        DrawLastRow(pen, paintInfo, graphics);
                    }
                    else
                    {
                        //No they don't
                        graphics.DrawRectangle(pen, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - 0, HEXSPACING_HEIGHT * 2);
                        graphics.DrawRectangle(pen, CaretHexXPos0, paintInfo.YStart, (HEXBYTES_WIDTH * 16) - 0, HEXSPACING_HEIGHT * 2);

                        graphics.DrawRectangle(pen, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * 2);
                        graphics.DrawRectangle(pen, CaretTextXPos0, paintInfo.YStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * 2);
                    }
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
                    graphics.DrawLine(pen, GetCaretTextXPos(paintInfo.LastRowColumn), endYPos, CaretTextXPos16, endYPos); //Bottom line
                }
                graphics.DrawLine(pen, RECTDRAW_HEXSTART, yStart, RECTDRAW_HEXSTART, endYPos); //Left line
                graphics.DrawLine(pen, RECTDRAW_HEXEND, yStart, RECTDRAW_HEXEND, endYPos); //Right line

                graphics.DrawLine(pen, RECTDRAW_TEXTSTART, yStart, RECTDRAW_TEXTSTART, endYPos); //Left line
                graphics.DrawLine(pen, CaretTextXPos16, yStart, CaretTextXPos16, endYPos); //Right line
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
        public event EventHandler SelectionChanged;
        public event ScriptDoneHandler ScriptDone;
    }
}
