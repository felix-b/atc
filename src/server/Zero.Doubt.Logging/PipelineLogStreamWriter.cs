using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class PipelineLogStreamWriter : ILogStreamWriter
    {
        private class ConcurrencyInfo
        {
            public int? TaskId;
            public int ManagedThreadId;
            public string TaskName;

            public override string ToString()
            {
                return $"[{TaskId?.ToString() ?? "?"}@{ManagedThreadId}|{TaskName}]";
            }
        }
        
        private static int _lastInstanceId = 0;

        private readonly int _instanceId = Interlocked.Increment(ref _lastInstanceId);
        private readonly ILogStreamWriter[] _pipeline;
        private readonly int _sinkCount;
        private long _currentSpanId = 0;
        //TODO: for debugging; to be removed
        private int _concurrency = 0;
        private ImmutableArray<ConcurrencyInfo> _concurrencyInfo = ImmutableArray<ConcurrencyInfo>.Empty;

        private PipelineLogStreamWriter(ILogStreamWriter[] pipeline)
        {
            _pipeline = pipeline;
            _sinkCount = pipeline.Length;
            
            Console.WriteLine($"PipelineLogStreamWriter.ctor #{_instanceId} sync-context [{SynchronizationContext.Current?.GetType()?.Name ?? "N/A"}]");
        }

        public void WriteAsyncParentSpanId(long spanId)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteAsyncParentSpanId(spanId);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteMessage(DateTime time, string id, LogLevel level)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteMessage(time, id, level);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteBeginMessage(DateTime time, string id, LogLevel level)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteBeginMessage(time, id, level);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteValue<T>(string key, T value)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteValue(key, value);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteException(Exception error)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteException(error);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteEndMessage()
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteEndMessage();
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteOpenSpan(long spanId, DateTime time, string messageId, LogLevel level)
        {
            _currentSpanId = spanId;
            
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteOpenSpan(spanId, time, messageId, level);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteBeginOpenSpan(long spanId, DateTime time, string messageId, LogLevel level)
        {
            _currentSpanId = spanId;

            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteBeginOpenSpan(spanId, time, messageId, level);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteEndOpenSpan()
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteEndOpenSpan();
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteCloseSpan(DateTime time)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteCloseSpan(time);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteBeginCloseSpan(DateTime time)
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteBeginCloseSpan(time);
            }
            
            DebugDecrementConcurrency(info);
        }

        public void WriteEndCloseSpan()
        {
            var info = DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteEndCloseSpan();
            }
            
            DebugDecrementConcurrency(info);
        }

        public long GetCurrentSpanId()
        {
            return _currentSpanId;
        }

        private ConcurrencyInfo DebugIncrementConcurrency()
        {
            var newConcurrency = Interlocked.Increment(ref _concurrency);
            var newInfo = new ConcurrencyInfo() {
                TaskId = Task.CurrentId,
                ManagedThreadId = Thread.CurrentThread.ManagedThreadId, 
                TaskName = LogEngine.CurrentTaskName,
            };
            var newConcurrencyInfo = _concurrencyInfo.Add(newInfo);
            _concurrencyInfo = newConcurrencyInfo;
            if (newConcurrency > 1)
            {
                Console.WriteLine(
                    $"!!! LOGGER CONCURRENCY VIOLATED: concurrent={newConcurrency} " +
                    $"!!! SYNC-CTX {SynchronizationContext.Current?.GetType()?.Name ?? "N/A"} " +
                    $"!!! {string.Join(" ", newConcurrencyInfo.Select(x => x.ToString()))}"
                );
            }
            return newInfo;
        }

        private void DebugDecrementConcurrency(ConcurrencyInfo info)
        {
            _concurrencyInfo = _concurrencyInfo.Remove(info);
            Interlocked.Decrement(ref _concurrency);
        }
        
        public static Func<ILogStreamWriter> CreateFactory(IEnumerable<Func<ILogStreamWriter>> factorySinks)
        {
            var factory = new Factory(factorySinks);
            return factory.CreateWriter;
        }
        
        public class Factory
        {
            private readonly Func<ILogStreamWriter>[] _factorySinks;

            public Factory(IEnumerable<Func<ILogStreamWriter>> factorySinks)
            {
                _factorySinks = factorySinks.ToArray();
            }

            public PipelineLogStreamWriter CreateWriter()
            {
                var pipeline = new ILogStreamWriter[_factorySinks.Length];

                for (int i = 0; i < pipeline.Length; i++)
                {
                    pipeline[i] = _factorySinks[i]();
                }

                return new PipelineLogStreamWriter(pipeline);
            }
        }
    }
}