using System;
using System.IO;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public class ExpandableStream : Stream, IDisposable
    {
        long length;
        long position;
        bool overwrite;
        List<MemoryStream> fragments;
        public ExpandableStream()
        {
            this.fragments = new List<MemoryStream>();
            this.overwrite = true;
        }
        public bool Overwrite
        {
            get
            {
                return overwrite;
            }
            set
            {
                overwrite = value;
            }
        }
        public bool EndOfStream
        {
            get
            {
                return position == length;
            }
        }
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }
        public override long Length
        {
            get
            {
                return length;
            }
        }
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value >= 0 && value <= length)
                {
                    position = value;
                }
                else
                {
                    throw new Exception();
                }
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            long subPosition;
            int readBytes = 0;
            long remainingBytes = count;
            int currFragmentIndex = GetFragmentIndex(this.position, out subPosition);
            MemoryStream currFragment = fragments[currFragmentIndex];
            long lengthFromPosition = currFragment.Length - subPosition;
            currFragment.Position = subPosition;
            //System.Windows.Forms.MessageBox.Show("subPosition = " + subPosition);
            while (remainingBytes > 0 && this.position != this.length)
            {
                if (lengthFromPosition < remainingBytes) //Can we read the remaining bytes?
                {
                    //No we can't. Let's read what we can
                    currFragment.Read(buffer, offset + readBytes, (int)lengthFromPosition);
                    remainingBytes -= lengthFromPosition;
                    readBytes += (int)lengthFromPosition;
                    currFragmentIndex++;
                    if (currFragmentIndex < fragments.Count)
                    {
                        currFragment = fragments[currFragmentIndex];
                        lengthFromPosition = currFragment.Length;
                        currFragment.Position = 0;
                    }
                }
                else
                {
                    //Yes we can! Just read normally
                    readBytes += currFragment.Read(buffer, offset + readBytes, (int)remainingBytes);
                    remainingBytes = 0;
                }
            }
            this.position += readBytes;
            return readBytes;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                break;
                case SeekOrigin.Current:
                    position += offset;
                break;
                case SeekOrigin.End:
                    position = length - offset;
                break;
            }
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            if (value >= 0)
            {
                if (fragments.Count != 0)
                {
                    long extraLength;
                    if (value > length)
                    {
                        //Append 0s to the last fragment
                        extraLength = value - length;
                        fragments[fragments.Count - 1].Write(new byte[extraLength], 0, (int)extraLength);
                        this.length = value;
                    }
                    else if (value < length)
                    {
                        //Shorten the stream
                        MemoryStream currFragment;
                        int currFragmentIndex = fragments.Count - 1;
                        extraLength = length - value;
                        while (extraLength > 0)
                        {
                            currFragment = fragments[currFragmentIndex];
                            if (extraLength >= currFragment.Length)
                            {
                                fragments.RemoveAt(currFragmentIndex); //Delete the entire fragment
                                extraLength -= currFragment.Length;
                                currFragmentIndex--;
                            }
                            else
                            {
                                currFragment.SetLength(currFragment.Length - extraLength); //Remove the extra length from the current fragment
                                extraLength = 0;
                            }
                        }
                        this.length = value;
                    }
                }
                else
                {
                    AddFirstFragment(new byte[value], 0, (int)value);
                }
            }
            else
            {
                throw new Exception();
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            //System.Windows.Forms.MessageBox.Show("Write");
            if (overwrite)
            {
                OverWrite(buffer, offset, count);
            }
            else
            {
                InsertWrite(buffer, offset, count);
            }
        }
        public void OverWrite(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return;
            }
            if (fragments.Count != 0)
            {
                long subPosition;
                int lengthFromSubPosition;
                int currFragmentIndex = GetFragmentIndex(this.position, out subPosition);
                bool isLastFragment = currFragmentIndex == (fragments.Count - 1);
                MemoryStream currFragment = fragments[currFragmentIndex];
                currFragment.Position = subPosition;
                lengthFromSubPosition = (int)(currFragment.Length - subPosition);
                if (isLastFragment || lengthFromSubPosition >= count) //Are we on the last fragment? | Are we going to end up appending?
                {
                    //Yes we are || We won't end up appending! Just write normally
                    if (isLastFragment)
                    {
                        ExtraLengthAddCheck(count - lengthFromSubPosition);
                    }
                    currFragment.Write(buffer, offset, count);
                }
                else
                {
                    //Overwrite and make sure we don't append
                    int remainingBytes = count - lengthFromSubPosition;
                    currFragment.Write(buffer, offset, lengthFromSubPosition);
                    subPosition = lengthFromSubPosition; //Reutilizing variable. The subPosition of the "buffer"
                    currFragmentIndex++;
                    while (remainingBytes > 0)
                    {
                        currFragment = fragments[currFragmentIndex];
                        currFragment.Position = 0;
                        isLastFragment = currFragmentIndex == (fragments.Count - 1);
                        if (!isLastFragment && currFragment.Length < remainingBytes) //Is it the last fragment? | Are we going to be able to write the remaining bytes?
                        {
                            currFragment.Write(buffer, offset + (int)subPosition, (int)currFragment.Length);
                            remainingBytes -= (int)currFragment.Length;
                            subPosition += currFragment.Length;
                        }
                        else
                        {
                            //Yes it is! || We can write the remaining bytes! Just write the remaining bytes normally
                            if (isLastFragment)
                            {
                                System.Windows.Forms.MessageBox.Show("extraLength = " + (remainingBytes - currFragment.Length));
                                ExtraLengthAddCheck(remainingBytes - currFragment.Length);
                            }
                            currFragment.Write(buffer, offset + (int)subPosition, remainingBytes);
                            remainingBytes = 0;
                        }
                        currFragmentIndex++;
                    }
                }
            }
            else
            {
                AddFirstFragment(buffer, offset, count);
            }
            this.position += count;
        }
        private void ExtraLengthAddCheck(long extraLength)
        {
            if (extraLength > 0)
            {
                this.length += extraLength;
            }
        }
        public void InsertWrite(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return;
            }
            if (fragments.Count != 0)
            {
                long subPosition;
                MemoryStream currFragment;
                int currFragmentIndex = GetFragmentIndex(this.position, out subPosition);
                if (subPosition == 0) //Are we going to write at the start of the fragment?
                {
                    //Yes! Let's check if it is the first fragment...
                    if (currFragmentIndex != 0) //Is it the first fragment?
                    {
                        //It's not! Get the previous fragment and just append to it
                        currFragment = fragments[currFragmentIndex - 1];
                        currFragment.Position = currFragment.Length;
                        currFragment.Write(buffer, offset, count);
                    }
                    else
                    {
                        currFragment = new MemoryStream();
                        currFragment.Write(buffer, offset, count);
                        fragments.Insert(0, currFragment);
                    }
                }
                else
                {
                    currFragment = fragments[currFragmentIndex];
                    if (subPosition == currFragment.Length) //Are we going to write at the end of the fragment?
                    {
                        //Yes we are! Let's just append normally
                        currFragment.Position = subPosition;
                        currFragment.Write(buffer, offset, count);
                    }
                    else
                    {
                        //We are going to write before the end of the fragment and after it starts
                        int lengthFromSubPosition = (int)(currFragment.Length - subPosition);
                        MemoryStream newFragment = new MemoryStream(); //Right split
                        currFragment.Position = subPosition;
                        currFragmentIndex++; //Reutilizing variable. The insertion index
                        using (BinaryReader fragmentReader = new BinaryReader(currFragment))
                        {
                            newFragment.Write(fragmentReader.ReadBytes(lengthFromSubPosition), 0, lengthFromSubPosition);
                        }
                        fragments.Insert(currFragmentIndex, newFragment);
                        newFragment = new MemoryStream(); //Inserted fragment
                        newFragment.Write(buffer, offset, count);
                        fragments.Insert(currFragmentIndex, newFragment);
                        currFragment.SetLength(subPosition);
                    }
                }
                this.length += count;
            }
            else
            {
                AddFirstFragment(buffer, offset, count);
            }
            this.position += count;
        }
        private void AddFirstFragment(byte[] buffer, int offset, int count)
        {
            MemoryStream newFragment = new MemoryStream();
            newFragment.Write(buffer, offset, count);
            fragments.Add(newFragment);
            this.length = count;
        }
        private MemoryStream GetFragment(long position, out long subPosition)
        {
            return fragments[GetFragmentIndex(position, out subPosition)];
        }
        private int GetFragmentIndex(long position, out long subPosition)
        {
            long nextPosition;
            long currPosition = 0;
            for (int currFragmentIndex = 0; currFragmentIndex < fragments.Count; currFragmentIndex++)
            {
                nextPosition = currPosition + fragments[currFragmentIndex].Length;
                //System.Windows.Forms.MessageBox.Show(nextPosition + " vs " + position);
                if (nextPosition >= position)
                {
                    subPosition = position - currPosition;
                    return currFragmentIndex;
                }
                currPosition = nextPosition;
            }
            subPosition = 0;
            return -1;
        }
        public override void Flush() { }
    }
}
