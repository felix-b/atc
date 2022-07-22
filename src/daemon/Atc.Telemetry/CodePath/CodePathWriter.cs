namespace Atc.Telemetry.CodePath;

public class CodePathWriter
{
    private readonly ICodePathEnvironment _environment;
    private readonly string _loggerName;

    public CodePathWriter(ICodePathEnvironment environment, string loggerName)
    {
        _environment = environment;
        _loggerName = loggerName;
    }

    public void Message(string id, CodePathLogLevel level)
    {
        var buffer = _environment.NewBuffer();
        buffer.WriteMessage(CurrentSpanId, Time, id, level);
        buffer.Flush();
    }

    public void Message<T1>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1)
    {
        var buffer = _environment.NewBuffer();
        
        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteEndMessage();
        
        buffer.Flush();
    }

    public void Message<T1, T2>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2)
    {
        var buffer = _environment.NewBuffer();
        
        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteEndMessage();
        
        buffer.Flush();
    }

    public void Message<T1, T2, T3>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3)
    {
        var buffer = _environment.NewBuffer();
        
        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteEndMessage();

        buffer.Flush();
    }

    public void Message<T1, T2, T3, T4>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4)
    {
        var buffer = _environment.NewBuffer();
        
        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteEndMessage();

        buffer.Flush();
    }

    public void Message<T1, T2, T3, T4, T5>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5)
    {
        var buffer = _environment.NewBuffer();

        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteEndMessage();

        buffer.Flush();
    }

    public void Message<T1, T2, T3, T4, T5, T6>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5,
        in (string name, T6 value) pair6)
    {
        var buffer = _environment.NewBuffer();

        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteValue(pair6.name, pair6.value);
        buffer.WriteEndMessage();
        
        buffer.Flush();
    }

    public void Message<T1, T2, T3, T4, T5, T6, T7>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5,
        in (string name, T6 value) pair6,
        in (string name, T7 value) pair7)
    {
        var buffer = _environment.NewBuffer();

        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteValue(pair6.name, pair6.value);
        buffer.WriteValue(pair7.name, pair7.value);
        buffer.WriteEndMessage();

        buffer.Flush();
    }

    public void Message<T1, T2, T3, T4, T5, T6, T7, T8>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5,
        in (string name, T6 value) pair6,
        in (string name, T7 value) pair7,
        in (string name, T8 value) pair8)
    {
        var buffer = _environment.NewBuffer();

        buffer.WriteBeginMessage(CurrentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteValue(pair6.name, pair6.value);
        buffer.WriteValue(pair7.name, pair7.value);
        buffer.WriteValue(pair8.name, pair8.value);
        buffer.WriteEndMessage();
        
        buffer.Flush();
    }

    public TraceSpan Span(string id, CodePathLogLevel level)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.Flush();

        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1, T2>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1, T2, T3>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1, T2, T3, T4>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1, T2, T3, T4, T5>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1, T2, T3, T4, T5, T6>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5,
        in (string name, T6 value) pair6)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteValue(pair6.name, pair6.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }
    
    public TraceSpan Span<T1, T2, T3, T4, T5, T6, T7>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5,
        in (string name, T6 value) pair6,
        in (string name, T7 value) pair7)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteValue(pair6.name, pair6.value);
        buffer.WriteValue(pair7.name, pair7.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public TraceSpan Span<T1, T2, T3, T4, T5, T6, T7, T8>(
        string id,
        CodePathLogLevel level,
        in (string name, T1 value) pair1,
        in (string name, T2 value) pair2,
        in (string name, T3 value) pair3,
        in (string name, T4 value) pair4,
        in (string name, T5 value) pair5,
        in (string name, T6 value) pair6,
        in (string name, T7 value) pair7,
        in (string name, T8 value) pair8)
    {
        SpawnNewSpan(out var spanId, out var parentSpanId);

        var buffer = _environment.NewBuffer();
        buffer.WriteBeginOpenSpan(spanId, parentSpanId, Time, id, level);
        buffer.WriteValue(pair1.name, pair1.value);
        buffer.WriteValue(pair2.name, pair2.value);
        buffer.WriteValue(pair3.name, pair3.value);
        buffer.WriteValue(pair4.name, pair4.value);
        buffer.WriteValue(pair5.name, pair5.value);
        buffer.WriteValue(pair6.name, pair6.value);
        buffer.WriteValue(pair7.name, pair7.value);
        buffer.WriteValue(pair8.name, pair8.value);
        buffer.WriteEndOpenSpan();
        buffer.Flush();
        
        return new TraceSpan(this, spanId, parentSpanId);
    }

    public bool ShouldWrite(CodePathLogLevel level)
    {
        return level <= _environment.GetLogLevel(_loggerName);
    }

    public void SpawnNewSpan(out ulong spanId, out ulong parentSpanId)
    {
        parentSpanId = _environment.GetCurrentSpanId();
        spanId = _environment.TakeNextSpanId();
        _environment.SetCurrentSpanId(spanId);
    }

    public CodePathLogLevel Level => _environment.GetLogLevel(_loggerName);
    public DateTime Time => _environment.GetUtcNow();
    public ulong CurrentSpanId => _environment.GetCurrentSpanId();

    private static readonly string _errorCodeKey = "ErrorCode";

    // public static readonly CodePathWriter Noop = CreateNoop();
    //
    // private static CodePathWriter CreateNoop()
    // {
    //     var noopStream = new NoopLogStreamWriter();
    //     return new LogWriter(
    //         getLevel: () => CodePathLogLevel.Error, 
    //         getStream: () => noopStream, 
    //         getTime: () => DateTime.UtcNow);
    // }
    
    public struct TraceSpan : ITraceSpan
    {
        private readonly CodePathWriter? _writer;
        private readonly ulong _spanId;
        private readonly ulong _parentSpanId;
        private bool _disposed;
        private Exception? _exception;
        private string? _errorCode;

        public TraceSpan(CodePathWriter writer, ulong spanId, ulong parentSpanId)
        {
            _writer = writer;
            _spanId = spanId;
            _parentSpanId = parentSpanId;
            _disposed = false;
            _exception = null;
            _errorCode = null;
        }

        public void Fail(Exception exception)
        {
            if (!_disposed)
            {
                _exception = exception;
            }
        }

        public void Fail(string errorCode)
        {
            if (!_disposed)
            {
                _errorCode = errorCode;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var buffer = _writer!._environment.NewBuffer();

            if (_exception == null && _errorCode == null)
            {
                buffer.WriteCloseSpan(_spanId, _writer.Time);
            }
            else
            {
                buffer.WriteBeginCloseSpan(_spanId, _writer.Time);
                if (_errorCode != null)
                {
                    buffer.WriteValue(_errorCodeKey, _errorCode);
                }
                if (_exception != null)
                {
                    buffer.WriteException(_exception);
                }
                buffer.WriteEndCloseSpan();
            }
            
            buffer.Flush();
            _writer._environment.SetCurrentSpanId(_parentSpanId);
        }
    }
}
