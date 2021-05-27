using System;
using System.Buffers;

namespace Zero.Latency.Servers
{
    public interface IMessageSerializer
    {
        void SerializeMessage(object message, IBufferWriter<byte> writer);
        object DeserializeMessage(ArraySegment<byte> buffer);
    }
}
