using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class BinaryLogStreamReader
    {
        private readonly Stream _input;
        private readonly BinaryReader _reader;
        private readonly Node _root = CreateRootNode();

        private readonly Dictionary<int, string> _stringByKey = new();
        private readonly Dictionary<int, Node> _currentNodeByStreamId = new();
        private readonly Dictionary<int, Node> _nodeById = new();
        private int _lassNodeId = 0;

        public BinaryLogStreamReader(Stream input)
        {
            _input = input;
            _reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);
        }

        public void ReadToEnd()
        {
            var eofExpected = true;
            
            try
            {
                while (true)
                {
                    var opCode = ReadOpCode();

                    switch (opCode)
                    {
                        case LogStreamOpCode.BeginStreamChunk:
                            eofExpected = false;
                            ReadStreamChunk();
                            eofExpected = true;
                            break;
                        case LogStreamOpCode.StringKey:
                            eofExpected = false;
                            ReadStringKey();
                            eofExpected = true;
                            break;
                        default:
                            throw CreateInvalidDataException($"unexpected opcode 0x{(byte)opCode:XX}");
                    }
                }
            }
            catch (EndOfStreamException)
            {
                if (!eofExpected)
                {
                    throw CreateInvalidDataException("unexpected end of file");
                }
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

        private void ReadStringKey()
        {
            var key = _reader.ReadInt32();
            var value = _reader.ReadString();
            _stringByKey.Add(key, value);
        }

        private string ReadString()
        {
            var key = _reader.ReadInt32();
            return GetStringByKey(key);
        }

        private void ReadStreamChunk()
        {
            var streamId = _reader.ReadInt32();
            _reader.ReadInt64(); // startedAtUtc as DateTime.Ticks
            _reader.ReadInt64(); // flushedAtUtc as DateTime.Ticks
            _reader.ReadInt64(); // stream chunk length

            LogStreamOpCode opCode;

            while ((opCode = ReadOpCode()) != LogStreamOpCode.EndStreamChunk)
            {
                Node node;

                switch (opCode)
                {
                    case LogStreamOpCode.Message:
                        ReadMessage(streamId, isSpan: false, out node);
                        break;
                    case LogStreamOpCode.BeginMessage:
                        ReadMessage(streamId, isSpan: false, out node);
                        ReadValues(node, LogStreamOpCode.EndMessage);
                        break;
                    case LogStreamOpCode.OpenSpan:
                        ReadMessage(streamId, isSpan: true, out node);
                        _currentNodeByStreamId[streamId] = node;
                        break;
                    case LogStreamOpCode.BeginOpenSpan:
                        ReadMessage(streamId, isSpan: true, out node);
                        ReadValues(node, LogStreamOpCode.EndOpenSpan);
                        _currentNodeByStreamId[streamId] = node;
                        break;
                    case LogStreamOpCode.CloseSpan:
                        node = GetCurrentNode(streamId, throwIfNotFound: true)!;
                        node.SetEndTime(new DateTime(_reader.ReadInt64(), DateTimeKind.Utc));
                        SetCurrentNodeToParent(streamId, node);
                        break;
                    case LogStreamOpCode.BeginCloseSpan:
                        node = GetCurrentNode(streamId, throwIfNotFound: true)!;
                        node.SetEndTime(new DateTime(_reader.ReadInt64(), DateTimeKind.Utc));
                        ReadValues(node, LogStreamOpCode.EndCloseSpan);
                        SetCurrentNodeToParent(streamId, node);
                        break;
                    default:
                        throw CreateInvalidDataException($"unexpected opcode 0x{(byte)opCode:XX}");
                }
            }
        }

        private void ReadMessage(int streamId, bool isSpan, out Node node)
        {
            var parentNode = GetCurrentNode(streamId) ?? _root;

            var timeTicks = _reader.ReadInt64();
            var messageIdKey = _reader.ReadInt32();
            var logLevel = (LogLevel)_reader.ReadSByte();

            node = new Node(
                nodeId: ++_lassNodeId,
                parentNode,
                time: new DateTime(timeTicks, DateTimeKind.Utc),
                messageId: GetStringByKey(messageIdKey),
                logLevel,
                depth: parentNode.Depth + 1,
                isSpan);

            parentNode.AddNode(node);
            _nodeById.Add(node.NodeId, node);
            
            if (node.IsSpan)
            {
                _currentNodeByStreamId[streamId] = node;
            }
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
                $"ZDL stream invalid: {reason ?? "reason unknown"}. Position <= {_input.Position}");
        }

        private Node? GetCurrentNode(int streamId, bool throwIfNotFound = false)
        {
            if (_currentNodeByStreamId.TryGetValue(streamId, out var node))
            {
                return node;
            }

            if (throwIfNotFound)
            {
                throw CreateInvalidDataException($"expected current node for stream id {streamId}");
            }

            return null;
        }

        private void SetCurrentNodeToParent(int streamId, Node node)
        {
            if (node.Parent != null && node.Parent != _root)
            {
                _currentNodeByStreamId[streamId] = node.Parent;
            }
            else
            {
                _currentNodeByStreamId.Remove(streamId);
            }
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
            return new Node(0, null, DateTime.MinValue, string.Empty, LogLevel.Debug, depth: -1, isSpan: true);
        }

        public class Node
        {
            private List<NamedValue>? _values = null;
            private List<Node>? _nodes = null;

            public Node(int nodeId, Node? parent, DateTime time, string messageId, LogLevel level, int depth, bool isSpan)
            {
                NodeId = nodeId;
                Parent = parent;
                Time = time;
                MessageId = messageId;
                Level = level;
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
            private static readonly IReadOnlyDictionary<LogStreamOpCode, Func<BinaryLogStreamReader, NamedValue>> __readerByOpCode =
                new Dictionary<LogStreamOpCode, Func<BinaryLogStreamReader, NamedValue>>() {
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

            public static NamedValue ReadValue(LogStreamOpCode opCode, BinaryLogStreamReader logReader)
            {
                if (!__readerByOpCode.TryGetValue(opCode, out var func))
                {
                    throw logReader.CreateInvalidDataException($"unknown value opcode: 0x{(byte) opCode:XX}");
                }

                return func(logReader);
            }
        }

    }
}
