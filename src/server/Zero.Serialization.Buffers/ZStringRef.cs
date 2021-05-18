using System;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public struct ZStringRef : IEquatable<ZStringRef>
    {
        private ZRef<StringRecord> _inner;

        public ZStringRef(ZRef<StringRecord> inner)
        {
            _inner = inner;
        }

        public string GetValueNonCached()
        {
            return _inner.Get().GetStrAndDeflate();
        }
        
        public string Value => _inner.Get().Str;

        public int Length => Value.Length;

        public char this[int index] => Value[index];

        public ZRef<StringRecord> GetRef() => _inner;

        public bool Equals(ZStringRef other)
        {
            return _inner.ByteIndex == other.ByteIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is ZStringRef other && Equals(other);
        }
        
        public static bool operator==(ZStringRef left, ZStringRef right)
        {
            return (left._inner.ByteIndex == right._inner.ByteIndex);
        }

        public static bool operator !=(ZStringRef left, ZStringRef right)
        {
            return !(left._inner == right._inner);
        }

        public override int GetHashCode()
        {
            return _inner.GetHashCode();
        }
        
        internal int ByteIndex => _inner.ByteIndex;
        
        public static implicit operator string(ZStringRef r) => r.Value;

        public static implicit operator ZRef<StringRecord>(ZStringRef r) => r._inner;
    }
}
