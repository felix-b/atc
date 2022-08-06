namespace Atc.Telemetry;

public class TelemetryProvider : ITelemetryProvider
{
    private readonly Dictionary<Type, ITelemetry> _telemetryByType;
    
    public TelemetryProvider(params ITelemetryImplementationMap[] implementationMaps)
    {
        _telemetryByType = implementationMaps
            .SelectMany(map => map.GetEntries())
            .ToDictionary(entry => entry.InterfaceType, entry => entry.Factory());
    }
    
    public void Dispose()
    {
        // nothing
    }

    public T GetTelemetry<T>() where T : class, ITelemetry
    {
        try
        {
            return (T)_telemetryByType[typeof(T)];
        }
        catch (Exception e)
        {
            Console.WriteLine($"TelemetryProvider.GetTelemetry failed for '{typeof(T).FullName}': {e.Message}");
            throw;
        }
    }
}
