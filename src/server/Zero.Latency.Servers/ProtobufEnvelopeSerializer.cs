using System;
using System.Buffers;
using ProtoBuf;

namespace Zero.Latency.Servers
{
    public class ProtobufEnvelopeSerializer<TIncoming> : IMessageSerializer
        where TIncoming : class
    {
        public void SerializeMessage(object message, IBufferWriter<byte> writer)
        {
            Serializer.Serialize(writer, message);
        }

        public object DeserializeMessage(ArraySegment<byte> buffer)
        {
            return Serializer.Deserialize<TIncoming>(buffer.AsSpan());
        }
    }
}
