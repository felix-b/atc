using System.Threading.Channels;

namespace Atc.Telemetry.CodePath;

public class CodePathEnvironment : ICodePathEnvironment
{
    public const ulong RootSpanId = 0;
    
    private readonly CodePathLogLevel _logLevel;
    private readonly ICodePathExporter _output;
    private readonly AsyncLocal<ulong> _currentSpanId = new();
    private readonly CodePathStringMap _stringMap = new();
    private ulong _nextSpanId = 0;

    public CodePathEnvironment(CodePathLogLevel logLevel, ICodePathExporter output)
    {
        _logLevel = logLevel;
        _output = output;
        _currentSpanId.Value = RootSpanId;

        output.InjectEnvironment(this);
    }

    public virtual DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }

    public virtual CodePathLogLevel GetLogLevel(string? loggerName = null)
    {
        return _logLevel; //TODO: support level per logger
    }

    public ulong GetCurrentSpanId()
    {
        return _currentSpanId.Value;
    }

    public void SetCurrentSpanId(ulong spanId)
    {
        _currentSpanId.Value = spanId;
    }

    public ulong TakeNextSpanId()
    {
        return Interlocked.Increment(ref _nextSpanId);
    }

    public CodePathBufferWriter NewBuffer()
    {
        return new CodePathBufferWriter(_stringMap, output: _output);
    }

    public CodePathStringMap GetStringMap()
    {
        return _stringMap;
    }
}
