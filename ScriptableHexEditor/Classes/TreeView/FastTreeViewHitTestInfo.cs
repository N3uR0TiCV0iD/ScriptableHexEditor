using System;
using System.Drawing;
namespace ScriptableHexEditor
{
    public class FastTreeViewHitTestInfo
    {
        Point location;
        FastTreeNode node;
        public FastTreeViewHitTestInfo(FastTreeNode node, Point location)
        {
            this.node = node;
            this.location = location;
        }
        public FastTreeNode Node
        {
            get
            {
                return node;
            }
        }
        public Point Location
        {
            get
            {
                return location;
            }
        }
    }
}
