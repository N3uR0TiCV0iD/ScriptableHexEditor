using System;
using System.Collections;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class FastNodesCollection : IList<FastTreeNode>, ICollection<FastTreeNode>, IEnumerable<FastTreeNode>
    {
        FastTreeNode node; //The node that has this collection
        List<FastTreeNode> nodes;
        FastTreeView treeView;
        public FastNodesCollection(FastTreeView treeView, FastTreeNode node)
        {
            this.node = node;
            this.treeView = treeView;
            this.nodes = new List<FastTreeNode>();
        }
        public FastTreeNode this[int index]
        {
            get
            {
                return nodes[index];
            }
            set
            {
                nodes[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return nodes.Count;
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        public FastTreeNode Add(string text)
        {
            FastTreeNode newNode = new FastTreeNode(treeView, text, this.node);
            newNode.ImageIndex = treeView.ImageIndex;
            newNode.ImageKey = treeView.ImageKey;
            Add(newNode);
            return newNode;
        }
        public void Add(FastTreeNode item)
        {
            nodes.Add(item);
            treeView.NodeRefreshCheck(item);
        }
        public void Clear()
        {
            nodes.Clear();
            treeView.Refresh();
        }
        public bool Contains(FastTreeNode item)
        {
            return nodes.Contains(item);
        }
        public void CopyTo(FastTreeNode[] array, int arrayIndex)
        {
            nodes.CopyTo(array, arrayIndex);
            treeView.Refresh();
        }
        public IEnumerator<FastTreeNode> GetEnumerator()
        {
            return nodes.GetEnumerator();
        }
        public int IndexOf(FastTreeNode item)
        {
            return nodes.IndexOf(item);
        }
        public void Insert(int index, FastTreeNode item)
        {
            throw new NotImplementedException();
        }
        public bool Remove(FastTreeNode item)
        {
            if (nodes.Remove(item))
            {
                treeView.Refresh();
                return true;
            }
            return false;
        }
        public void RemoveAt(int index)
        {
            nodes.RemoveAt(index);
            treeView.Refresh();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
