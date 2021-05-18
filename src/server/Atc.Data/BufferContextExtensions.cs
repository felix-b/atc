using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data
{
    public static class BufferContextExtensions
    {
        public static ref WorldData GetWorldData(this IBufferContext context)
        {
            return ref context.GetBuffer<WorldData>()[0];
        }
    }
}