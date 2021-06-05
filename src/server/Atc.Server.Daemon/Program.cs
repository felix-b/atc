using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Atc.Data.Primitives;
using Atc.Server;
using Atc.Server.Daemon;
using Atc.World;
using Atc.World.Redux;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Zero.Doubt.Logging;
using Zero.Latency.Servers;

[assembly:GenerateLogger(typeof(IEndpointLogger))]
[assembly:GenerateLogger(typeof(WorldService.ILogger))]
[assembly:GenerateLogger(typeof(IAtcdLogger))]

namespace Atc.Server.Daemon
{
    class Program
    {
        private static IServiceTaskSynchronizer? _taskSynchronizer = null;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            // var hostBuilder = new ServiceHostBuilder(new EchoAcceptorMiddleware());
            // var host = hostBuilder.CreateHost();
            // host.Run();

            var container = CompositionRoot();
            RunEndpoint(container).Wait();
            
            Console.WriteLine("Goodbye World!");
        }

        private static IContainer CompositionRoot()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(ConsoleLog.Writer).As<LogWriter>();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<IEndpointLogger>()).As<IEndpointLogger>();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<WorldService.ILogger>()).As<WorldService.ILogger>();

            builder.RegisterType<RuntimeStateStore>().As<IRuntimeStateStore>().SingleInstance();
            builder.RegisterType<RuntimeWorld>().SingleInstance().WithParameter("startTime", DateTime.Now);
            builder.RegisterType<WorldService>().SingleInstance();

            builder.Register(c => _taskSynchronizer!).SingleInstance().As<IServiceTaskSynchronizer>();
            builder.RegisterType<RuntimeClock>().SingleInstance().WithParameter("interval", TimeSpan.FromSeconds(10));
            
            return builder.Build();
        }

        private static async Task RunEndpoint(IContainer container)
        {
            ConsoleLog.Level = LogLevel.Debug;
            
            // var store = new RuntimeStateStore();
            // var world = new RuntimeWorld(store, DateTime.Now);
            // var service = new WorldService(world, new WorldService.Logger(ConsoleLog.Writer));

            var service = container.Resolve<WorldService>();
            var world = container.Resolve<RuntimeWorld>();
            
            await using var endpoint = WebSocketEndpoint
                .Define()
                    .ReceiveMessagesOfType<AtcProto.ClientToServer>()
                    .WithDiscriminator(m => m.PayloadCase)
                    .SendMessagesOfType<AtcProto.ServerToClient>()
                    .ListenOn(portNumber: 9002, urlPath: "/ws")
                    .BindToServiceInstance(service)
                .Create(out _taskSynchronizer);

            AddDemoPlanes();

            var clock = container.Resolve<RuntimeClock>();// new RuntimeClock(TimeSpan.FromSeconds(10), taskSynchronizer, world);
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
                        var heading = nextHeadingDegrees % 360;
                        nextHeadingDegrees += 45;
                        
                        _taskSynchronizer!.SubmitTask(() => {
                            // this callback runs on the Input thread
                            world.AddAircraft(
                                "B738",
                                $"N{location.Lat}{location.Lon}",
                                location,
                                Bearing.FromTrueDegrees(heading));
                        });
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
