using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Atc.World.LLHZ;
using Atc.World.Testability.AI;
using Atc.World.Testability.Comms;
using Zero.Doubt.Logging;
using Zero.Loss.Actors;
using Zero.Loss.Actors.Impl;

namespace Atc.World.Testability
{
    public class WorldSetup
    {
        private readonly DateTime _worldStartTimeUtc = new DateTime(2021, 10, 15, 10, 30, 0, DateTimeKind.Utc);
        private MemoryStream? _logBinaryOut = null;
        private Action? _flushLogBinaryOutputs = null;
        private Func<IEnumerable<InspectingLogWriter.LogEntry>>? _getInspectableLogEntries = null;

        public WorldSetup(
            Action<SimpleDependencyContext>? configureDependencyContext = null,
            LogType logType = LogType.Noop)
        {
            var effectiveLogWriter = CreateLogWriter(logType);

            DependencyContextBuilder = new SimpleDependencyContext();
            DependencyContextBuilder.WithSingleton<ISystemEnvironment>(Environment);
            
            configureDependencyContext?.Invoke(DependencyContextBuilder);

            DependencyContextBuilder.WithSingleton<IVerbalizationService>(new TestVerbalizationService());
            DependencyContextBuilder.WithSingleton<LogWriter>(effectiveLogWriter);

            CommsLogger = ZLoggerFactory.CreateLogger<ICommsLogger>(effectiveLogWriter);
            var storeLogger = ZLoggerFactory.CreateLogger<StateStore.ILogger>(effectiveLogWriter);
            
            DependencyContextBuilder.WithSingleton<ICommsLogger>(CommsLogger);
            DependencyContextBuilder.WithSingleton<StateStore.ILogger>(storeLogger);
            DependencyContextBuilder.WithSingleton<WorldActor.ILogger>(ZLoggerFactory.CreateLogger<WorldActor.ILogger>(effectiveLogWriter));
            DependencyContextBuilder.WithSingleton<ISoundSystemLogger>(ZLoggerFactory.CreateLogger<ISoundSystemLogger>(effectiveLogWriter));
            DependencyContextBuilder.WithSingleton<AIRadioOperatingActor.ILogger>(ZLoggerFactory.CreateLogger<AIRadioOperatingActor.ILogger>(effectiveLogWriter));

            Store = new StateStore(storeLogger);
            DependencyContextBuilder.WithSingleton<IStateStore>(Store);
            
            Supervisor = new SupervisorActor(Store, DependencyContextBuilder);
            DependencyContextBuilder.WithSingleton<ISupervisorActor>(Supervisor);
            
            WorldActor.RegisterType(Supervisor);
            RadioStationActor.RegisterType(Supervisor);
            GroundRadioStationAetherActor.RegisterType(Supervisor);
            AircraftActor.RegisterType(Supervisor);
            DummyCycledTransmittingActor.RegisterType(Supervisor);
            DummyPingPongActor.RegisterType(Supervisor);
            LlhzAirportActor.RegisterType(Supervisor);
            LlhzControllerActor.RegisterType(Supervisor);
            LlhzPilotActor.RegisterType(Supervisor);

            World = Supervisor.CreateActor<WorldActor>(id => {
                return new WorldActor.WorldActivationEvent(
                    id, 
                    _worldStartTimeUtc);
            });
            DependencyContextBuilder.WithSingleton<IWorldContext>(World.Get());

            Stations = new();
            Aethers = new();
        }

        private LogWriter CreateLogWriter(LogType type)
        {
            switch (type)
            {
                case LogType.Inspectable:
                    return InspectingLogWriter.Create(LogLevel.Debug, SafeWorldUtcNow, out _getInspectableLogEntries);
                case LogType.BinaryStream:
                    _logBinaryOut = new MemoryStream();
                    var logStream = new BinaryLogStream(_logBinaryOut);
                    var logStreamWriter = (BinaryLogStreamWriter) logStream.CreateWriter();
                    _flushLogBinaryOutputs = logStreamWriter.Flush;
                    return new LogWriter(() => LogLevel.Debug, SafeWorldUtcNow, () => logStreamWriter);
                default:
                    return LogWriter.Noop;
            }
        }
        
        public void AddIntentListener(Action<Intent> onIntent)
        {
            Store.AddEventListener(ListenToIntents, out _);

            void ListenToIntents(in StateEventEnvelope envelope)
            {
                if (envelope.Event is ImmutableStateMachine.TriggerEvent trigger && trigger.Intent != null)
                {
                    onIntent(trigger.Intent);
                }
            }
        }

        public ActorRef<RadioStationActor> AddAirStation(Frequency frequency, GeoPoint location, Altitude elevation, string callsign)
        {
            var station = Supervisor.CreateActor<RadioStationActor>(id => new RadioStationActor.ActivationEvent(
                id, 
                location, 
                elevation, 
                frequency, 
                Name: callsign, 
                callsign));
            
            Stations.Add(station);
            return station;
        }

        public GroundStationResult AddGroundStation(Frequency frequency, GeoPoint location, string callsign)
        {
            var station = Supervisor.CreateActor<RadioStationActor>(id => new RadioStationActor.ActivationEvent(
                id, 
                location, 
                Altitude.FromFeetMsl(10), 
                frequency, 
                Name: callsign, 
                callsign));
            
            var aether = World.Get().AddGroundStation(station);
            
            Stations.Add(station);
            Aethers.Add(aether);

            return new(station, aether);
        }

        public void RunWorldFastForward(TimeSpan iterationInterval, int iterationCount)
        {
            for (int iteration = 0 ; iteration < iterationCount ; iteration++)
            {
                World.Get().ProgressBy(iterationInterval);
            }
        }

        public void RunWorldRealTime(TimeSpan iterationInterval, int iterationCount)
        {
            for (int iteration = 0 ; iteration < iterationCount ; iteration++)
            {
                Thread.Sleep(iterationInterval);
                World.Get().ProgressBy(iterationInterval);
            }
        }

        public T ResolveDependency<T>() where T : class
        {
            return DependencyContext.Resolve<T>();
        }

        public MemoryStream GetBinaryLogStream()
        {
            return _logBinaryOut 
                ?? throw new InvalidOperationException("Binary log output was not initialized");
        }

        public BinaryLogStreamReader.Node ReadBinaryLogStream()
        {
            _flushLogBinaryOutputs?.Invoke();
            
            var stream = GetBinaryLogStream();
            stream.Position = 0;
            
            var reader = new BinaryLogStreamReader(stream);
            reader.ReadToEnd();
            
            stream.Position = stream.Length;

            return reader.RootNode;
        }
        
        public IEnumerable<InspectingLogWriter.LogEntry> GetInspectableLogEntries()
        {
            if (_getInspectableLogEntries != null)
            {
                return _getInspectableLogEntries();
            }

            throw new InvalidOperationException("Inspectable logs were not enabled");
        }

        public DateTime SafeWorldUtcNow()
        {
            return World.CanGet
                ? World.Get().UtcNow()
                : _worldStartTimeUtc;
        }

        public DateTime WorldStartTimeUtc => _worldStartTimeUtc;
        public SimpleDependencyContext DependencyContextBuilder { get; }
        public IActorDependencyContext DependencyContext => DependencyContextBuilder;
        public StateStore Store { get; }
        public SupervisorActor Supervisor { get; }
        public ActorRef<WorldActor> World { get; }
        public List<ActorRef<RadioStationActor>> Stations { get; }
        public List<ActorRef<GroundRadioStationAetherActor>> Aethers { get; }
        public IWorldContext WorldContext => World.Get();
        public ICommsLogger CommsLogger { get; }
        public TestSystemEnvironment Environment { get; } = new();
        
        public enum LogType
        {
            Noop = 0,
            Inspectable = 1,
            BinaryStream = 2
        }
        
        public record GroundStationResult(
            ActorRef<RadioStationActor> Station,
            ActorRef<GroundRadioStationAetherActor> Aethers
        );

        public class TestSystemEnvironment : ISystemEnvironment
        {
            private static readonly string _thisAssemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location)!;
            private Random? _realRandom = new Random();
            private Queue<int>? _presetRandomValues = null;
            
            public string GetInstallRelativePath(string relativePath)
            {
                return Path.Combine(
                    _thisAssemblyFolderPath,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    relativePath);
            }

            public int Random(int min, int max)
            {
                if (_presetRandomValues != null)
                {
                    var nextValue = _presetRandomValues.Dequeue();
                    var result = nextValue % (max - min) + min;
                    return result;
                }

                return _realRandom!.Next(min, max);
            }

            DateTime ISystemEnvironment.UtcNow()
            {
                return this.UtcNow;
            }

            public void EnqueueRandomValues(params int[] values)
            {
                _presetRandomValues = new Queue<int>(values);
                _realRandom = null;
            }
            
            public DateTime UtcNow { get; set; } = DateTime.UtcNow;
        }
    }
}
