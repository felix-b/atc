using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.Math;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Geo.Gps;
using Microsoft.AspNetCore.Http;
using ProtoBuf;
using ProtoBuf.WellKnownTypes;
using Zero.Loss.Actors;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.World
{
    public partial class AircraftActor : StatefulActor<AircraftActor.AircraftState>
    {
        public static readonly string TypeString = "aircraft";
        
        private readonly IWorldContext _world;
        private readonly IStateStore _store;

        public AircraftActor(
            IWorldContext world, 
            IStateStore store,
            ActivationEvent activation)
            : base(TypeString, activation.UniqueId, activation.InitialState)
        {
            _world = world;
            _store = store;
        }

        public void ProgressBy(TimeSpan delta)
        {
            var groundDistance = Distance.FromNauticalMiles(State.GroundSpeed.Knots * delta.TotalHours);
            GeoMath.CalculateGreatCircleDestination(
                State.Location,
                State.Track,
                groundDistance,
                out var newLocation);

            _store.Dispatch(this, new MovedEvent(newLocation));
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

        public int StateVersion => State.Version;
        public uint Id => State.Data.Id;
        public string TailNo => State.Data.TailNo;
        public string TypeIcao => State.Data.Type.Get().Icao;
        public ZRef<AirlineData>? AirlineData => State.Data.Airline;
        public ZRef<AircraftTypeData> TypeData => State.Data.Type;
        public Altitude Altitude => State.Altitude;
        public GeoPoint Location => State.Location;
        public Bearing Heading => State.Heading;
        public Speed GroundSpeed => State.GroundSpeed;
        public Angle Pitch => State.Pitch;
        public Angle Roll => State.Roll;

        
        public static ActorRef<AircraftActor> SpawnNewAircraft(
            ISupervisorActor supervisor,
            uint id,
            string typeIcao,
            string tailNo,
            string? callsign,
            string? airlineIcao,
            string? liveryId,
            uint? modeS,
            AircraftCategories category,
            OperationTypes operations,
            GeoPoint location,
            Altitude altitude,
            Bearing heading,
            Bearing? track = null,
            Speed? groundSpeed = null,
            Angle? pitch = null,
            Angle? roll = null)
        {
            var context = BufferContext.Current;
            ref var worldData = ref context.GetWorldData();

            context.TryGetString(liveryId ?? string.Empty, out var liveryIdRef);
            var effectiveCallsign = callsign ?? tailNo; 
            
            var data = new AircraftData {
                Id = id,
                Type = worldData.TypeByIcao[typeIcao],
                ModeS = modeS, 
                Category = category,
                Operations = operations,
                Airline = airlineIcao != null
                    ? worldData.AirlineByIcao[airlineIcao] 
                    : null,
                LiveryId = liveryIdRef
            };

            var com1Radio = RadioStationActor.Create(
                supervisor,
                location,
                altitude,
                Frequency.FromKhz(0),
                name: $"{tailNo}/COM1",
                effectiveCallsign);

            var initialState = new AircraftState(
                Version: 1,
                Data: data,
                Location: location,
                Altitude: altitude,
                Pitch: pitch ?? Angle.FromDegrees(0),
                Roll: roll ?? Angle.FromDegrees(0),
                Heading: heading,
                Track: track ?? heading,
                GroundSpeed: groundSpeed ?? Speed.FromKnots(0),
                Com1Radio: com1Radio
            );

            var actor = supervisor.CreateActor(uniqueId => new ActivationEvent(
                uniqueId,
                initialState
            ));

            return actor;
        }
        
        public static ActorRef<AircraftActor> LoadAircraftFromCache(
            ISupervisorActor supervisor,
            ZRef<AircraftData> dataRef,
            GeoPoint location,
            Altitude altitude,
            Bearing heading,
            Bearing? track = null,
            Speed? groundSpeed = null,
            Angle? pitch = null,
            Angle? roll = null,
            string? callsign = null)
        {
            ref var data = ref dataRef.Get();
            var tailNo = data.TailNo;
            var effectiveCallsign = callsign ?? tailNo.Value; 

            var com1Radio = RadioStationActor.Create(
                supervisor,
                location,
                altitude,
                Frequency.FromKhz(0),
                name: $"{tailNo.Value}/COM1",
                effectiveCallsign);

            var initialState = new AircraftState(
                Version: 1,
                Data: data,
                Location: location,
                Altitude: altitude,
                Pitch: pitch ?? Angle.FromDegrees(0),
                Roll: roll ?? Angle.FromDegrees(0),
                Heading: heading,
                Track: track ?? heading,
                GroundSpeed: groundSpeed ?? Speed.FromKnots(0),
                Com1Radio: com1Radio
            );
            
            var actor = supervisor.CreateActor(uniqueId => new ActivationEvent(
                uniqueId,
                initialState
            ));

            return actor;
        }
    }
}
