using Atc.Data.Buffers.Impl;

namespace Atc.Data.Buffers
{
    public interface IBufferContext
    {
        public TypedBuffer<T> GetBuffer<T>() where T : struct;
        public BufferContextWalker GetWalker();
    }
}
