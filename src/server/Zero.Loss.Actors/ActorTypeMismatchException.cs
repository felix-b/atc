using System;
using System.Runtime.Serialization;

namespace Zero.Loss.Actors
{
    public class ActorTypeMismatchException : Exception
    {
        public ActorTypeMismatchException()
        {
        }

        protected ActorTypeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ActorTypeMismatchException(string? message) : base(message)
        {
        }

        public ActorTypeMismatchException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}