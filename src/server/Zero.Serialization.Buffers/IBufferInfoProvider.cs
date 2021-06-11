using System.Collections.Generic;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public interface IBufferInfoProvider
    {
        IEnumerable<(string Label, string Value)> GetInfo(ITypedBuffer buffer);
    }
}
