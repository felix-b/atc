using Atc.Data.Buffers.Impl;

namespace Atc.Data.Buffers
{
    public struct StringRef
    {
        private BufferPtr<StringRecord> _inner;

        public StringRef(BufferPtr<StringRecord> inner)
        {
            _inner = inner;
        }

        public string Value => _inner.Get().Str;

        public int Length => Value.Length;

        public char this[int index] => Value[index];
        
        public static implicit operator string(StringRef r) => r.Value;
    }
}
