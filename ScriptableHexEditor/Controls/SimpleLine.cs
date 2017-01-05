using System;
using System.Windows.Forms;
namespace ScriptableHexEditor
{
    public class SimpleLine : Control
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
        }
    }
}