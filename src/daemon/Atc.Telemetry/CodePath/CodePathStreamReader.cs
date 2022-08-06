using System.Globalization;
using System.Text;

namespace Atc.Telemetry.CodePath;

public class CodePathStreamReader
{
    private readonly Node _root = CreateRootNode();
    private readonly Dictionary<int, string> _stringByKey = new();
    private readonly Dictionary<int, Node> _nodeById = new();
    private readonly Dictionary<ulong, Node> _nodeBySpanId = new();

    private Stream _input;
    private BinaryReader _reader;
    private int _lassNodeId = 0;

    public CodePathStreamReader()
    {
        _input = new MemoryStream(Array.Empty<byte>());
        _reader = new BinaryReader(_input, Encoding.UTF8, leaveOpen: true);
    }

    public void ReadLogFile(Stream input)
    {
        _input = input;
        _reader = new BinaryReader(_input, Encoding.UTF8, leaveOpen: true);

        try
        {
            var opCode = ReadOpCode();
            if (opCode != LogStreamOpCode.BeginStreamChunk)
            {
                throw CreateInvalidDataException($"unexpected opcode {opCode}");
            }
            
            ReadStreamChunk();
        }
        catch (EndOfStreamException)
        {
            // if (!eofExpected)
            // {
            //     throw CreateInvalidDataException("unexpected end of file");
            // }
        }
    }

    public void ReadBuffer(Stream buffer)
    {
        _input = buffer;
        _reader = new BinaryReader(_input, Encoding.UTF8, leaveOpen: true);

        try
        {
            ReadStreamChunk();
        }
        catch (EndOfStreamException)
        {
        }
    }

    public Node? TryGetNodeById(int id)
    {
        if (_nodeById.TryGetValue(id, out var node))
        {
            return node;
        }
        return null;
    }

    public BinaryReader Binary => _reader;
    public Node RootNode => _root;
        
    private LogStreamOpCode ReadOpCode()
    {
        return (LogStreamOpCode)_reader.ReadByte();
    }

    private void ReadOpCode(LogStreamOpCode expected)
    {
        var actual = ReadOpCode();
        ValidateOpCode(expected, actual);
    }

    private void ValidateOpCode(LogStreamOpCode expected, LogStreamOpCode actual)
    {
        if (actual != expected)
        {
            throw CreateInvalidDataException(
                $"expected opcode 0x{(byte)expected:XX}, but found byte 0x{(byte)actual:XX}");
        }
    }

    private string ReadString()
    {
        int key = _reader.ReadInt32();
        string value = GetStringByKey(key);
        return value;
    }

    private void ReadStreamChunk()
    {
        LogStreamOpCode opCode;

        while ((opCode = ReadOpCode()) != LogStreamOpCode.EndStreamChunk)
        {
            Node node;

            switch (opCode)
            {
                case LogStreamOpCode.StringKey:
                    ReadStringKeyEntry();
                    break;
                case LogStreamOpCode.Message:
                    ReadMessage(isSpan: false, out node);
                    break;
                case LogStreamOpCode.BeginMessage:
                    ReadMessage(isSpan: false, out node);
                    ReadValues(node, LogStreamOpCode.EndMessage);
                    break;
                case LogStreamOpCode.OpenSpan:
                    ReadSpanMessage(out node);
                    break;
                case LogStreamOpCode.BeginOpenSpan:
                    ReadSpanMessage(out node);
                    ReadValues(node, LogStreamOpCode.EndOpenSpan);
                    break;
                case LogStreamOpCode.CloseSpan:
                case LogStreamOpCode.BeginCloseSpan:
                    ReadCloseSpan(opCode);
                    break;
                default:
                    throw CreateInvalidDataException($"unexpected opcode 0x{(byte)opCode:XX}");
            }
        }
    }

    private void ReadSpanMessage(out Node node)
    {
        var spanId = _reader.ReadUInt64(); 
        ReadMessage(isSpan: true, out node);
        _nodeBySpanId[spanId] = node;
    }

    private void ReadCloseSpan(LogStreamOpCode opCode)
    {
        var spanId = _reader.ReadUInt64();
        var time = new DateTime(_reader.ReadInt64(), DateTimeKind.Utc);

        if (!_nodeBySpanId.TryGetValue(spanId, out var node))
        {
            node = new Node(
                nodeId: ++_lassNodeId,
                _root,
                time: time,
                messageId: "unknown_span",
                LogLevel.Debug,
                threadId: -1,
                depth: _root.Depth + 1,
                isSpan: true);
        }
        
        node.SetEndTime(time);

        if (opCode == LogStreamOpCode.BeginCloseSpan)
        {
            ReadValues(node, LogStreamOpCode.EndCloseSpan);
        }
    }

    private void ReadStringKeyEntry()
    {
        var key = _reader.ReadInt32();
        var value = _reader.ReadString();
        _stringByKey[key] = value;
    }
    
    private void ReadMessage(bool isSpan, out Node node)
    {
        var parentSpanId = _reader.ReadUInt64();
        var parentNode = _nodeBySpanId.ContainsKey(parentSpanId)
            ? _nodeBySpanId[parentSpanId]
            : _root;

        var timeTicks = _reader.ReadInt64();
        var messageId = ReadString();
        var logLevel = (LogLevel)_reader.ReadSByte();
        var threadId = _reader.ReadInt32();

        node = new Node(
            nodeId: ++_lassNodeId,
            parent: parentNode,
            time: new DateTime(timeTicks, DateTimeKind.Utc),
            messageId: messageId,
            level: logLevel,
            threadId: threadId,
            depth: parentNode.Depth + 1,
            isSpan);

        parentNode.AddNode(node);
        _nodeById.Add(node.NodeId, node);
    }

    private void ReadValues(Node node, LogStreamOpCode expectedClosingOpCode)
    {
        var opCode = ReadOpCode();

        while (IsValueOpCode(opCode))
        {
            var value = ValueReader.ReadValue(opCode, this);
            node.AddValue(value);
                
            opCode = ReadOpCode();
        }

        ValidateOpCode(expectedClosingOpCode, opCode);

        static bool IsValueOpCode(LogStreamOpCode opCode)
        {
            return (((byte)opCode) & 0xF0) == 0xC0;
        }
    }
        
    private InvalidDataException CreateInvalidDataException(string? reason = null)
    {
        return new InvalidDataException(
            $"Telemetry stream invalid: {reason ?? "reason unknown"}. Position <= {_input.Position}");
    }

    private string GetStringByKey(int key)
    {
        if (_stringByKey.TryGetValue(key, out var value))
        {
            return value;
        }
        throw CreateInvalidDataException($"unknown string key: {key}");
    }

    private static Node CreateRootNode()
    {
        return new Node(
            nodeId: 0, 
            parent: null, 
            DateTime.MinValue, 
            messageId: string.Empty, 
            level: LogLevel.Debug,
            threadId: -1,
            depth: -1, 
            isSpan: true);
    }

    public class Node
    {
        private List<NamedValue>? _values = null;
        private List<Node>? _nodes = null;

        public Node(int nodeId, Node? parent, DateTime time, string messageId, LogLevel level, int threadId, int depth, bool isSpan)
        {
            NodeId = nodeId;
            Parent = parent;
            Time = time;
            MessageId = messageId;
            Level = level;
            ThreadId = threadId;
            Depth = depth;
            IsSpan = isSpan;
        }

        public void AddValue(NamedValue value)
        {
            if (_values == null)
            {
                _values = new();
            }
            _values.Add(value);
        }

        public void AddNode(Node node)
        {
            if (_nodes == null)
            {
                _nodes = new();
            }
            _nodes.Add(node);
        }

        public void SetEndTime(DateTime utc)
        {
            if (EndTime.HasValue)
            {
                throw new InvalidOperationException("EndTime was already set");
            }
            EndTime = utc;
        }
            
        public int NodeId { get; }
        public Node? Parent { get; }
        public DateTime Time { get; }
        public string MessageId { get; }
        public LogLevel Level { get; }
        public int ThreadId { get; }
        public int Depth { get; }
        public bool IsSpan { get; }
        public DateTime? EndTime { get; private set; }

        public IReadOnlyList<NamedValue> Values => _values as IReadOnlyList<NamedValue> ?? Array.Empty<NamedValue>();
        public IReadOnlyList<Node> Nodes => _nodes as IReadOnlyList<Node> ?? Array.Empty<Node>();
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - Time : null;
    }

    public struct NamedValue
    {
        public NamedValue(LogStreamOpCode type, string name, string value)
        {
            Type = type;
            Name = name;
            Value = value;
        }

        public LogStreamOpCode Type { get; }
        public string Name { get; }
        public string Value { get; }
    }
        
    private static class ValueReader
    {
        private static readonly IReadOnlyDictionary<LogStreamOpCode, Func<CodePathStreamReader, NamedValue>> __readerByOpCode =
            new Dictionary<LogStreamOpCode, Func<CodePathStreamReader, NamedValue>>() {
                { 
                    LogStreamOpCode.ExceptionValue, 
                    logReader => {
                        var type = logReader.Binary.ReadString();
                        var message = logReader.Binary.ReadString();
                        return new NamedValue(LogStreamOpCode.ExceptionValue, string.Empty, $"{type}: {message}");
                    }
                },                
                { 
                    LogStreamOpCode.BoolValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadBoolean(); 
                        return new NamedValue(LogStreamOpCode.BoolValue, name, value.ToString()); 
                    }
                },                
                { 
                    LogStreamOpCode.Int8Value, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadByte(); 
                        return new NamedValue(LogStreamOpCode.Int8Value, name, value.ToString()); 
                    }
                },
                { 
                    LogStreamOpCode.Int16Value, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadInt16(); 
                        return new NamedValue(LogStreamOpCode.Int16Value, name, value.ToString()); 
                    }
                },
                { 
                    LogStreamOpCode.Int32Value, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadInt32(); 
                        return new NamedValue(LogStreamOpCode.Int32Value, name, value.ToString()); 
                    }
                },
                { 
                    LogStreamOpCode.Int64Value, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadInt64(); 
                        return new NamedValue(LogStreamOpCode.Int64Value, name, value.ToString()); 
                    }
                },
                { 
                    LogStreamOpCode.UInt64Value, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadUInt64(); 
                        return new NamedValue(LogStreamOpCode.UInt64Value, name, value.ToString()); 
                    }
                },
                { 
                    LogStreamOpCode.FloatValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadSingle(); 
                        return new NamedValue(LogStreamOpCode.FloatValue, name, value.ToString(CultureInfo.InvariantCulture)); 
                    }
                },
                { 
                    LogStreamOpCode.DoubleValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadDouble(); 
                        return new NamedValue(LogStreamOpCode.DoubleValue, name, value.ToString(CultureInfo.InvariantCulture)); 
                    }
                },
                { 
                    LogStreamOpCode.DecimalValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadDecimal(); 
                        return new NamedValue(LogStreamOpCode.DecimalValue, name, value.ToString(CultureInfo.InvariantCulture)); 
                    }
                },
                { 
                    LogStreamOpCode.StringValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = logReader.Binary.ReadString(); 
                        return new NamedValue(LogStreamOpCode.StringValue, name, value); 
                    }
                },
                { 
                    LogStreamOpCode.TimeSpanValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = new TimeSpan(logReader.Binary.ReadInt64()); 
                        return new NamedValue(LogStreamOpCode.TimeSpanValue, name, value.ToString()); 
                    }
                },
                { 
                    LogStreamOpCode.DateTimeValue, 
                    logReader => {
                        var name = logReader.ReadString();
                        var value = new DateTime(logReader.Binary.ReadInt64(), DateTimeKind.Utc); 
                        return new NamedValue(LogStreamOpCode.DateTimeValue, name, value.ToString("o")); 
                    }
                },
            };

        public static NamedValue ReadValue(LogStreamOpCode opCode, CodePathStreamReader logReader)
        {
            if (!__readerByOpCode.TryGetValue(opCode, out var func))
            {
                throw logReader.CreateInvalidDataException($"unknown value opcode: 0x{(byte) opCode:XX}");
            }

            return func(logReader);
        }
    }
}
