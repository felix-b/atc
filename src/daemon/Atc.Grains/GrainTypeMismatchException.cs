using System.Runtime.Serialization;

namespace Atc.Grains;

public class GrainTypeMismatchException : Exception
{
    public GrainTypeMismatchException()
    {
    }

    protected GrainTypeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public GrainTypeMismatchException(string? message) : base(message)
    {
    }

    public GrainTypeMismatchException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
