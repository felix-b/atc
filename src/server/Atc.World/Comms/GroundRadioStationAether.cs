using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class GroundRadioStationAether
    {
        private readonly IWorldContext _world;
        private readonly IRuntimeStateStore _store;
        private readonly ICommsLogger _logger;
        private readonly RuntimeRadioStation _groundStation;

        public GroundRadioStationAether(
            IWorldContext world, 
            IRuntimeStateStore store, 
            ICommsLogger logger, 
            RuntimeRadioStation groundStation)
        {
            _world = world;
            _store = store;
            _logger = logger;
            _groundStation = groundStation;
            _stationById.Add(groundStation.Id, groundStation);
            _silenceSinceUtc = _world.UtcNow();
        }

        public void AddStation(RuntimeRadioStation station)
        {
            _stationById.Add(station.Id, station);
            _logger.AetherStationAdded(aether: _groundStation.ToString(), station: station.ToString());

            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station && otherStation.IsTransmitting(out var transmission))
                {
                    station.BeginReceivingTransmission(transmission!);
                }
            }
        }

        public void RemoveStation(RuntimeRadioStation station)
        {
            if (station.IsTransmitting(out var transmission) && transmission != null)
            {
                station.AbortTransmission();
            }
        
            station.AbortReceivingAllTransmissions();
            _stationById.Remove(station.Id);

            _logger.AetherStationRemoved(aether: _groundStation.ToString(), station: station.ToString());
        }

        public ulong TakeNextTransmissionId()
        {
            return ++_lastTransmissionId;
        }

        public ulong RegisterForTransmission<TState>(IHaveRuntimeState<TState> speaker, int cookie)
            where TState : class
        {
            var tokenId = ++_lastTransmissionQueueTokenId;
            var token = new TransmissionQueueToken(tokenId, speaker, () => {
                _store.Dispatch(speaker, new QueuedTransmissionStartEvent(cookie));
            });

            _pendingTransmissions.Enqueue(token);
            _logger.RegisteredPendingTransmission(tokenId, speaker.ToString()!, cookie);
            
            return tokenId;
        }

        public bool IsReachableBy(RuntimeRadioStation station)
        {
            return station.IsReachableBy(_groundStation);
        }
        
        public void OnTransmissionStarted(RuntimeRadioStation station, RuntimeRadioStation.TransmissionState transmission)
        {
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.BeginReceivingTransmission(transmission);
                }
            }
        }

        public void OnTransmissionAborted(RuntimeRadioStation station, RuntimeRadioStation.TransmissionState transmission)
        {
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.AbortReceivingTransmission(transmission.Id);
                }
            }

            _silenceSinceUtc = _world.UtcNow();
        }

        public void OnTransmissionCompleted(RuntimeRadioStation station, RuntimeRadioStation.TransmissionState transmission, Intent intent)
        {
            foreach (var otherStation in _stationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.CompleteReceivingTransmission(transmission.Id, intent);
                }
            }

            _silenceSinceUtc = _world.UtcNow();
        }

        public RuntimeRadioStation GroundStation => _groundStation;
        
        public record QueuedTransmissionStartEvent(int Cookie) : IRuntimeStateEvent;
        
        public record TransmissionQueueToken(ulong Id, object Target, Action Callback);
    }
}
