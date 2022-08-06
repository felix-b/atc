using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Channels;

namespace Atc.Telemetry.CodePath;

public class CodePathBufferWriter
{
    private readonly ICodePathExporter _output;
    private readonly CodePathStringMap _stringMap;
    private readonly Buffer _buffer;
    private List<StringKeyEntry>? _newStringKeys;

    public CodePathBufferWriter(CodePathStringMap stringMap, ICodePathExporter output)
    {
        _stringMap = stringMap;
        _output = output;
        _buffer = new Buffer(_stringMap, OnStringKeyAdded);
        _newStringKeys = null;
    }

    public void Flush()
    {
        _buffer.Flush();

        if (_newStringKeys != null)
        {
            var streamWithStringEntries = PrefixWithAddedStringEntries();
            _output.PushBuffer(streamWithStringEntries);
        }
        else
        {
            _output.PushBuffer(_buffer.Stream);
        }
    }

    public void WriteMessage(ulong parentSpanId, DateTime time, string id, LogLevel level)
    {
        _buffer.WriteOpCode(LogStreamOpCode.Message);
        _buffer.WriteSpanId(parentSpanId);
        _buffer.WriteTime(time);
        _buffer.WriteStringKey(id);
        _buffer.WriteLogLevel(level);
        _buffer.WriteThreadId(Thread.CurrentThread.ManagedThreadId);
    }

    public void WriteBeginMessage(ulong parentSpanId, DateTime time, string id, LogLevel level)
    {
        _buffer.WriteOpCode(LogStreamOpCode.BeginMessage);
        _buffer.WriteSpanId(parentSpanId);
        _buffer.WriteTime(time);
        _buffer.WriteStringKey(id);
        _buffer.WriteLogLevel(level);
        _buffer.WriteThreadId(Thread.CurrentThread.ManagedThreadId);
    }

    public void WriteValue<T>(string key, T value)
    {
        ValueWriter.WriteValue<T>(in _buffer, key, value);
    }

    public void WriteException(Exception error)
    {
        ValueWriter.WriteValue<Exception>(in _buffer, string.Empty, error);
    }

    public void WriteEndMessage()
    {
        _buffer.WriteOpCode(LogStreamOpCode.EndMessage);
    }

    public void WriteOpenSpan(ulong spanId, ulong parentSpanId, DateTime time, string messageId, LogLevel level)
    {
        _buffer.WriteOpCode(LogStreamOpCode.OpenSpan);
        _buffer.WriteSpanId(spanId);
        _buffer.WriteSpanId(parentSpanId);
        _buffer.WriteTime(time);
        _buffer.WriteStringKey(messageId);
        _buffer.WriteLogLevel(level);
        _buffer.WriteThreadId(Thread.CurrentThread.ManagedThreadId);
    }

    public void WriteBeginOpenSpan(ulong spanId, ulong parentSpanId, DateTime time, string messageId, LogLevel level)
    {
        _buffer.WriteOpCode(LogStreamOpCode.BeginOpenSpan);
        _buffer.WriteSpanId(spanId);
        _buffer.WriteSpanId(parentSpanId);
        _buffer.WriteTime(time);
        _buffer.WriteStringKey(messageId);
        _buffer.WriteLogLevel(level);
        _buffer.WriteThreadId(Thread.CurrentThread.ManagedThreadId);
    }

    public void WriteEndOpenSpan()
    {
        _buffer.WriteOpCode(LogStreamOpCode.EndOpenSpan);
    }

    public void WriteCloseSpan(ulong spanId, DateTime time)
    {
        _buffer.WriteOpCode(LogStreamOpCode.CloseSpan);
        _buffer.WriteSpanId(spanId);
        _buffer.WriteTime(time);
    }

    public void WriteBeginCloseSpan(ulong spanId, DateTime time)
    {
        _buffer.WriteOpCode(LogStreamOpCode.BeginCloseSpan);
        _buffer.WriteSpanId(spanId);
        _buffer.WriteTime(time);
    }

    public void WriteEndCloseSpan()
    {
        _buffer.WriteOpCode(LogStreamOpCode.EndCloseSpan);
    }

    private MemoryStream PrefixWithAddedStringEntries()
    {
        var streamWithStringEntries = new MemoryStream();
        var stringEntriesWriter = new BinaryWriter(streamWithStringEntries, Encoding.UTF8, leaveOpen: true);

        WriteAddedStringKeyEntries(stringEntriesWriter);
        stringEntriesWriter.Flush();

        _buffer.Stream.Position = 0;
        _buffer.Stream.WriteTo(streamWithStringEntries);
    
        return streamWithStringEntries;
    }

    private void WriteAddedStringKeyEntries(BinaryWriter writer)
    {
        if (_newStringKeys != null)
        {
            for (int i = 0; i < _newStringKeys.Count; i++)
            {
                WriteStringKeyEntry(writer, _newStringKeys[i]);
            }
        }
    }

    private void WriteStringKeyEntry(BinaryWriter writer, in StringKeyEntry entry)
    {
        _stringMap.WriteEntry(writer, entry.Key, entry.Value);
    }

    private void OnStringKeyAdded(in StringKeyEntry entry)
    {
        if (_newStringKeys == null)
        {
            _newStringKeys = new();
        }
        _newStringKeys.Add(entry);
    }

    private readonly struct Buffer
    {
        public readonly MemoryStream Stream;
        public readonly BinaryWriter Writer;
        
        private readonly CodePathStringMap _stringMap;
        private readonly StringKeyAddedCallback _onStringKeyAdded;

        public Buffer(CodePathStringMap stringMap, StringKeyAddedCallback onStringKeyAdded)
        {
            _stringMap = stringMap;
            _onStringKeyAdded = onStringKeyAdded;
            
            Stream = new MemoryStream(capacity: 128);
            Writer = new BinaryWriter(Stream, Encoding.UTF8, leaveOpen: true);
        }
        
        public void WriteOpCode(LogStreamOpCode opCode)
        {
            Writer.Write((byte)opCode);
        }
    
        public void WriteTime(DateTime time)
        {
            Writer.Write(time.Ticks);
        }

        public void WriteSpanId(ulong spanId)
        {
            Writer.Write(spanId);
        }

        public void WriteStringKey(string s)
        {
            var key = _stringMap.GetStringKey(s, out var createdNew);
            
            if (createdNew)
            {
                var newEntry = new StringKeyEntry(key, s);
                _onStringKeyAdded(in newEntry);
            }

            Writer.Write(key);
        }

        public void WriteLogLevel(LogLevel level)
        {
            Writer.Write((sbyte)level);
        }

        public void WriteThreadId(int threadId)
        {
            Writer.Write(threadId);
        }

        public void Flush()
        {
            Writer.Flush();
            Stream.Flush();
            Writer.Dispose();
        }
    }

    private delegate void StringKeyAddedCallback(in StringKeyEntry entry);
    
    private readonly struct StringKeyEntry
    {
        public StringKeyEntry(int key, string value)
        {
            Key = key;
            Value = value;
        }

        public readonly int Key;
        public readonly string Value;
    }

    private delegate void WriteValueCallback<T>(in Buffer buffer, string key, T value);

    private static class ValueWriter
    {
        private static readonly IReadOnlyDictionary<Type, Delegate> __writerByType =
            new Dictionary<Type, Delegate>() {
                { typeof(Exception), new WriteValueCallback<Exception>(WriteExceptionValue) },
                { typeof(bool), new WriteValueCallback<bool>(WriteBoolValue) },
                { typeof(byte), new WriteValueCallback<byte>(WriteByteValue) },
                { typeof(Int16), new WriteValueCallback<Int16>(WriteInt16Value) },
                { typeof(Int32), new WriteValueCallback<Int32>(WriteInt32Value) },
                { typeof(Int64), new WriteValueCallback<Int64>(WriteInt64Value) },
                { typeof(UInt64), new WriteValueCallback<UInt64>(WriteUInt64Value) },
                { typeof(float), new WriteValueCallback<float>(WriteFloatValue) },
                { typeof(double), new WriteValueCallback<double>(WriteDoubleValue) },
                { typeof(decimal), new WriteValueCallback<decimal>(WriteDecimalValue) },
                { typeof(string), new WriteValueCallback<string>(WriteStringValue) },
                { typeof(TimeSpan), new WriteValueCallback<TimeSpan>(WriteTimeSpanValue) },
                { typeof(DateTime), new WriteValueCallback<DateTime>(WriteDateTimeValue) },
            };
        
        private static void WriteExceptionValue(in Buffer buffer, string key, Exception value)
        {
            if (value != null)
            {
                buffer.WriteOpCode(LogStreamOpCode.ExceptionValue);
                buffer.Writer.Write(value.GetType().FullName ?? string.Empty);
                buffer.Writer.Write(value.Message);
            }
        }
        
        private static void WriteBoolValue(in Buffer buffer, string key, bool value)
        {
            buffer.WriteOpCode(LogStreamOpCode.BoolValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteByteValue(in Buffer buffer, string key, byte value)
        {
            buffer.WriteOpCode(LogStreamOpCode.Int8Value);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteInt16Value(in Buffer buffer, string key, Int16 value)
        {
            buffer.WriteOpCode(LogStreamOpCode.Int16Value);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteInt32Value(in Buffer buffer, string key, Int32 value)
        {
            buffer.WriteOpCode(LogStreamOpCode.Int32Value);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteInt64Value(in Buffer buffer, string key, Int64 value)
        {
            buffer.WriteOpCode(LogStreamOpCode.Int64Value);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteUInt64Value(in Buffer buffer, string key, UInt64 value)
        {
            buffer.WriteOpCode(LogStreamOpCode.UInt64Value);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteFloatValue(in Buffer buffer, string key, float value)
        {
            buffer.WriteOpCode(LogStreamOpCode.FloatValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteDoubleValue(in Buffer buffer, string key, double value)
        {
            buffer.WriteOpCode(LogStreamOpCode.DoubleValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteDecimalValue(in Buffer buffer, string key, decimal value)
        {
            buffer.WriteOpCode(LogStreamOpCode.DecimalValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteStringValue(in Buffer buffer, string key, string value)
        {
            buffer.WriteOpCode(LogStreamOpCode.StringValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value);
        }
        
        private static void WriteTimeSpanValue(in Buffer buffer, string key, TimeSpan value)
        {
            buffer.WriteOpCode(LogStreamOpCode.TimeSpanValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value.Ticks);
        }
        
        private static void WriteDateTimeValue(in Buffer buffer, string key, DateTime value)
        {
            buffer.WriteOpCode(LogStreamOpCode.DateTimeValue);
            buffer.WriteStringKey(key);
            buffer.Writer.Write(value.Ticks);
        }

        public static void WriteValue<T>(in Buffer buffer, string key, T value)
        {
            if (!__writerByType.TryGetValue(typeof(T), out var untypedDelegate))
            {
                WriteStringValue(in buffer, key, value?.ToString() ?? string.Empty);
                return;
            }

            var typedDelegate = (WriteValueCallback<T>)untypedDelegate;
            typedDelegate(in buffer, key, value);
        }
    }
}