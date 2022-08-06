using System.Runtime.Serialization;

namespace Atc.Grains;

public class EventFailedException : Exception
{
    public EventFailedException()
    {
    }

    protected EventFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public EventFailedException(string? message) : base(message)
    {
    }

    public EventFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
