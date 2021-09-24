using System.Collections.Generic;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioEther
    {
        private readonly IWorldContext _world;
        private readonly RuntimeRadioStationFactory _stationFactory;
        private readonly Dictionary<ulong, RuntimeRadioStation> _stationById = new();
        private ulong _lastTransmissionId;

        public RuntimeRadioEther(IWorldContext world, RuntimeRadioStationFactory stationFactory)
        {
            _world = world;
            _stationFactory = stationFactory;
            _lastTransmissionId = 0;
        }

        public RuntimeRadioStation CreateAircraftStation(RuntimeAircraft aircraft, Frequency frequency)
        {
            var station = _stationFactory.CreateStation(
                this,
                getLocation: () => aircraft.GetState().Location,
                getElevation:() => aircraft.GetState().Altitude,
                frequency);
            _stationById.Add(station.Id, station);
            return station;
        }

        public RuntimeRadioStation CreateGroundStation(GeoPoint location, Altitude elevation, Frequency frequency)
        {
            var station = _stationFactory.CreateStation(
                this,
                getLocation: () => location,
                getElevation:() => elevation,
                frequency);
            _stationById.Add(station.Id, station);
            return station;
        }

        public void RemoveStation(ulong stationId)
        {
            if (_stationById.TryGetValue(stationId, out var station) && station.IsTransmitting())
            {
                OnTransmissionAborted(station, station.GetState().OutgoingTransmission!);
            }
            _stationById.Remove(stationId);
        }
        
        public ulong TakeNextTransmissionId()
        {
            return ++_lastTransmissionId;
        }

        public void UpdateIncomingTransmissions(RuntimeRadioStation station)
        {
            var remainingIncomingTransmissionIds = station.GetState().IncomingTransmissions
                .Select(t => t.Id)
                .ToHashSet();
            
            //TODO: optimize geographically (and in any other way)
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station &&
                    otherStation.Frequency == station.Frequency &&
                    station.IsReachableBy(otherStation) &&
                    otherStation.IsTransmitting())
                {
                    var transmission = otherStation.GetState().OutgoingTransmission!;
                    if (!remainingIncomingTransmissionIds.Remove(transmission.Id))
                    {
                        station.BeginReceivingTransmission(transmission);
                    }
                }
            }

            foreach (var id in remainingIncomingTransmissionIds)
            {
                station.AbortReceivingTransmission(id);
            }
        }

        public void OnTransmissionStarted(RuntimeRadioStation station, RuntimeRadioStation.TransmissionState transmission)
        {
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station &&
                    otherStation.Frequency == station.Frequency &&
                    otherStation.IsReachableBy(station))
                {
                    otherStation.BeginReceivingTransmission(transmission);
                }
            }
        }

        public void OnTransmissionAborted(RuntimeRadioStation station, RuntimeRadioStation.TransmissionState transmission)
        {
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station &&
                    otherStation.Frequency == station.Frequency &&
                    otherStation.IsReachableBy(station))
                {
                    otherStation.AbortReceivingTransmission(transmission.Id);
                }
            }
        }

        public void OnTransmissionCompleted(RuntimeRadioStation station, RuntimeRadioStation.TransmissionState transmission, Intent intent)
        {
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station &&
                    otherStation.Frequency == station.Frequency &&
                    otherStation.IsReachableBy(station))
                {
                    otherStation.CompleteReceivingTransmission(transmission.Id, intent);
                }
            }
        }
    }
}
