using System;
using System.Runtime.InteropServices;

namespace Zero.Serialization.Buffers.Impl
{
    public unsafe struct StringRecord : IVariableSizeRecord
    {
        private int _length; 
        private int _inflatedHandle;
        private fixed char _chars[1];

        public StringRecord(int length)
        {
            _length = length >= 0 
                ? length 
                : throw new ArgumentOutOfRangeException(nameof(length), "String length cannot be negative");

            _length = length;
            _inflatedHandle = -1;
            _chars[0] = '\0';
        }

        public override string ToString()
        {
            return Str;
        }

        public int SizeOf()
        {
            return SizeOf(_length);
        }

        public string Str
        {
            get
            {
                if (_inflatedHandle >= 0)
                {
                    return BufferContext.Current.GetInflatedString(_inflatedHandle);
                }

                string s;

                fixed (char* p = _chars)
                {
                    s = new string(p, 0, _length);
                }

                return s;
            }
        }

        internal void SetValue(string value)
        {
            _length = value.Length;
            _inflatedHandle = -1;                                                                                                                                                       

            for (int i = 0; i < value.Length; i++)
            {
                _chars[i] = value[i];
            }
        }

        internal int InflatedHandle
        {
            get => _inflatedHandle;
            set => _inflatedHandle = value;
        }

        //TODO: having a managed ref changes the layout; remove _inflated, to use simple layout assumptions  
        private static readonly int _baseSize = 10;//Marshal.SizeOf(typeof(StringRecord));

        public static ZRef<StringRecord> Allocate(string value, IBufferContext? context = null)
        {
            var effectiveContext = context ?? BufferContext.Current;
            var realContext = effectiveContext as BufferContext;

            if (realContext != null && realContext.TryGetString(value, out var stringRef))
            {
                return stringRef!.Value;
            }
            
            var size = SizeOf(charCount: value.Length);
            var ptr = effectiveContext.GetBuffer<StringRecord>().Allocate(size);
            ptr.Get().SetValue(value);

            realContext?.RegisterAllocatedString(value, ptr);
            return ptr;
        }
        
        public static int SizeOf(int charCount)
        {
            return _baseSize + (charCount - 1) * sizeof(char);
        }
    }
}
