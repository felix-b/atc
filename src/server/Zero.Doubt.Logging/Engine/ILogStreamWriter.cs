using System;

namespace Zero.Doubt.Logging.Engine
{
    public interface ILogStreamWriter
    {
        void WriteMessage(DateTime time, string id, LogLevel level);
        void WriteBeginMessage(DateTime time, string id, LogLevel level);
        void WriteValue<T>(string key, T value);
        void WriteException(Exception error);
        void WriteEndMessage();
        void WriteOpenSpan(DateTime time, string id, LogLevel level);
        void WriteBeginOpenSpan(DateTime time, string id, LogLevel level);
        void WriteEndOpenSpan();
        void WriteCloseSpan(DateTime time);
        void WriteBeginCloseSpan(DateTime time);
        void WriteEndCloseSpan();
    }
}
