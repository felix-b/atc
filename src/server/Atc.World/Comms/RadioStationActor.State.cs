using System;
using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Primitives;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public partial class RadioStationActor
    {
        public record StationState(
            Frequency Frequency,
            Func<GeoPoint> LocationGetter,
            Func<Altitude> ElevationGetter,
            bool PoweredOn,
            ActorRef<GroundRadioStationAetherActor>? Aether,
            ImmutableArray<TransmissionState> IncomingTransmissions,
            TransmissionState? OutgoingTransmission,
            RadioTransmissionWave? PendingTransmissionWave
        );

        public record TransmissionState(
            // Transmission Id enables deterministically repeatable pseudo-randomization of speech - by taking modulus of Id
            // In this way we avoid storing/replicating the entire synthesized speech wave
            // Not sure we want though - if using paid cloud speech services, it's beneficial to synthesize once and share the wave
            ulong Id, 
            string TransmittingStationId,
            DateTime StartedAtSystemUtc,
            RadioTransmissionWave Wave
        );

        public record ActivationEvent(
            string UniqueId,
            GeoPoint Location, 
            Altitude Elevation, 
            Frequency Frequency, 
            string Name,
            string Callsign
        ) : IActivationStateEvent<RadioStationActor>;

        public record SetOwnerAircraftEvent(
            ActorRef<AircraftActor> Aircraft
        ) : IStateEvent;
        
        public record PoweredOnEvent() : IStateEvent;

        public record PoweredOffEvent() : IStateEvent;

        public record FrequencySwitchedEvent(
            Frequency NewFrequency,
            ActorRef<GroundRadioStationAetherActor>? Aether
        ) : IStateEvent;

        public record AIEnqueuedTransmissionEvent(
            RadioTransmissionWave Wave
        ) : IStateEvent;

        public record StartedTransmittingEvent(
            TransmissionState Transmission
        ) : IStateEvent;

        public record StoppedTransmittingEvent() : IStateEvent;

        public record StartedReceivingTransmissionEvent(
            TransmissionState Transmission
        ) : IStateEvent;

        public record StoppedReceivingTransmissionEvent(
            ulong? TransmissionId
        ) : IStateEvent;

        protected override StationState Reduce(StationState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case SetOwnerAircraftEvent setOwner:
                    return stateBefore with {
                        LocationGetter = () => setOwner.Aircraft.Get().Location,
                        ElevationGetter = () => setOwner.Aircraft.Get().Altitude
                    };
                case PoweredOnEvent:
                    return stateBefore with {
                        PoweredOn = true,
                        Aether = null,
                        IncomingTransmissions = ImmutableArray<TransmissionState>.Empty,
                        OutgoingTransmission = null
                    };
                case PoweredOffEvent:
                    return stateBefore with {
                        PoweredOn = false,
                        Aether = null,
                        IncomingTransmissions = ImmutableArray<TransmissionState>.Empty,
                        OutgoingTransmission = null
                    };
                case FrequencySwitchedEvent switched:
                    return stateBefore with {
                        Frequency = switched.NewFrequency,
                        Aether = switched.Aether,
                        IncomingTransmissions = ImmutableArray<TransmissionState>.Empty
                    }; 
                case AIEnqueuedTransmissionEvent enqueued:
                    return stateBefore with {
                        PendingTransmissionWave = enqueued.Wave
                    };
                case StartedTransmittingEvent startedTransmitting:
                    return stateBefore with {
                        OutgoingTransmission = startedTransmitting.Transmission,
                        PendingTransmissionWave = null
                    };
                case StoppedTransmittingEvent:
                    return stateBefore with {
                        OutgoingTransmission = null,
                    };
                case StartedReceivingTransmissionEvent startedReceiving:
                    var alreadyReceivingThisTransmission = 
                        stateBefore.IncomingTransmissions.Any(t => t.Id == startedReceiving.Transmission.Id);
                    if (!alreadyReceivingThisTransmission)
                    {
                        return stateBefore with {
                            IncomingTransmissions = stateBefore.IncomingTransmissions.Add(startedReceiving.Transmission)
                        };
                    }
                    break;
                case StoppedReceivingTransmissionEvent stoppedReceiving:
                    return stateBefore with {
                        IncomingTransmissions = stateBefore.IncomingTransmissions.RemoveAll(
                            t => stoppedReceiving.TransmissionId == null || t.Id == stoppedReceiving.TransmissionId
                        )
                    };
            }
            
            return stateBefore;
        }

        private static StationState CreateInitialState(ActivationEvent activation)
        {
            return new StationState(
                activation.Frequency,
                LocationGetter: () => activation.Location,
                ElevationGetter: () => activation.Elevation,
                PoweredOn: false,
                Aether: null,
                IncomingTransmissions: ImmutableArray<TransmissionState>.Empty,
                OutgoingTransmission: null,
                PendingTransmissionWave: null);
        }
    }
}
