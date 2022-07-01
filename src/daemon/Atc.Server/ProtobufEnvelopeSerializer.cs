using System.Buffers;

namespace Atc.Server;

public class ProtobufEnvelopeSerializer<TEnvelopeIn> : IMessageSerializer
    where TEnvelopeIn : class
{
    private readonly IEndpointTelemetry _telemetry;

    public ProtobufEnvelopeSerializer(IEndpointTelemetry telemetry)
    {
        _telemetry = telemetry;
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