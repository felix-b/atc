using System;
using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioStation : IHaveRuntimeState<RuntimeRadioStation.RuntimeState>
    {
        public RuntimeState GetState()
        {
            return _state;
        }

        void IHaveRuntimeState<RuntimeState>.SetState(RuntimeState newState)
        {
            _state = newState;
        }

        RuntimeState IHaveRuntimeState<RuntimeState>.Reduce(RuntimeState currentState, IRuntimeStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case FrequencySwitchedEvent switched:
                    return currentState with {
                        Frequency = switched.NewFrequency,
                        IncomingTransmissions = switched.IncomingTransmissions,
                    };
                case StartedTransmittingEvent startedTransmitting:
                    return currentState with {
                        OutgoingTransmission = startedTransmitting.Transmission
                    };
                case StoppedTransmittingEvent:
                    return currentState with {
                        OutgoingTransmission = null
                    };
                case StartedReceivingTransmissionEvent startedReceiving:
                    var alreadyReceivingThisTransmission = 
                        currentState.IncomingTransmissions.Any(t => t.Id == startedReceiving.Transmission.Id);
                    if (!alreadyReceivingThisTransmission)
                    {
                        return currentState with {
                            IncomingTransmissions = currentState.IncomingTransmissions.Add(startedReceiving.Transmission)
                        };
                    }
                    break;
                case StoppedReceivingTransmissionEvent stoppedReceiving:
                    return currentState with {
                        IncomingTransmissions = currentState.IncomingTransmissions.RemoveAll(
                            t => t.Id == stoppedReceiving.TransmissionId
                        )
                    };
            }
            
            return currentState;
        }
        
        public record RuntimeState(
            ulong StationId,
            Frequency Frequency,
            ImmutableArray<TransmissionState> IncomingTransmissions,
            TransmissionState? OutgoingTransmission
        );

        public record TransmissionState(
            ulong TransmittingStationId,
            ulong Id,
            DateTime StartedAtUtc,
            RuntimeWave Wave
        );

        public record FrequencySwitchedEvent(
            Frequency NewFrequency,
            ImmutableArray<TransmissionState> IncomingTransmissions
        ) : IRuntimeStateEvent;

        public record StartedTransmittingEvent(
            TransmissionState Transmission
        ) : IRuntimeStateEvent;

        public record StoppedTransmittingEvent() : IRuntimeStateEvent;

        public record StartedReceivingTransmissionEvent(
            TransmissionState Transmission
        ) : IRuntimeStateEvent;

        public record StoppedReceivingTransmissionEvent(
            ulong TransmissionId
        ) : IRuntimeStateEvent;
    }
}
