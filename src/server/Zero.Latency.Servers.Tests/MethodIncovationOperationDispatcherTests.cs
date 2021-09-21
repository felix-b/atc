using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TestProto;

namespace Zero.Latency.Servers.Tests
{
    [TestFixture]
    public class MethodIncovationOperationDispatcherTests
    {
        private readonly IEndpointLogger _logger = new NoopEndpointLogger();

        [Test]
        public void CanInvokeServiceMethods()
        {
            var log = new List<string>();

            var service = new TestServiceOne() {
                OnHello = (conn, req) => {
                    log.Add($"Hello:{req.hello.Query}");
                },
                OnGoodbye = (conn, req) => {
                    log.Add($"Goodbye:{req.goodbye.Farewell}");
                },
            };
            
            var dispatcher = new MethodInvocationOperationDispatcher<
                TestClientToServer, 
                TestServerToClient,
                TestClientToServer.PayloadOneofCase
            >(
                service,
                request => request.PayloadCase,
                _logger
            );

            dispatcher.DispatchOperation(
                new TestConnectionContext<TestServerToClient>(), 
                new TestClientToServer() {
                    Id = 123,
                    hello = new() {
                        Query = "ABC"
                    }
                }
            );
            dispatcher.DispatchOperation(
                new TestConnectionContext<TestServerToClient>(), 
                new TestClientToServer() {
                    Id = 123,
                    goodbye = new() {
                        Farewell = "XYZ"
                    }
                }
            );

            log.Should().BeEquivalentTo(new[] { "Hello:ABC", "Goodbye:XYZ" });
        }
    }
}