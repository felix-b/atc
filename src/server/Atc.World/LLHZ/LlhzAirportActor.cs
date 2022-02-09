   using System;
   using System.Collections.Generic;
   using System.Collections.Immutable;
   using System.Linq;
   using Atc.Data;
   using Atc.Data.Control;
   using Atc.Data.Primitives;
   using Atc.World.Abstractions;
   using Atc.World.Comms;
   using Atc.World.Traffic.Maneuvers;
   using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzAirportActor : StatefulActor<LlhzAirportActor.LlhzAirportState>, IStartableActor, IDisposable
    {
        public const string TypeString = "llhz-airport";
        
        public record LlhzAirportState(
            TerminalInformation Information,
            ImmutableArray<ActorRef<IStatefulActor>> Children
        );

        public record LlhzAirportActivationEvent(
            string UniqueId,
            int AircraftCount
        ) : IActivationStateEvent<LlhzAirportActor>;

        public record AddChildrenEvent(
            ImmutableArray<ActorRef<IStatefulActor>> Children
        ) : IStateEvent;
        
        private readonly ISupervisorActor _supervisor;
        private readonly IWorldContext _world;
        private readonly IStateStore _store;
        private readonly int _aircraftCount;

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
            _aircraftCount = activation.AircraftCount;
        }

        public IEnumerable<ActorRef<T>> GetChildrenOfType<T>() where T : class, IStatefulActor
        {
            return State.Children
                .Select(actorRef => actorRef.Get())
                .Where(actor => actor is T)
                .Select(actor => _supervisor.GetRefToActorInstance<T>((T) actor));
        }

        public ActorRef<Traffic.AircraftActor> GetAircraftByCallsign(string callsign)
        {
            return GetChildrenOfType<Traffic.AircraftActor>().First(aircraft => aircraft.Get().Callsign == callsign);
        }
        
        void IStartableActor.Start()
        {
            CreateAllActors();
        }

        void IDisposable.Dispose()
        {
            DeleteAllActors();
        }

        public TerminalInformation Information => State.Information;
        
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
                    "Hertzlia Clearance");
                var twrPrimaryRadio = AddGroundStation(
                    Frequency.FromKhz(122200),
                    new GeoPoint(32.179766d, 34.834404d),
                    "Hertzlia");
                var twrSecondaryRadio = AddGroundStation(
                    Frequency.FromKhz(129400),
                    new GeoPoint(32.179766d, 34.834404d),
                    "Hertzlia");
                var plutoPrimaryRadio = AddGroundStation(
                    Frequency.FromKhz(118400),
                    new GeoPoint(32.179766d, 34.834404d),
                    "Pluto");
                var plutoSecondaryRadio = AddGroundStation(
                    Frequency.FromKhz(119150),
                    new GeoPoint(32.179766d, 34.834404d),
                    "Pluto");

                var thisRef = _supervisor.GetRefToActorInstance(this);
                var clrDelController = _supervisor.CreateActor<LlhzControllerActor>(
                    uniqueId => new LlhzControllerActor.ActivationEvent(
                        uniqueId, 
                        ControllerPositionType.ClearanceDelivery, 
                        clrDelRadio, 
                        thisRef));
                var towerController = _supervisor.CreateActor<LlhzControllerActor>(
                    uniqueId => new LlhzControllerActor.ActivationEvent(
                        uniqueId, 
                        ControllerPositionType.Local, 
                        twrPrimaryRadio, 
                        thisRef));

                allChildren.AddRange(new ActorRef<IStatefulActor>[] { 
                    clrDelRadio,
                    twrPrimaryRadio,
                    twrSecondaryRadio,
                    plutoPrimaryRadio,
                    plutoSecondaryRadio,
                    clrDelController,
                    towerController
                });
            }

            void CreateTraffic()
            {
                uint nextAircraftId = 1;
                
                for (
                    int i = 0 ; 
                    i < _aircraftCount && i < LlhzFacts.AircraftList.Count && i < LlhzFacts.ParkingStands.Count ; 
                    i++)
                {
                    var aircraftItem = LlhzFacts.AircraftList[i];
                    var parkingStandItem = LlhzFacts.ParkingStands[i];
                    var aircraftActor = AddAircraft(
                        ref nextAircraftId,
                        aircraftItem.TypeIcao, 
                        aircraftItem.TailNo, 
                        parkingStandItem, 
                        allChildren);
                    StartNewAIFlight(
                        allChildren, 
                        aircraftActor,
                        parkingStandItem,
                        DepartureIntentType.ToStayInPattern, 
                        circuitCount: 4);
                }
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

        private ActorRef<Traffic.AircraftActor> AddAircraft(
            ref uint nextId, 
            string typeIcao, 
            string tailNo, 
            LlhzFacts.ParkingStandItem parkingStand,
            List<ActorRef<IStatefulActor>> output)
        {
            var maneuver = ParkedColdAndDarkManeuver.Create(
                startUtc: _world.UtcNow(),
                finishUtc: DateTime.MaxValue,
                location: parkingStand.Location,
                heading: parkingStand.Heading);

            var aircraft = _world.SpawnNewAircraft(
                typeIcao,
                tailNo,
                callsign: tailNo,
                airlineIcao: null,
                AircraftCategories.Prop,
                OperationTypes.GA,
                maneuver);
            
            // var aircraft = Traffic.AircraftActor.SpawnNewAircraft(
            //     _supervisor,
            //     _world,
            //     id: nextId++,
            //     typeIcao,
            //     tailNo,
            //     callsign: tailNo, //.Substring(tailNo.Length - 3, 3),
            //     airlineIcao: null,
            //     liveryId: null,
            //     modeS: null,
            //     AircraftCategories.Prop,
            //     OperationTypes.GA,
            //     maneuver);
            
            output.AddRange(new ActorRef<IStatefulActor>[] {
                aircraft, 
                aircraft.Get().Com1Radio //TODO: make actors remove their children in Dispose()?
            });

            return aircraft;
        }

        private ActorRef<LlhzPilotActor> StartNewAIFlight(
            List<ActorRef<IStatefulActor>> output,
            ActorRef<Traffic.AircraftActor> aircraft,
            LlhzFacts.ParkingStandItem parkingStand, 
            DepartureIntentType departureType,
            int? circuitCount = null,
            string? destinationIcao = null)
        {
            var pilot = _supervisor.CreateActor<LlhzPilotActor>(
                uniqueId => new LlhzPilotActor.ActivationEvent(
                    uniqueId, 
                    aircraft, 
                    departureType, 
                    circuitCount, 
                    parkingStand.InitialTaxiwayPoint)
            );
            output.Add(pilot);            
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
            var atis = CreateRandomAtis(world.UtcNow());
            return new LlhzAirportState(atis, ImmutableArray<ActorRef<IStatefulActor>>.Empty);
        }

        private static TerminalInformation CreateRandomAtis(DateTime utcNow)
        {
            var wind = CreateRandomWind();
            var runway = wind.Direction.HasValue
                ? (wind.Direction.Value.Max.Degrees <= 40 || wind.Direction.Value.Min.Degrees >= 180 ? "29" : "11")
                : "29";
            var activeRunwayList = ImmutableList<string>.Empty.Add(runway);
            
            return new TerminalInformation(
                Icao: "LLHZ",
                Designator: "B",
                Wind: wind,
                Qnh: Pressure.FromInHgX100(3015 - 40 + utcNow.Millisecond % 40),
                ActiveRunwaysDeparture: activeRunwayList,
                ActiveRunwaysArrival: activeRunwayList);

            Wind CreateRandomWind()
            {
                //TODO
                return new Wind(Bearing.FromTrueDegrees(290), Speed.FromKnots(10), null);
            }
        }
    }
}
