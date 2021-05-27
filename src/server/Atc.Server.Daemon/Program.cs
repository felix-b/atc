using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Atc.Data.Primitives;
using Atc.World;
using Atc.World.Redux;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Zero.Doubt.Logging;
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
            RunEndpoint().Wait();
            
            Console.WriteLine("Goodbye World!");
        }

        private static async Task RunEndpoint()
        {
            ConsoleLog.Level = LogLevel.Debug;
            
            var store = new RuntimeStateStore();
            var world = new RuntimeWorld(store, DateTime.Now);
            var service = new WorldService(world, new WorldServiceLogger(ConsoleLog.Writer));
            
            await using var endpoint = WebSocketEndpoint
                .Define()
                    .ReceiveMessagesOfType<AtcProto.ClientToServer>()
                    .WithDiscriminator(m => m.PayloadCase)
                    .SendMessagesOfType<AtcProto.ServerToClient>()
                    .ListenOn(portNumber: 9002, urlPath: "/ws")
                    .BindToServiceInstance(service)
                .Create(out var taskSynchronizer);

            AddDemoPlanes();
            
            var clock = new RuntimeClock(TimeSpan.FromSeconds(10), taskSynchronizer, world);
            clock.Start();
            
            await endpoint.RunAsync();

            void AddDemoPlanes()
            {
                var nextHeadingDegrees = 0;
                
                for (var lat = 10 ; lat <= 50 ; lat += 10)
                {
                    for (var lon = 10 ; lon <= 50 ; lon += 10)
                    {
                        var location = new GeoPoint(lat, lon);
                        
                        taskSynchronizer.SubmitTask(() => {
                            world.AddAircraft(
                                "B738",
                                $"N{location.Lat}{location.Lon}",
                                location,
                                Bearing.FromTrueDegrees(nextHeadingDegrees % 360));
                        });

                        nextHeadingDegrees += 90;
                    }
                }
            }
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
