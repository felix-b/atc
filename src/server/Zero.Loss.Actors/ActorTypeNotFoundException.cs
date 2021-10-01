using System;
using System.Runtime.Serialization;

namespace Zero.Loss.Actors
{
    public class ActorTypeNotFoundException : Exception
    {
        public ActorTypeNotFoundException()
        {
        }

        protected ActorTypeNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ActorTypeNotFoundException(string? message) : base(message)
        {
        }

        public ActorTypeNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}