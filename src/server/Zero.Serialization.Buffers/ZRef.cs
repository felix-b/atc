using System;

namespace Zero.Serialization.Buffers
{
    public readonly struct ZRef<T> : IEquatable<ZRef<T>>
        where T : struct
    {
        private readonly int _byteIndex;

        public ZRef(int byteIndex)
        {
            _byteIndex = byteIndex;
        }
        
        public ref T Get()
        {
            if (IsNull)
            {
                throw new NullReferenceException($"Attempt to dereference a null ZRecord<{typeof(T).Name}>");
            }
            
            var buffer = BufferContextScope.CurrentContext.GetBuffer<T>(); 
            return ref buffer[_byteIndex];
        }

        public bool Equals(ZRef<T> other)
        {
            return _byteIndex == other._byteIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is ZRef<T> other && Equals(other);
        }
        
        public static bool operator==(ZRef<T> left, ZRef<T> right)
        {
            return (left._byteIndex == right._byteIndex);
        }

        public static bool operator !=(ZRef<T> left, ZRef<T> right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return _byteIndex;
        }
        
        public bool IsNull => _byteIndex < 0;
        
        public int ByteIndex => _byteIndex;

        public static readonly ZRef<T> Null = new ZRef<T>(-1);

        public static implicit operator ZRefAny(ZRef<T> zref)
        {
            return new ZRefAny(zref._byteIndex);
        }
    }

    public readonly struct ZRefAny : IEquatable<ZRefAny>
    {
        private readonly int _byteIndex;

        public ZRefAny(int byteIndex)
        {
            _byteIndex = byteIndex;
        }
        
        public ref T GetAs<T>() where T : unmanaged
        {
            if (IsNull)
            {
                throw new NullReferenceException($"Attempt to dereference a null ZRecord<{typeof(T).Name}>");
            }
            
            var buffer = BufferContextScope.CurrentContext.GetBuffer<T>(); 
            return ref buffer[_byteIndex];
        }

        public ZRef<T> AsZRef<T>() where T : unmanaged
        {
            return new ZRef<T>(_byteIndex);
        }

        public bool Equals(ZRefAny other)
        {
            return _byteIndex == other._byteIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is ZRefAny other && Equals(other);
        }
        
        public static bool operator==(ZRefAny left, ZRefAny right)
        {
            return (left._byteIndex == right._byteIndex);
        }

        public static bool operator !=(ZRefAny left, ZRefAny right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return _byteIndex;
        }
        
        public bool IsNull => _byteIndex < 0;
        
        public int ByteIndex => _byteIndex;

        public static readonly ZRefAny Null = new ZRefAny(-1);
    }

    public static class ZRef
    {
        public const int NullByteIndex = -1;

        public static bool IsNull(int byteIndex)
        {
            return (byteIndex < 0);
        }

        public static bool IsNull<T>(ZRef<T> ptr) where T : struct
        {
            return ptr.IsNull;
        }
    }
}
