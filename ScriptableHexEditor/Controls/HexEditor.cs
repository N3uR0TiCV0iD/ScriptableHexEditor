using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
namespace ScriptableHexEditor
{
    //Lots and lots of magic numbers here... Be prepared :^)
    public partial class HexEditor : Control
    {
        //TODO: Fields background/rectangle highlight (Can be disabled) | Fields tree view
        [DllImport("user32.dll")] private static extern bool CreateCaret(IntPtr hwnd, IntPtr hBMP, int w, int h);
        [DllImport("user32.dll")] private static extern bool SetCaretPos(int x, int y);
        [DllImport("user32.dll")] private static extern bool ShowCaret(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool HideCaret(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool DestroyCaret();
        const int HEXBYTES_WIDTH = CHARWIDTH * 3;
        const int HEXSPACING_HEIGHT = 20;
        const int CHARWIDTH = 10;
        BinaryReader dataReader;
        MemoryStream dataStream;
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
            this.dataStream = new MemoryStream();
            this.scrollBar.Dock = DockStyle.Right;
            this.SelectionColor = SystemColors.Highlight;
            filePath = "ScriptableHexEditor.vshost.exe.manifest"; //TMP
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
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
                selectionStart = value;
                if (this.Focused && !caretHidden)
                {
                    SetCaretPos(GetCaretHexXPos(value % 16), GetCaretYPos(value / 16));
                }
                OnSelectionChanged(false);
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
                selectionEnd = selectionStart + value;
                OnSelectionChanged(true);
            }
        }
        public void RunScript(string scriptPath)
        {

        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Y >= 20 && e.X >= 85)
            {
                this.Cursor = Cursors.IBeam;
                if (mouseSelecting)
                {
                    selectionEnd = ComputeCaretPosition(e.X, e.Y);
                    OnSelectionChanged(true);
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
                    selectionStart = ComputeCaretPosition(e.X, e.Y);
                    mouseSelecting = e.Button == MouseButtons.Left;
                    OnSelectionChanged(false);
                }
                else if (e.Clicks == 2 && e.Button == MouseButtons.Left)
                {
                    //In Offset Area
                    selectionStart = 16 * ((e.Y - 30) / HEXSPACING_HEIGHT);
                    selectionEnd = selectionStart + 16;
                    OnSelectionChanged(true);
                }
            }
            this.Select();
        }
        protected virtual void OnSelectionChanged(bool selecting)
        {
            if (!selecting)
            {
                selectionEnd = selectionStart;
                if (inHexArea)
                {
                    SetCaretPos(GetCaretHexXPos(selectionStart % 16), GetCaretYPos(selectionStart / 16));
                }
                else
                {
                    SetCaretPos(GetCaretTextXPos(selectionStart % 16), GetCaretYPos(selectionStart / 16));
                }
                SafeShowCaret();
            }
            else
            {
                SafeHideCaret();
            }
            SelectionChanged?.Invoke(this, null);
            this.selecting = selecting;
            wroteNibble = false;
            this.Refresh();
        }
        private void SafeShowCaret()
        {
            if (caretHidden)
            {
                ShowCaret(this.Handle);
                caretHidden = false;
            }
        }
        private void SafeHideCaret()
        {
            if (!caretHidden)
            {
                HideCaret(this.Handle);
                caretHidden = true;
            }
        }
        private int ComputeCaretPosition(int xPos, int yPos)
        {
            int caretPosition = 16 * ((yPos - 30) / HEXSPACING_HEIGHT);
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
            int newValue;
            if (e.Delta > 0)
            {
                //Scroll Up
                newValue = this.scrollBar.Value - 1;
                if (newValue > this.scrollBar.Minimum)
                {
                    this.scrollBar.Value = newValue;
                }
            }
            else
            {
                //Scroll Down
                newValue = this.scrollBar.Value + 1;
                if (newValue < this.scrollBar.Maximum)
                {
                    this.scrollBar.Value = newValue;
                }
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    if (ModifierKeys != Keys.Shift)
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
                    if (ModifierKeys != Keys.Shift)
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
                    if (ModifierKeys != Keys.Shift)
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
                    if (ModifierKeys != Keys.Shift)
                    {
                        if (selectionEnd <= (dataStream.Length - 16))
                        {
                            selectionStart = selectionEnd + 16;
                        }
                        else
                        {
                            selectionStart = (int)dataStream.Length;
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
                case Keys.PageUp:
                break;
                case Keys.PageDown:
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
            e.Graphics.FillRectangle(SystemBrushes.Control, 0, 0, this.Width, this.Height);
            e.Graphics.FillRectangle(Brushes.White, 85, 20, this.Width - 85, this.Height - 20);
            if (selectionLength != 0)
            {
                const int HIGHLIGHT_XOFFSET = 7;
                int selectedRows;
                int realSelectionEnd;
                int realSelectionStart;
                if (selectionLength > 0)
                {
                    //Forward selection
                    realSelectionStart = selectionStart;
                    realSelectionEnd = selectionEnd;
                }
                else
                {
                    //Backwards selection
                    selectionLength = -selectionLength;
                    realSelectionStart = selectionEnd;
                    realSelectionEnd = selectionStart;
                }
                int lastRowColumn = realSelectionEnd % 16;
                int firstRowColumn = realSelectionStart % 16;
                int firstRowRow = realSelectionStart / 16;
                bool isFirstRowFullySelected = firstRowColumn == 0 && selectionLength >= 16;
                bool isLastRowFullySelected = lastRowColumn == 0;
                int yStart = 25 + (firstRowRow * HEXSPACING_HEIGHT);
                selectedRows = (int)Math.Ceiling((firstRowColumn + selectionLength) / 16F);
                switch (selectedRows)
                {
                    default: //selectedRows > 2
                        if (isFirstRowFullySelected && isLastRowFullySelected)
                        {
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart, (HEXBYTES_WIDTH * 16) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT * selectedRows);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * selectedRows);
                        }
                        else if (isFirstRowFullySelected)
                        {
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart, (HEXBYTES_WIDTH * 16) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT * (selectedRows - 1));
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart + ((selectedRows - 1) * HEXSPACING_HEIGHT), (HEXBYTES_WIDTH * lastRowColumn) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);

                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * (selectedRows - 1));
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart + ((selectedRows - 1) * HEXSPACING_HEIGHT), CHARWIDTH * lastRowColumn, HEXSPACING_HEIGHT);
                        }
                        else if (isLastRowFullySelected)
                        {
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(firstRowColumn), yStart, (HEXBYTES_WIDTH * (16 - firstRowColumn)) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart + HEXSPACING_HEIGHT, (HEXBYTES_WIDTH * 16) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT * (selectedRows - 1));

                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(firstRowColumn), yStart, CHARWIDTH * (16 - firstRowColumn), HEXSPACING_HEIGHT);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart + HEXSPACING_HEIGHT, CHARWIDTH * 16, HEXSPACING_HEIGHT * (selectedRows - 1));
                        }
                        else
                        {
                            //Cannot be batched
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(firstRowColumn), yStart, (HEXBYTES_WIDTH * (16 - firstRowColumn)) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart + HEXSPACING_HEIGHT, (HEXBYTES_WIDTH * 16) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT * (selectedRows - 2));
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart + ((selectedRows - 1) * HEXSPACING_HEIGHT), (HEXBYTES_WIDTH * lastRowColumn) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);

                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(firstRowColumn), yStart, CHARWIDTH * (16 - firstRowColumn), HEXSPACING_HEIGHT);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart + HEXSPACING_HEIGHT, CHARWIDTH * 16, HEXSPACING_HEIGHT * (selectedRows - 2));
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart + ((selectedRows - 1) * HEXSPACING_HEIGHT), CHARWIDTH * lastRowColumn, HEXSPACING_HEIGHT);
                        }
                        break;
                    case 1:
                        e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(firstRowColumn), yStart, (HEXBYTES_WIDTH * selectionLength) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);
                        e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(firstRowColumn), yStart, CHARWIDTH * selectionLength, HEXSPACING_HEIGHT);
                    break;
                    case 2:
                        if (isFirstRowFullySelected && isLastRowFullySelected)
                        {
                            //Can be batched
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart, (HEXBYTES_WIDTH * 16) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT * 2);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart, CHARWIDTH * 16, HEXSPACING_HEIGHT * 2);
                        }
                        else
                        {
                            if (isLastRowFullySelected)
                            {
                                lastRowColumn = 16;
                            }
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(firstRowColumn), yStart, (HEXBYTES_WIDTH * (16 - firstRowColumn)) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretHexXPos(0), yStart + HEXSPACING_HEIGHT, (HEXBYTES_WIDTH * lastRowColumn) - HIGHLIGHT_XOFFSET, HEXSPACING_HEIGHT);

                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(firstRowColumn), yStart, CHARWIDTH * (16 - firstRowColumn), HEXSPACING_HEIGHT);
                            e.Graphics.FillRectangle(selectionBrush, GetCaretTextXPos(0), yStart + HEXSPACING_HEIGHT, CHARWIDTH * lastRowColumn, HEXSPACING_HEIGHT);
                        }
                    break;
                }
            }
            e.Graphics.DrawLine(Pens.Black, 0, 20, this.Width, 20);
            e.Graphics.DrawLine(Pens.Black, 85, 0, 85, this.Height);
            e.Graphics.DrawLine(Pens.Black, 567, 0, 567, this.Height);
            e.Graphics.DrawString("0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  0123456789ABCDEF", this.Font, Brushes.Gray, 95, 0);
            dataStream.Position = 0;
            while (yPos < this.Height && dataStream.Position != dataStream.Length)
            {
                fileOffset = (16 * ((yPos - 25) / 20)).ToString("X");
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
            /*
            for (int times2 = 0; times2 < 16; times2++)
            {
                e.Graphics.DrawLine(Pens.Black, 572 + (times2 * CHARWIDTH), 0, 572 + (times2 * CHARWIDTH), this.Height);
            }
            //*/
            /*
            for (int times3 = 0; times3 < 16; times3++)
            {
                const int xd = 80;
                e.Graphics.DrawLine(Pens.Black, xd + (times3 * HEXBYTES_WIDTH), 0, xd + (times3 * HEXBYTES_WIDTH), this.Height);
            }
            for (int times4 = 0; times4 < 20; times4++)
            {
                e.Graphics.DrawLine(Pens.Black, 0, 20 + (times4 * HEXSPACING_HEIGHT), this.Width, 20 + (times4 * HEXSPACING_HEIGHT));
            }
            //*/
        }
        private int GetCaretHexXPos(int caretColumn)
        {
            return 90 + (caretColumn * HEXBYTES_WIDTH) - (caretColumn / 6);
        }
        private int GetCaretTextXPos(int caretColumn)
        {
            return 572 + (caretColumn * CHARWIDTH);
        }
        private int GetCaretYPos(int caretRow)
        {
            return 25 + (caretRow * HEXSPACING_HEIGHT);
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
            }
            return base.IsInputKey(keyData);
        }
        protected override void OnGotFocus(EventArgs e)
        {
            CreateCaret(this.Handle, IntPtr.Zero, 2, HEXSPACING_HEIGHT);
            SetCaretPos(GetCaretHexXPos(selectionStart % 16), GetCaretYPos(selectionStart / 16));
            ShowCaret(this.Handle);
            base.OnGotFocus(e);
        }
        protected override void OnLostFocus(EventArgs e)
        {
            DestroyCaret();
            base.OnLostFocus(e);
        }
        public event EventHandler SelectionChanged;
    }
}
