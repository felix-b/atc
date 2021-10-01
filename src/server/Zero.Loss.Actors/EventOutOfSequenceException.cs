using System;
using System.Runtime.Serialization;

namespace Zero.Loss.Actors
{
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
}