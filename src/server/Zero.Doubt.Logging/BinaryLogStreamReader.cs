using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class BinaryLogStreamReader
    {
        private readonly Stream _input;
        private readonly BinaryReader _reader;
        private readonly Node _root = new Node(0, DateTime.MinValue, string.Empty, isSpan: true);
        private readonly Dictionary<int, Node> _currentNodeByStreamId = new();
        private int _lassNodeId = 0;

        public BinaryLogStreamReader(Stream input)
        {
            _input = input;
            _reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);
        }

        public void ReadToEnd()
        {
            try
            {
                while (true)
                {
                    var opCode = (LogStreamOpCode)_reader.ReadByte();

                    switch (opCode)
                    {
                        case LogStreamOpCode.BeginStreamChunk:
                            ReadStreamChunk();
                            ValidateClosingOpCode(LogStreamOpCode.EndStreamChunk);
                            break;
                        case LogStreamOpCode.StringKey:
                            ReadStringKey();
                            break;
                        default:
                            
                    }
                }
            }
            catch (EndOfStreamException)
            {
            }
        }

        public Node Root => _root;

        private InvalidDataException CreateInvalidDataException(string? reason = null)
        {
            return new InvalidDataException($"Log stream invalid at position {_reader.}")
        }
        
        public class Node
        {
            private List<NameValue>? _values = null;
            private List<Node>? _nodes = null;

            public Node(int nodeId, DateTime time, string messageId, bool isSpan)
            {
                NodeId = nodeId;
                Time = time;
                MessageId = messageId;
                IsSpan = isSpan;
            }

            public void AddValue(string key, string value)
            {
                if (_values == null)
                {
                    _values = new();
                }
                _values.Add(new NameValue(key, value));
            }

            public void AddNode(Node node)
            {
                if (_nodes == null)
                {
                    _nodes = new();
                }
                _nodes.Add(node);
            }

            public int NodeId { get; }
            public DateTime Time { get; }
            public string MessageId { get; }
            private bool IsSpan { get; }
            public DateTime? EndTime { get; }

            public IReadOnlyList<NameValue> Values => _values as IReadOnlyList<NameValue> ?? Array.Empty<NameValue>();
            public IReadOnlyList<Node> Nodes => _nodes as IReadOnlyList<Node> ?? Array.Empty<Node>();
            public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - Time : null;
        }

        public struct NameValue
        {
            public NameValue(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public string Value { get; }
        }
    }
}
