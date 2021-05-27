using System;
using System.Buffers;

namespace Zero.Latency.Servers
{
    public interface IMessageSerializer
    {
        void SerializeOutgoingEnvelope(object envelope, IBufferWriter<byte> writer);
        object DeserializeIncomingEnvelope(ArraySegment<byte> buffer);
    }
}
