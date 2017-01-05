using System;
using System.IO;
using System.Drawing;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public partial class MainForm : Form
    {
        IFieldContainer rootFields;
        List<string> recentFilesList;
        public MainForm()
        {
            InitializeComponent();
            this.recentFilesList = new List<string>();
            this.hexEditor1.ScriptDone += HexEditor1_ScriptDone;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            using (RegistryKey appRegistryKey = Registry.CurrentUser.OpenSubKey("Software\\HiT\\ScriptableHexEditor"))
            {
                if (appRegistryKey != null)
                {
                    object registryValue;
                    for (int currRecentFileNumber = 1; currRecentFileNumber <= 10; currRecentFileNumber++)
                    {
                        registryValue = appRegistryKey.GetValue("RecentFile" + currRecentFileNumber);
                        if (registryValue != null)
                        {
                            AddPathToRecentFiles(registryValue.ToString());
                        }
                    }
                    if (recentFilesList.Count == 0)
                    {
                        separator3.Visible = false;
                    }
                }
                else
                {
                    separator3.Visible = false;
                }
            }
            if (!Directory.Exists("Scripts\\"))
            {
                Directory.CreateDirectory("Scripts\\");
            }
            foreach (var currScriptFile in Directory.GetFiles("Scripts\\", "*.lua"))
            {
                ToolStripMenuItem scriptFileMenuItem = new ToolStripMenuItem(Path.GetFileName(currScriptFile));
                scriptFileMenuItem.Click += ScriptFileMenuItem_Click;
                runScriptMenuItem.DropDownItems.Add(scriptFileMenuItem);
            }
            hexEditor1.Select();
        }
        private void AddPathToRecentFiles(string path)
        {
            ToolStripMenuItem recentFileMenuItem = new ToolStripMenuItem(path);
            fileMenuItem.DropDownItems.Insert(separator3.MergeIndex, recentFileMenuItem);
            //fileMenuItem.DropDownItems.IndexOf(separator3)
            recentFilesList.Add(path);
        }
        private void ScriptFileMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem scriptFileMenuItem = (ToolStripMenuItem)sender;
            hexEditor1.RunScript("Scripts\\" + scriptFileMenuItem.Text);
        }
        private void HexEditor1_ScriptDone(IFieldContainer rootFields)
        {
            fieldsView.Nodes.Clear();
            this.rootFields = rootFields;
            AddFieldsToNode(fieldsView.Nodes, rootFields);
        }
        private void AddFieldsToNode(TreeNodeCollection targetNodes, IFieldContainer fieldsContainer)
        {
            TreeNode currFieldNode;
            FieldInfo currField;
            for (int currFieldIndex = 0; currFieldIndex < fieldsContainer.FieldsCount; currFieldIndex++)
            {
                currField = fieldsContainer.GetField(currFieldIndex);
                currFieldNode = targetNodes.Add(currField.Name);
                if (currField.GetType() == typeof(FieldContainerInfo))
                {
                    AddFieldsToNode(currFieldNode.Nodes, (FieldContainerInfo)currField);
                }
            }
        }
        private void fieldsView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            IFieldContainer currFieldContainer = rootFields;
            Stack<int> fieldIndices = new Stack<int>();
            TreeNode currNode = e.Node;
            while (currNode != null)
            {
                fieldIndices.Push(currNode.Index);
                currNode = currNode.Parent;
            }
            while (fieldIndices.Count > 1)
            {
                currFieldContainer = (IFieldContainer)currFieldContainer.GetField(fieldIndices.Pop());
            }
            hexEditor1.SelectionStart = currFieldContainer.GetField(fieldIndices.Pop()).FileOffset;
        }
        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            string recentFileID;
            RegistryKey appRegistryKey = Registry.CurrentUser.OpenSubKey("Software\\HiT\\ScriptableHexEditor", true);
            if (appRegistryKey == null)
            {
                appRegistryKey = Registry.CurrentUser.CreateSubKey("Software\\HiT\\ScriptableHexEditor");
            }
            for (int currRecentFileNumber = 1; currRecentFileNumber <= 10; currRecentFileNumber++)
            {
                recentFileID = "RecentFile" + currRecentFileNumber;
                if (appRegistryKey.GetValue(recentFileID) != null)
                {
                    appRegistryKey.DeleteValue(recentFileID);
                }
            }
            for (int currRecentFileIndex = 0; currRecentFileIndex < recentFilesList.Count; currRecentFileIndex++)
            {
                appRegistryKey.SetValue("RecentFile" + (currRecentFileIndex + 1), recentFilesList[currRecentFileIndex], RegistryValueKind.String);
            }
            appRegistryKey.Close();
            appRegistryKey = null;
        }
    }
}
