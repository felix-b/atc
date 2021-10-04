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

        public ulong RegisterForTransmission(ActorRef<IStatefulActor> speaker, int cookie)
        {
            var tokenId = State.LastTransmissionQueueTokenId + 1;
            var token = new TransmissionQueueToken(tokenId, speaker, cookie);

            _store.Dispatch(this, new TransmissionTokenEnqueuedEvent(token));
            _logger.RegisteredPendingTransmission(tokenId, speaker.UniqueId, cookie);
            
            return tokenId;
        }

        public bool IsReachableBy(ActorRef<RadioStationActor> station)
        {
            return station.Get().IsReachableBy(_groundStation.Get());
        }
        
        public void OnTransmissionStarted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission)
        {
            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().BeginReceivingTransmission(transmission);
                }
            }
        }

        public void OnTransmissionAborted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission)
        {
            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().AbortReceivingTransmission(transmission.Id);
                }
            }

            _store.Dispatch(this, new SilenceStartedEvent(_world.UtcNow()));
        }

        public void OnTransmissionCompleted(
            ActorRef<RadioStationActor> station, 
            RadioStationActor.TransmissionState transmission, 
            Intent intent)
        {
            foreach (var otherStation in State.StationById.Values)
            {
                if (otherStation != station)
                {
                    otherStation.Get().CompleteReceivingTransmission(transmission.Id, intent);
                }
            }

            _store.Dispatch(this, new SilenceStartedEvent(_world.UtcNow()));
        }

        public void OnSilence()
        {
            if (State.PendingTransmissionTokens.IsEmpty)
            {
                return;
            }

            var token = State.PendingTransmissionTokens.Peek();
            _store.Dispatch(this, new TransmissionTokenDequeuedEvent());
            _store.Dispatch(token.Target, new QueuedTransmissionStartEvent(token.Cookie));
        }

        public ActorRef<RadioStationActor> GroundStation => _groundStation;
        
        public record TransmissionQueueToken(
            ulong Id, 
            ActorRef<IStatefulActor> Target, 
            int Cookie);
    }
}
