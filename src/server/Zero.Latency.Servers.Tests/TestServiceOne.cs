using System;
using System.Threading.Tasks;
using TestProto;

namespace Zero.Latency.Servers.Tests
{
    public class TestServiceOne
    {
        [PayloadCase(TestClientToServer.PayloadOneofCase.hello)]
        public void Hello(IDeferredConnectionContext<TestServerToClient> connection, TestClientToServer request)
        {
            OnHello?.Invoke(connection, request);
        }

        [PayloadCase(TestClientToServer.PayloadOneofCase.goodbye)]
        public void Goodbye(IDeferredConnectionContext<TestServerToClient> connection, TestClientToServer request)
        {
            OnGoodbye?.Invoke(connection, request);            
        }

        public Action<IDeferredConnectionContext<TestServerToClient>, TestClientToServer>? OnHello { get; set; }
        public Action<IDeferredConnectionContext<TestServerToClient>, TestClientToServer>? OnGoodbye { get; set; }
    }
}
