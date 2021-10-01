using System;
using System.Runtime.Serialization;

namespace Zero.Loss.Actors
{
    public class ActorNotFoundException : Exception
    {
        public ActorNotFoundException()
        {
        }

        protected ActorNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ActorNotFoundException(string? message) : base(message)
        {
        }

        public ActorNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
