using System;
using System.Collections;
using System.Collections.Generic;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.World.Redux;
using Zero.Latency.Servers;

namespace Atc.World
{
    public partial class RuntimeWorld
    {
        private readonly IRuntimeStateStore _store;
        private readonly ILogger _logger;
        private readonly HashSet<IWorldObserver> _observers = new(); 
        private RuntimeState _state;
        private ulong _tickCount;
        private TimeSpan _timestamp;

        public RuntimeWorld(IRuntimeStateStore store, ILogger logger, DateTime startTime)
        {
            _store = store;
            _logger = logger;
            _state = new RuntimeState(
                Version: 1, 
                AllAircraft: new HashSet<RuntimeAircraft>(capacity: 32768),
                NextAircraftId: 1);
        }

        public IObservableQuery<RuntimeAircraft> QueryTraffic(in GeoRect rect)
        {
            return new TrafficObservableQuery(this, rect);
        }

        public void AddAircraft(string typeIcao, string tailNo, GeoPoint location, Bearing heading)
        {
            using var lifecycle = new OperationLifecycle(this);
            
            _store.Dispatch(this, new AircraftAddedEvent(
                Id: _state.NextAircraftId,
                TypeIcao: typeIcao,
                TailNo: tailNo,
                LiveryId: string.Empty,
                Category: AircraftCategories.Jet,
                Operations: OperationTypes.Airline,
                Location: location,
                Altitude: Altitude.FromFlightLevel(180),
                Pitch: Angle.FromDegrees(0),
                Roll: Angle.FromDegrees(0),
                Heading: heading,
                Track: heading,
                GroundSpeed: Speed.FromKnots(350)
            ));
        }
        
        public void ProgressBy(TimeSpan delta)
        {
            _tickCount++;
            _timestamp += delta;

            using var logSpan = _logger.ProgressBy((int) delta.TotalMilliseconds, (int) _timestamp.TotalMilliseconds, _tickCount);
            using var lifecycle = new OperationLifecycle(this);

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

            foreach (var aircraft in _state.AllAircraft)
            {
                aircraft.ProgressBy(delta);
            }
        }

        public void RegisterObserver(IWorldObserver observer)
        {
            _observers.Add(observer);
        }

        public TimeSpan Timestamp => _timestamp;
    }
}
