using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Atc.Data.Primitives;
using Atc.Math;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioStation
    {
        private readonly IWorldContext _world;
        private readonly IRuntimeStateStore _store;
        private readonly RuntimeRadioEther _ether;
        private readonly Func<GeoPoint> _getLocation;
        private readonly Func<Altitude> _getElevation;
        private RuntimeRadioStation.RuntimeState _state; 

        public RuntimeRadioStation(
            IWorldContext world,
            IRuntimeStateStore store, 
            RuntimeRadioEther ether,
            ulong stationId,
            Func<GeoPoint> getLocation, 
            Func<Altitude> getElevation, 
            Frequency frequency)
        {
            _world = world;
            _store = store;
            _ether = ether;
            _getLocation = getLocation;
            _getElevation = getElevation;
            
            _state = new RuntimeState(
                stationId, 
                frequency, 
                IncomingTransmissions: ImmutableArray<TransmissionState>.Empty, 
                OutgoingTransmission: null);
        }

        public void BeginTransmission(RuntimeWave wave)
        {
            var transmission = new TransmissionState(
                _state.StationId,
                _ether.TakeNextTransmissionId(),
                _world.UtcNow(),
                wave);

            _store.Dispatch(this, new StartedTransmittingEvent(transmission));
            _ether.OnTransmissionStarted(this, transmission);
        }

        public void AbortTransmission()
        {
            var transmission = _state.OutgoingTransmission;
            _store.Dispatch(this, new StoppedTransmittingEvent());
            
            if (transmission != null)
            {
                _ether.OnTransmissionAborted(this, transmission);
            }
        }

        public void CompleteTransmission(Intent intent)
        {
            var transmission = _state.OutgoingTransmission;
            _store.Dispatch(this, new StoppedTransmittingEvent());
            
            if (transmission != null)
            {
                _ether.OnTransmissionCompleted(this, transmission, intent);
            }
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

        public void CompleteReceivingTransmission(ulong id, Intent intent)
        {
            var transmission = _state.IncomingTransmissions.FirstOrDefault(t => t.Id == id);
            _store.Dispatch(new StoppedReceivingTransmissionEvent(id));
            
            if (transmission != null)
            {
                IntentReceived?.Invoke(this, transmission, intent);
            }
            else
            {
                //TODO: log error!
            }
        }
        
        public bool IsReachableBy(RuntimeRadioStation other)
        {
            // assuming VHF band - check line of sight
            var maxRangeNm = (System.Math.Sqrt(Elevation.Feet) + System.Math.Sqrt(other.Elevation.Feet)) * 1.225;
            var actualDistance = GeoMath.QuicklyApproximateDistance(Location, other.Location);
            var isInLineOfSight = (actualDistance.NauticalMiles <= maxRangeNm);
            return isInLineOfSight;
        }

        public bool IsTransmitting()
        {
            return _state.OutgoingTransmission != null;
        }

        public void PowerOff()
        {
            
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

        public ulong Id => _state.StationId;
        public Frequency Frequency => _state.Frequency;
        public GeoPoint Location => _getLocation();
        public Altitude Elevation => _getElevation();

        public event Action<RuntimeRadioStation, TransmissionState, Intent>? IntentReceived;

        public enum TransceiverStatus
        {
            Silence,
            ReceivingSingleTransmission,
            ReceivingMutualCancellation,
            Transmitting
        }
    }
}
