using System.Runtime.Serialization;

namespace Atc.Grains;

public class InvalidEventException : Exception
{
    public InvalidEventException()
    {
    }

    protected InvalidEventException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public InvalidEventException(string? message) : base(message)
    {
    }

    public InvalidEventException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
