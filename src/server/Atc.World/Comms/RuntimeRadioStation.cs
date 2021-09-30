using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Atc.Data.Primitives;
using Atc.Math;
using Atc.World.Abstractions;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioStation
    {
        private readonly IWorldContext _world;
        private readonly IRuntimeStateStore _store;
        private readonly ICommsLogger _logger;
        private readonly Func<GeoPoint> _getLocation;
        private readonly Func<Altitude> _getElevation;
        private readonly string _name;
        private readonly string _callsign;
        private RuntimeRadioStation.RuntimeState _state; 

        public RuntimeRadioStation(
            IWorldContext world,
            IRuntimeStateStore store,
            ICommsLogger logger,
            ulong stationId,
            Func<GeoPoint> getLocation, 
            Func<Altitude> getElevation, 
            Frequency frequency, 
            string name,
            string callsign)
        {
            _world = world;
            _store = store;
            _logger = logger;
            _getLocation = getLocation;
            _getElevation = getElevation;
            _name = name;
            _callsign = callsign;

            _state = new RuntimeState(
                stationId, 
                frequency, 
                PoweredOn: false,
                Aether: null,
                IncomingTransmissions: ImmutableArray<TransmissionState>.Empty, 
                OutgoingTransmission: null);
        }

        public void PowerOn()
        {
            if (_state.PoweredOn)
            {
                return;
            }

            _logger.StationPoweringOn(station: ToString());
            
            _store.Dispatch(this, new PoweredOnEvent());
            TuneTo(_state.Frequency);
        }

        public void PowerOff()
        {
            if (!_state.PoweredOn)
            {
                return;
            }
            
            _state.Aether?.RemoveStation(this);
            _store.Dispatch(this, new PoweredOffEvent());
        }
        
        public void TuneTo(Frequency frequency)
        {
            if (!_state.PoweredOn)
            {
                _store.Dispatch(this, new FrequencySwitchedEvent(frequency, Aether: null));
                return;
            }
            
            _logger.StationTuningTo(station: ToString(), khz: frequency.Khz);

            var oldAether = _state.Aether;
            oldAether?.RemoveStation(this);

            var newAether = _world.TryFindRadioAether(this);
            _store.Dispatch(this, new FrequencySwitchedEvent(frequency, newAether));
            newAether?.AddStation(this);
        }

        public void CheckReachableAether()
        {
            if (!_state.PoweredOn)
            {
                return;
            }

            var frequency = _state.Frequency;
            
            if (_state.Aether != null && !_state.Aether.IsReachableBy(this))
            {
                _state.Aether.RemoveStation(this);
                _store.Dispatch(this, new FrequencySwitchedEvent(frequency, Aether: null));
            }

            if (_state.Aether == null)
            {
                var newAether = _world.TryFindRadioAether(this);
                if (newAether != null)
                {
                    _store.Dispatch(this, new FrequencySwitchedEvent(frequency, newAether));
                    newAether.AddStation(this);
                }
            }
        }

        public void AIRegisterForTransmission<TState>(IHaveRuntimeState<TState> speaker, int cookie)
            where TState : class
        {
            GetAetherOrThrow().RegisterForTransmission(speaker, cookie);
        }

        public void BeginTransmission(RuntimeWave wave)
        {
            var aether = GetAetherOrThrow();
            
            var transmission = new TransmissionState(
                _state.StationId,
                aether.TakeNextTransmissionId(),
                _world.UtcNow(),
                wave);

            _store.Dispatch(this, new StartedTransmittingEvent(transmission));
            aether.OnTransmissionStarted(this, transmission);
        }

        public void AbortTransmission()
        {
            var transmission = _state.OutgoingTransmission;
            _store.Dispatch(this, new StoppedTransmittingEvent());
            
            if (transmission != null)
            {
                GetAetherOrThrow().OnTransmissionAborted(this, transmission);
            }
        }

        public void CompleteTransmission(Intent intent)
        {
            var transmission = _state.OutgoingTransmission 
                ?? throw new InvalidOperationException("No outgoing transmission");
            
            _store.Dispatch(this, new StoppedTransmittingEvent());
            GetAetherOrThrow().OnTransmissionCompleted(this, transmission, intent);
        }

        public void BeginReceivingTransmission(TransmissionState transmission)
        {
            //var waveDuration = _speechPlayer.Format.GetWaveDuration(wave.Length);
            //var transmission = new TransmissionState(transmittingStationId, transmissionId, startedAtUtc, waveDuration, wave);
            _store.Dispatch(new StartedReceivingTransmissionEvent(transmission));
        }

        public void AbortReceivingTransmission(ulong id)
        {
            _store.Dispatch(new StoppedReceivingTransmissionEvent(id));
        }

        public void AbortReceivingAllTransmissions()
        {
            _store.Dispatch(new StoppedReceivingTransmissionEvent(TransmissionId: null));
        }

        public void CompleteReceivingTransmission(ulong id, Intent intent)
        {
            var transmission = _state.IncomingTransmissions.FirstOrDefault(t => t.Id == id)
                ?? throw new InvalidOperationException("Did not start receiving specified transmission");
            
            _store.Dispatch(new StoppedReceivingTransmissionEvent(id));
            IntentReceived?.Invoke(this, transmission, intent);
        }
        
        public bool IsReachableBy(RuntimeRadioStation other)
        {
            // assuming VHF band - check line of sight
            var maxRangeNm = (System.Math.Sqrt(Elevation.Feet) + System.Math.Sqrt(other.Elevation.Feet)) * 1.225;
            var actualDistance = GeoMath.QuicklyApproximateDistance(Location, other.Location);
            var isInSight = (actualDistance.NauticalMiles <= maxRangeNm);
            return isInSight;
        }

        public bool IsPoweredOn()
        {
            return _state.PoweredOn;
        }

        public bool IsTransmitting(out TransmissionState? transmission)
        {
            transmission = _state.OutgoingTransmission; 
            return transmission != null;
        }

        public TransceiverStatus GetStatus()
        {
            if (_state.IncomingTransmissions.Length > 0)
            {
                return _state.IncomingTransmissions.Length == 1 && _state.OutgoingTransmission == null
                    ? TransceiverStatus.ReceivingSingleTransmission
                    : TransceiverStatus.ReceivingMutualCancellation;
            }
            else
            {
                return _state.OutgoingTransmission != null
                    ? TransceiverStatus.Transmitting
                    : TransceiverStatus.Silence;
            }
        }

        public override string ToString()
        {
            return $"{Frequency.Khz}|{Name}";
        }

        public ulong Id => _state.StationId;
        public string Name => _name;
        public Frequency Frequency => _state.Frequency;
        public GeoPoint Location => _getLocation();
        public Altitude Elevation => _getElevation();
        public string Callsign => _callsign;

        public event Action<RuntimeRadioStation, TransmissionState, Intent>? IntentReceived;

        private GroundRadioStationAether GetAetherOrThrow()
        {
            return 
                _state.Aether 
                ?? throw new InvalidOperationException("No reachable ground station on current frequency");
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
