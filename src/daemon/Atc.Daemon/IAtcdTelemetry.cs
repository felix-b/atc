using Atc.Telemetry;

namespace Atc.Daemon;

[TelemetryName("Atcd")]
public interface IAtcdTelemetry : ITelemetry
{
    ITraceSpan InitializeFir(string icao);
}
