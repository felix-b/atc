using System;
using System.Collections.Immutable;
using Atc.World.Abstractions;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public partial class GroundRadioStationAetherActor
    {
        public static readonly string TypeString = "aether";
        
        private readonly IWorldContext _world;
        private readonly IStateStore _store;
        private readonly ICommsLogger _logger;
        private readonly ActorRef<RadioStationActor> _groundStation;
        
        [NotEventSourced]
        private IDeferHandle? _silenceTrigger = null;

        [NotEventSourced]
        private RadioStationActor.TransmissionDurationUpdateCallback? _durationUpdateCallback = null;

        public GroundRadioStationAetherActor(
            IWorldContext world, 
            IStateStore store, 
            ICommsLogger logger, 
            ActivationEvent activation)
            : base(TypeString, activation.UniqueId, CreateInitialState(activation))
        {
            _world = world;
            _store = store;
            _logger = logger;
            _groundStation = activation.GroundStation;
        }

        public void AddStation(ActorRef<RadioStationActor> station)
        {
            _store.Dispatch(this, new StationAddedEvent(station));
            _logger.AetherStationAdded(aether: _groundStation.ToString(), station: station.ToString());

            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station && otherStation.Get().IsTransmitting(out var transmission))
                {
                    station.Get().BeginReceivingTransmission(transmission!);
                }
            }
        }

        public void RemoveStation(ActorRef<RadioStationActor> stationRef)
        {
            var station = stationRef.Get();
            
            if (station.IsTransmitting(out var transmission) && transmission != null)
            {
                station.AbortTransmission();
            }
        
            station.AbortReceivingAllTransmissions();
            _store.Dispatch(this, new StationRemovedEvent(stationRef));

            _logger.AetherStationRemoved(aether: _groundStation.ToString(), station: station.ToString());
        }

        public ulong TakeNextTransmissionId()
        {
            var result = State.LastTransmissionId + 1;
            _store.Dispatch(this, new TransmissionIdTakenEvent(result));
            return result;
        }

        public void AIEnqueueForTransmission(
            ActorRef<IRadioOperatingActor> speaker, 
            string? toCallsign,
            int cookie, 
            out ulong tokenId)
        {
            tokenId = State.LastTransmissionQueueTokenId + 1;
            var token = new TransmissionQueueToken(tokenId, speaker, cookie);

            _store.Dispatch(this, new TransmissionTokenEnqueuedEvent(token));
            _logger.RegisteredPendingTransmission(tokenId, speaker.UniqueId, cookie);

            if (IsSilentForNextTransmission(speaker.Get().Party.Callsign, toCallsign)) 
            {
                OnSilence();
                //_world.Defer($"aether-next-conversation|{UniqueId}", OnSilence);
            }
        }

        public bool IsReachableBy(ActorRef<RadioStationActor> station)
        {
            return station.Get().IsReachableBy(_groundStation.Get());
        }

        public bool IsSilentForNextTransmission()
        {
            return IsSilentForNextTransmission(fromCallsign: string.Empty, toCallsign: null);
        }

        public bool IsSilentForNextTransmission(string fromCallsign, string? toCallsign)
        {
            if (!State.IsSilent)
            {
                return false;
            }

            if (fromCallsign == State.LastTransmissionOriginatorCallsign)
            {
                return true;
            }

            if (fromCallsign == State.LastTransmissionRecipientCallsign && toCallsign == State.LastTransmissionOriginatorCallsign)
            {
                return true;
            }
            
            return (State.SilenceSinceUtc + AviationDomain.SilenceDurationBeforeNewConversation < _world.UtcNow());
        }
        
        public void OnTransmissionStarted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission,
            RadioStationActor.TransmissionDurationUpdateCallback? onDurationUpdate)
        {
            DisarmSilenceTrigger();
            _durationUpdateCallback = onDurationUpdate;
            
            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().BeginReceivingTransmission(transmission);
                }
            }

            _store.Dispatch(this, new TransmissionStartedEvent(station.UniqueId, station.Get().Callsign));
        }

        public void OnTransmissionAborted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission)
        {
            _store.Dispatch(this, new TransmissionEndedEvent(
                station.UniqueId, 
                _world.UtcNow(),
                OriginatorCallsign: station.Get().Callsign,
                RecipientCallsign: null));

            _durationUpdateCallback = null;
            ArmSilenceTrigger();

            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().AbortReceivingTransmission(transmission.Id);
                }
            }
        }

        public void OnTransmissionCompleted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission, 
            Intent intent)
        {
            _store.Dispatch(this, new TransmissionEndedEvent(
                station.UniqueId, 
                _world.UtcNow(),
                OriginatorCallsign: intent.Header.OriginatorCallsign,
                RecipientCallsign: intent.Header.RecipientCallsign));
            _durationUpdateCallback = null;
            ArmSilenceTrigger();

            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().CompleteReceivingTransmission(transmission.Id, intent);
                }
            }
        }

        public void OnSilence()
        {
            if (State.PendingTransmissionTokens.IsEmpty)
            {
                
                return;
            }

            var token = State.PendingTransmissionTokens.Peek();
            _store.Dispatch(this, new TransmissionTokenDequeuedEvent());
            
            token.Target.Get().BeginQueuedTransmission(token.Cookie);
        }

        public void OnActualTransmissionDurationAvailable(TimeSpan duration)
        {
            try
            {
                _durationUpdateCallback?.Invoke(duration);
            }
            finally
            {
                _durationUpdateCallback = null;
            }
        }

        public ActorRef<RadioStationActor> GroundStation => _groundStation;
        public ImmutableDictionary<string, ActorRef<RadioStationActor>> StationById => State.StationById;

        private void ArmSilenceTrigger()
        {
            _silenceTrigger?.Cancel();
            _silenceTrigger = _world.DeferBy(
                $"aether-silence-detected|{UniqueId}",
                AviationDomain.SilenceDurationBeforeNewConversation, 
                OnSilence);
        }

        private void DisarmSilenceTrigger()
        {
            _silenceTrigger?.Cancel();
            _silenceTrigger = null;
        }

        public record TransmissionQueueToken(
            ulong Id, 
            ActorRef<IRadioOperatingActor> Target, 
            int Cookie);

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<GroundRadioStationAetherActor, ActivationEvent>(
                TypeString, 
                (activation, dependencies) => new GroundRadioStationAetherActor(
                    dependencies.Resolve<IWorldContext>(),
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<ICommsLogger>(),
                    activation
                ));
        }
    }
}
