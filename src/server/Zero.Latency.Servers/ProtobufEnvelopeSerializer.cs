using System;
using System.Buffers;

namespace Zero.Latency.Servers
{
    public class ProtobufEnvelopeSerializer<TEnvelopeIn> : IMessageSerializer
        where TEnvelopeIn : class
    {
        private readonly IEndpointLogger _logger;

        public ProtobufEnvelopeSerializer(IEndpointLogger logger)
        {
            _logger = logger;
        }

        public void SerializeOutgoingEnvelope(object envelope, IBufferWriter<byte> writer)
        {
            ProtoBuf.Serializer.Serialize(writer, envelope);
        }

        public object DeserializeIncomingEnvelope(ArraySegment<byte> buffer)
        {
            return ProtoBuf.Serializer.Deserialize<TEnvelopeIn>(buffer.AsSpan());
        }
    }
}
