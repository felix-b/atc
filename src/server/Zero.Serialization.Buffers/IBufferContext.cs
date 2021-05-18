using System;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public interface IBufferContext
    {
        TypedBuffer<T> GetBuffer<T>() where T : struct;
        ITypedBuffer GetBuffer(Type recordType);
        BufferContextWalker GetWalker();
        ZStringRef GetString(string s);
        bool TryGetString(string s, out ZStringRef stringRef);
    }
}
