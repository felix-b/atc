using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Atc.World.LLHZ;
using Atc.World.Testability.Comms;
using Zero.Doubt.Logging;
using Zero.Loss.Actors;
using Zero.Loss.Actors.Impl;

namespace Atc.World.Testability
{
    public class WorldSetup
    {
        private readonly DateTime _worldStartTimeUtc = new DateTime(2021, 10, 15, 10, 30, 0, DateTimeKind.Utc);
        private Func<IEnumerable<InspectingLogWriter.LogEntry>>? _getInspectableLogEntries = null;

        public WorldSetup(
            Action<SimpleDependencyContext>? configureDependencyContext = null,
            bool enableInspectableLogs = false)
        {
            var effectiveLogWriter = enableInspectableLogs
                ? InspectingLogWriter.Create(LogLevel.Debug, SafeWorldUtcNow, out _getInspectableLogEntries)
                : LogWriter.Noop;

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
            // DummyCycledTransmittingActor.RegisterType(Supervisor);
            // DummyPingPongActor.RegisterType(Supervisor);
            LlhzAirportActor.RegisterType(Supervisor);
            LlhzDeliveryControllerActor.RegisterType(Supervisor);
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

        public void RunWorldIterations(TimeSpan iterationInterval, int iterationCount)
        {
            for (int iteration = 0 ; iteration < iterationCount ; iteration++)
            {
                World.Get().ProgressBy(iterationInterval);
            }
        }

        public IEnumerable<InspectingLogWriter.LogEntry> GetLogEntries()
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

        public record GroundStationResult(
            ActorRef<RadioStationActor> Station,
            ActorRef<GroundRadioStationAetherActor> Aethers
        );

        public class TestSystemEnvironment : ISystemEnvironment
        {
            private static readonly string _thisAssemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location)!;

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

            DateTime ISystemEnvironment.UtcNow()
            {
                return this.UtcNow;
            }

            public DateTime UtcNow { get; set; } = DateTime.UtcNow;
        }
    }
}
