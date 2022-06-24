using System.Runtime.Serialization;

namespace Atc.Grains;

public class GrainNotFoundException : Exception
{
    public GrainNotFoundException()
    {
    }

    protected GrainNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public GrainNotFoundException(string? message) : base(message)
    {
    }

    public GrainNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
