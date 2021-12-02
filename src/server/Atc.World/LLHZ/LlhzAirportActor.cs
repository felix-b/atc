   using System;
   using System.Collections.Generic;
   using System.Collections.Immutable;
   using System.Linq;
   using Atc.Data;
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

        public record AddChildrenEvent(
            ImmutableArray<ActorRef<IStatefulActor>> Children
        ) : IStateEvent;
        
        private readonly ISupervisorActor _supervisor;
        private readonly IWorldContext _world;
        private readonly IStateStore _store;

        public LlhzAirportActor(
            ISupervisorActor supervisor, 
            IStateStore store,
            IWorldContext world,
            LlhzAirportActivationEvent activation) 
            : base(TypeString, activation.UniqueId, CreateInitialState(world, activation))
        {
            _store = store;
            _supervisor = supervisor;
            _world = world;
        }

        public IEnumerable<ActorRef<T>> GetChildrenOfType<T>() where T : class, IStatefulActor
        {
            return State.Children
                .Select(actorRef => actorRef.Get())
                .Where(actor => actor is T)
                .Select(actor => _supervisor.GetRefToActorInstance<T>((T) actor));
        }

        public ActorRef<AircraftActor> GetAircraftByCallsign(string callsign)
        {
            return GetChildrenOfType<AircraftActor>().First(aircraft => aircraft.Get().Callsign == callsign);
        }
        
        void IStartableActor.Start()
        {
            CreateAllActors();
        }

        void IDisposable.Dispose()
        {
            DeleteAllActors();
        }

        protected override LlhzAirportState Reduce(LlhzAirportState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case AddChildrenEvent addChildren:
                    return stateBefore with {
                        Children = stateBefore.Children.AddRange(addChildren.Children)
                    };
                default:
                    return stateBefore;
            }
        }

        private void CreateAllActors()
        {
            List<ActorRef<IStatefulActor>> allChildren = new();

            CreateAtc();
            CreateTraffic();
            
            _store.Dispatch(this, new AddChildrenEvent(allChildren.ToImmutableArray()));

            void CreateAtc()
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
                var plutoPrimaryRadio = AddGroundStation(
                    Frequency.FromKhz(118400),
                    new GeoPoint(32.179766d, 34.834404d),
                    "Pluto");
                var plutoSecondaryRadio = AddGroundStation(
                    Frequency.FromKhz(119150),
                    new GeoPoint(32.179766d, 34.834404d),
                    "Pluto");

                var thisRef = _supervisor.GetRefToActorInstance(this);
                var clrDelController = _supervisor.CreateActor<LlhzDeliveryControllerActor>(
                    uniqueId => new LlhzDeliveryControllerActor.ActivationEvent(uniqueId, clrDelRadio, thisRef)
                );

                allChildren.AddRange(new ActorRef<IStatefulActor>[] { 
                    clrDelRadio,
                    twrPrimaryRadio,
                    twrSecondaryRadio,
                    plutoPrimaryRadio,
                    plutoSecondaryRadio,
                    clrDelController
                });
            }

            void CreateTraffic()
            {
                uint nextAircraftId = 1;
                var aircraft1 = AddAircraft(ref nextAircraftId, "C172", "4XCGK", allChildren);
                StartNewAIFlight(aircraft1, DepartureIntentType.ToStayInPattern, allChildren);
            }
        }

        private void DeleteAllActors()
        {
            foreach (var actor in State.Children.Reverse())
            {
                _supervisor.DeleteActor(actor);
            }
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

        private ActorRef<AircraftActor> AddAircraft(
            ref uint nextId, 
            string typeIcao, 
            string tailNo, 
            List<ActorRef<IStatefulActor>> destination)
        {
            var aircraft = AircraftActor.SpawnNewAircraft(
                _supervisor,
                id: nextId++,
                typeIcao,
                tailNo,
                callsign: tailNo.Substring(tailNo.Length - 3, 3),
                airlineIcao: null,
                liveryId: null,
                modeS: null,
                AircraftCategories.Prop,
                OperationTypes.GA,
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetAgl(0),
                heading: Bearing.FromTrueDegrees(15));
            
            destination.AddRange(new ActorRef<IStatefulActor>[] {
                aircraft, 
                aircraft.Get().Com1Radio //TODO: make actors remove their children in Dispose()?
            });

            return aircraft;
        }

        private ActorRef<LlhzPilotActor> StartNewAIFlight(
            ActorRef<AircraftActor> aircraft, 
            DepartureIntentType departureType,
            List<ActorRef<IStatefulActor>> destination)
        {
            var pilot = _supervisor.CreateActor<LlhzPilotActor>(
                uniqueId => new LlhzPilotActor.ActivationEvent(uniqueId, aircraft, departureType)
            );
            destination.Add(pilot);            
            return pilot;
        }
        
        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<LlhzAirportActor, LlhzAirportActivationEvent>(
                TypeString,
                (activation, dependencies) => new LlhzAirportActor(
                    dependencies.Resolve<ISupervisorActor>(),
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(), 
                    activation
                )
            );
        }

        public static LlhzAirportState CreateInitialState(IWorldContext world, LlhzAirportActivationEvent activation)
        {
            var atis = LlhzAtis.CreateRandom(world.UtcNow());
            return new LlhzAirportState(atis, ImmutableArray<ActorRef<IStatefulActor>>.Empty);
        }
    }
}
