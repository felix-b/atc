namespace Atc.Telemetry;

public interface ITraceSpan : IDisposable
{
    void Fail(Exception error);
}