namespace Zero.Doubt.Logging.Engine
{
    // OpCodes for LogStreamWriter
    // bit encoding:
    // high byte = subject, low byte = subject value : 
    // - 0xA* = stream subject 
    // - 0xB* = message subject 
    // - 0xC* = value subject
    // (TBD)
    public enum LogStreamOpCode : byte
    {
        Noop = 0,
        // stream op-codes
        BeginStreamChunk = 0xA0,
        EndStreamChunk = 0xA1,
        StringKey = 0xA2,
        // message op-codes
        Message = 0xB1,
        BeginMessage = 0xB1,
        EndMessage = 0xB2,
        OpenSpan = 0xB3,
        BeginOpenSpan = 0xB4,
        EndOpenSpan = 0xB5,
        CloseSpan = 0xB6,
        BeginCloseSpan = 0xB7,
        EndCloseSpan = 0xB8,  
        // value op-codes
        ExceptionValue = 0xC0,
        BoolValue = 0xC1,
        Int8Value = 0xC2,
        Int16Value = 0xC3,
        Int32Value = 0xC4,
        Int64Value = 0xC5,
        UInt64Value = 0xC6,
        FloatValue = 0xC7,
        DoubleValue = 0xC8,
        DecimalValue = 0xC9,
        StringValue = 0xCA,
        TimeSpanValue = 0xCB,
        DateTimeValue = 0xCC,
    }
}
