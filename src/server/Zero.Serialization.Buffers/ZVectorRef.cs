using System.Collections;
using System.Collections.Generic;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public struct ZVectorRef<T> : IEnumerable<ZCursor<T>>
        where T : unmanaged
    {
        private ZRef<VectorRecord<T>> _inner;

        public ZVectorRef(ZRef<VectorRecord<T>> inner)
        {
            _inner = inner;
        }
        
        public void Add(ref T value)
        {
            _inner.Get().Add(ref value);
        }

        public void Add(T value)
        {
            _inner.Get().Add(ref value);
        }

        public IEnumerator<ZCursor<T>> GetEnumerator()
        {
            return _inner.Get().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public ref VectorRecord<T> GetVectorRecord() => ref _inner.Get();
        
        public int Count => _inner.Get().Count;

        public ref T this[int index]
        {
            get
            {
                return ref _inner.Get()[index];
            }
        }
    }
}
