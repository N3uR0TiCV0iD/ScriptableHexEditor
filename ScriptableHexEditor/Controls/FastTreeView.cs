using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;
namespace ScriptableHexEditor
{
    //I've made this control since changing the text of a node in the regular TreeView freezes the application. I assume this is because it updates all the paths of the sub nodes (Which I retrieve dynamically)
    public class FastTreeView : Control
    {
        public delegate void NodeEventHandler(object sender, FastTreeViewEventArgs e);
        public delegate void NodeMouseClickHandler(object sender, FastTreeNodeMouseClickEventArg e);
        const int IMAGETEXT_XOFFSET = 3;
        const int NODETEXT_XOFFSET = 17;
        const int NODETEXT_YOFFSET = 7;
        const int XPOS_START = 7;
        int indent;
        Pen borderPen;
        bool showLines;
        bool labelEdit;
        int itemHeight;
        Color lineColor;
        Pen nodeLinesPen;
        int halfItemHeight;
        bool hideSelection;
        bool showPlusMinus;
        bool showRootLines;
        ImageList imageList;
        VScrollBar scrollbar;
        string pathSeparator;
        int defaultImageIndex;
        string defaultImageKey;
        BorderStyle borderStyle;
        FastNodesCollection nodes;
        FastTreeNode selectedNode;
        SolidBrush foregroundBrush;
        List<FastTreeNode> visibleNodes;
        VisualStyleRenderer expandedNode;
        VisualStyleRenderer collapsedNode;
        public FastTreeView()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.collapsedNode = new VisualStyleRenderer(VisualStyleElement.TreeView.Glyph.Closed);
            this.expandedNode = new VisualStyleRenderer(VisualStyleElement.TreeView.Glyph.Opened);
            this.borderPen = new Pen(Color.FromArgb(130, 135, 144));
            this.foregroundBrush = new SolidBrush(this.ForeColor);
            this.nodes = new FastNodesCollection(this, null);
            this.lineColor = Color.FromArgb(109, 109, 109);
            this.visibleNodes = new List<FastTreeNode>();
            this.nodeLinesPen = new Pen(this.lineColor);
            this.nodeLinesPen.DashStyle = DashStyle.Dot;
            this.borderStyle = BorderStyle.FixedSingle;
            this.BackColor = SystemColors.Window;
            this.scrollbar = new VScrollBar();
            this.Controls.Add(this.scrollbar);
            this.scrollbar.LargeChange = 1;
            this.pathSeparator = "\\";
            this.hideSelection = true;
            this.showPlusMinus = true;
            this.showRootLines = true;
            this.halfItemHeight = 8;
            this.showLines = true;
            this.itemHeight = 16;
            this.indent = 19;
            this.scrollbar.ValueChanged += scrollbar_ValueChanged;
        }
        public FastNodesCollection Nodes
        {
            get
            {
                return nodes;
            }
        }
        public FastTreeNode SelectedNode
        {
            get
            {
                return selectedNode;
            }
            set
            {
                FastTreeViewEventArgs eventArgs = new FastTreeViewEventArgs(value);
                BeforeSelect?.Invoke(this, eventArgs);
                selectedNode = value;
                AfterSelect?.Invoke(this, eventArgs);
                if (value != null)
                {
                    FastTreeNode currNode = value.Parent;
                    while (currNode != null)
                    {
                        currNode.Expand();
                        currNode = currNode.Parent;
                    }
                    EnsureVisible(value, false);
                }
                this.Refresh();
            }
        }
        public int ImageIndex
        {
            get
            {
                return defaultImageIndex;
            }
            set
            {
                if (IsImageIndexValid(value))
                {
                    defaultImageIndex = value;
                    defaultImageKey = null;
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public string ImageKey
        {
            get
            {
                return defaultImageKey;
            }
            set
            {
                if (IsImageKeyValid(value))
                {
                    defaultImageKey = value;
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public ImageList ImageList
        {
            get
            {
                return imageList;
            }
            set
            {
                if (value != null)
                {
                    itemHeight = value.ImageSize.Height;
                    halfItemHeight = itemHeight / 2;
                    //TODO: Update all nodes to make sure that they all have valid image keys/indexes
                }
                imageList = value;
                this.Refresh();
            }
        }
        public bool LabelEdit
        {
            get
            {
                return labelEdit;
            }
            set
            {
                labelEdit = value;
            }
        }
        public string PathSeparator
        {
            get
            {
                return pathSeparator;
            }
            set
            {
                pathSeparator = value;
            }
        }
        public bool HideSelection
        {
            get
            {
                return hideSelection;
            }
            set
            {
                hideSelection = value;
                if (IsNodeVisible(this.selectedNode))
                {
                    this.Refresh();
                }
            }
        }
        public BorderStyle BorderStyle
        {
            get
            {
                return borderStyle;
            }
            set
            {
                borderStyle = value;
                this.Refresh();
            }
        }
        public int Indent
        {
            get
            {
                return indent;
            }
            set
            {
                indent = value;
                this.Refresh();
            }
        }
        public Color LineColor
        {
            get
            {
                return lineColor;
            }
            set
            {
                lineColor = value;
                nodeLinesPen = new Pen(value);
                nodeLinesPen.DashStyle = DashStyle.Dot;
                this.Refresh();
            }
        }
        public int ItemHeight
        {
            get
            {
                return itemHeight;
            }
            set
            {
                halfItemHeight = itemHeight / 2;
                itemHeight = value;
                this.Refresh();
            }
        }
        public bool ShowLines
        {
            get
            {
                return showLines;
            }
            set
            {
                showLines = value;
                this.Refresh();
            }
        }
        public bool ShowPlusMinus
        {
            get
            {
                return showPlusMinus;
            }
            set
            {
                showPlusMinus = value;
                this.Refresh();
            }
        }
        public bool ShowRootLines
        {
            get
            {
                return showRootLines;
            }
            set
            {
                showRootLines = value;
                this.Refresh();
            }
        }
        public int TotalExpandedNodes
        {
            get
            {
                int count = nodes.Count;
                foreach (var currNode in nodes)
                {
                    if (currNode.IsExpanded)
                    {
                        count += currNode.TotalExpandedNodes;
                    }
                }
                return count;
            }
        }
        public int VisibleCount
        {
            get
            {
                return visibleNodes.Count;
            }
        }
        public override Color ForeColor
        {
            get
            {
                return base.ForeColor;
            }
            set
            {
                foregroundBrush = new SolidBrush(value);
                base.ForeColor = value;
            }
        }
        public override Font Font
        {
            get
            {
                return base.Font;
            }
            set
            {
                base.Font = value;
            }
        }
        public void ExpandAll()
        {
            foreach (var currNode in nodes)
            {
                currNode.ExpandAll();
            }
        }
        public void CollapseAll()
        {
            foreach (var currNode in nodes)
            {
                currNode.CollapseAll();
            }
        }
        public void ScrollUp()
        {
            ScrollUp(1);
        }
        public void ScrollUp(int amount)
        {
            int newValue = scrollbar.Value - amount;
            if (newValue < scrollbar.Minimum)
            {
                newValue = scrollbar.Minimum;
            }
            scrollbar.Value = newValue;
        }
        public void ScrollDown()
        {
            ScrollDown(1);
        }
        public void ScrollDown(int amount)
        {
            int newValue = scrollbar.Value + amount;
            if (newValue > scrollbar.Maximum)
            {
                newValue = scrollbar.Maximum;
            }
            scrollbar.Value = newValue;
        }
        public void EnsureVisible(FastTreeNode node)
        {
            EnsureVisible(node, false);
        }
        public void EnsureVisible(FastTreeNode node, bool middle)
        {
            int expandedTreeIndex;
            if (middle)
            {
                expandedTreeIndex = node.ExpandedTreeIndex - (GetDisplayableNodes() / 2);
                if (expandedTreeIndex >= 0)
                {
                    expandedTreeIndex *= 2; //Reutilizing variable
                    if (expandedTreeIndex > scrollbar.Maximum)
                    {
                        expandedTreeIndex = scrollbar.Maximum;
                    }
                    scrollbar.Value = expandedTreeIndex;
                }
                else
                {
                    scrollbar.Value = 0;
                }
            }
            else if (!IsNodeVisible(node))
            {
                int firstVisibleNode = scrollbar.Value / 2;
                expandedTreeIndex = node.ExpandedTreeIndex;
                if (expandedTreeIndex < firstVisibleNode) //Is the node closer to the start or to the start?
                {
                    scrollbar.Value = expandedTreeIndex * 2;
                }
                else
                {
                    int lastVisibleNode = firstVisibleNode + GetDisplayableNodes();
                    scrollbar.Value = scrollbar.Value + ((expandedTreeIndex - lastVisibleNode) * 2);
                }
            }
        }
        public bool IsNodeVisible(FastTreeNode node)
        {
            return visibleNodes.Contains(node);
        }
        public int GetNodeCount(bool includeSubTrees)
        {
            int count = nodes.Count;
            if (includeSubTrees)
            {
                foreach (var currNode in nodes)
                {
                    count += currNode.GetNodeCount(true);
                }
            }
            return count;
        }
        public FastTreeNode GetNodeAt(Point point)
        {
            FastTreeViewHitTestInfo hitResult = HitTest(point);
            return hitResult != null ? hitResult.Node : null;
        }
        public FastTreeNode GetNodeAt(int x, int y)
        {
            FastTreeViewHitTestInfo hitResult = HitTest(x, y);
            return hitResult != null ? hitResult.Node : null;
        }
        public FastTreeViewHitTestInfo HitTest(Point point)
        {
            return HitTest(point.X, point.Y);
        }
        public FastTreeViewHitTestInfo HitTest(int x, int y)
        {
            int visibleIndex;
            int realY = y - 2;
            if (scrollbar.Value % 2 != 0)
            {
                realY += halfItemHeight;
            }
            visibleIndex = realY / itemHeight;
            if (visibleIndex < visibleNodes.Count)
            {
                FastTreeNode possibleNode = visibleNodes[visibleIndex];
                int nodeStart = XPOS_START + (possibleNode.Level * indent);
                int nodeEnd = nodeStart + NODETEXT_XOFFSET + TextRenderer.MeasureText(possibleNode.Text, this.Font).Width;
                if (imageList != null)
                {
                    nodeEnd += imageList.ImageSize.Width + IMAGETEXT_XOFFSET;
                }
                if (x >= nodeStart && x <= nodeEnd)
                {
                    return new FastTreeViewHitTestInfo(possibleNode, new Point(x - nodeStart, 0));
                }
            }
            return null;
        }
        public int GetVisibleIndex(FastTreeNode node)
        {
            return visibleNodes.IndexOf(node);
        }
        public bool IsImageIndexValid(int index)
        {
            return imageList == null || (index >= 0 && index < imageList.Images.Count);
        }
        public bool IsImageKeyValid(string key)
        {
            return imageList == null || String.IsNullOrEmpty(key) || imageList.Images.ContainsKey(key);
        }
        public void NodeRefreshCheck(FastTreeNode node)
        {
            if (node.Parent == null || node.Parent.IsExpanded)
            {
                this.Refresh();
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            FastTreeViewHitTestInfo hitResult = HitTest(e.X, e.Y);
            if (hitResult != null)
            {
                NodeMouseClick?.Invoke(this, new FastTreeNodeMouseClickEventArg());
                if (hitResult.Location.X >= NODETEXT_XOFFSET)
                {
                    this.SelectedNode = hitResult.Node;
                }
                else
                {
                    hitResult.Node.Toggle();
                }
            }
            this.Focus();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            FastTreeViewHitTestInfo hitResult = HitTest(e.X, e.Y);
            if (hitResult != null)
            {
                NodeMouseDoubleClick?.Invoke(this, new FastTreeNodeMouseClickEventArg());
                hitResult.Node.Toggle();
            }
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
        private void scrollbar_ValueChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    selectedNode.Expand();
                break;
                case Keys.Left:
                    selectedNode.Collapse();
                break;
                case Keys.Up:
                    FastTreeNode previousNode = selectedNode.PreviousUpNode;
                    if (previousNode != null)
                    {
                        this.SelectedNode = previousNode;
                    }
                break;
                case Keys.Down:
                    if (!selectedNode.IsExpanded)
                    {
                        FastTreeNode nextNode = selectedNode.NextNode;
                        if (nextNode != null)
                        {
                            this.SelectedNode = nextNode;
                        }
                        else
                        {
                            FastTreeNode currNode = selectedNode.Parent;
                            while (nextNode == null && currNode != null)
                            {
                                nextNode = currNode.NextNode;
                                currNode = currNode.Parent;
                            }
                            if (nextNode != null)
                            {
                                this.SelectedNode = nextNode;
                            }
                            else
                            {
                                ScrollDown();
                            }
                        }
                    }
                    else
                    {
                        this.SelectedNode = selectedNode.Nodes[0];
                    }
                break;
            }
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            scrollbar.Height = this.Height - 2;
            scrollbar.Location = new Point(this.Width - scrollbar.Width - 1, 1);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            int displayableNodes = GetDisplayableNodes();
            int totalExpandedNodes = this.TotalExpandedNodes;
            int yPos = 2 - (scrollbar.Value * halfItemHeight);
            int previousYPos = 0;
            visibleNodes.Clear();
            if (totalExpandedNodes > displayableNodes)
            {
                scrollbar.Maximum = (totalExpandedNodes - displayableNodes) * 2;
                scrollbar.Visible = true;
            }
            else
            {
                scrollbar.Visible = false;
                scrollbar.Maximum = 0;
            }
            switch (borderStyle)
            {
                case BorderStyle.None:
                    e.Graphics.Clear(this.BackColor);
                break;
                case BorderStyle.FixedSingle:
                    e.Graphics.Clear(this.BackColor);
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                break;
                case BorderStyle.Fixed3D:
                    ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle);
                break;
            }
            for (int currNodeIndex = 0; currNodeIndex < nodes.Count; currNodeIndex++)
            {
                previousYPos = DrawNode(nodes[currNodeIndex], ref yPos, previousYPos, 0, e.Graphics);
                if (yPos > this.Height)
                {
                    if (currNodeIndex != nodes.Count - 1)
                    {
                        int lineXPos = XPOS_START + 7;
                        e.Graphics.DrawLine(nodeLinesPen, lineXPos, previousYPos + 14, lineXPos, this.Height); //Missing Next Down line
                    }
                    break;
                }
            }
        }
        private int GetDisplayableNodes()
        {
            return this.Height / itemHeight;
        }
        private int DrawNode(FastTreeNode node, ref int yPos, int previousYPos, int level, Graphics graphics) //Returns a new previousYPos
        {
            int xPos;
            int lineXPos;
            int oldYPos = yPos;
            bool drawNode = yPos > (-itemHeight + 2);
            if (drawNode)
            {
                int textXPos;
                Brush usingBrush;
                int rightLineYPos;
                visibleNodes.Add(node);
                xPos = XPOS_START + (level * indent);
                textXPos = xPos + NODETEXT_XOFFSET;
                rightLineYPos = yPos + 8;
                lineXPos = xPos + 7;
                if (imageList != null)
                {
                    Image drawingImage;
                    if (node.ImageKey != null)
                    {
                        drawingImage = imageList.Images[node.ImageKey];
                    }
                    else
                    {
                        drawingImage = imageList.Images[node.ImageIndex];
                    }
                    graphics.DrawImage(drawingImage, textXPos, yPos, drawingImage.Width, itemHeight);
                    textXPos += imageList.ImageSize.Width + IMAGETEXT_XOFFSET;
                }
                if (!node.IsSelected)
                {
                    usingBrush = foregroundBrush;
                }
                else
                {
                    int nodeTextWidth = (int)graphics.MeasureString(node.Text, this.Font).Width;
                    graphics.FillRectangle(SystemBrushes.Highlight, textXPos, yPos, nodeTextWidth, itemHeight);
                    ControlPaint.DrawFocusRectangle(graphics, new Rectangle(textXPos, yPos, nodeTextWidth, itemHeight));
                    usingBrush = SystemBrushes.HighlightText;
                }
                graphics.DrawString(node.Text, this.Font, usingBrush, textXPos, yPos);
                if (showLines && (showRootLines || level != 0))
                {
                    graphics.DrawLine(nodeLinesPen, lineXPos, rightLineYPos, lineXPos + 9, rightLineYPos); //Right line
                    if (!node.IsLastNode)
                    {
                        graphics.DrawLine(nodeLinesPen, lineXPos, oldYPos + 10, lineXPos, oldYPos + 14); //Missing Down line
                    }
                    if (previousYPos != 0)
                    {
                        graphics.DrawLine(nodeLinesPen, lineXPos, previousYPos + 14, lineXPos, yPos + 7); //Previous Down line
                    }
                }
            }
            else
            {
                //USELESS
                xPos = 0;
                lineXPos = 0;
                //END USELESS
            }
            yPos += itemHeight;
            if (node.HasChildren)
            {
                int previousYPosNow = oldYPos + 2;
                if (node.IsExpanded)
                {
                    if (drawNode && showPlusMinus)
                    {
                        expandedNode.DrawBackground(graphics, new Rectangle(xPos, oldYPos + 1, 16, 16));
                    }
                    foreach (var currNode in node.Nodes)
                    {
                        previousYPosNow = DrawNode(currNode, ref yPos, previousYPosNow, level + 1, graphics);
                        if (yPos > this.Height)
                        {
                            if (!node.IsLastNode)
                            {
                                graphics.DrawLine(nodeLinesPen, lineXPos, oldYPos + 14, lineXPos, this.Height); //Missing Next Down line
                            }
                            break;
                        }
                    }
                }
                else if (drawNode && showPlusMinus)
                {
                    collapsedNode.DrawBackground(graphics, new Rectangle(xPos, oldYPos + 1, 16, 16));
                }
            }
            return oldYPos;
        }
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down: return true;
            }
            return base.IsInputKey(keyData);
        }
        public event NodeMouseClickHandler NodeMouseDoubleClick;
        public event NodeMouseClickHandler NodeMouseClick;
        public event NodeEventHandler BeforeSelect;
        public event NodeEventHandler AfterSelect;
    }
}
