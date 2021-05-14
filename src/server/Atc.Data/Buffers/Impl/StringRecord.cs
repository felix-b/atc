using System;
using System.Runtime.InteropServices;

namespace Atc.Data.Buffers.Impl
{
    public unsafe struct StringRecord : IVariableSizeRecord
    {
        private int _length; 
        private string? _inflated;
        private fixed char _chars[1];

        public StringRecord(int length)
        {
            _length = length >= 0 
                ? length 
                : throw new ArgumentOutOfRangeException(nameof(length), "String length cannot be negative");

            _length = length;
            _inflated = null;
            _chars[0] = '\0';
        }

        private void SetValue(string value)
        {
            _length = value.Length;
            _inflated = null;

            for (int i = 0; i < value.Length; i++)
            {
                _chars[i] = value[i];
            }
        }

        public override string ToString()
        {
            if (_inflated == null)
            {
                fixed (char* p = _chars)
                {
                    _inflated = new string(p, 0, _length);
                }
            }
            
            return _inflated;
        }

        public int SizeOf()
        {
            return SizeOf(_length);
        }

        public string Str => ToString();
        
        private static readonly int _baseSize = Marshal.SizeOf(typeof(StringRecord));

        public static BufferPtr<StringRecord> Allocate(string value, IBufferContext? context = null)
        {
            var size = SizeOf(charCount: value.Length);
            var effectiveContext = context ?? BufferContext.Current; 
            var ptr = effectiveContext.GetBuffer<StringRecord>().Allocate(size);
            ptr.Get().SetValue(value);
            return ptr;
        }
        
        public static int SizeOf(int charCount)
        {
            return _baseSize + (charCount - 1) * sizeof(char);
        }
    }
}
