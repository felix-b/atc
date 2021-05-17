using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public interface IBufferContext
    {
        public TypedBuffer<T> GetBuffer<T>() where T : struct;
        public BufferContextWalker GetWalker();
    }
}
