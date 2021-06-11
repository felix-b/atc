using System;
using System.Collections.Generic;

namespace Zero.Serialization.Buffers
{
    public record BufferContextInfo(
        IReadOnlyList<BufferInfo> Buffers
    );

    public record BufferInfo(
        Type RecordType,
        int RecordCount,
        int BufferSizeBytes,
        int RecordSizeBytes,
        bool IsVariableSize,
        int BytesToSkipToEndOfBuffer 
    );
}