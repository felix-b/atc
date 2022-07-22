namespace Atc.Telemetry.CodePath;

public class CodePathTelemetryProvider : ITelemetryProvider
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public T GetTelemetry<T>() where T : class, ITelemetry
    {
        throw new NotImplementedException();
    }
}