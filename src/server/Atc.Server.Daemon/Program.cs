using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Math;
using Atc.Server;
using Atc.Server.Daemon;
using Atc.Sound;
using Atc.Speech.AzurePlugin;
using Atc.Speech.WinLocalPlugin;
using Atc.World;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Atc.World.LLHZ;
using Atc.World.Traffic.Maneuvers;
using Autofac;
using Just.Cli;
using Zero.Doubt.Logging;
using Zero.Latency.Servers;
using Zero.Loss.Actors;
using Zero.Loss.Actors.Impl;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;
using AircraftActor = Atc.World.Traffic.AircraftActor;
using IContainer = Autofac.IContainer;

[assembly:GenerateLogger(typeof(IAtcdLogger))]
[assembly:GenerateLogger(typeof(IEndpointLogger))]
[assembly:GenerateLogger(typeof(StateStore.ILogger))]
[assembly:GenerateLogger(typeof(WorldService.ILogger))]
[assembly:GenerateLogger(typeof(WorldActor.ILogger))]
[assembly:GenerateLogger(typeof(ISoundSystemLogger))]
[assembly:GenerateLogger(typeof(ICommsLogger))]
[assembly:GenerateLogger(typeof(AIRadioOperatingActor.ILogger))]

namespace Atc.Server.Daemon
{
    class Program
    {
        private static IServiceTaskSynchronizer? _taskSynchronizer = null;
        
        static int Main(string[] args)
        {
            if (!ParseCommandLine(args, out var cacheFilePath, out var listenPort, out var llhzMode))
            {
                Console.WriteLine("atcd - Air Traffic & Control daemon");
                Console.WriteLine("call: atcd --listen <port_number> --cache <file_path>");
                Console.WriteLine("  or: atcd --listen <port_number> --llhz");
                return 1;
            }

            Console.WriteLine($"atc daemon starting {(llhzMode ? "[LLHZ mode ON]" : "")}.");
            InitializeLogging();

            try
            {
                BufferContextScope.UseStaticScope();

                var container = CompositionRoot(llhzMode);
                var logger = container.Resolve<IAtcdLogger>();

                using var audioContext = container.Resolve<AudioContextScope>();
                using var cacheContext = LoadCache(cacheFilePath, logger, llhzMode);

                //BufferContext.Current.RunIntegrityCheck("Program - after load");
                
                RunEndpoint(container, listenPort, llhzMode).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 100;
            }

            Console.WriteLine("atc daemon down.");
            return 0;
        }

        private static bool ParseCommandLine(string[] args, out string cacheFilePath, out int listenPort, out bool llhzMode)
        {
            // TODO: make it short
            
            string? cacheFilePathValue = Environment.GetEnvironmentVariable("ATC_CACHE");
            int? listenPortValue = ParseIntOrDefault(Environment.GetEnvironmentVariable("ATC_PORT"));

            var parserBuilder = CommandLineParser.NewBuilder();
            parserBuilder.NamedValue<string>("--cache", value => cacheFilePathValue = value);
            parserBuilder.NamedValue<int>("--listen", value => listenPortValue = value);
            
            //TODO: remove
            bool llhzModeWasSet = false;
            parserBuilder.NamedFlag("--llhz", value => llhzModeWasSet = value);
            
            var parser = parserBuilder.Build();
            var success = parser.Parse(args);

            if (success && cacheFilePathValue != null && listenPortValue != null)
            {
                llhzMode = llhzModeWasSet;
                cacheFilePath = cacheFilePathValue;
                listenPort = listenPortValue.Value;
                return true;
            }

            llhzMode = false;
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

        private static IDisposable LoadCache(string filePath, IAtcdLogger logger, bool llhzMode)
        {
            if (llhzMode)
            {
                return new LlhzBufferContext();
            }
            
            logger.LoadingCache(filePath);
            
            using var file = File.OpenRead(filePath);
            var context = BufferContext.ReadFrom(file);

            logger.CacheLoaded();
            return new BufferContextScope(context);
        }

        private static IContainer CompositionRoot(bool llhzMode)
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
            builder.RegisterType(ZLoggerFactory.GetGeneratedLoggerType<AIRadioOperatingActor.ILogger>()).AsImplementedInterfaces();

            builder.RegisterType<AutofacActorDependencyContext>().As<IActorDependencyContext>().InstancePerDependency();
            builder.RegisterType<StateStore>().As<IStateStore, IInternalStateStore>().SingleInstance();
            builder.RegisterType<SupervisorActor>().As<ISupervisorActor, ISupervisorActorInit>().SingleInstance();
            builder.Register(CreateWorld).As<WorldActor, IWorldContext>().SingleInstance();
            builder.RegisterType<WorldService>().SingleInstance();
            builder.RegisterType<RadioSpeechPlayer>().As<IRadioSpeechPlayer>().InstancePerDependency();
            builder.RegisterType<UserRadioMonitor>().InstancePerDependency();

            builder.Register(c => _taskSynchronizer!).SingleInstance().As<IServiceTaskSynchronizer>();
            builder.RegisterType<RuntimeClock>().SingleInstance().WithParameter("interval", TimeSpan.FromMilliseconds(250));
            builder.RegisterType<RealSystemEnvironment>().As<ISystemEnvironment>().SingleInstance();

            LoadSpeechPlugins(builder);

            builder.RegisterType<AudioContextScope>().SingleInstance();
            builder.RegisterType<RadioSpeechPlayer>().InstancePerDependency();

            //TODO: remove
            if (llhzMode)
            {
                builder.RegisterType<TempMockLlhzRadio>().SingleInstance(); 
                builder.RegisterType<LlhzVerbalizationService>().As<IVerbalizationService>().SingleInstance();
            }

            var container = builder.Build();
            RegisterActorTypes(container.Resolve<ISupervisorActorInit>(), llhzMode);

            return container;
        }

        private static void RegisterActorTypes(ISupervisorActorInit supervisor, bool llhzMode)
        {
            WorldActor.RegisterType(supervisor);

            RadioStationActor.RegisterType(supervisor);
            GroundRadioStationAetherActor.RegisterType(supervisor);
            AircraftActor.RegisterType(supervisor);

            if (llhzMode)
            {
                LlhzAirportActor.RegisterType(supervisor);
                LlhzControllerActor.RegisterType(supervisor);
                LlhzPilotActor.RegisterType(supervisor);
            }
    
            // supervisor.RegisterActorType<AircraftActor, AircraftActor.ActivationEvent>(
            //     AircraftActor.TypeString, 
            //     (activation, dependencies) => new AircraftActor(
            //         dependencies.Resolve<IWorldContext>(),
            //         dependencies.Resolve<IStateStore>(),
            //         activation
            //     ));
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
            //builder.RegisterType<WindowsSpeechSynthesisPlugin>().As<ISpeechSynthesisPlugin>().SingleInstance();
            builder.RegisterType<AzureSpeechSynthesisPlugin>().As<ISpeechSynthesisPlugin>().SingleInstance();
        }

        private static async Task RunEndpoint(IContainer container, int listenPortNumber, bool llhzMode)
        {
            // var store = new RuntimeStateStore();
            // var world = new RuntimeWorld(store, DateTime.Now);
            // var service = new WorldService(world, new WorldService.Logger(ConsoleLog.Writer));

            var service = container.Resolve<WorldService>();
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

            if (llhzMode)
            {
                StartLlhzAirport(container.Resolve<ISupervisorActor>());
            }
            else
            {
                AddDemoPlanes(container.Resolve<WorldActor>());
            }

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
        }

        private static void AddDemoPlanes(WorldActor world)
        {
            var nextHeadingDegrees = 0;

            for (var lat = 10; lat <= 50; lat += 2)
            {
                for (var lon = 10; lon <= 50; lon += 2)
                {
                    var location = new GeoPoint(lat, lon);
                    var heading = nextHeadingDegrees % 360;
                    nextHeadingDegrees += 15;

                    _taskSynchronizer!.SubmitTask(
                        () => {
                            // this callback runs on the Input thread
                            var maneuver = CreateManeuver(location, heading, TimeSpan.FromHours(5));
                            world.SpawnNewAircraft(
                                "B738",
                                $"N{location.Lat}{location.Lon}",
                                callsign: null,
                                airlineIcao: null,
                                AircraftCategories.Jet,
                                OperationTypes.Airline,
                                maneuver);
                        });
                }
            }

            MockupMoveManeuver CreateManeuver(GeoPoint fromLocation, int heading, TimeSpan duration)
            {
                var startTime = world.UtcNow();
                var finishTime = startTime.Add(duration);
                var groundSpeed = Speed.FromKnots(350);

                GeoMath.CalculateGreatCircleDestination(
                    fromLocation,
                    Bearing.FromTrueDegrees(heading),
                    Distance.FromNauticalMiles(groundSpeed.Knots * duration.TotalHours),
                    out var toLocation);

                return World.Traffic.Maneuvers.MockupMoveManeuver.Create(
                    "move-straight",
                    startTime,
                    finishTime,
                    fromLocation,
                    toLocation);
            }
        }

        private static void StartLlhzAirport(ISupervisorActor supervisor)
        {
            supervisor.CreateActor<LlhzAirportActor>(
                uniqueId => new LlhzAirportActor.LlhzAirportActivationEvent(uniqueId, AircraftCount: 4)
            );
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
