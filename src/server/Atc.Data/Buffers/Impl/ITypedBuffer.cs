using System;
using System.Collections.Generic;
using System.IO;

namespace Atc.Data.Buffers.Impl
{
    public interface ITypedBuffer
    {
        void WriteTo(Stream output);
        Memory<byte> GetRecordMemory(int index, out int offset);        
        ref byte[] GetRawBytesRef(int firstByteIndex);
        bool ReadOnly { get; }
        int InitialCapacity { get; }
        Type RecordType { get; }
        StructTypeHandler TypeHandler { get; }
        int RecordSize { get; }
        bool IsVariableRecordSize { get; }
        int RecordCount { get; }
        IReadOnlyList<int> RecordOffsets { get; }
        int TotalBytes { get; }
        int AllocatedBytes { get; }
    }
}