namespace Atc.Telemetry;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class TelemetryNameAttribute : Attribute
{
    public TelemetryNameAttribute(string name)
    {
        Name = name;
    }
    
    public string Name { get; }    
}
