using System.Collections.Generic;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public struct IntMap<TValue>
        where TValue : struct
    {
        private readonly BufferPtr<IntMapRecord<TValue>> _inner;

        public IntMap(BufferPtr<IntMapRecord<TValue>> inner)
        {
            _inner = inner;
        }

        public bool Contains(int key) => _inner.Get().Contains(key);

        public void Add(int key, BufferPtr<TValue> value) => _inner.Get().Add(key, value);

        public void Add(int key, TValue value) => _inner.Get().Add(key, AllocateValue(value));

        public void Set(int key, BufferPtr<TValue> value) => _inner.Get().Set(key, value);

        public void Set(int key, TValue value) => _inner.Get().Set(key, AllocateValue(value));

        public bool TryAdd(int key, BufferPtr<TValue> value) => _inner.Get().TryAdd(key, value);

        public bool TryAdd(int key, TValue value) => _inner.Get().TryAdd(key, AllocateValue(value));

        public BufferPtr<TValue>? TryGetValue(int key) => _inner.Get().TryGetValue(key);

        public int Count => _inner.Get().Count;

        public IEnumerable<int> Keys => _inner.Get().Keys;

        public IEnumerable<BufferPtr<TValue>> Values => _inner.Get().Values;

        public ref TValue this[int key] => ref _inner.Get()[key].Get();

        private BufferPtr<TValue> AllocateValue(TValue value)
        {
            return BufferContext.Current.AllocateRecord(value);
        }
    }
}
