using System.Runtime.Serialization;

namespace Atc.Server;

public class OperationMethodNotFoundException : Exception
{
    public OperationMethodNotFoundException()
    {
    }

    protected OperationMethodNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OperationMethodNotFoundException(string? message) : base(message)
    {
    }

    public OperationMethodNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
