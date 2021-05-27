using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using TestProto;

namespace Zero.Latency.Servers.Tests
{
    [TestFixture(Category = "e2e")]
    public class EndToEndTests
    {
        [Test]
        public async Task CanInstantiateAndDisposeEndpoint()
        {
            var endpoint = new WebSocketEndpoint(port: 57000, urlPath: "/ws", new NoopSocketAcceptor());
            await endpoint.DisposeAsync();
        }

        [Test]
        public async Task CanStartAndStopEndpoint()
        {
            await using var endpoint = new WebSocketEndpoint(port: 57000, urlPath: "/ws", new NoopSocketAcceptor());
            await endpoint.StartAsync();
            await endpoint.StopAsync(TimeSpan.FromSeconds(10));
        }

        [Test]
        public async Task CanStartAndStopTestService()
        {
            await using var endpoint = CreateTestServiceOneEndpoint();
            await endpoint.StartAsync();
            await endpoint.StopAsync(TimeSpan.FromSeconds(10));
        }

        private WebSocketEndpoint CreateTestServiceOneEndpoint()
        {
            return WebSocketEndpoint
                .Define()
                    .ReceiveMessagesOfType<TestClientToServer>()
                    .WithDiscriminator(m => m.PayloadCase)
                    .SendMessagesOfType<TestServerToClient>()
                    .ListenOn(portNumber: 57000, urlPath: "/ws")
                    .BindToServiceInstance(new TestServerToClient())
                .Create(out _);
        }
        
        private class NoopSocketAcceptor : ISocketAcceptor
        {
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            public async Task AcceptSocket(HttpContext context, WebSocket socket, CancellationToken cancel)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancel);
            }
        }
    }
}