﻿using System;
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
        private readonly ILogger _logger;
        private readonly Func<RuntimeRadioStationFactory> _radioStationFactory;
        private readonly HashSet<IWorldObserver> _observers = new();
        private readonly Dictionary<int, List<GroundRadioStationAether>> _radioAethersByKhz = new();
        private readonly DateTime _startedAtUtc;
        private RuntimeState _state;
        private ulong _tickCount = 0;
        private TimeSpan _timestamp = TimeSpan.Zero;

        public RuntimeWorld(
            IRuntimeStateStore store, 
            ILogger logger, 
            Func<RuntimeRadioStationFactory> radioStationFactory,
            DateTime startAtUtc)
        {
            _store = store;
            _logger = logger;
            _radioStationFactory = radioStationFactory;
            _startedAtUtc = startAtUtc;
            _state = new RuntimeState(
                Version: 1, 
                AircraftById: new Dictionary<uint, RuntimeAircraft>(),
                NextAircraftId: 0x1000000); //TODO: set high byte to Grain ID

            InitializeMockWorldObjects();
        }

        public IObservableQuery<RuntimeAircraft> QueryTraffic(in GeoRect rect)
        {
            return new TrafficObservableQuery(this, rect);
        }

        public void AddNewAircraft(
            string typeIcao,
            string tailNo,
            string? callsign,
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
                Callsign: callsign ?? tailNo,
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
            Angle? roll = null,
            string? callsign = null)
        {
            using var lifecycle = new OperationLifecycle(this, nameof(AddStoredAircraft));

            ref var data = ref dataRef.Get();
            var tailNo = data.TailNo;

            _store.Dispatch(this, new AircraftAddedEvent(
                Id: _state.NextAircraftId,
                TypeIcao: data.Type.Get().Icao,
                TailNo: tailNo,
                Callsign: callsign ?? tailNo,
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

        public GroundRadioStationAether? TryFindRadioAether(RuntimeRadioStation fromStation)
        {
            if (_radioAethersByKhz.TryGetValue(fromStation.Frequency.Khz, out var aetherList))
            {
                foreach (var aether in aetherList)
                {
                    if (aether.IsReachableBy(fromStation))
                    {
                        _logger.FoundRadioAether(
                            fromStation: fromStation.ToString(),
                            groundStation: aether.GroundStation.ToString(),
                            khz: fromStation.Frequency.Khz, 
                            lat: fromStation.Location.Lat, 
                            lon: fromStation.Location.Lon, 
                            feet: fromStation.Elevation.Feet);
                        return aether;
                    }
                }
            }

            _logger.FailedToFindRadioAether(
                fromStation: fromStation.ToString(),
                khz: fromStation.Frequency.Khz, 
                lat: fromStation.Location.Lat, 
                lon: fromStation.Location.Lon, 
                feet: fromStation.Elevation.Feet);
            return null;
        }

        public void DeferBy(TimeSpan time, Action action)
        {
            throw new NotImplementedException();
        }

        public void DeferUntil(DateTime utc, Action action)
        {
            throw new NotImplementedException();
        }

        public void DeferUntil(Func<bool> predicate, DateTime utc, Action onPredicateTrue, Action onTimeout)
        {
            throw new NotImplementedException();
        }

        public DateTime UtcNow()
        {
            return _startedAtUtc + _timestamp;
        }

        public TimeSpan Timestamp => _timestamp;

        public ILogger Logger => _logger;

        // TODO: remove
        private void InitializeMockWorldObjects()
        {
            var stationLlhzClrDel = _radioStationFactory().CreateGroundStation(
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetMsl(100),
                Frequency.FromKhz(130850),
                "Hertzliya Clearance",
                "Hertzliya Clearance",
                out var llhzClrDelAether);  
            var stationLlhzTwrPrimary = _radioStationFactory().CreateGroundStation(
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetMsl(100),
                Frequency.FromKhz(122200),
                "Hertzliya Tower",
                "Hertzliya",
                out var llhzTwr1Aether);  
            var stationLlhzTwrSecondary = _radioStationFactory().CreateGroundStation(
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetMsl(100),
                Frequency.FromKhz(129400),
                "Hertzliya Tower",
                "Hertzliya",
                out var llhzTwr2Aether);
            var stationPlutoPrimary = _radioStationFactory().CreateGroundStation(
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetMsl(100),
                Frequency.FromKhz(118400),
                "Pluto",
                "Pluto",
                out var pluto1Aether);
            var stationPlutoSecondary = _radioStationFactory().CreateGroundStation(
                new GeoPoint(32.179766d, 34.834404d),
                Altitude.FromFeetMsl(100),
                Frequency.FromKhz(119150),
                "Pluto",
                "Pluto",
                out var pluto2Aether);
                
            _radioAethersByKhz.Add(130850, new List<GroundRadioStationAether>() { llhzClrDelAether });
            _radioAethersByKhz.Add(122200, new List<GroundRadioStationAether>() { llhzTwr1Aether });
            _radioAethersByKhz.Add(129400, new List<GroundRadioStationAether>() { llhzTwr2Aether });
            _radioAethersByKhz.Add(118400, new List<GroundRadioStationAether>() { pluto1Aether });
            _radioAethersByKhz.Add(119150, new List<GroundRadioStationAether>() { pluto2Aether });
        }
    }
}
