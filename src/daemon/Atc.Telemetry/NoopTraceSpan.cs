namespace Atc.Telemetry;

public class NoopTraceSpan : ITraceSpan
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

    public static readonly NoopTraceSpan Instance = new NoopTraceSpan();
}

