using System;
using System.Collections;
using System.Collections.Generic;
namespace ScriptableHexEditor
{
    public partial class CollectionWrapper<T>
    {
        private class CollectionWrapperEnumerator<T> : IEnumerator<T>
        {
            IEnumerator iCollectionEnumerator;
            public CollectionWrapperEnumerator(IEnumerator iCollectionEnumerator)
            {
                this.iCollectionEnumerator = iCollectionEnumerator;
            }
            public T Current
            {
                get
                {
                    return (T)iCollectionEnumerator.Current;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    return iCollectionEnumerator.Current;
                }
            }
            public bool MoveNext()
            {
                return iCollectionEnumerator.MoveNext();
            }
            public void Reset()
            {
                iCollectionEnumerator.Reset();
            }
            public void Dispose()
            {
                throw new NotSupportedException();
            }
        }
    }
}
