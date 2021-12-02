using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class InspectingLogWriter : LogWriter
    {
        public InspectingLogWriter(
            LogLevel level, 
            Func<DateTime> getTime, 
            Func<ILogStreamWriter> getStream) 
            : base(getLevel: () => level, getTime, getStream)
        {
        }

        public static LogWriter Create(LogLevel level, Func<DateTime> getTime, out Func<IEnumerable<LogEntry>> getLogEntries)
        {
            var streamWriter = new LogStreamWriter();
            var logWriter = new InspectingLogWriter(level, getTime, () => streamWriter);
            getLogEntries = () => streamWriter.Entries;
            return logWriter;
        }

        public class LogEntry
        {
            public LogEntry(
                bool isSpan, 
                long? spanId,
                long parentSpanId,
                DateTime time, 
                string id, 
                LogLevel level, 
                Exception? failure, 
                TimeSpan? duration)
            {
                IsSpan = isSpan;
                SpanId = spanId;
                ParentSpanId = parentSpanId;
                Time = time;
                Id = id;
                Level = level;
                Failure = failure;
                Duration = duration;
                MutableKeyValuePairs = new Dictionary<string, object?>();
            }

            public bool IsSpan { get; internal set; }
            public long? SpanId { get; internal set; }
            public long ParentSpanId { get; internal set; }
            public DateTime Time { get; internal set; }
            public string Id { get; internal set; }
            public LogLevel Level { get; internal set; }
            public Exception? Failure { get; internal set; }
            public TimeSpan? Duration { get; internal set; }
            public IReadOnlyDictionary<string, object?> KeyValuePairs => MutableKeyValuePairs;
            internal Dictionary<string, object?> MutableKeyValuePairs { get; private set; }
        }

        public class LogStreamWriter : ILogStreamWriter
        {
            private readonly List<LogEntry> _entries = new();
            private readonly Dictionary<long, LogEntry> _openSpans = new();
            private long _currentSpanId = 0;
            private LogEntry? _pendingEntry = null;

            public LogStreamWriter()
            {
                WriteOpenSpan(0, DateTime.MinValue, string.Empty, LogLevel.Debug);
            }
            
            public void WriteAsyncParentSpanId(long spanId)
            {
                ValidatePendingEntry(expected: false);
                _currentSpanId = spanId;
            }

            public void WriteMessage(DateTime time, string id, LogLevel level)
            {
                WriteBeginMessage(time, id, level);
                WriteEndMessage();
            }

            public void WriteBeginMessage(DateTime time, string id, LogLevel level)
            {
                ValidatePendingEntry(expected: false);
                CreatePendingEntry(isSpan: false, spanId: _currentSpanId, time, id, level);
            }

            public void WriteValue<T>(string key, T value)
            {
                ValidatePendingEntry(expected: true);
                _pendingEntry!.MutableKeyValuePairs.Add(key, value);
            }

            public void WriteException(Exception error)
            {
                ValidatePendingEntry(expected: true);
                _pendingEntry!.Failure = error;
            }

            public void WriteEndMessage()
            {
                ValidatePendingEntry(expected: true, expectedSpan: false);
                FlushPendingEntry();
            }

            public void WriteOpenSpan(long spanId, DateTime time, string messageId, LogLevel level)
            {
                WriteBeginOpenSpan(spanId, time, messageId, level);
                FlushPendingEntry();
            }

            public void WriteBeginOpenSpan(long spanId, DateTime time, string messageId, LogLevel level)
            {
                ValidatePendingEntry(expected: false);
                CreatePendingEntry(isSpan: true, spanId, time, messageId, level);
                _currentSpanId = spanId;
                _openSpans.Add(spanId, _pendingEntry!);
            }

            public void WriteEndOpenSpan()
            {
                ValidatePendingEntry(expected: true);
                FlushPendingEntry();
            }

            public void WriteCloseSpan(DateTime time)
            {
                WriteBeginCloseSpan(time);
                WriteEndCloseSpan();
            }

            public void WriteBeginCloseSpan(DateTime time)
            {
                ValidatePendingEntry(expected: false);
                if (_currentSpanId == 0)
                {
                    throw new InvalidOperationException("Close span log instruction cannot be executed on the root span id=0");
                }
                
                _pendingEntry = _openSpans[_currentSpanId]!;
                _pendingEntry.Duration = time - _pendingEntry.Time;
            }

            public void WriteEndCloseSpan()
            {
                ValidatePendingEntry(expected: true, expectedSpan: true);

                var spanId = _currentSpanId;
                var parentSpanId = _pendingEntry!.ParentSpanId;
                
                FlushPendingEntry();

                _currentSpanId = parentSpanId;
                _openSpans.Remove(spanId);
            }

            public long GetCurrentSpanId()
            {
                return _currentSpanId;
            }

            public IReadOnlyList<LogEntry> Entries => _entries;

            private void CreatePendingEntry(bool isSpan, long? spanId, DateTime time, string messageId, LogLevel level)
            {
                _pendingEntry = new LogEntry(
                    isSpan, 
                    spanId, 
                    parentSpanId: _currentSpanId, 
                    time, 
                    messageId, 
                    level, 
                    failure: null, 
                    duration: null);
            }

            private void FlushPendingEntry()
            {
                if (_pendingEntry != null)
                {
                    _entries.Add(_pendingEntry);
                    _pendingEntry = null;
                }
            }

            private void ValidatePendingEntry(bool expected, bool? expectedSpan = null)
            {
                if (expected)
                {
                    if (_pendingEntry == null)
                    {
                        throw new InvalidOperationException("This log operation expects a pending entry, but there is none");
                    }
                    if (expectedSpan.HasValue && _pendingEntry.IsSpan != expectedSpan.Value)
                    {
                        throw new InvalidOperationException("This log operation is not compatible with pending entry IsSpan value");
                    }
                }
                else if (_pendingEntry != null)
                {
                    throw new InvalidOperationException("This log operation expects no pending entry, but a pending entry exists");
                }
            }
        }
    }
}