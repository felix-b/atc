namespace Atc.Telemetry;

public interface ITraceSpan : IDisposable
{
    void Fail(Exception exception);
    void Fail(string errorCode);
}