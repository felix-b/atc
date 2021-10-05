using System;
using System.Threading;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class LogWriter
    {
        private readonly Func<LogLevel> _getLevel;
        private readonly Func<DateTime> _getTime;
        private readonly Func<ILogStreamWriter> _getStream;
        private long _lastSpanId = 0;

        public LogWriter(Func<LogLevel> getLevel, Func<DateTime> getTime, Func<ILogStreamWriter> getStream)
        {
            _getLevel = getLevel;
            _getTime = getTime;
            _getStream = getStream;
        }

        public void Message(string id, LogLevel level)
        {
            if (level > _getLevel())
            {
                return;
            }
            
            var stream = _getStream();
            stream.WriteMessage(Time, id, level);
        }

        public void Message<T1>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2, T3>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2, T3, T4>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2, T3, T4, T5>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2, T3, T4, T5, T6>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5,
            in (string name, T6 value) pair6)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteValue(pair6.name, pair6.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2, T3, T4, T5, T6, T7>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5,
            in (string name, T6 value) pair6,
            in (string name, T7 value) pair7)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteValue(pair6.name, pair6.value);
            stream.WriteValue(pair7.name, pair7.value);
            stream.WriteEndMessage();
        }

        public void Message<T1, T2, T3, T4, T5, T6, T7, T8>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5,
            in (string name, T6 value) pair6,
            in (string name, T7 value) pair7,
            in (string name, T8 value) pair8)
        {
            if (level > _getLevel())
            {
                return;
            }

            var stream = _getStream();
            stream.WriteBeginMessage(Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteValue(pair6.name, pair6.value);
            stream.WriteValue(pair7.name, pair7.value);
            stream.WriteValue(pair8.name, pair8.value);
            stream.WriteEndMessage();
        }

        public LogSpan Span(string id, LogLevel level)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteOpenSpan(TakeNextSpanId(), Time, id, level);
            return new LogSpan(this);
        }

        public LogSpan Span<T1>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }

        public LogSpan Span<T1, T2>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }

        public LogSpan Span<T1, T2, T3>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }

        public LogSpan Span<T1, T2, T3, T4>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }

        public LogSpan Span<T1, T2, T3, T4, T5>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }

        public LogSpan Span<T1, T2, T3, T4, T5, T6>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5,
            in (string name, T6 value) pair6)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteValue(pair6.name, pair6.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }
        
        public LogSpan Span<T1, T2, T3, T4, T5, T6, T7>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5,
            in (string name, T6 value) pair6,
            in (string name, T7 value) pair7)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteValue(pair6.name, pair6.value);
            stream.WriteValue(pair7.name, pair7.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }

        public LogSpan Span<T1, T2, T3, T4, T5, T6, T7, T8>(
            string id,
            LogLevel level,
            in (string name, T1 value) pair1,
            in (string name, T2 value) pair2,
            in (string name, T3 value) pair3,
            in (string name, T4 value) pair4,
            in (string name, T5 value) pair5,
            in (string name, T6 value) pair6,
            in (string name, T7 value) pair7,
            in (string name, T8 value) pair8)
        {
            if (level > _getLevel())
            {
                return LogSpan.Noop();
            }

            var stream = _getStream();
            stream.WriteBeginOpenSpan(TakeNextSpanId(), Time, id, level);
            stream.WriteValue(pair1.name, pair1.value);
            stream.WriteValue(pair2.name, pair2.value);
            stream.WriteValue(pair3.name, pair3.value);
            stream.WriteValue(pair4.name, pair4.value);
            stream.WriteValue(pair5.name, pair5.value);
            stream.WriteValue(pair6.name, pair6.value);
            stream.WriteValue(pair7.name, pair7.value);
            stream.WriteValue(pair8.name, pair8.value);
            stream.WriteEndOpenSpan();
            return new LogSpan(this);
        }
        
        public LogLevel Level => _getLevel();
        public DateTime Time => _getTime();

        private long TakeNextSpanId()
        {
            return Interlocked.Increment(ref _lastSpanId);
        }

        private static readonly string _errorCodeKey = "ErrorCode";

        public static readonly LogWriter Noop = CreateNoop();
        
        private static LogWriter CreateNoop()
        {
            var noopStream = new NoopLogStreamWriter();
            return new LogWriter(
                getLevel: () => LogLevel.Error, 
                getStream: () => noopStream, 
                getTime: () => DateTime.UtcNow);
        }
        
        public struct LogSpan : IDisposable
        {
            private readonly LogWriter? _writer;
            private bool _disposed;
            private Exception? _exception;
            private string? _errorCode;

            public LogSpan(LogWriter? writer)
            {
                _writer = writer;
                _disposed = writer == null;
                _exception = null;
                _errorCode = null;
            }

            public void Succeed()
            {
                if (!_disposed)
                {
                    _exception = null;
                    _errorCode = null;
                    Dispose();
                }
            }
            
            public void Fail(Exception exception)
            {
                if (!_disposed)
                {
                    _exception = exception;
                }
            }

            public void Fail(string errorCode)
            {
                if (!_disposed)
                {
                    _errorCode = errorCode;
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                
                var stream = _writer!._getStream();

                if (_exception == null && _errorCode == null)
                {
                    stream.WriteCloseSpan(_writer.Time);
                }
                else
                {
                    stream.WriteBeginCloseSpan(_writer.Time);
                    if (_errorCode != null)
                    {
                        stream.WriteValue(_errorCodeKey, _errorCode);
                    }
                    if (_exception != null)
                    {
                        stream.WriteException(_exception);
                    }
                    stream.WriteEndCloseSpan();
                }
            }

            public static LogSpan Noop()
            {
                return new LogSpan(null);
            }
        }
    }
}
