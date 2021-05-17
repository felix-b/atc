using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public struct Vector<T> where T : struct
    {
        private BufferPtr<VectorRecord<T>> _inner;

        public Vector(BufferPtr<VectorRecord<T>> inner)
        {
            _inner = inner;
        }
        
        public BufferPtr<T> Add()
        {
            var itemPtr = BufferContext.Current.GetBuffer<T>().Allocate();
            _inner.Get().Add(itemPtr);
            return itemPtr;
        }

        public BufferPtr<T> Add(T value)
        {
            var itemPtr = BufferContext.Current.GetBuffer<T>().Allocate(value);
            _inner.Get().Add(itemPtr);
            return itemPtr;
        }

        public void Add(BufferPtr<T> itemPtr)
        {
            _inner.Get().Add(itemPtr);
        }
        
        public int Count => _inner.Get().Count;

        public ref T this[int index]
        {
            get
            {
                return ref _inner.Get()[index].Get();
            }
        }
    }
}
