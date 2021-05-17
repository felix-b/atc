using System;

namespace Zero.Serialization.Buffers
{
    public readonly struct BufferPtr<T> : IEquatable<BufferPtr<T>>
        where T : struct
    {
        private readonly int _byteIndex;

        public BufferPtr(int byteIndex)
        {
            _byteIndex = byteIndex;
        }
        
        public ref T Get()
        {
            if (IsNull)
            {
                throw new NullReferenceException($"Attempt to dereference BufferPtr<{typeof(T).Name}> which is set to null");
            }
            
            var buffer = BufferContextScope.CurrentContext.GetBuffer<T>(); 
            return ref buffer[_byteIndex];
        }

        public bool Equals(BufferPtr<T> other)
        {
            return _byteIndex == other._byteIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is BufferPtr<T> other && Equals(other);
        }
        
        public static bool operator==(BufferPtr<T> left, BufferPtr<T> right)
        {
            return (left._byteIndex == right._byteIndex);
        }

        public static bool operator !=(BufferPtr<T> left, BufferPtr<T> right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return _byteIndex;
        }
        
        public bool IsNull => _byteIndex < 0;
        
        public int ByteIndex => _byteIndex;

        public static readonly BufferPtr<T> Null = new BufferPtr<T>(-1);
    }

    public static class BufferPtr
    {
        public const int NullByteIndex = -1;

        public static bool IsNull(int byteIndex)
        {
            return (byteIndex < 0);
        }

        public static bool IsNull<T>(BufferPtr<T> ptr) where T : struct
        {
            return ptr.IsNull;
        }
    }
}
