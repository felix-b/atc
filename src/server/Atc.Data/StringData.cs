using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Atc.Data
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct StringData
    {
        [FieldOffset(0)] internal byte _length; 
        [FieldOffset(8)] internal string? _inflated;
        [FieldOffset(16)] internal fixed char _chars[100];

        public StringData(string value)
        {
            _inflated = null;
            _length = value.Length < 100 
                ? (byte)value.Length 
                : throw new ArgumentOutOfRangeException(nameof(value), "String of 100 or more characters is not supported.");


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
        
        public ref byte GetPinnableReference()
        {
            fixed (void* p = &_length)
            {
                return ref Unsafe.AsRef<byte>(p);
            }
        }
    }
}
