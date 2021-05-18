using System;
using System.Runtime.CompilerServices;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public unsafe readonly struct ZCursor<T> 
        where T : unmanaged
    {
        private readonly ITypedBuffer? _buffer;
        private readonly int _byteIndex;

        public ZCursor(ITypedBuffer buffer, int byteIndex)
        {
            _buffer = buffer;
            _byteIndex = byteIndex;
        }

        public ref T Get()
        {
            ValidateNotNull();

            ref var bytes = ref _buffer!.GetRawBytesRef(_byteIndex);
            ref var value = ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref bytes));
            return ref value;
        }

        public bool IsNull => _buffer == null || _byteIndex < 0;

        internal ZCursor<TField> AccessField<TField>(int offsetBytes) 
            where TField : unmanaged
        {
            ValidateNotNull();
            return new ZCursor<TField>(_buffer!, _byteIndex + offsetBytes);
        }

        private void ValidateNotNull()
        {
            if (IsNull)
            {
                throw new InvalidOperationException("ZCursor instance is a null reference");
            }
        }
    }
}