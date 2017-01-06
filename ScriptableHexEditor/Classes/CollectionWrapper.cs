using System;
using System.Collections;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public partial class CollectionWrapper<T> : ICollection<T>
    {
        ICollection iCollection;
        public CollectionWrapper(ICollection iCollection)
        {
            this.iCollection = iCollection;
        }
        public int Count
        {
            get
            {
                return iCollection.Count;
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }
        public bool Contains(T item)
        {
            foreach (var currItem in iCollection)
            {
                if (currItem.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new CollectionWrapperEnumerator<T>(iCollection.GetEnumerator());
        }
        public void Add(T item)
        {
            throw new NotSupportedException();
        }
        public void Clear()
        {
            throw new NotSupportedException();
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
    }
}
