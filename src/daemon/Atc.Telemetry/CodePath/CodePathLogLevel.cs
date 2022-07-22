namespace Atc.Telemetry.CodePath;

public enum CodePathLogLevel : sbyte
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
