namespace ScriptableHexEditor
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.backgroundDrawer = new System.Windows.Forms.Control();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator1 = new System.Windows.Forms.ToolStripSeparator();
            this.runScriptMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separator2 = new System.Windows.Forms.ToolStripSeparator();
            this.separator3 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.fieldsViewImageList = new System.Windows.Forms.ImageList(this.components);
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.toolMenuStrip = new System.Windows.Forms.ToolStrip();
            this.hexEditorContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.dataInspectorBox = new ScriptableHexEditor.DisableListBox();
            this.fieldsView = new ScriptableHexEditor.FastTreeView();
            this.hexEditor1 = new ScriptableHexEditor.HexEditor();
            this.mainMenu.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // backgroundDrawer
            // 
            this.backgroundDrawer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.backgroundDrawer.Location = new System.Drawing.Point(926, 47);
            this.backgroundDrawer.Name = "backgroundDrawer";
            this.backgroundDrawer.Size = new System.Drawing.Size(258, 2);
            this.backgroundDrawer.TabIndex = 4;
            // 
            // mainMenu
            // 
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenuItem,
            this.editMenuItem,
            this.viewMenuItem,
            this.optionsMenuItem,
            this.toolsMenuItem,
            this.helpMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "mainMenu";
            this.mainMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.mainMenu.Size = new System.Drawing.Size(1184, 24);
            this.mainMenu.TabIndex = 1;
            this.mainMenu.Text = "menuStrip1";
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newFileMenuItem,
            this.openFileMenuItem,
            this.saveFileMenuItem,
            this.saveAsMenuItem,
            this.separator1,
            this.runScriptMenuItem,
            this.closeFileMenuItem,
            this.separator2,
            this.separator3,
            this.exitMenuItem});
            this.fileMenuItem.Name = "fileMenuItem";
            this.fileMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileMenuItem.Text = "File";
            // 
            // newFileMenuItem
            // 
            this.newFileMenuItem.Name = "newFileMenuItem";
            this.newFileMenuItem.Size = new System.Drawing.Size(128, 22);
            this.newFileMenuItem.Text = "New";
            // 
            // openFileMenuItem
            // 
            this.openFileMenuItem.Name = "openFileMenuItem";
            this.openFileMenuItem.Size = new System.Drawing.Size(128, 22);
            this.openFileMenuItem.Text = "Open";
            // 
            // saveFileMenuItem
            // 
            this.saveFileMenuItem.Name = "saveFileMenuItem";
            this.saveFileMenuItem.Size = new System.Drawing.Size(128, 22);
            this.saveFileMenuItem.Text = "Save";
            // 
            // saveAsMenuItem
            // 
            this.saveAsMenuItem.Name = "saveAsMenuItem";
            this.saveAsMenuItem.Size = new System.Drawing.Size(128, 22);
            this.saveAsMenuItem.Text = "Save As...";
            // 
            // separator1
            // 
            this.separator1.Name = "separator1";
            this.separator1.Size = new System.Drawing.Size(125, 6);
            // 
            // runScriptMenuItem
            // 
            this.runScriptMenuItem.Name = "runScriptMenuItem";
            this.runScriptMenuItem.Size = new System.Drawing.Size(128, 22);
            this.runScriptMenuItem.Text = "Run Script";
            // 
            // closeFileMenuItem
            // 
            this.closeFileMenuItem.Name = "closeFileMenuItem";
            this.closeFileMenuItem.Size = new System.Drawing.Size(128, 22);
            this.closeFileMenuItem.Text = "Close";
            // 
            // separator2
            // 
            this.separator2.Name = "separator2";
            this.separator2.Size = new System.Drawing.Size(125, 6);
            // 
            // separator3
            // 
            this.separator3.Name = "separator3";
            this.separator3.Size = new System.Drawing.Size(125, 6);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(128, 22);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // editMenuItem
            // 
            this.editMenuItem.Name = "editMenuItem";
            this.editMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editMenuItem.Text = "Edit";
            // 
            // viewMenuItem
            // 
            this.viewMenuItem.Name = "viewMenuItem";
            this.viewMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewMenuItem.Text = "View";
            // 
            // optionsMenuItem
            // 
            this.optionsMenuItem.Name = "optionsMenuItem";
            this.optionsMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsMenuItem.Text = "Options";
            // 
            // toolsMenuItem
            // 
            this.toolsMenuItem.Name = "toolsMenuItem";
            this.toolsMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsMenuItem.Text = "Tools";
            // 
            // helpMenuItem
            // 
            this.helpMenuItem.Name = "helpMenuItem";
            this.helpMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpMenuItem.Text = "Help";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 47);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(990, 495);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.fieldsView);
            this.tabPage1.Controls.Add(this.hexEditor1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(982, 469);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // fieldsViewImageList
            // 
            this.fieldsViewImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("fieldsViewImageList.ImageStream")));
            this.fieldsViewImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.fieldsViewImageList.Images.SetKeyName(0, "Number");
            this.fieldsViewImageList.Images.SetKeyName(1, "Boolean");
            this.fieldsViewImageList.Images.SetKeyName(2, "Enum");
            this.fieldsViewImageList.Images.SetKeyName(3, "String");
            this.fieldsViewImageList.Images.SetKeyName(4, "Struct");
            this.fieldsViewImageList.Images.SetKeyName(5, "List");
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(982, 469);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // toolMenuStrip
            // 
            this.toolMenuStrip.Location = new System.Drawing.Point(0, 24);
            this.toolMenuStrip.Name = "toolMenuStrip";
            this.toolMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolMenuStrip.Size = new System.Drawing.Size(1184, 25);
            this.toolMenuStrip.TabIndex = 2;
            // 
            // hexEditorContextMenu
            // 
            this.hexEditorContextMenu.Name = "contextMenuStrip1";
            this.hexEditorContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // dataInspectorBox
            // 
            this.dataInspectorBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataInspectorBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.dataInspectorBox.FormattingEnabled = true;
            this.dataInspectorBox.Items.AddRange(new object[] {
            "byte\t   |  ",
            "sbyte\t   |  ",
            "bool\t   |  ",
            "short\t   |  ",
            "ushort\t   |  ",
            "half float\t   |  ",
            "int\t   |  ",
            "uint\t   |  ",
            "float\t   |  ",
            "long\t   |  ",
            "ulong\t   |  ",
            "double\t   |  ",
            "enum\t   |  "});
            this.dataInspectorBox.Location = new System.Drawing.Point(989, 67);
            this.dataInspectorBox.Name = "dataInspectorBox";
            this.dataInspectorBox.Size = new System.Drawing.Size(195, 472);
            this.dataInspectorBox.TabIndex = 3;
            // 
            // fieldsView
            // 
            this.fieldsView.BackColor = System.Drawing.SystemColors.Window;
            this.fieldsView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.fieldsView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fieldsView.HideSelection = true;
            this.fieldsView.ImageIndex = 0;
            this.fieldsView.ImageKey = null;
            this.fieldsView.ImageList = this.fieldsViewImageList;
            this.fieldsView.Indent = 19;
            this.fieldsView.ItemHeight = 16;
            this.fieldsView.LabelEdit = false;
            this.fieldsView.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.fieldsView.Location = new System.Drawing.Point(3, 3);
            this.fieldsView.Name = "fieldsView";
            this.fieldsView.PathSeparator = "\\";
            this.fieldsView.SelectedNode = null;
            this.fieldsView.ShowLines = true;
            this.fieldsView.ShowPlusMinus = true;
            this.fieldsView.ShowRootLines = true;
            this.fieldsView.Size = new System.Drawing.Size(221, 463);
            this.fieldsView.TabIndex = 5;
            this.fieldsView.AfterSelect += new ScriptableHexEditor.FastTreeView.NodeEventHandler(this.FieldsView_AfterSelect);
            // 
            // hexEditor1
            // 
            this.hexEditor1.Dock = System.Windows.Forms.DockStyle.Right;
            this.hexEditor1.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexEditor1.Location = new System.Drawing.Point(224, 3);
            this.hexEditor1.Name = "hexEditor1";
            this.hexEditor1.SelectionColor = System.Drawing.SystemColors.Highlight;
            this.hexEditor1.SelectionLength = 0;
            this.hexEditor1.SelectionStart = 0;
            this.hexEditor1.Size = new System.Drawing.Size(755, 463);
            this.hexEditor1.TabIndex = 3;
            this.hexEditor1.Text = "hexEditor2";
            this.hexEditor1.SelectionChanged += new System.EventHandler(this.HexEditor_SelectionChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 542);
            this.Controls.Add(this.backgroundDrawer);
            this.Controls.Add(this.dataInspectorBox);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.toolMenuStrip);
            this.Controls.Add(this.mainMenu);
            this.Name = "MainForm";
            this.Text = "0";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ToolStrip toolMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem newFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runScriptMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeFileMenuItem;
        private System.Windows.Forms.ToolStripSeparator separator1;
        private System.Windows.Forms.ToolStripSeparator separator2;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripSeparator separator3;
        private System.Windows.Forms.Control backgroundDrawer;
        private HexEditor hexEditor1;
        private System.Windows.Forms.ToolStripMenuItem viewMenuItem;
        private DisableListBox dataInspectorBox;
        private FastTreeView fieldsView;
        private System.Windows.Forms.ImageList fieldsViewImageList;
        private System.Windows.Forms.ContextMenuStrip hexEditorContextMenu;
        private System.Windows.Forms.ToolTip toolTip;
    }
}

