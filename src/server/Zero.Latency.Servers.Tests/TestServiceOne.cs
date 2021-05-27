using System;
using System.Threading.Tasks;
using TestProto;

namespace Zero.Latency.Servers.Tests
{
    public class TestServiceOne
    {
        [PayloadCase(TestClientToServer.PayloadOneofCase.hello)]
        public ValueTask Hello(IConnectionContext<TestServerToClient> connection, TestClientToServer request)
        {
            OnHello?.Invoke(connection, request);
            return ValueTask.CompletedTask;
        }

        [PayloadCase(TestClientToServer.PayloadOneofCase.goodbye)]
        public ValueTask Goodbye(IConnectionContext<TestServerToClient> connection, TestClientToServer request)
        {
            OnGoodbye?.Invoke(connection, request);            
            return ValueTask.CompletedTask;
        }

        public Action<IConnectionContext<TestServerToClient>, TestClientToServer>? OnHello { get; set; }
        public Action<IConnectionContext<TestServerToClient>, TestClientToServer>? OnGoodbye { get; set; }
    }
}
