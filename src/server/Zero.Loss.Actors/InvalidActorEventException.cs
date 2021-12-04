using System;
using System.Runtime.Serialization;

namespace Zero.Loss.Actors
{
    public class InvalidActorEventException : Exception
    {
        public InvalidActorEventException() : 
            this("The event is not valid for the current state of the actor")
        {
        }

        protected InvalidActorEventException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidActorEventException(string message) : base(message)
        {
        }

        public InvalidActorEventException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
