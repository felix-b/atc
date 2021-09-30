using System;
using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioStation : IHaveRuntimeState<RuntimeRadioStation.RuntimeState>
    {
        public record RuntimeState(
            ulong StationId,
            Frequency Frequency,
            bool PoweredOn,
            GroundRadioStationAether? Aether,
            ImmutableArray<TransmissionState> IncomingTransmissions,
            TransmissionState? OutgoingTransmission
        );

        public record TransmissionState(
            ulong TransmittingStationId,
            ulong Id,
            DateTime StartedAtUtc,
            RuntimeWave Wave
        );

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
                case PoweredOnEvent:
                    return currentState with {
                        PoweredOn = true,
                        Aether = null,
                        IncomingTransmissions = ImmutableArray<TransmissionState>.Empty,
                        OutgoingTransmission = null
                    };
                case PoweredOffEvent:
                    return currentState with {
                        PoweredOn = false,
                        Aether = null,
                        IncomingTransmissions = ImmutableArray<TransmissionState>.Empty,
                        OutgoingTransmission = null
                    };
                case FrequencySwitchedEvent switched:
                    return currentState with {
                        Frequency = switched.NewFrequency,
                        Aether = switched.Aether,
                        IncomingTransmissions = ImmutableArray<TransmissionState>.Empty
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
                            t => stoppedReceiving.TransmissionId == null || t.Id == stoppedReceiving.TransmissionId
                        )
                    };
            }
            
            return currentState;
        }
        
        public record PoweredOnEvent() : IRuntimeStateEvent;

        public record PoweredOffEvent() : IRuntimeStateEvent;

        public record FrequencySwitchedEvent(
            Frequency NewFrequency,
            GroundRadioStationAether? Aether
        ) : IRuntimeStateEvent;

        public record StartedTransmittingEvent(
            TransmissionState Transmission
        ) : IRuntimeStateEvent;

        public record StoppedTransmittingEvent() : IRuntimeStateEvent;

        public record StartedReceivingTransmissionEvent(
            TransmissionState Transmission
        ) : IRuntimeStateEvent;

        public record StoppedReceivingTransmissionEvent(
            ulong? TransmissionId
        ) : IRuntimeStateEvent;
    }
}
