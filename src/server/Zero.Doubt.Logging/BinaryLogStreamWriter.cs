using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class BinaryLogStreamWriter : ILogStreamWriter
    {
        private readonly int _streamId;
        private readonly BinaryLogStream _output;
        private int _spanDepth = 0;
        private RootSpanBuffer? _buffer = null;
        private long _currentSpanId = 0;

        public BinaryLogStreamWriter(int streamId, BinaryLogStream output)
        {
            _streamId = streamId;
            _output = output;
        }

        public void WriteAsyncParentSpanId(long spanId)
        {
            var buffer = GetBuffer();

            buffer.WriteOpCode(LogStreamOpCode.AsyncParentSpanId);
            buffer.WriteSpanId(spanId);
        }

        public void WriteMessage(DateTime time, string id, LogLevel level)
        {
            var buffer = GetBuffer();

            buffer.WriteOpCode(LogStreamOpCode.Message);
            buffer.WriteTime(time);
            buffer.WriteMessageId(id);
            buffer.WriteLogLevel(level);
        }

        public void WriteBeginMessage(DateTime time, string id, LogLevel level)
        {
            var buffer = GetBuffer();

            buffer.WriteOpCode(LogStreamOpCode.BeginMessage);
            buffer.WriteTime(time);
            buffer.WriteMessageId(id);
            buffer.WriteLogLevel(level);
        }

        public void WriteValue<T>(string key, T value)
        {
            var buffer = GetBuffer();
            ValueWriter.WriteValue<T>(buffer, key, value);
        }

        public void WriteException(Exception error)
        {
            var buffer = GetBuffer();
            ValueWriter.WriteValue<Exception>(buffer, string.Empty, error);
        }

        public void WriteEndMessage()
        {
            var buffer = GetBuffer();
            buffer.WriteOpCode(LogStreamOpCode.EndMessage);
        }

        public void WriteOpenSpan(long spanId, DateTime time, string messageId, LogLevel level)
        {
            var buffer = GetBuffer();

            buffer.WriteOpCode(LogStreamOpCode.OpenSpan);
            buffer.WriteSpanId(spanId);
            buffer.WriteTime(time);
            buffer.WriteMessageId(messageId);
            buffer.WriteLogLevel(level);

            _currentSpanId = spanId;
            IncrementSpanDepth();
        }

        public void WriteBeginOpenSpan(long spanId, DateTime time, string messageId, LogLevel level)
        {
            var buffer = GetBuffer();

            buffer.WriteOpCode(LogStreamOpCode.BeginOpenSpan);
            buffer.WriteSpanId(spanId);
            buffer.WriteTime(time);
            buffer.WriteMessageId(messageId);
            buffer.WriteLogLevel(level);

            _currentSpanId = spanId;
        }

        public void WriteEndOpenSpan()
        {
            var buffer = GetBuffer();
            buffer.WriteOpCode(LogStreamOpCode.EndOpenSpan);
            
            IncrementSpanDepth();
        }

        public void WriteCloseSpan(DateTime time)
        {
            var buffer = GetBuffer();
            buffer.WriteOpCode(LogStreamOpCode.CloseSpan);
            buffer.WriteTime(time);

            DecrementSpanDepth();
        }

        public void WriteBeginCloseSpan(DateTime time)
        {
            var buffer = GetBuffer();
            buffer.WriteOpCode(LogStreamOpCode.BeginCloseSpan);
            buffer.WriteTime(time);
        }

        public void WriteEndCloseSpan()
        {
            var buffer = GetBuffer();
            buffer.WriteOpCode(LogStreamOpCode.EndCloseSpan);

            DecrementSpanDepth();
        }

        public long GetCurrentSpanId()
        {
            return _currentSpanId;
        }

        private RootSpanBuffer GetBuffer()
        {
            if (_buffer == null)
            {
                _buffer = new RootSpanBuffer(_output);
            }
            return _buffer;
        }

        private void IncrementSpanDepth()
        {
            _spanDepth++;
        }

        private void DecrementSpanDepth()
        {
            _spanDepth--;

            if (_spanDepth == 0 && _buffer != null)
            {
                FlushBuffer();
            }
        }

        private void FlushBuffer()
        {
            if (_buffer != null)
            {
                var temp = _buffer;
                _buffer = null;
                temp.Flush(_streamId);
                temp.Dispose();
            }
        }

        private class RootSpanBuffer
        {
            private readonly BinaryLogStream _output;
            private readonly MemoryStream _stream;
            private readonly BinaryWriter _writer;
            private readonly DateTime _startedAtUtc;

            public RootSpanBuffer(BinaryLogStream output)
            {
                _output = output;
                _stream = new MemoryStream();
                _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
                _startedAtUtc = DateTime.UtcNow;
            }

            public void Dispose()
            {
                _writer.Dispose();
                _stream.Dispose();
            }
            
            public void Flush(int streamId)
            {
                _writer.Flush();
                _output.Flush(streamId, _startedAtUtc, _stream);
            }

            public void WriteOpCode(LogStreamOpCode opCode)
            {
                _writer.Write((byte)opCode);
            }
            
            public void WriteTime(DateTime time)
            {
                _writer.Write(time.Ticks);
            }

            public void WriteSpanId(long spanId)
            {
                _writer.Write(spanId);
            }

            public void WriteMessageId(string messageId)
            {
                var stringKey = _output.GetStringKey(messageId);
                _writer.Write(stringKey);
            }

            public void WriteValueKey(string valueKey)
            {
                var stringKey = _output.GetStringKey(valueKey);
                _writer.Write(stringKey);
            }

            public void WriteLogLevel(LogLevel level)
            {
                _writer.Write((sbyte)level);
            }

            public DateTime StartedAtUtc => _startedAtUtc;
            public BinaryWriter Writer => _writer;
        }

        private static class ValueWriter
        {
            private static readonly IReadOnlyDictionary<Type, Delegate> __writerByType =
                new Dictionary<Type, Delegate>() {
                    { 
                        typeof(Exception), 
                        new Action<RootSpanBuffer, string, Exception>((buffer, key, value) => {
                            if (value != null)
                            {
                                buffer.WriteOpCode(LogStreamOpCode.ExceptionValue);
                                buffer.Writer.Write(value.GetType().FullName);
                                buffer.Writer.Write(value.Message);
                            }
                        })
                    },                
                    { 
                        typeof(bool), 
                        new Action<RootSpanBuffer, string, bool>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.BoolValue);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },                
                    { 
                        typeof(byte), 
                        new Action<RootSpanBuffer, string, byte>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.Int8Value);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(Int16), 
                        new Action<RootSpanBuffer, string, Int16>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.Int16Value);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(Int32), 
                        new Action<RootSpanBuffer, string, Int32>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.Int32Value);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(Int64), 
                        new Action<RootSpanBuffer, string, Int64>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.Int64Value);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(UInt64), 
                        new Action<RootSpanBuffer, string, UInt64>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.UInt64Value);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(float), 
                        new Action<RootSpanBuffer, string, float>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.FloatValue);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(double), 
                        new Action<RootSpanBuffer, string, double>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.DoubleValue);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(decimal), 
                        new Action<RootSpanBuffer, string, decimal>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.DecimalValue);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value);
                        })
                    },
                    { 
                        typeof(string), 
                        new Action<RootSpanBuffer, string, string>((buffer, key, value) => {
                            if (value != null)
                            {
                                buffer.WriteOpCode(LogStreamOpCode.StringValue);
                                buffer.WriteValueKey(key);
                                buffer.Writer.Write(value);
                            }
                        })
                    },
                    { 
                        typeof(TimeSpan), 
                        new Action<RootSpanBuffer, string, TimeSpan>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.TimeSpanValue);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value.Ticks);
                        })
                    },
                    { 
                        typeof(DateTime), 
                        new Action<RootSpanBuffer, string, DateTime>((buffer, key, value) => {
                            buffer.WriteOpCode(LogStreamOpCode.DateTimeValue);
                            buffer.WriteValueKey(key);
                            buffer.Writer.Write(value.Ticks);
                        })
                    },
                };

            public static void WriteValue<T>(RootSpanBuffer buffer, string key, T value)
            {
                if (!__writerByType.TryGetValue(typeof(T), out var untypedDelegate))
                {
                    return;
                }

                var typedDelegate = (Action<RootSpanBuffer, string, T>)untypedDelegate;
                typedDelegate(buffer, key, value);
            }
        }
    }
}
