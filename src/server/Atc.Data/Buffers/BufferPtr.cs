namespace Atc.Data.Buffers
{
    public readonly struct BufferPtr<T>
        where T : struct
    {
        private readonly int _byteIndex;

        public BufferPtr(int byteIndex)
        {
            _byteIndex = byteIndex;
        }
        
        public ref T Get()
        {
            var buffer = BufferContextScope.CurrentContext.GetBuffer<T>(); 
            return ref buffer[_byteIndex];
        }
        
        internal int ByteIndex => _byteIndex;
    }
}
