using System.Runtime.CompilerServices;

namespace Atc.Telemetry;

public interface ITelemetryImplementationMap
{
    TelemetryImplementationEntry[] GetEntries();
}

public record TelemetryImplementationEntry(
    Type InterfaceType,
    Func<ITelemetry> Factory
);
