using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class PipelineLogStreamWriter : ILogStreamWriter
    {
        private static int _lastInstanceId = 0;

        private readonly int _instanceId = Interlocked.Increment(ref _lastInstanceId);
        private readonly ILogStreamWriter[] _pipeline;
        private readonly int _sinkCount;
        //TODO: for debugging; to be removed
        private int _concurrency = 0;

        private PipelineLogStreamWriter(ILogStreamWriter[] pipeline)
        {
            _pipeline = pipeline;
            _sinkCount = pipeline.Length;
            
            Console.WriteLine($"PipelineLogStreamWriter.ctor #{_instanceId} sync-context [{SynchronizationContext.Current?.GetType()?.Name ?? "N/A"}]");
        }

        public void WriteMessage(DateTime time, string id, LogLevel level)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteMessage(time, id, level);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteBeginMessage(DateTime time, string id, LogLevel level)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteBeginMessage(time, id, level);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteValue<T>(string key, T value)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteValue(key, value);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteException(Exception error)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteException(error);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteEndMessage()
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteEndMessage();
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteOpenSpan(DateTime time, string id, LogLevel level)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteOpenSpan(time, id, level);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteBeginOpenSpan(DateTime time, string id, LogLevel level)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteBeginOpenSpan(time, id, level);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteEndOpenSpan()
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteEndOpenSpan();
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteCloseSpan(DateTime time)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteCloseSpan(time);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteBeginCloseSpan(DateTime time)
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteBeginCloseSpan(time);
            }
            
            DebugDecrementConcurrency();
        }

        public void WriteEndCloseSpan()
        {
            DebugIncrementConcurrency();
            
            for (int i = 0; i < _sinkCount; i++)
            {
                _pipeline[i].WriteEndCloseSpan();
            }
            
            DebugDecrementConcurrency();
        }

        private void DebugIncrementConcurrency()
        {
            var newConcurrency = Interlocked.Increment(ref _concurrency); 
            if (newConcurrency > 1)
            {
                Console.WriteLine($"!!! BINARY-LOG-STREAM-WRITER: CONCURRENCY INVARIANT VIOLATED: {newConcurrency} !!! SYNC-CTX {SynchronizationContext.Current?.GetType()?.Name ?? "N/A"}");
            }
        }

        private void DebugDecrementConcurrency()
        {
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