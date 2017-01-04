using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            hexEditor1.changed += HexEditor1_changed;
            for (int i = 0; i < 25; i++)
            {
                treeView1.Nodes.Add("Some field");
            }
            hexEditor1.Select();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void HexEditor1_changed(int newValue)
        {
            this.Text = hexEditor1.SelectionStart + " -> " + (hexEditor1.SelectionStart + hexEditor1.SelectionLength) + " | " + newValue.ToString();
        }
    }
}
