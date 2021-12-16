using System;
using System.Collections.Generic;
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
        private readonly ISystemEnvironment _environment;
        private readonly string _name;
        private readonly string _callsign;
        
        [NotEventSourced]
        private readonly Dictionary<ulong, ListenerCallback> _listenerById = new();
        [NotEventSourced] 
        private ulong _nextListenerId = 1;

        public RadioStationActor(
            ISupervisorActor supervisor,
            IStateStore store,
            IWorldContext world,
            ICommsLogger logger,
            ISystemEnvironment environment,
            ActivationEvent activation)
            : base(TypeString, activation.UniqueId, CreateInitialState(activation))
        {
            _supervisor = supervisor;
            _store = store;
            _world = world;
            _logger = logger;
            _environment = environment;
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

            InvokeListeners(receivedIntent: null);
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

            var newAether = _world.TryFindRadioAether(thisRef, frequency);
            _store.Dispatch(this, new FrequencySwitchedEvent(frequency, newAether));
            newAether?.Get().AddStation(thisRef);

            InvokeListeners(receivedIntent: null);
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
                var newAether = _world.TryFindRadioAether(thisRef, frequency);
                if (newAether != null)
                {
                    _store.Dispatch(this, new FrequencySwitchedEvent(frequency, newAether));
                    newAether.Value.Get().AddStation(thisRef);
                }
            }
        }

        public void AIEnqueueForTransmission(
            IRadioOperatingActor speaker,
            string? toCallsign,
            int cookie, 
            out ulong queueTokenId)
        {
            GetAetherOrThrow().AIEnqueueForTransmission(
                _supervisor.GetRefToActorInstance(speaker),
                toCallsign,
                cookie, 
                out queueTokenId);
        }

        public void BeginTransmission(RadioTransmissionWave wave, TransmissionDurationUpdateCallback? onDurationUpdate = null)
        {
            var aether = GetAetherOrThrow();
            
            var transmission = new TransmissionState(
                aether.TakeNextTransmissionId(),
                UniqueId,
                StartedAtSystemUtc: _environment.UtcNow(),
                wave);

            _store.Dispatch(this, new StartedTransmittingEvent(transmission));

            var thisRef = this.GetRef(_supervisor);
            aether.OnTransmissionStarted(thisRef, transmission, onDurationUpdate);

            InvokeListeners(receivedIntent: null);
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

            InvokeListeners(receivedIntent: null);
        }

        public void CompleteTransmission(Intent intent)
        {
            var transmission = State.OutgoingTransmission 
                ?? throw new InvalidOperationException("No outgoing transmission");
            
            _store.Dispatch(this, new StoppedTransmittingEvent());

            var thisRef = this.GetRef(_supervisor);
            GetAetherOrThrow().OnTransmissionCompleted(thisRef, transmission, intent);

            InvokeListeners(receivedIntent: null);
        }

        public void BeginReceivingTransmission(TransmissionState transmission)
        {
            //var waveDuration = _speechPlayer.Format.GetWaveDuration(wave.Length);
            //var transmission = new TransmissionState(transmittingStationId, transmissionId, startedAtUtc, waveDuration, wave);
            _store.Dispatch(this, new StartedReceivingTransmissionEvent(transmission));
            InvokeListeners(receivedIntent: null);
        }

        public void AbortReceivingTransmission(ulong id)
        {
            _store.Dispatch(this, new StoppedReceivingTransmissionEvent(id));
            InvokeListeners(receivedIntent: null);
        }

        public void AbortReceivingAllTransmissions()
        {
            _store.Dispatch(this, new StoppedReceivingTransmissionEvent(TransmissionId: null));
            InvokeListeners(receivedIntent: null);
        }

        public void CompleteReceivingTransmission(ulong id, Intent intent)
        {
            var transmission = State.IncomingTransmissions.FirstOrDefault(t => t.Id == id)
                ?? throw new InvalidOperationException("Did not start receiving specified transmission");
            
            _store.Dispatch(this, new StoppedReceivingTransmissionEvent(id));
            IntentReceived?.Invoke(this, transmission, intent);

            InvokeListeners(intent);
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

        public void AddListener(ListenerCallback listener, out ulong listenerId)
        {
            listenerId = _nextListenerId++;
            _listenerById.Add(listenerId, listener);
            InvokeSingleListener(receivedIntent: null, listenerId, listener, GetStatus());
        }

        public void RemoveListener(ulong listenerId)
        {
            _listenerById.Remove(listenerId);
        }

        public TransceiverStatus GetStatus()
        {
            if (State.Aether == null)
            {
                return TransceiverStatus.NoReachableAether;
            }
            else if (State.IncomingTransmissions.Length > 0)
            {
                return State.IncomingTransmissions.Length == 1 && State.OutgoingTransmission == null
                    ? TransceiverStatus.ReceivingSingleTransmission
                    : TransceiverStatus.ReceivingMutualCancellation;
            }
            else if (State.OutgoingTransmission != null)
            {
                return TransceiverStatus.Transmitting;
            }
            else
            {
                return GetAetherOrThrow().IsSilentForNextTransmission()
                    ? TransceiverStatus.Silence
                    : TransceiverStatus.DetectingSilence;
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
        public ActorRef<GroundRadioStationAetherActor>? Aether => State.Aether;

        public TransmissionState? SingleIncomingTransmission =>
            State.IncomingTransmissions.Length == 1
                ? State.IncomingTransmissions[0]
                : null;

        [NotEventSourced]
        public event IntentReceivedCallback? IntentReceived;

        private GroundRadioStationAetherActor GetAetherOrThrow()
        {
            return 
                State.Aether?.Get() 
                ?? throw new InvalidOperationException("No reachable ground station on current frequency");
        }

        private void InvokeListeners(Intent? receivedIntent)
        {
            if (_listenerById.Count == 0)
            {
                return;
            }
            
            using var logSpan = _logger.InvokeAllListeners(UniqueId, intentType: receivedIntent?.Header.Type);
            var status = GetStatus();
            
            foreach (var listenerIdPair in _listenerById)
            {
                InvokeSingleListener(receivedIntent, listenerIdPair.Key, listenerIdPair.Value, status);
            }
        }

        private void InvokeSingleListener(Intent? receivedIntent, ulong listenerId, ListenerCallback? listenerCallback,
            TransceiverStatus status)
        {
            using var logSpan = _logger.InvokingListener(UniqueId, listenerId: listenerId);

            try
            {
                listenerCallback(this, status, receivedIntent);
            }
            catch (Exception e)
            {
                logSpan.Fail(e);
            }
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

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<RadioStationActor, ActivationEvent>(
                TypeString, 
                (activation, dependencies) => new RadioStationActor(
                    dependencies.Resolve<ISupervisorActor>(),
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(),
                    dependencies.Resolve<ICommsLogger>(),
                    dependencies.Resolve<ISystemEnvironment>(),
                    activation
                ));
        }

        public delegate void ListenerCallback(RadioStationActor station, TransceiverStatus status, Intent? receivedIntent);

        public delegate void IntentReceivedCallback(RadioStationActor station, TransmissionState transmission, Intent intent);

        public delegate void TransmissionDurationUpdateCallback(TimeSpan actualDuration);
    }
}
