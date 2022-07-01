namespace Atc.Telemetry;

public interface ITelemetryProvider : IDisposable
{
    T GetTelemetry<T>() where T : ITelemetry;
}
