using System.Buffers;

namespace Atc.Server;

public interface IMessageSerializer
{
    void SerializeOutgoingEnvelope(object envelope, IBufferWriter<byte> writer);
    object DeserializeIncomingEnvelope(ArraySegment<byte> buffer);
}