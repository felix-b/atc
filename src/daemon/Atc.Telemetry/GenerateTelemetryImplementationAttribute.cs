namespace Atc.Telemetry;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class GenerateTelemetryImplementationAttribute : Attribute
{
    public Type? InterfaceType { get; set; }    
}
