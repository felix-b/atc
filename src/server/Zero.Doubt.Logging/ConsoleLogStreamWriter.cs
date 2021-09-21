using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class ConsoleLogStreamWriter : ILogStreamWriter
    {
        private static int _nextInstanceId = 1;
        private static readonly Dictionary<LogLevel, string> _logLevelLabels = new() {
            {LogLevel.Audit, "AUD"},
            {LogLevel.Critical, "CRI"},
            {LogLevel.Error, "ERR"},
            {LogLevel.Warning, "WAR"},
            {LogLevel.Success, "SUC"},
            {LogLevel.Info, "inf"},
            {LogLevel.Debug, "dbg"},
        };

        private readonly StringBuilder _messageValues = new();
        private readonly int _instanceId;
        private DateTime _messageTime = DateTime.MinValue;
        private string _messageId = string.Empty;
        private LogLevel _messageLevel = LogLevel.Quiet;

        public ConsoleLogStreamWriter()
        {
            _instanceId = Interlocked.Increment(ref _nextInstanceId);
            Console.WriteLine($">>> ConsoleLogStreamWriter#{_instanceId}.ctor@[{Thread.CurrentThread.ManagedThreadId}]");
        }

        ~ConsoleLogStreamWriter()
        {
            Console.WriteLine($">>> ConsoleLogStreamWriter#{_instanceId}.Finalize@[{Thread.CurrentThread.ManagedThreadId}]");
        }
            
        public void WriteMessage(DateTime time, string id, LogLevel level)
        {
            _messageTime = time;
            _messageId = id;
            _messageLevel = level;
            FlushMessage();
        }

        public void WriteBeginMessage(DateTime time, string id, LogLevel level)
        {
            _messageTime = time;
            _messageId = id;
            _messageLevel = level;
        }

        public void WriteValue<T>(string key, T value)
        {
            _messageValues.Append($" {key}={value}");
        }

        public void WriteException(Exception error)
        {
            _messageValues.Append($" [{error.GetType().Name}|{error.Message}]");
        }

        public void WriteEndMessage()
        {
            FlushMessage();
        }

        public void WriteOpenSpan(DateTime time, string id, LogLevel level)
        {
            // _messageTime = time;
            // _messageId = id;
            // _messageLevel = level;
            // FlushMessage();
        }

        public void WriteBeginOpenSpan(DateTime time, string id, LogLevel level)
        {
            // _messageTime = time;
            // _messageId = id;
            // _messageLevel = level;
        }

        public void WriteEndOpenSpan()
        {
            // FlushMessage();
            DiscardMessage();
        }

        public void WriteCloseSpan(DateTime time)
        {
        }

        public void WriteBeginCloseSpan(DateTime time)
        {
        }

        public void WriteEndCloseSpan()
        {
            DiscardMessage();
        }

        private void FlushMessage()
        {
            Console.WriteLine($"{_messageTime:HH:mm:ss.fff} {_logLevelLabels[_messageLevel]} | {_messageId} : {_messageValues}");
            DiscardMessage();
        }
 
        private void DiscardMessage()
        {
            _messageValues.Clear();
        }

        public static Func<ILogStreamWriter> Factory => () => new ConsoleLogStreamWriter();
    }
}