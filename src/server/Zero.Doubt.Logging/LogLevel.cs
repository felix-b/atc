namespace Zero.Doubt.Logging
{
    public enum LogLevel : sbyte
    {
        Quiet = -1,
        Audit = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Success = 4,
        Info = 5,
        Debug = 7
    }
}
