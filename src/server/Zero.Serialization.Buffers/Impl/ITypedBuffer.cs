using System;
using System.Collections.Generic;
using System.IO;

namespace Zero.Serialization.Buffers.Impl
{
    public unsafe interface ITypedBuffer
    {
        void WriteTo(Stream output);
        ref byte[] GetRawBytesRef(int firstByteIndex);
        byte* GetRawRecordPointer(int firstByteIndex);
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
        int MaxRecordSizeBytes { get; }
    }
}