using System;
using System.Collections.Immutable;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public partial class GroundRadioStationAetherActor : StatefulActor<GroundRadioStationAetherActor.AetherState>
    {
        public record AetherState(
            ImmutableDictionary<string, ActorRef<RadioStationActor>> StationById,
            ImmutableQueue<TransmissionQueueToken> PendingTransmissionTokens,
            ImmutableHashSet<string> TransmittingStationIds,
            bool IsSilent,
            DateTime SilenceSinceUtc,
            ulong LastTransmissionId,
            ulong LastTransmissionQueueTokenId
        );

        public record ActivationEvent(
            string UniqueId,
            DateTime TimetsampUtc,
            ActorRef<RadioStationActor> GroundStation
        ) : IActivationStateEvent<GroundRadioStationAetherActor>;
        
        public record StationAddedEvent(ActorRef<RadioStationActor> StationActor) : IStateEvent;
        public record StationRemovedEvent(ActorRef<RadioStationActor> StationActor) : IStateEvent;
        public record TransmissionTokenEnqueuedEvent(TransmissionQueueToken Token) : IStateEvent;
        public record TransmissionTokenDequeuedEvent() : IStateEvent;
        public record TransmissionIdTakenEvent(ulong Id) : IStateEvent;
        public record TransmissionStartedEvent(string StationId) : IStateEvent;
        public record TransmissionEndedEvent(string StationId, DateTime Utc) : IStateEvent;

        protected override AetherState Reduce(AetherState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case StationAddedEvent stationAdded:
                    return stateBefore with {
                        StationById = stateBefore.StationById.Add(stationAdded.StationActor.UniqueId, stationAdded.StationActor)
                    };
                case StationRemovedEvent stationRemoved:
                    return stateBefore with {
                        StationById = stateBefore.StationById.Remove(stationRemoved.StationActor.UniqueId)
                    };
                case TransmissionTokenEnqueuedEvent tokenEnqueued:
                    return stateBefore with {
                        LastTransmissionQueueTokenId = tokenEnqueued.Token.Id,
                        PendingTransmissionTokens = stateBefore.PendingTransmissionTokens.Enqueue(tokenEnqueued.Token)
                    };
                case TransmissionTokenDequeuedEvent:
                    return stateBefore with {
                        PendingTransmissionTokens = stateBefore.PendingTransmissionTokens.Dequeue()
                    };
                case TransmissionIdTakenEvent idTaken:
                    return stateBefore with {
                        LastTransmissionId = idTaken.Id
                    };
                case TransmissionStartedEvent transmissionStarted:
                    return stateBefore with {
                        IsSilent = false,
                        TransmittingStationIds = stateBefore.TransmittingStationIds.Add(transmissionStarted.StationId)
                    };
                case TransmissionEndedEvent transmissionEnded:
                    var transmittingStationIds = stateBefore.TransmittingStationIds.Remove(transmissionEnded.StationId);
                    return stateBefore with {
                        TransmittingStationIds = transmittingStationIds,
                        IsSilent = transmittingStationIds.IsEmpty,
                        SilenceSinceUtc = (transmittingStationIds.IsEmpty ? transmissionEnded.Utc : stateBefore.SilenceSinceUtc)
                    };
                default:
                    return stateBefore;
            }
        }

        private static AetherState CreateInitialState(ActivationEvent activation)
        {
            return new AetherState(
                StationById: ImmutableDictionary<string, ActorRef<RadioStationActor>>.Empty,
                PendingTransmissionTokens: ImmutableQueue<TransmissionQueueToken>.Empty,
                TransmittingStationIds: ImmutableHashSet<string>.Empty,
                LastTransmissionId: 0,
                LastTransmissionQueueTokenId: 0,
                IsSilent: true,
                SilenceSinceUtc: activation.TimetsampUtc);
        }
    }
}
