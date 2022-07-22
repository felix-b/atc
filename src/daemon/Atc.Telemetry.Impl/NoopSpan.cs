namespace Atc.Telemetry.Impl;

public class NoopSpan : ITraceSpan
{
    public void Dispose()
    {
    }

    public void Fail(Exception exception)
    {
    }

    public void Fail(string errorCode)
    {
    }

    public static readonly NoopSpan Instance = new NoopSpan();
}

