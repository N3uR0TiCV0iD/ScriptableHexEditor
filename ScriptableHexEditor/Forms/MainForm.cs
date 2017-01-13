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
        const int PRIMITIVES_ENDINDEX = (int)FieldType.Enum - 1;
        static readonly string[] PRIMITIVE_STRINGS = new string[] {
            "byte\t   |  ", "sbyte\t   |  ", "bool\t   |  ",
            "short\t   |  ", "ushort\t   |  ", "half float\t   |  ",
            "int\t   |  ", "uint\t   |  ", "float\t   |  ",
            "long\t   |  ", "ulong\t   |  ", "double\t   |  ",
            "enum\t   |  "
        };
        List<string> recentFilesList;
        IFieldsContainer rootFields;
        bool skipNodeSelection;
        bool skipNodeFind;
        public MainForm()
        {
            InitializeComponent();
            this.recentFilesList = new List<string>();
            this.hexEditor1.ScriptDone += HexEditor_ScriptDone;
            //this.fieldsView.GotFocus += FieldsView_GotFocus;
            this.hexEditor1.DebugString += HexEditor1_DebugString; //TMP
            this.hexEditor1.GotFocus += HexEditor_GotFocus;
            this.fieldsView.MouseWheel += FieldsView_MouseWheel;
            this.hexEditor1.DataChanged += HexEditor_DataChanged;
            for (int currItemIndex = 0; currItemIndex < dataInspectorBox.Items.Count; currItemIndex++)
            {
                dataInspectorBox.SetItemState(currItemIndex, false);
            }
        }
        //TMP
        private void HexEditor1_DebugString(string message)
        {
            this.Text += " " + message;
        }
        //END TMP
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
            runScriptMenuItem.Enabled = runScriptMenuItem.DropDownItems.Count != 0;
            //TMP
            hexEditor1.Select();
            //END TMP
        }
        private void AddPathToRecentFiles(string path)
        {
            ToolStripMenuItem recentFileMenuItem = new ToolStripMenuItem(path);
            fileMenuItem.DropDownItems.Insert(separator3.MergeIndex, recentFileMenuItem);
            //fileMenuItem.DropDownItems.IndexOf(separator3)
            recentFilesList.Add(path);
        }
        private void HexEditor_GotFocus(object sender, EventArgs e)
        {
            UpdateListBox((HexEditor)sender);
        }
        private void HexEditor_SelectionChanged(object sender, EventArgs e)
        {
            HexEditor hexEditor = (HexEditor)sender;
            FieldInfo selectedField = hexEditor.SelectedField;
            UpdateListBox(hexEditor, selectedField);
            if (!skipNodeFind)
            {
                FocusField(selectedField);
            }
        }
        private void UpdateListBox(HexEditor hexEditor)
        {
            UpdateListBox(hexEditor, hexEditor.SelectedField);
        }
        private void UpdateListBox(HexEditor hexEditor, FieldInfo selectedField)
        {
            if (selectedField == null) //Is he on a field? (Usually happens when a script has been loaded)
            {
                //No he isn't (Unusual when a script is loaded). Let's use the SelectionLength as a reference
                int enabledEnd;
                byte[] bulkRead;
                int dataTypeLength;
                if (hexEditor.SelectionLength != 0)
                {
                    switch (hexEditor.SelectionLength) //Clamps the selection
                    {
                        //Byte selection
                        case 1:
                            dataTypeLength = 1;
                        break;
                        //Short selection
                        case 2:
                        case 3:
                            dataTypeLength = 2;
                        break;
                        //Int|Float selection
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                            dataTypeLength = 4;
                        break;
                        //Long|Double|Bytes|No selection
                        default:
                            dataTypeLength = 8;
                        break;
                    }
                    bulkRead = hexEditor.BulkRead(dataTypeLength);
                }
                else
                {
                    bulkRead = hexEditor.BulkRead();
                    if (bulkRead != null)
                    {
                        dataTypeLength = bulkRead.Length;
                    }
                    else
                    {
                        dataTypeLength = 0;
                    }
                }
                if (dataTypeLength != 0)
                {
                    enabledEnd = ((int)Math.Log(dataTypeLength, 2) + 1) * 3;
                    UpdateListBoxValues(bulkRead);
                }
                else
                {
                    enabledEnd = 0;
                }
                for (int currTypeIndex = 0; currTypeIndex < enabledEnd; currTypeIndex++)
                {
                    dataInspectorBox.SetItemState(currTypeIndex, true);
                }
                for (int currTypeIndex = enabledEnd; currTypeIndex <= PRIMITIVES_ENDINDEX; currTypeIndex++)
                {
                    dataInspectorBox.SetItemState(currTypeIndex, false);
                }
                dataInspectorBox.Items[12] = PRIMITIVE_STRINGS[12];
                dataInspectorBox.SetItemState(12, false);
            }
            else if (selectedField.Type != FieldType.CString && selectedField.Type != FieldType.UTFString) //Yes he is! Let's use the field's length as a reference
            {                                                                                              //if it is a "data inspector worthy" type :P
                bool isEnum = selectedField.Type == FieldType.Enum;
                byte[] fieldBytes = hexEditor.ReadField(selectedField);
                int fieldIndex = !isEnum ? (int)selectedField.Type : 6; //(*) First we record which field won't get reset
                UpdateListBoxValues(fieldBytes);                        //(*) Secondly we update the values based on the field's bytes
                for (int currTypeIndex = 0; currTypeIndex < fieldIndex; currTypeIndex++)
                {
                    dataInspectorBox.Items[currTypeIndex] = PRIMITIVE_STRINGS[currTypeIndex]; //(*) Here we reset the values (Inefficient? kinda...) and states of all but the one we didn't want reset
                    dataInspectorBox.SetItemState(currTypeIndex, false);
                }
                dataInspectorBox.SetItemState(fieldIndex, true);
                for (int currTypeIndex = fieldIndex + 1; currTypeIndex <= PRIMITIVES_ENDINDEX; currTypeIndex++)
                {
                    dataInspectorBox.Items[currTypeIndex] = PRIMITIVE_STRINGS[currTypeIndex]; //(*) ^=
                    dataInspectorBox.SetItemState(currTypeIndex, false);
                }
                if (isEnum)
                {
                    EnumFieldInfo enumField = (EnumFieldInfo)selectedField;
                    dataInspectorBox.Items[12] = PRIMITIVE_STRINGS[12] + enumField.EnumName + "." + hexEditor.GetEnumKey(enumField, BitConverter.ToInt32(fieldBytes, 0));
                }
                else
                {
                    dataInspectorBox.Items[12] = PRIMITIVE_STRINGS[12];
                }
                dataInspectorBox.SetItemState(PRIMITIVES_ENDINDEX + 1, isEnum);
            }
            else
            {
                //It seems the field wasn't "data inspector worthy". Let's clear the values and disable all the items
                for (int currTypeIndex = 0; currTypeIndex < dataInspectorBox.Items.Count; currTypeIndex++)
                {
                    dataInspectorBox.Items[currTypeIndex] = PRIMITIVE_STRINGS[currTypeIndex];
                    dataInspectorBox.SetItemState(currTypeIndex, false);
                }
            }
        }
        private void UpdateListBoxValues(byte[] bulkRead)
        {
            //Byte selection
            if (bulkRead.Length >= 1)
            {
                dataInspectorBox.Items[0] = PRIMITIVE_STRINGS[0] + bulkRead[0];
                dataInspectorBox.Items[1] = PRIMITIVE_STRINGS[1] + unchecked((sbyte)bulkRead[0]);
                dataInspectorBox.Items[2] = PRIMITIVE_STRINGS[2] + (bulkRead[0] != 0);
            }
            else
            {
                dataInspectorBox.Items[0] = PRIMITIVE_STRINGS[0];
                dataInspectorBox.Items[1] = PRIMITIVE_STRINGS[1];
                dataInspectorBox.Items[2] = PRIMITIVE_STRINGS[2];
            }
            //Short selection
            if (bulkRead.Length >= 2)
            {
                dataInspectorBox.Items[3] = PRIMITIVE_STRINGS[3] + BitConverter.ToInt16(bulkRead, 0);
                dataInspectorBox.Items[4] = PRIMITIVE_STRINGS[4] + BitConverter.ToUInt16(bulkRead, 0);
                dataInspectorBox.Items[5] = PRIMITIVE_STRINGS[5] + Half.ToHalf(bulkRead, 0);
            }
            else
            {
                dataInspectorBox.Items[3] = PRIMITIVE_STRINGS[3];
                dataInspectorBox.Items[4] = PRIMITIVE_STRINGS[4];
                dataInspectorBox.Items[5] = PRIMITIVE_STRINGS[5];
            }
            //Int selection
            if (bulkRead.Length >= 4)
            {
                dataInspectorBox.Items[6] = PRIMITIVE_STRINGS[6] + BitConverter.ToInt32(bulkRead, 0);
                dataInspectorBox.Items[7] = PRIMITIVE_STRINGS[7] + BitConverter.ToUInt32(bulkRead, 0);
                dataInspectorBox.Items[8] = PRIMITIVE_STRINGS[8] + BitConverter.ToSingle(bulkRead, 0);
            }
            else
            {
                dataInspectorBox.Items[6] = PRIMITIVE_STRINGS[6];
                dataInspectorBox.Items[7] = PRIMITIVE_STRINGS[7];
                dataInspectorBox.Items[8] = PRIMITIVE_STRINGS[8];
            }
            //Long selection
            if (bulkRead.Length >= 8)
            {
                dataInspectorBox.Items[9] = PRIMITIVE_STRINGS[9] + BitConverter.ToInt64(bulkRead, 0);
                dataInspectorBox.Items[10] = PRIMITIVE_STRINGS[10] + BitConverter.ToUInt64(bulkRead, 0);
                dataInspectorBox.Items[11] = PRIMITIVE_STRINGS[11] + BitConverter.ToDouble(bulkRead, 0);
            }
            else
            {
                dataInspectorBox.Items[9] = PRIMITIVE_STRINGS[9];
                dataInspectorBox.Items[10] = PRIMITIVE_STRINGS[10];
                dataInspectorBox.Items[11] = PRIMITIVE_STRINGS[11];
            }
        }
        private void FocusField(FieldInfo field)
        {
            if (field != null) //Do we have a valid field?
            {
                //Yes, let's find and select the corresponding tree node
                skipNodeSelection = true;
                fieldsView.SelectedNode = FindTreeNode(field);
                fieldsView.EnsureVisible(fieldsView.SelectedNode, true);
                skipNodeSelection = false;
            }
        }
        private FastTreeNode FindTreeNode(FieldInfo field)
        {
            Stack<int> nodeIndices = new Stack<int>();
            IFieldsContainer currFieldContainer;
            FieldInfo currField = field;
            FastTreeNode currTreeNode;
            while (currField != null)
            {
                currFieldContainer = currField.ParentContainer;
                nodeIndices.Push(currFieldContainer.IndexOf(currField));
                if (currFieldContainer.GetType() == typeof(FieldContainerInfo))
                {
                    currField = (FieldContainerInfo)currFieldContainer;
                }
                else
                {
                    currField = null;
                }
            }
            if (nodeIndices.Count != 0)
            {
                currTreeNode = fieldsView.Nodes[nodeIndices.Pop()];
                while (nodeIndices.Count != 0)
                {
                    currTreeNode = currTreeNode.Nodes[nodeIndices.Pop()];
                }
                return currTreeNode;
            }
            return null;
        }
        private void HexEditor_DataChanged(object sender, int fileOffset, int length)
        {
            HexEditor hexEditor = (HexEditor)sender;
            FieldInfo field = hexEditor.FindFieldAt(fileOffset);
            if (field != null)
            {
                FastTreeNode treeNode = FindTreeNode(field);
                if (treeNode != null)
                {
                    treeNode.Text = field.Name + " = " + ReadFieldData(hexEditor, field);
                }
            }
        }
        private void ScriptFileMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem scriptFileMenuItem = (ToolStripMenuItem)sender;
            hexEditor1.RunScript("Scripts\\" + scriptFileMenuItem.Text);
        }
        private void HexEditor_ScriptDone(object sender, IFieldsContainer rootFields)
        {
            HexEditor hexEditor = (HexEditor)sender;
            this.rootFields = rootFields;
            UpdateListBox(hexEditor);
            fieldsView.Nodes.Clear();
            skipNodeFind = true;
            hexEditor.StartReading(0);
            AddFieldsToNode(fieldsView.Nodes, hexEditor, rootFields);
            foreach (FastTreeNode currRootNode in fieldsView.Nodes)
            {
                currRootNode.Expand();
            }
            FocusField(hexEditor.SelectedField);
            hexEditor.DoneReading();
            skipNodeFind = false;
        }
        private void AddFieldsToNode(FastNodesCollection targetNodes, HexEditor hexEditor, IFieldsContainer fieldsContainer)
        {
            FieldInfo currField;
            FastTreeNode currFieldNode;
            for (int currFieldIndex = 0; currFieldIndex < fieldsContainer.FieldsCount; currFieldIndex++)
            {
                currField = fieldsContainer.GetField(currFieldIndex);
                if (currField.GetType() == typeof(FieldContainerInfo))
                {
                    currFieldNode = targetNodes.Add(currField.Name);
                    currFieldNode.ImageKey = GetFieldImageKey(currField.Type);
                    AddFieldsToNode(currFieldNode.Nodes, hexEditor, (FieldContainerInfo)currField);
                }
                else
                {
                    targetNodes.Add(currField.Name + " = " + ReadFieldData(hexEditor, currField)).ImageKey = GetFieldImageKey(currField.Type);
                }
            }
        }
        private object ReadFieldData(HexEditor hexEditor, FieldInfo field)
        {
            object fieldData;
            switch (field.Type)
            {
                default:
                    hexEditor.ReadData(field.Type, false, out fieldData);
                break;
                case FieldType.Enum:
                    EnumFieldInfo enumField = (EnumFieldInfo)field;
                    if (hexEditor.ReadEnum(enumField.EnumName, false, out fieldData))
                    {
                        fieldData = enumField.EnumName + "." + fieldData;
                    }
                break;
                case FieldType.CString:
                    hexEditor.ReadCString(false, out fieldData);
                    fieldData = "\"" + fieldData + "\"";
                break;
                case FieldType.UTFString:
                    hexEditor.ReadUTFString(false, out fieldData);
                    fieldData = "\"" + fieldData + "\"";
                break;
            }
            return fieldData;
        }
        private string GetFieldImageKey(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Byte:
                case FieldType.SByte:
                case FieldType.Short:
                case FieldType.UShort:
                case FieldType.HalfFloat:
                case FieldType.Int:
                case FieldType.UInt:
                case FieldType.Float:
                case FieldType.Long:
                case FieldType.ULong:
                case FieldType.Double: return "Number";

                case FieldType.Bool: return "Boolean";

                case FieldType.Enum: return "Enum";

                case FieldType.CString:
                case FieldType.UTFString: return "String";

                case FieldType.Struct: return "Struct";

                case FieldType.List: return "List";

                default: throw new Exception();
            }
        }
        private void FieldsView_AfterSelect(object sender, FastTreeViewEventArgs e)
        {
            if (!skipNodeSelection)
            {
                this.Text = e.Node.Text;
                FieldInfo selectedFieldInfo = GetFieldFromNode(e.Node);
                skipNodeFind = true;
                hexEditor1.SelectionStart = selectedFieldInfo.FileOffset;
                hexEditor1.SelectionLength = selectedFieldInfo.Length;
                skipNodeFind = false;
            }
        }
        private FieldInfo GetFieldFromNode(FastTreeNode node)
        {
            IFieldsContainer currFieldContainer = rootFields;
            Stack<int> fieldIndices = new Stack<int>();
            FastTreeNode currNode = node;
            while (currNode != null)
            {
                fieldIndices.Push(currNode.Index);
                currNode = currNode.Parent;
            }
            while (fieldIndices.Count > 1)
            {
                currFieldContainer = (IFieldsContainer)currFieldContainer.GetField(fieldIndices.Pop());
            }
            return currFieldContainer.GetField(fieldIndices.Pop());
        }
        private void FieldsView_MouseWheel(object sender, MouseEventArgs e)
        {
            Control fieldsViewer = (Control)sender;
            if (!fieldsViewer.Bounds.Contains(e.Location))
            {
                if (e.Delta > 0)
                {
                    hexEditor1.ScrollUp();
                }
                else
                {
                    hexEditor1.ScrollDown();
                }
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }
        private void FieldsView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //MessageBox.Show(GetFieldFromNode(e.Node).Type.ToString());
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
