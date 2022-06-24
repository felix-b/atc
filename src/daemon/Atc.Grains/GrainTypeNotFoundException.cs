using System.Runtime.Serialization;

namespace Atc.Grains;

public class GrainTypeNotFoundException : Exception
{
    public GrainTypeNotFoundException()
    {
    }

    protected GrainTypeNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public GrainTypeNotFoundException(string? message) : base(message)
    {
    }

    public GrainTypeNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
