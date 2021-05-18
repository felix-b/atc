using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public struct ZIntMapRef<TValue>
        where TValue : unmanaged
    {
        private readonly ZRef<IntMapRecord<TValue>> _inner;

        public ZIntMapRef(ZRef<IntMapRecord<TValue>> inner)
        {
            _inner = inner;
        }

        public bool Contains(int key) => _inner.Get().Contains(key);

        public void Add(int key, ref TValue value) => _inner.Get().Add(key, ref value);

        public void Add(int key, TValue value) => _inner.Get().Add(key, value);

        public void Set(int key, ref TValue value) => _inner.Get().Set(key, ref value);

        public void Set(int key, TValue value) => _inner.Get().Set(key, value);

        public bool TryAdd(int key, ref TValue value) => _inner.Get().TryAdd(key, ref value);

        public bool TryAdd(int key, TValue value) => _inner.Get().TryAdd(key, value);

        public bool TryGetValue(int key, out TValue value)
        {
            ref var valueOrNull = ref _inner.Get().TryGetValue(key);
            if (!Unsafe.IsNullRef(ref valueOrNull))
            {
                value = valueOrNull;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public int Count => _inner.Get().Count;

        public IEnumerable<int> Keys => _inner.Get().Keys;

        public IEnumerable<ZCursor<TValue>> Values => _inner.Get().Values;

        public ref TValue this[int key] => ref _inner.Get()[key];
    }
}
