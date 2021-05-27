namespace Zero.Doubt.Logging.Engine
{
    // OpCodes for LogStreamWriter
    // bit encoding:
    // bit 1 = subject : 0=value, 1=message
    // (TBD)
    public enum LogStreamOpCode : byte
    {
        Noop = 0,
        
        Message = 0x01,
        BeginMessage,
        Exception,
        EndMessage,
        OpenSpan,
        BeginOpenSpan,
        EndOpenSpan,
        CloseSpan,
        BeginCloseSpan,
        EndCloseSpan,        

        BoolValue = 0x10,
        Int8Value,
        Int16Value,
        Int32Value,
        Int64Value,
        FloatValue,
        DoubleValue,
        DecimalValue,
        StringValue,
        TimeSpanValue,
        DateTimeValue,
    }
}
