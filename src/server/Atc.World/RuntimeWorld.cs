using System;
using System.Collections;
using System.Collections.Generic;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.World.Comms;
using Atc.World.Redux;
using Zero.Latency.Servers;
using Zero.Serialization.Buffers;

namespace Atc.World
{
    public partial class RuntimeWorld : IWorldContext
    {
        private readonly IRuntimeStateStore _store;
        private readonly Func<RuntimeRadioEther> _etherFactory;
        private readonly ILogger _logger;
        private readonly HashSet<IWorldObserver> _observers = new();
        private readonly DateTime _startedAtUtc;
        private RuntimeRadioEther? _ether = null;
        private RuntimeState _state;
        private ulong _tickCount = 0;
        private TimeSpan _timestamp = TimeSpan.Zero;

        public RuntimeWorld(
            IRuntimeStateStore store, 
            Func<RuntimeRadioEther> etherFactory, 
            ILogger logger, 
            DateTime startAtUtc)
        {
            _store = store;
            _etherFactory = etherFactory;
            _logger = logger;
            _startedAtUtc = startAtUtc;
            _state = new RuntimeState(
                Version: 1, 
                AircraftById: new Dictionary<uint, RuntimeAircraft>(),
                NextAircraftId: 0x1000000); //TODO: set high byte to Grain ID
        }

        public IObservableQuery<RuntimeAircraft> QueryTraffic(in GeoRect rect)
        {
            return new TrafficObservableQuery(this, rect);
        }

        public void AddNewAircraft(
            string typeIcao,
            string tailNo,
            string? airlineIcao,
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
            using var lifecycle = new OperationLifecycle(this, nameof(AddNewAircraft));

            _store.Dispatch(this, new AircraftAddedEvent(
                Id: _state.NextAircraftId,
                TypeIcao: typeIcao,
                TailNo: tailNo,
                ModeS: null,
                LiveryId: string.Empty,
                Category: category,
                Operations: operations,
                AirlineIcao: airlineIcao,
                Location: location,
                Altitude: altitude,
                Pitch: pitch ?? Angle.FromDegrees(0),
                Roll: roll ?? Angle.FromDegrees(0),
                Heading: heading,
                Track: track ?? heading,
                GroundSpeed: groundSpeed ?? Speed.FromKnots(0)
            ));
        }
        
        public void AddStoredAircraft(
            ZRef<AircraftData> dataRef, 
            GeoPoint location,
            Altitude altitude,
            Bearing heading,
            Bearing? track = null,
            Speed? groundSpeed = null,
            Angle? pitch = null,
            Angle? roll = null)
        {
            using var lifecycle = new OperationLifecycle(this, nameof(AddStoredAircraft));

            ref var data = ref dataRef.Get();

            _store.Dispatch(this, new AircraftAddedEvent(
                Id: _state.NextAircraftId,
                TypeIcao: data.Type.Get().Icao,
                TailNo: data.TailNo,
                ModeS: data.ModeS,
                LiveryId: string.Empty,
                Category: data.Category,
                Operations: data.Operations,
                AirlineIcao: data.Airline?.Get().Icao,
                Location: location,
                Altitude: altitude,
                Pitch: pitch ?? Angle.FromDegrees(0),
                Roll: roll ?? Angle.FromDegrees(0),
                Heading: heading,
                Track: track ?? heading,
                GroundSpeed: groundSpeed ?? Speed.FromKnots(0)
            ));
        }
        
        public void ProgressBy(TimeSpan delta)
        {
            _tickCount++;
            _timestamp += delta;

            using var logSpan = _logger.ProgressBy((int) delta.TotalMilliseconds, (int) _timestamp.TotalMilliseconds, _tickCount);
            using var lifecycle = new OperationLifecycle(this, nameof(ProgressBy));

            // 1. create new empty ChangeSet and make it current in the context
            // 2. call ProgressBy on all parties involved   
            //    ?> abstract parties subscribing to ProgressTo?
            //    ?> pipeline?
            // 3. every party updates its state Redux-style:
            //    - an event is dispatched to a store
            //    - a reducer processes the event and produces new state
            //    - the event is published to the current ChangeSet
            //    - changes to state are published to the current ChangeSet 
            //    - existing subscriptions are run against the ChangeSet and the observers are invoked as necessary 
            // 4.

            foreach (var aircraft in _state.AircraftById.Values)
            {
                using (_logger.ProgressByAircraft(aircraft.Id))
                {
                    aircraft.ProgressBy(delta);
                }
            }
        }

        public void RegisterObserver(IWorldObserver observer)
        {
            _logger.RegisteringObserver(observer.Name);
            _observers.Add(observer);
        }

        public ulong CreateUniqueId(ulong lastId)
        {
            //TODO: apply shard ranges one day
            return lastId + 1;
        }

        public DateTime UtcNow()
        {
            return _startedAtUtc + _timestamp;
        }

        public TimeSpan Timestamp => _timestamp;

        public ILogger Logger => _logger;

        private RuntimeRadioEther GetEther()
        {
            if (_ether == null)
            {
                _ether = _etherFactory();
            }
            return _ether;
        }
    }
}
