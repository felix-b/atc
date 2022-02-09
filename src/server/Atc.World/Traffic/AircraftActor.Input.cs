using System;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.Math;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Microsoft.AspNetCore.Http;
using Zero.Loss.Actors;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.World.Traffic
{
    public partial class AircraftActor : StatefulActor<AircraftActor.AircraftState>
    {
        public static readonly string TypeString = "aircraft";
        
        private readonly IWorldContext _world;
        private readonly IStateStore _store;

        public AircraftActor(
            IWorldContext world, 
            IStateStore store,
            AircraftActor.ActivationEvent activation)
            : base(TypeString, activation.UniqueId, activation.InitialState)
        {
            _world = world;
            _store = store;
        }

        public void ProgressBy(TimeSpan delta)
        {
            // var groundDistance = Distance.FromNauticalMiles(State.GroundSpeed.Knots * delta.TotalHours);
            // GeoMath.CalculateGreatCircleDestination(
            //     State.Location,
            //     State.Track,
            //     groundDistance,
            //     out var newLocation);
            //
            // _store.Dispatch(this, new AircraftActor.MovedEvent(newLocation));
        }

        public void PowerAvionicsOn(Frequency com1Frequency)
        {
            var com1Radio = State.Com1Radio.Get(); 
            com1Radio.TuneTo(com1Frequency);
            com1Radio.PowerOn();
        }

        public void PowerAvionicsOff()
        {
            var com1Radio = State.Com1Radio.Get(); 
            com1Radio.PowerOff();
        }

        public void TuneCom1To(Frequency frequency)
        {
            var com1Radio = State.Com1Radio.Get(); 
            com1Radio.TuneTo(frequency);
        }

        public AircraftSituation GetCurrentSituation(bool forceRefresh = false)
        {
            var utcNow = _world.UtcNow();
            var isLastKnownSituationStale = (
                utcNow.Subtract(State.LastKnownSituation.Utc) >= AviationFacts.ControllerRadarRefreshRate ||
                State.LastFetchedSituationVersion != State.LastKnownSituationVersion
            );

            if (isLastKnownSituationStale || forceRefresh)
            {
                var newSituation = State.CurrentManeuver.GetAircraftSituation(utcNow);
                _store.Dispatch(this, new UpdateLastKnownSituationEvent(newSituation));
            }

            return State.LastKnownSituation;
        }

        public void ReplaceManeuver(Maneuver newManeuver)
        {
            _store.Dispatch(this, new ReplaceManeuverEvent(newManeuver));
        }

        public int StateVersion => State.Version;
        public uint Id => State.Data.Id;
        public string TailNo => State.Data.TailNo;
        public string TypeIcao => State.Data.Type.Get().Icao;
        public ZRef<AirlineData>? AirlineData => State.Data.Airline;
        public ZRef<AircraftTypeData> TypeData => State.Data.Type;
        public Altitude Altitude => State.LastKnownSituation.Altitude;
        public GeoPoint Location => State.LastKnownSituation.Location;
        public Bearing Heading => State.LastKnownSituation.Heading;
        public Speed GroundSpeed => State.LastKnownSituation.GroundSpeed;
        public Angle Pitch => State.LastKnownSituation.Pitch;
        public Angle Roll => State.LastKnownSituation.Roll;
        public ActorRef<RadioStationActor> Com1Radio => State.Com1Radio;
        public string Callsign => State.Callsign;
        
        public static ActorRef<AircraftActor> SpawnNewAircraft(
            ISupervisorActor supervisor,
            IWorldContext world,
            uint id,
            string typeIcao,
            string tailNo,
            string? callsign,
            string? airlineIcao,
            string? liveryId,
            uint? modeS,
            AircraftCategories category,
            OperationTypes operations,
            Maneuver maneuver)
        {
            var context = BufferContext.Current;
            ref var worldData = ref context.GetWorldData();

            context.TryGetString(liveryId ?? string.Empty, out var liveryIdRef);
            var effectiveCallsign = callsign ?? tailNo; 
            
            var data = new AircraftData {
                Id = id,
                Type = worldData.TypeByIcao[typeIcao],
                TailNo = BufferContext.Current.TryGetString(tailNo, out var tailNoStringRef)
                    ? tailNoStringRef
                    : new ZStringRef(ZRef<StringRecord>.Null),
                ModeS = modeS, 
                Category = category,
                Operations = operations,
                Airline = airlineIcao != null
                    ? worldData.AirlineByIcao[airlineIcao] 
                    : null,
                LiveryId = liveryIdRef
            };

            var situation = maneuver.GetAircraftSituation(world.UtcNow());
            var com1Radio = RadioStationActor.Create(
                supervisor,
                situation.Location,
                situation.Altitude,
                Frequency.FromKhz(0),
                name: $"{tailNo}/COM1",
                effectiveCallsign);

            var initialState = new AircraftActor.AircraftState(
                Version: 1,
                Data: data,
                LastKnownSituation: situation,
                LastKnownSituationVersion: 1,
                LastFetchedSituationVersion: 0, 
                Com1Radio: com1Radio,
                Callsign: effectiveCallsign,
                CurrentManeuver: maneuver
            );

            var actor = supervisor.CreateActor(uniqueId => new AircraftActor.ActivationEvent(
                uniqueId,
                initialState
            ));

            return actor;
        }
        
        public static ActorRef<AircraftActor> LoadAircraftFromCache(
            ISupervisorActor supervisor,
            IWorldContext world,
            ZRef<AircraftData> dataRef,
            Maneuver currentManeuver,
            string? callsign = null)
        {
            ref var data = ref dataRef.Get();
            var tailNo = data.TailNo;
            var effectiveCallsign = callsign ?? tailNo.Value; 

            var situation = currentManeuver.GetAircraftSituation(world.UtcNow());
            var com1Radio = RadioStationActor.Create(
                supervisor,
                situation.Location,
                situation.Altitude,
                Frequency.FromKhz(0),
                name: $"{tailNo.Value}/COM1",
                effectiveCallsign);

            var initialState = new AircraftActor.AircraftState(
                Version: 1,
                Data: data,
                LastKnownSituation: situation,
                LastKnownSituationVersion: 1,
                LastFetchedSituationVersion: 0, 
                Com1Radio: com1Radio,
                Callsign: effectiveCallsign,
                CurrentManeuver: currentManeuver
            );
            
            var actor = supervisor.CreateActor(uniqueId => new AircraftActor.ActivationEvent(
                uniqueId,
                initialState
            ));

            return actor;
        }
        
        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<AircraftActor,  AircraftActor.ActivationEvent>(
                TypeString,
                (activation, dependencies) => new AircraftActor(
                    dependencies.Resolve<IWorldContext>(), 
                    dependencies.Resolve<IStateStore>(),
                    activation
                )
            );
        }
    }
}
