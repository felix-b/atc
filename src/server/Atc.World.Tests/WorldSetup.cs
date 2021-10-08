using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.World.Abstractions;
using Atc.World.Comms;
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
            DependencyContext = new SimpleDependencyContext();
            DependencyContext.WithSingleton<ISystemEnvironment>(Environment);
            DependencyContext.WithSingleton<IVerbalizationService>(new SimpleVerbalizationService());
            
            CommsLogger = ZLoggerFactory.CreateLogger<ICommsLogger>(LogWriter.Noop);
            var storeLogger = ZLoggerFactory.CreateLogger<StateStore.ILogger>(LogWriter.Noop);
            
            DependencyContext.WithSingleton<ICommsLogger>(CommsLogger);
            DependencyContext.WithSingleton<StateStore.ILogger>(storeLogger);
            DependencyContext.WithSingleton<WorldActor.ILogger>(ZLoggerFactory.CreateLogger<WorldActor.ILogger>(LogWriter.Noop));
            DependencyContext.WithSingleton<ISoundSystemLogger>(ZLoggerFactory.CreateLogger<ISoundSystemLogger>(LogWriter.Noop));

            Store = new StateStore(storeLogger);
            DependencyContext.WithSingleton<IStateStore>(Store);
            
            Supervisor = new SupervisorActor(Store, DependencyContext);
            DependencyContext.WithSingleton<ISupervisorActor>(Supervisor);
            
            WorldActor.RegisterType(Supervisor);
            RadioStationActor.RegisterType(Supervisor);
            GroundRadioStationAetherActor.RegisterType(Supervisor);
            DummyControllerActor.RegisterType(Supervisor);

            World = Supervisor.CreateActor<WorldActor>(id => new WorldActor.WorldActivationEvent(
                id, 
                new DateTime(2021, 10, 15, 10, 30, 0, DateTimeKind.Utc)));
            DependencyContext.WithSingleton<IWorldContext>(World.Get());

            configureDependencyContext?.Invoke(DependencyContext);
            
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

        public SimpleDependencyContext DependencyContext { get; }
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
