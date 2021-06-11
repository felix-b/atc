using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public readonly struct ZStringMapRef<TValue>
        where TValue : unmanaged
    {
        private readonly ZRef<IntMapRecord<TValue>> _inner;

        public ZStringMapRef(ZRef<IntMapRecord<TValue>> inner)
        {
            _inner = inner;
        }

        public bool Contains(ZStringRef key) => _inner.Get().Contains(key.ByteIndex);

        public void Add(ZStringRef key, ref TValue value) => _inner.Get().Add(key.ByteIndex, ref value);

        public void Add(ZStringRef key, TValue value) => _inner.Get().Add(key.ByteIndex, value);

        public void Set(ZStringRef key, ref TValue value) => _inner.Get().Set(key.ByteIndex, ref value);

        public void Set(ZStringRef key, TValue value) => _inner.Get().Set(key.ByteIndex, value);

        public bool TryAdd(ZStringRef key, ref TValue value) => _inner.Get().TryAdd(key.ByteIndex, ref value);

        public bool TryAdd(ZStringRef key, TValue value) => _inner.Get().TryAdd(key.ByteIndex, value);

        public bool TryGetValue(string key, out TValue value)
        {
            if (BufferContext.Current.TryGetString(key, out var stringRef))
            {
                return TryGetValue(stringRef, out value);
            }

            value = default(TValue);
            return false;
        }

        public bool TryGetValue(ZStringRef key, out TValue value)
        {
            ref var valueOrNull = ref _inner.Get().TryGetValue(key.ByteIndex);
            if (!Unsafe.IsNullRef(ref valueOrNull))
            {
                value = valueOrNull;
                return true;
            }
            value = default(TValue);
            return false;
        } 

        public int Count => _inner.Get().Count;

        public IEnumerable<ZStringRef> Keys => 
            _inner.Get().Keys.Select(byteIndex => new ZStringRef(new ZRef<StringRecord>(byteIndex)));

        public IEnumerable<ZCursor<TValue>> Values => _inner.Get().Values;

        public ref TValue this[ZStringRef key] => ref _inner.Get()[key.ByteIndex];

        public ref TValue this[string key]
        {
            get
            {
                var stringRef = BufferContext.Current.GetString(key);
                return ref _inner.Get()[stringRef.ByteIndex];                
            }
        }
    }
}
