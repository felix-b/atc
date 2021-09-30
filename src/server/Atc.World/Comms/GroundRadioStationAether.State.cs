using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class GroundRadioStationAether : IHaveRuntimeState<GroundRadioStationAether.RuntimeState>
    {
        private RuntimeState _state;
        
        public record RuntimeState(
            ImmutableDictionary<ulong, RuntimeRadioStation> StationById,
            ImmutableQueue<TransmissionQueueToken> PendingTransmissions,
            DateTime SilenceSinceUtc,
            ulong LastTransmissionId,
            ulong LastTransmissionQueueTokenId
        );

        public record StationAddedEvent(RuntimeRadioStation Station) : IRuntimeStateEvent;
        public record StationRemovedEvent(RuntimeRadioStation Station) : IRuntimeStateEvent;
        public record TransmissionTokenEnqueuedEvent(TransmissionQueueToken Token) : IRuntimeStateEvent;
        public record TransmissionTokenDequeuedEvent() : IRuntimeStateEvent;
        public record TransmissionIdTakenEvent(ulong Id) : IRuntimeStateEvent;
        public record SilenceStartedEvent(DateTime Utc) : IRuntimeStateEvent;

        public RuntimeState Reduce(RuntimeState currentState, IRuntimeStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case StationAddedEvent stationAdded:
                    return currentState with {
                        StationById = currentState.StationById.Add(stationAdded.Station.Id, stationAdded.Station)
                    };
                case StationRemovedEvent stationRemoved:
                    return currentState with {
                        StationById = currentState.StationById.Remove(stationRemoved.Station.Id)
                    };
                case TransmissionTokenEnqueuedEvent tokenEnqueued:
                    return currentState with {
                        StationById = currentState.StationById.Remove(stationRemoved.Station.Id)
                    };
                
            }
        }

        public RuntimeState GetState()
        {
            return _state;
        }

        public void SetState(RuntimeState newState)
        {
            _state = newState;
        }
    }
}
