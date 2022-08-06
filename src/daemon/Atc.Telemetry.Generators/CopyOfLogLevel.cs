namespace Atc.Telemetry.Generators;

// copied from Atc.Telemetry (which cannot be referenced from here because current project targets netstandard2.0)
public enum CopyOfLogLevel : sbyte
{
    Quiet = -1,
    Audit = 0,
    Critical = 1,
    Error = 2,
    Warning = 3,
    Info = 4,
    Verbose = 5,
    Debug = 6
}
