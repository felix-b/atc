using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Atc.Data;
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
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

[assembly:GenerateLogger(typeof(IAtcdLogger))]
[assembly:GenerateLogger(typeof(IEndpointLogger))]
[assembly:GenerateLogger(typeof(WorldService.ILogger))]
[assembly:GenerateLogger(typeof(RuntimeWorld.ILogger))]

namespace Atc.Server.Daemon
{
    class Program
    {
        private static IServiceTaskSynchronizer? _taskSynchronizer = null;
        
        static void Main(string[] args)
        {
            Console.WriteLine("atc daemon starting");
            // var hostBuilder = new ServiceHostBuilder(new EchoAcceptorMiddleware());
            // var host = hostBuilder.CreateHost();
            // host.Run();

            BufferContextScope.UseStaticScope();
            ConsoleLog.Level = LogLevel.Debug;

            var container = CompositionRoot();
            var logger = container.Resolve<IAtcdLogger>();

            using (LoadCache(args[0], logger))
            {
                RunEndpoint(container).Wait();
            }
            
            Console.WriteLine("atc daemon down.");
        }

        private static IDisposable LoadCache(string filePath, IAtcdLogger logger)
        {
            logger.LoadingCache(filePath);
            
            using var file = File.OpenRead(filePath);
            var context = BufferContext.ReadFrom(file);

            logger.CacheLoaded();
            return new BufferContextScope(context);
        }

        private static IContainer CompositionRoot()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(ConsoleLog.Writer).As<LogWriter>();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<IAtcdLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<IEndpointLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<WorldService.ILogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<RuntimeWorld.ILogger>()).AsImplementedInterfaces();

            builder.RegisterType<RuntimeStateStore>().As<IRuntimeStateStore>().SingleInstance();
            builder.RegisterType<RuntimeWorld>().SingleInstance().WithParameter("startTime", DateTime.Now);
            builder.RegisterType<WorldService>().SingleInstance();

            builder.Register(c => _taskSynchronizer!).SingleInstance().As<IServiceTaskSynchronizer>();
            builder.RegisterType<RuntimeClock>().SingleInstance().WithParameter("interval", TimeSpan.FromSeconds(10));
            
            return builder.Build();
        }

        private static async Task RunEndpoint(IContainer container)
        {
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

            var clock = container.Resolve<RuntimeClock>();
            clock.Start();

            await endpoint.StartAsync();

            Console.WriteLine("atc daemon up.");                    
            
            await endpoint.WaitForShutdownAsync();

            void AddDemoPlanes()
            {
                var nextHeadingDegrees = 0;
                
                for (var lat = 10 ; lat <= 50 ; lat += 2)
                {
                    for (var lon = 10 ; lon <= 50 ; lon += 2)
                    {
                        var location = new GeoPoint(lat, lon);
                        var heading = nextHeadingDegrees % 360;
                        nextHeadingDegrees += 15;
                        
                        _taskSynchronizer!.SubmitTask(() => {
                            // this callback runs on the Input thread
                            world.AddNewAircraft(
                                "B738",
                                $"N{location.Lat}{location.Lon}",
                                airlineIcao: null,
                                AircraftCategories.Jet,
                                OperationTypes.Airline,
                                location,
                                Altitude.FromFeetMsl(20000 + lat * 100 + lon * 100), 
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
