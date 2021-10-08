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
        private IDeferHandle? _silenceTrigger = null;

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

        public void EnqueueTransmission(ActorRef<RadioStationActor> station, int cookie, out ulong tokenId)
        {
            tokenId = State.LastTransmissionQueueTokenId + 1;
            var token = new TransmissionQueueToken(tokenId, station, cookie);

            _store.Dispatch(this, new TransmissionTokenEnqueuedEvent(token));
            _logger.RegisteredPendingTransmission(tokenId, station.UniqueId, cookie);

            if (IsSilentForNewConversation()) //TODO: differentiate a new conversation from continuation of the current one
            {
                _world.Defer(OnSilence);
            }
        }

        public bool IsReachableBy(ActorRef<RadioStationActor> station)
        {
            return station.Get().IsReachableBy(_groundStation.Get());
        }

        public bool IsSilentForNewConversation()
        {
            return State.SilenceSinceUtc + AviationDomain.SilenceDurationBeforeNewConversation < _world.UtcNow();
        }
        
        public void OnTransmissionStarted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission)
        {
            DisarmSilenceTrigger();
            
            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().BeginReceivingTransmission(transmission);
                }
            }

            _store.Dispatch(this, new TransmissionStartedEvent(station.UniqueId));
        }

        public void OnTransmissionAborted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission)
        {
            _store.Dispatch(this, new TransmissionEndedEvent(station.UniqueId, _world.UtcNow()));
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
            ArmSilenceTrigger();
            _store.Dispatch(this, new TransmissionEndedEvent(station.UniqueId, _world.UtcNow()));

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

        public ActorRef<RadioStationActor> GroundStation => _groundStation;
        public ImmutableDictionary<string, ActorRef<RadioStationActor>> StationById => State.StationById;

        private void ArmSilenceTrigger()
        {
            _silenceTrigger?.Cancel();
            _silenceTrigger = _world.DeferBy(AviationDomain.SilenceDurationBeforeNewConversation, OnSilence);
        }

        private void DisarmSilenceTrigger()
        {
            _silenceTrigger?.Cancel();
            _silenceTrigger = null;
        }

        public record TransmissionQueueToken(
            ulong Id, 
            ActorRef<RadioStationActor> Target, 
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
