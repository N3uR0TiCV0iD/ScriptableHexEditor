using System;
namespace ScriptableHexEditor
{
    public class FastTreeViewEventArgs : EventArgs
    {
        FastTreeNode node;
        public FastTreeViewEventArgs(FastTreeNode node)
        {
            this.node = node;
        }
        public FastTreeNode Node
        {
            get
            {
                return node;
            }
        }
    }
}
