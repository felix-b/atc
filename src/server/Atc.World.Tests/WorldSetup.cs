using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.Tests.AI;
using Atc.World.Tests.Comms;
using Zero.Doubt.Logging;
using Zero.Loss.Actors;
using Zero.Loss.Actors.Impl;

namespace Atc.World.Tests
{
    public class WorldSetup
    {
        public WorldSetup(Action<SimpleDependencyContext>? configureDependencyContext = null)
        {
            DependencyContextBuilder = new SimpleDependencyContext();
            DependencyContextBuilder.WithSingleton<ISystemEnvironment>(Environment);
            
            configureDependencyContext?.Invoke(DependencyContextBuilder);

            DependencyContextBuilder.WithSingleton<IVerbalizationService>(new TestVerbalizationService());
            DependencyContextBuilder.WithSingleton<LogWriter>(LogWriter.Noop);

            CommsLogger = ZLoggerFactory.CreateLogger<ICommsLogger>(LogWriter.Noop);
            var storeLogger = ZLoggerFactory.CreateLogger<StateStore.ILogger>(LogWriter.Noop);
            
            DependencyContextBuilder.WithSingleton<ICommsLogger>(CommsLogger);
            DependencyContextBuilder.WithSingleton<StateStore.ILogger>(storeLogger);
            DependencyContextBuilder.WithSingleton<WorldActor.ILogger>(ZLoggerFactory.CreateLogger<WorldActor.ILogger>(LogWriter.Noop));
            DependencyContextBuilder.WithSingleton<ISoundSystemLogger>(ZLoggerFactory.CreateLogger<ISoundSystemLogger>(LogWriter.Noop));

            Store = new StateStore(storeLogger);
            DependencyContextBuilder.WithSingleton<IStateStore>(Store);
            
            Supervisor = new SupervisorActor(Store, DependencyContextBuilder);
            DependencyContextBuilder.WithSingleton<ISupervisorActor>(Supervisor);
            
            WorldActor.RegisterType(Supervisor);
            RadioStationActor.RegisterType(Supervisor);
            GroundRadioStationAetherActor.RegisterType(Supervisor);
            DummyCycledTransmittingActor.RegisterType(Supervisor);
            DummyPingPongActor.RegisterType(Supervisor);

            World = Supervisor.CreateActor<WorldActor>(id => new WorldActor.WorldActivationEvent(
                id, 
                new DateTime(2021, 10, 15, 10, 30, 0, DateTimeKind.Utc)));
            DependencyContextBuilder.WithSingleton<IWorldContext>(World.Get());

            Stations = new();
            Aethers = new();
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
            DateTime ISystemEnvironment.UtcNow()
            {
                return this.UtcNow;
            }

            public DateTime UtcNow { get; set; } = DateTime.UtcNow;
        }
    }
}
