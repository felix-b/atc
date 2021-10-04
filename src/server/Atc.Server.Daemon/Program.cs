using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Server;
using Atc.Server.Daemon;
using Atc.Sound;
using Atc.Speech.Abstractions;
using Atc.Speech.WinLocalPlugin;
using Atc.World;
using Atc.World.Comms;
using Autofac;
using Just.Cli;
using Zero.Doubt.Logging;
using Zero.Latency.Servers;
using Zero.Loss.Actors;
using Zero.Loss.Actors.Impl;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;
using IContainer = Autofac.IContainer;

[assembly:GenerateLogger(typeof(IAtcdLogger))]
[assembly:GenerateLogger(typeof(IEndpointLogger))]
[assembly:GenerateLogger(typeof(StateStore.ILogger))]
[assembly:GenerateLogger(typeof(WorldService.ILogger))]
[assembly:GenerateLogger(typeof(WorldActor.ILogger))]
[assembly:GenerateLogger(typeof(ISoundSystemLogger))]
[assembly:GenerateLogger(typeof(ICommsLogger))]

namespace Atc.Server.Daemon
{
    class Program
    {
        private static IServiceTaskSynchronizer? _taskSynchronizer = null;
        
        static int Main(string[] args)
        {
            if (!ParseCommandLine(args, out var cacheFilePath, out var listenPort))
            {
                Console.WriteLine("atcd - Air Traffic & Control daemon");
                Console.WriteLine("call: atcd --cache <file_path> --listen <port_number>");
                return 1;
            }

            Console.WriteLine("atc daemon starting.");
            InitializeLogging();

            try
            {
                BufferContextScope.UseStaticScope();

                var container = CompositionRoot();
                var logger = container.Resolve<IAtcdLogger>();

                using var audioContext = container.Resolve<AudioContextScope>();
                using var cacheContext = LoadCache(cacheFilePath, logger);

                //BufferContext.Current.RunIntegrityCheck("Program - after load");
                
                RunEndpoint(container, listenPort).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 100;
            }

            Console.WriteLine("atc daemon down.");
            return 0;
        }

        private static bool ParseCommandLine(string[] args, out string cacheFilePath, out int listenPort)
        {
            // TODO: make it short
            
            string? cacheFilePathValue = Environment.GetEnvironmentVariable("ATC_CACHE");
            int? listenPortValue = ParseIntOrDefault(Environment.GetEnvironmentVariable("ATC_PORT"));

            var parserBuilder = CommandLineParser.NewBuilder();
            parserBuilder.NamedValue<string>("--cache", value => cacheFilePathValue = value);
            parserBuilder.NamedValue<int>("--listen", value => listenPortValue = value);
            
            var parser = parserBuilder.Build();
            var success = parser.Parse(args);

            if (success && cacheFilePathValue != null && listenPortValue != null)
            {
                cacheFilePath = cacheFilePathValue;
                listenPort = listenPortValue.Value;
                return true;
            }

            cacheFilePath = string.Empty;
            listenPort = -1;
            return false;
            
            int? ParseIntOrDefault(string? s)
            {
                if (s != null && Int32.TryParse(s, out var value))
                {
                    return value;
                }
                return null;
            }
        }

        private static void InitializeLogging()
        {
            var binaryLogFilePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location ?? string.Empty) ?? string.Empty,
                "log.zdl");

            var binaryStream = BinaryLogStream.Create(binaryLogFilePath);
            
            LogEngine.Level = LogLevel.Debug;
            LogEngine.SetTargetToPipeline(
                binaryStream.CreateWriter,
                ConsoleLogStreamWriter.Factory
            );

            LogEngine.BranchAsyncTask("atcd main");
            Console.WriteLine($"writing log file to: {binaryLogFilePath}");
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

            builder.RegisterInstance(LogEngine.Writer).As<LogWriter>();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<IAtcdLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<IEndpointLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<StateStore.ILogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<WorldService.ILogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<WorldActor.ILogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<ISoundSystemLogger>()).AsImplementedInterfaces();
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<ICommsLogger>()).AsImplementedInterfaces();

            builder.RegisterType<AutofacActorDependencyContext>().As<IActorDependencyContext>().InstancePerDependency();
            builder.RegisterType<StateStore>().As<IStateStore, IInternalStateStore>().SingleInstance();
            builder.RegisterType<SupervisorActor>().As<ISupervisorActor, ISupervisorActorInit>().SingleInstance();
            builder.Register(CreateWorld).As<WorldActor, IWorldContext>().SingleInstance();
            builder.RegisterType<WorldService>().SingleInstance();

            builder.Register(c => _taskSynchronizer!).SingleInstance().As<IServiceTaskSynchronizer>();
            builder.RegisterType<RuntimeClock>().SingleInstance().WithParameter("interval", TimeSpan.FromSeconds(10));

            LoadSpeechPlugins(builder);

            builder.RegisterType<AudioContextScope>().InstancePerDependency();
            builder.RegisterType<RadioSpeechPlayer>().SingleInstance();
            builder.RegisterType<TempMockLlhzRadio>().SingleInstance();

            var container = builder.Build();
            RegisterActorTypes(container.Resolve<ISupervisorActorInit>());

            return container;
        }

        private static void RegisterActorTypes(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<WorldActor, WorldActor.WorldActivationEvent>(
                WorldActor.TypeString, 
                (activation, dependencies) => new WorldActor(
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<WorldActor.ILogger>(),
                    dependencies.Resolve<ISupervisorActor>(),
                    activation
                ));

            supervisor.RegisterActorType<AircraftActor, AircraftActor.ActivationEvent>(
                AircraftActor.TypeString, 
                (activation, dependencies) => new AircraftActor(
                    dependencies.Resolve<IWorldContext>(),
                    dependencies.Resolve<IStateStore>(),
                    activation
                ));

            supervisor.RegisterActorType<RadioStationActor, RadioStationActor.ActivationEvent>(
                RadioStationActor.TypeString, 
                (activation, dependencies) => new RadioStationActor(
                    dependencies.Resolve<ISupervisorActor>(),
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(),
                    dependencies.Resolve<ICommsLogger>(),
                    activation
                ));

            supervisor.RegisterActorType<GroundRadioStationAetherActor, GroundRadioStationAetherActor.ActivationEvent>(
                RadioStationActor.TypeString, 
                (activation, dependencies) => new GroundRadioStationAetherActor(
                    dependencies.Resolve<IWorldContext>(),
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<ICommsLogger>(),
                    activation
                ));
        }
        
        private static WorldActor CreateWorld(IComponentContext components)
        {
            var supervisor = components.Resolve<ISupervisorActor>();
            var worldRef = supervisor.CreateActor<WorldActor>(
                uniqueId => new WorldActor.WorldActivationEvent(
                    uniqueId, 
                    DateTime.UtcNow));
            return worldRef.Get();
        }
        
        private static void LoadSpeechPlugins(ContainerBuilder builder)
        {
            //TODO: load dynamically according to operating system
            builder.RegisterType<WindowsSpeechSynthesisPlugin>().As<ISpeechSynthesisPlugin>().SingleInstance();
        }

        private static async Task RunEndpoint(IContainer container, int listenPortNumber)
        {
            // var store = new RuntimeStateStore();
            // var world = new RuntimeWorld(store, DateTime.Now);
            // var service = new WorldService(world, new WorldService.Logger(ConsoleLog.Writer));

            var service = container.Resolve<WorldService>();
            var world = container.Resolve<WorldActor>();
            var endpointLogger = container.Resolve<IEndpointLogger>();
            
            await using var endpoint = WebSocketEndpoint
                .Define()
                    .ReceiveMessagesOfType<AtcProto.ClientToServer>()
                    .WithDiscriminator(m => m.PayloadCase)
                    .SendMessagesOfType<AtcProto.ServerToClient>()
                    .ListenOn(listenPortNumber, urlPath: "/ws")
                    .BindToServiceInstance(service)
                .Create(endpointLogger, out _taskSynchronizer);

            //BufferContext.Current.RunIntegrityCheck("Program-before-add-demo-planes");

            AddDemoPlanes();

            //BufferContext.Current.RunIntegrityCheck("Program-after-add-demo-planes");

            var clock = container.Resolve<RuntimeClock>();
            clock.Start();

            await endpoint.StartAsync();

            Console.WriteLine($"listening for connections on http://localhost:{listenPortNumber}/ws");                    
            Console.WriteLine("atc daemon up.");  
            
            //BufferContext.Current.RunIntegrityCheck("Program - before run endpoint");
            
            await endpoint.WaitForShutdownAsync();
            
            Console.WriteLine("atc daemon - listening stopped.");                    
            Console.WriteLine("atc daemon stopping.");                    

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
                            
                            world.SpawnNewAircraft(
                                "B738",
                                $"N{location.Lat}{location.Lon}",
                                callsign: null,  
                                airlineIcao: null,
                                AircraftCategories.Jet,
                                OperationTypes.Airline,
                                location,
                                Altitude.FromFeetMsl(20000 + lat * 100 + lon * 100), 
                                heading: Bearing.FromTrueDegrees(heading),
                                groundSpeed: Speed.FromKnots(350));
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
