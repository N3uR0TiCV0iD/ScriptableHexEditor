using System;
using System.IO;
namespace ScriptableHexEditor
{
    public class StreamFragment
    {
        MemoryStream stream;
        public StreamFragment()
        {
            this.stream = new MemoryStream();
        }
    }
}
