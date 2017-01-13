using System;
using System.Text;
namespace ScriptableHexEditor
{
    public class FastTreeNode
    {
        string tag;
        string text;
        string name;
        bool expanded;
        int imageIndex;
        string imageKey;
        FastTreeNode parent;
        FastTreeView treeView;
        FastNodesCollection nodes;
        public FastTreeNode(FastTreeView treeView, string text)
        {
            this.text = text;
            this.name = text;
            this.treeView = treeView;
            this.nodes = new FastNodesCollection(treeView, this);
        }
        public FastTreeNode(FastTreeView treeView, string text, FastTreeNode parent) : this(treeView, text)
        {
            this.parent = parent;
        }
        public FastNodesCollection Nodes
        {
            get
            {
                return nodes;
            }
        }
        public bool IsExpanded
        {
            get
            {
                return expanded;
            }
        }
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                treeView.NodeRefreshCheck(this);
            }
        }
        public string ImageKey
        {
            get
            {
                return imageKey;
            }
            set
            {
                if (treeView.IsImageKeyValid(value))
                {
                    imageIndex = 0;
                    imageKey = value;
                    treeView.NodeRefreshCheck(this);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public int ImageIndex
        {
            get
            {
                return imageIndex;
            }
            set
            {
                if (treeView.IsImageIndexValid(value))
                {
                    imageKey = null;
                    imageIndex = value;
                    treeView.NodeRefreshCheck(this);
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        public bool HasChildren
        {
            get
            {
                return nodes.Count != 0;
            }
        }
        public FastTreeView TreeView
        {
            get
            {
                return treeView;
            }
        }
        public int Index
        {
            get
            {
                return this.ParentNodes.IndexOf(this);
            }
        }
        public int Level
        {
            get
            {
                int level = 0;
                FastTreeNode currNode = this.parent;
                while (currNode != null)
                {
                    currNode = currNode.parent;
                    level++;
                }
                return level;
            }
        }
        public bool IsSelected
        {
            get
            {
                return treeView.SelectedNode == this;
            }
        }
        public bool IsFirstNode
        {
            get
            {
                return this.FirstNode == this;
            }
        }
        public FastTreeNode FirstNode
        {
            get
            {
                return this.ParentNodes[0];
            }
        }
        public FastTreeNode PreviousNode
        {
            get
            {
                FastNodesCollection parentNodes = this.ParentNodes;
                int previousIndex = parentNodes.IndexOf(this) - 1;
                if (previousIndex >= 0)
                {
                    return parentNodes[previousIndex];
                }
                return null;
            }
        }
        public FastTreeNode NextNode
        {
            get
            {
                FastNodesCollection parentNodes = this.ParentNodes;
                int nextIndex = parentNodes.IndexOf(this) + 1;
                if (nextIndex < parentNodes.Count)
                {
                    return parentNodes[nextIndex];
                }
                return null;
            }
        }
        public FastTreeNode LastNode
        {
            get
            {
                FastNodesCollection parentNodes = this.ParentNodes;
                return parentNodes[parentNodes.Count - 1];
            }
        }
        public bool IsLastNode
        {
            get
            {
                return this.LastNode == this;
            }
        }
        public FastTreeNode Parent
        {
            get
            {
                return parent;
            }
        }
        public FastNodesCollection ParentNodes
        {
            get
            {
                if (this.parent != null)
                {
                    return this.parent.nodes;
                }
                else
                {
                    return treeView.Nodes;
                }
            }
        }
        public string FullPath
        {
            get
            {
                StringBuilder fullPath = new StringBuilder();
                FastTreeNode currNode = this.parent;
                fullPath.Append(text);
                while (currNode != null)
                {
                    fullPath.Insert(0, currNode.text + treeView.PathSeparator);
                    currNode = currNode.parent;
                }
                return fullPath.ToString();
            }
        }
        public string Tag
        {
            get
            {
                return tag;
            }
            set
            {
                tag = value;
            }
        }
        public int TotalExpandedNodes
        {
            get
            {
                int count = nodes.Count;
                foreach (var currNode in nodes)
                {
                    if (currNode.expanded)
                    {
                        count += currNode.TotalExpandedNodes;
                    }
                }
                return count;
            }
        }
        public bool IsVisible
        {
            get
            {
                return treeView.IsNodeVisible(this);
            }
        }
        public int VisibleIndex
        {
            get
            {
                return treeView.GetVisibleIndex(this);
            }
        }
        public int ExpandedTreeIndex
        {
            get
            {
                int index = 0;
                FastTreeNode currNode = this.PreviousUpNode;
                while (currNode != null)
                {
                    currNode = currNode.PreviousUpNode;
                    index++;
                }
                return index;
            }
        }
        public FastTreeNode PreviousUpNode
        {
            get
            {
                if (this.Index != 0)
                {
                    FastTreeNode previousNode = this.PreviousNode;
                    while (previousNode.IsExpanded && previousNode.HasChildren) //Goes to the last non-expanded child node from the previous node from the selected one
                    {
                        previousNode = previousNode.Nodes[previousNode.Nodes.Count - 1];
                    }
                    return previousNode;
                }
                else if (parent != null)
                {
                    return parent;
                }
                return null;
            }
        }
        public void Expand()
        {
            expanded = this.HasChildren;
            treeView.NodeRefreshCheck(this);
        }
        public void ExpandAll()
        {
            Expand();
            foreach (var currNode in nodes)
            {
                currNode.ExpandAll();
            }
        }
        public void Collapse()
        {
            expanded = false;
            treeView.NodeRefreshCheck(this);
        }
        public void CollapseAll()
        {
            Collapse();
            foreach (var currNode in nodes)
            {
                currNode.CollapseAll();
            }
        }
        public void EnsureVisible()
        {
            treeView.EnsureVisible(this);
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
        public void Remove()
        {
            this.ParentNodes.Remove(this);
        }
        public void Toggle()
        {
            if (!expanded)
            {
                Expand();
            }
            else
            {
                Collapse();
            }
        }
    }
}
