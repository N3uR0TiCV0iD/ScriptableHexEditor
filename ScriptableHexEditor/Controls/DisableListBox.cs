using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class DisableListBox : ListBox
    {
        List<int> disabledItems; //Can't use HashSet since we are using .NET 2.0 (Introduced in .NET 3.5)
        public DisableListBox()
        {
            this.disabledItems = new List<int>();
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public void SetItemState(int index, bool enable)
        {
            bool isEnabled = !disabledItems.Contains(index);
            if (!enable)
            {
                //We want to disable it
                if (isEnabled) //Is it currently enabled?
                {
                    //Yes it is, we can mark it as disabled now
                    disabledItems.Add(index);
                    this.Invalidate();
                }
            }
            else if (!isEnabled) //We want to enable it. Is it currently disabled?
            {
                //Yes it is, we can mark it as enabled now
                disabledItems.Remove(index);
                this.Invalidate();
            }
        }
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            this.Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Brush usingBrush;
            if (this.Items.Count != 0)
            {
                int yPos;
                for (int currItemIndex = 0; currItemIndex < this.Items.Count; currItemIndex++)
                {
                    yPos = currItemIndex * this.ItemHeight;
                    if (currItemIndex != this.SelectedIndex)
                    {
                        usingBrush = !disabledItems.Contains(currItemIndex) ? SystemBrushes.WindowText : SystemBrushes.GrayText;
                    }
                    else
                    {
                        e.Graphics.FillRectangle(SystemBrushes.Highlight, 0, yPos, this.Width, this.ItemHeight);
                        usingBrush = SystemBrushes.HighlightText;
                    }
                    e.Graphics.DrawString(this.Items[currItemIndex].ToString(), this.Font, usingBrush, 0, yPos);
                }
            }
            else if (this.DesignMode) //&& this.Items.Count == 0
            {
                //Draw Control Name as an item is design mode
                e.Graphics.DrawString(this.Name, this.Font, SystemBrushes.WindowText, 0, 0);
            }
        }
    }
}
