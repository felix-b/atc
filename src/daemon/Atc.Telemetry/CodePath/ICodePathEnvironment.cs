namespace Atc.Telemetry.CodePath;

public interface ICodePathEnvironment
{
    DateTime GetUtcNow();
    LogLevel GetLogLevel(string? loggerName = null);
    ulong GetCurrentSpanId();
    void SetCurrentSpanId(ulong spanId);
    ulong TakeNextSpanId();
    CodePathBufferWriter NewBuffer();
    CodePathStringMap GetStringMap();
}
