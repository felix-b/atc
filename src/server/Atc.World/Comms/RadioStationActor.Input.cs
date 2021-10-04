using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Atc.Data.Primitives;
using Atc.Math;
using Atc.World.Abstractions;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    public partial class RadioStationActor : StatefulActor<RadioStationActor.StationState>
    {
        public static readonly string TypeString = "radio";
        
        private readonly ISupervisorActor _supervisor;
        private readonly IStateStore _store;
        private readonly IWorldContext _world;
        private readonly ICommsLogger _logger;
        private readonly string _name;
        private readonly string _callsign;

        public RadioStationActor(
            ISupervisorActor supervisor,
            IStateStore store,
            IWorldContext world,
            ICommsLogger logger,
            ActivationEvent activation)
            : base(TypeString, activation.UniqueId, CreateInitialState(activation))
        {
            _supervisor = supervisor;
            _store = store;
            _world = world;
            _logger = logger;
            _name = activation.Name;
            _callsign = activation.Callsign;
        }

        public void PowerOn()
        {
            if (State.PoweredOn)
            {
                return;
            }

            _logger.StationPoweringOn(station: ToString());
            
            _store.Dispatch(this, new PoweredOnEvent());
            TuneTo(State.Frequency);
        }

        public void PowerOff()
        {
            if (!State.PoweredOn)
            {
                return;
            }
            
            var thisRef = this.GetRef(_supervisor);
            State.Aether?.Get().RemoveStation(thisRef);

            _store.Dispatch(this, new PoweredOffEvent());
        }
        
        public void TuneTo(Frequency frequency)
        {
            if (!State.PoweredOn)
            {
                _store.Dispatch(this, new FrequencySwitchedEvent(frequency, Aether: null));
                return;
            }
            
            _logger.StationTuningTo(station: ToString(), khz: frequency.Khz);
            var thisRef = this.GetRef(_supervisor);
            
            var oldAether = State.Aether;
            oldAether?.Get().RemoveStation(thisRef);

            var newAether = _world.TryFindRadioAether(thisRef);
            _store.Dispatch(this, new FrequencySwitchedEvent(frequency, newAether));
            newAether?.Get().AddStation(thisRef);
        }

        public void CheckReachableAether()
        {
            if (!State.PoweredOn)
            {
                return;
            }

            var frequency = State.Frequency;
            var thisRef = this.GetRef(_supervisor);
            
            if (State.Aether != null && !State.Aether.Value.Get().IsReachableBy(thisRef))
            {
                State.Aether.Value.Get().RemoveStation(thisRef);
                _store.Dispatch(this, new FrequencySwitchedEvent(frequency, Aether: null));
            }

            if (State.Aether == null)
            {
                var newAether = _world.TryFindRadioAether(thisRef);
                if (newAether != null)
                {
                    _store.Dispatch(this, new FrequencySwitchedEvent(frequency, newAether));
                    newAether.Value.Get().AddStation(thisRef);
                }
            }
        }

        public void AIRegisterForTransmission(ActorRef<IStatefulActor> speaker, int cookie)
        {
            GetAetherOrThrow().RegisterForTransmission(speaker, cookie);
        }

        public void BeginTransmission(RuntimeWave wave)
        {
            var aether = GetAetherOrThrow();
            
            var transmission = new TransmissionState(
                aether.TakeNextTransmissionId(),
                UniqueId,
                _world.UtcNow(),
                wave);

            _store.Dispatch(this, new StartedTransmittingEvent(transmission));

            var thisRef = this.GetRef(_supervisor);
            aether.OnTransmissionStarted(thisRef, transmission);
        }

        public void AbortTransmission()
        {
            var transmission = State.OutgoingTransmission;
            _store.Dispatch(this, new StoppedTransmittingEvent());
            
            if (transmission != null)
            {
                var thisRef = this.GetRef(_supervisor);
                GetAetherOrThrow().OnTransmissionAborted(thisRef, transmission);
            }
        }

        public void CompleteTransmission(Intent intent)
        {
            var transmission = State.OutgoingTransmission 
                ?? throw new InvalidOperationException("No outgoing transmission");
            
            _store.Dispatch(this, new StoppedTransmittingEvent());

            var thisRef = this.GetRef(_supervisor);
            GetAetherOrThrow().OnTransmissionCompleted(thisRef, transmission, intent);
        }

        public void BeginReceivingTransmission(TransmissionState transmission)
        {
            //var waveDuration = _speechPlayer.Format.GetWaveDuration(wave.Length);
            //var transmission = new TransmissionState(transmittingStationId, transmissionId, startedAtUtc, waveDuration, wave);
            _store.Dispatch(this, new StartedReceivingTransmissionEvent(transmission));
        }

        public void AbortReceivingTransmission(ulong id)
        {
            _store.Dispatch(this, new StoppedReceivingTransmissionEvent(id));
        }

        public void AbortReceivingAllTransmissions()
        {
            _store.Dispatch(this, new StoppedReceivingTransmissionEvent(TransmissionId: null));
        }

        public void CompleteReceivingTransmission(ulong id, Intent intent)
        {
            var transmission = State.IncomingTransmissions.FirstOrDefault(t => t.Id == id)
                ?? throw new InvalidOperationException("Did not start receiving specified transmission");
            
            _store.Dispatch(this, new StoppedReceivingTransmissionEvent(id));
            IntentReceived?.Invoke(this, transmission, intent);
        }
        
        public bool IsReachableBy(RadioStationActor other)
        {
            // assuming VHF band - check line of sight
            var maxRangeNm = (System.Math.Sqrt(Elevation.Feet) + System.Math.Sqrt(other.Elevation.Feet)) * 1.225;
            var actualDistance = GeoMath.QuicklyApproximateDistance(Location, other.Location);
            var isInSight = (actualDistance.NauticalMiles <= maxRangeNm);
            return isInSight;
        }

        public bool IsPoweredOn()
        {
            return State.PoweredOn;
        }

        public bool IsTransmitting(out TransmissionState? transmission)
        {
            transmission = State.OutgoingTransmission; 
            return transmission != null;
        }

        public TransceiverStatus GetStatus()
        {
            if (State.IncomingTransmissions.Length > 0)
            {
                return State.IncomingTransmissions.Length == 1 && State.OutgoingTransmission == null
                    ? TransceiverStatus.ReceivingSingleTransmission
                    : TransceiverStatus.ReceivingMutualCancellation;
            }
            else
            {
                return State.OutgoingTransmission != null
                    ? TransceiverStatus.Transmitting
                    : TransceiverStatus.Silence;
            }
        }

        public override string ToString()
        {
            return $"{Frequency.Khz}|{Name}";
        }

        public string Name => _name;
        public string Callsign => _callsign;
        public Frequency Frequency => State.Frequency;
        public GeoPoint Location => State.LocationGetter();
        public Altitude Elevation => State.ElevationGetter();

        public event Action<RadioStationActor, TransmissionState, Intent>? IntentReceived;

        private GroundRadioStationAetherActor GetAetherOrThrow()
        {
            return 
                State.Aether?.Get() 
                ?? throw new InvalidOperationException("No reachable ground station on current frequency");
        }

        public static ActorRef<RadioStationActor> Create(
            ISupervisorActor supervisor,
            GeoPoint location, 
            Altitude elevation,
            Frequency frequency, 
            string name, 
            string callsign)
        {
            return supervisor.CreateActor<RadioStationActor>(uniqueId => new ActivationEvent(uniqueId,
                location,
                elevation,
                frequency,
                Name: name,
                Callsign: callsign
            ));
        }
        
        public enum TransceiverStatus
        {
            Silence,
            ReceivingSingleTransmission,
            ReceivingMutualCancellation,
            Transmitting
        }
    }
}
