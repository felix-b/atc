using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AtcProto;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Zero.Latency.Servers;

namespace Atc.Server.Daemon
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // var hostBuilder = new ServiceHostBuilder(new EchoAcceptorMiddleware());
            // var host = hostBuilder.CreateHost();
            // host.Run();
            RunDaemon().Wait();
            
            Console.WriteLine("Goodbye World!");
        }

        private static async Task RunDaemon()
        {
            // await using var endpoint = new WebSocketEndpoint(port: 57000, urlPath: "/ws", new NoopSocketAcceptor());

            var serviceInstance = new WorldService();
            var endpoint = WebSocketEndpoint
                .Define()
                    .ReceiveMessagesOfType<ClientToServer>()
                    .WithDiscriminator(m => m.PayloadCase)
                    .SendMessagesOfType<ServerToClient>()
                    .ListenOn(portNumber: 9002, urlPath: "/ws")
                    .BindToServiceInstance(serviceInstance)
                .Create();            
            
            await endpoint.RunAsync();
        }

        // private class NoopSocketAcceptor : ISocketAcceptor
        // {
        //     public ValueTask DisposeAsync()
        //     {
        //         return ValueTask.CompletedTask;
        //     }
        //
        //     public async Task AcceptSocket(HttpContext context, WebSocket socket, CancellationToken cancel)
        //     {
        //         await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancel);
        //     }
        // }
    }
}
