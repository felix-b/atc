using System;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class NoopLogStreamWriter : ILogStreamWriter
    {
        public void WriteMessage(DateTime time, string id, LogLevel level)
        {
        }

        public void WriteBeginMessage(DateTime time, string id, LogLevel level)
        {
        }

        public void WriteValue<T>(string key, T value)
        {
        }

        public void WriteException(Exception error)
        {
        }

        public void WriteEndMessage()
        {
        }

        public void WriteOpenSpan(DateTime time, string id, LogLevel level)
        {
        }

        public void WriteBeginOpenSpan(DateTime time, string id, LogLevel level)
        {
        }

        public void WriteEndOpenSpan()
        {
        }

        public void WriteCloseSpan(DateTime time)
        {
        }

        public void WriteBeginCloseSpan(DateTime time)
        {
        }

        public void WriteEndCloseSpan()
        {
        }
    }
}
