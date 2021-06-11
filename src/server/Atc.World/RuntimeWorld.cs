﻿using System;
using System.Collections;
using System.Collections.Generic;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.World.Redux;
using Zero.Latency.Servers;
using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

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
            using var lifecycle = new OperationLifecycle(this);

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
            using var lifecycle = new OperationLifecycle(this);

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

            foreach (var aircraft in _state.AircraftById.Values)
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
