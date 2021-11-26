   using System;
   using System.Collections.Immutable;
   using Atc.Data.Primitives;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzAirportActor : StatefulActor<LlhzAirportActor.LlhzAirportState>, IStartableActor, IDisposable
    {
        public const string TypeString = "llhz-airport";
        
        public record LlhzAirportState(
            LlhzAtis Atis,
            ImmutableArray<ActorRef<IStatefulActor>> Children
        );

        public record LlhzAirportActivationEvent(
            string UniqueId
        ) : IActivationStateEvent<LlhzAirportActor>;
        
        private readonly ISupervisorActor _supervisor;
        private readonly IWorldContext _world;

        public LlhzAirportActor(
            ISupervisorActor supervisor, 
            IWorldContext world,
            LlhzAirportActivationEvent activation) 
            : base(TypeString, activation.UniqueId, CreateInitialState(world, activation))
        {
            _supervisor = supervisor;
            _world = world;
        }

        void IStartableActor.Start()
        {
            CreateAllActors();
        }

        void IDisposable.Dispose()
        {
            foreach (var actor in State.Children)
            {
                _supervisor.DeleteActor(actor);
            }
        }

        protected override LlhzAirportState Reduce(LlhzAirportState stateBefore, IStateEvent @event)
        {
            return stateBefore;
        }

        private void CreateAllActors()
        {
            var clrDelRadio = AddGroundStation(
                Frequency.FromKhz(130850),
                new GeoPoint(32.179766d, 34.834404d),
                "Hertzliya Clearance");  
            var twrPrimaryRadio = AddGroundStation(
                Frequency.FromKhz(122200),
                new GeoPoint(32.179766d, 34.834404d),
                "Hertzliya Tower");  
            var twrSecondaryRadio = AddGroundStation(
                Frequency.FromKhz(129400),
                new GeoPoint(32.179766d, 34.834404d),
                "Hertzliya Tower");
            var plutoPrimarRadio = AddGroundStation(
                Frequency.FromKhz(118400),
                new GeoPoint(32.179766d, 34.834404d),
                "Pluto");
            var plutoSecondaryRadio = AddGroundStation(
                Frequency.FromKhz(119150),
                new GeoPoint(32.179766d, 34.834404d),
                "Pluto");
            
            
        }
        
        private ActorRef<RadioStationActor> AddGroundStation(Frequency frequency, GeoPoint location, string callsign)
        {
            var station = _supervisor.CreateActor<RadioStationActor>(id => new RadioStationActor.ActivationEvent(
                id, 
                location, 
                Altitude.FromFeetMsl(10), 
                frequency, 
                Name: callsign, 
                callsign));
            _world.AddGroundStation(station);
            return station;
        }

        public static LlhzAirportState CreateInitialState(IWorldContext world, LlhzAirportActivationEvent activation)
        {
            var atis = LlhzAtis.CreateRandom(world.UtcNow());
            return new LlhzAirportState(atis, ImmutableArray<ActorRef<IStatefulActor>>.Empty);
        }

    }
}
