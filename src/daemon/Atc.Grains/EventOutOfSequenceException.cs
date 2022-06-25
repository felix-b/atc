using System.Runtime.Serialization;

namespace Atc.Grains;

public class EventOutOfSequenceException : Exception
{
    public EventOutOfSequenceException()
    {
    }

    protected EventOutOfSequenceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public EventOutOfSequenceException(string? message) : base(message)
    {
    }

    public EventOutOfSequenceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}