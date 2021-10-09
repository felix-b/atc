using System;
using System.Threading;
using System.Threading.Tasks;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Just.Utility;
using Zero.Loss.Actors;

namespace Atc.World.Comms
{
    [NotEventSourced]
    public class RadioStationSoundMonitor : IDisposable
    {
        private readonly ISupervisorActor _supervisor;
        private readonly IVerbalizationService _verbalization;
        private readonly ISpeechSynthesisPlugin _synthesizer;
        private readonly IRadioSpeechPlayer _player;
        private readonly ICommsLogger _logger;
        private readonly RadioStationActor _radioStation;
        private readonly CancellationTokenSource _disposing;
        private readonly ulong _listenerId;
        private TransceiverStatus _lastStatus = TransceiverStatus.Silence;
        private ulong? _lastReceivedTransmissionId = null;
        private Task? _playTask = null;
        private CancellationTokenSource? _playCancellation = null;
        
        public RadioStationSoundMonitor(
            ISupervisorActor supervisor,
            IVerbalizationService verbalization,
            ISpeechSynthesisPlugin synthesizer, 
            IRadioSpeechPlayer player, 
            ICommsLogger logger,
            RadioStationActor radioStation)
        {
            _supervisor = supervisor;
            _verbalization = verbalization;
            _synthesizer = synthesizer;
            _player = player;
            _logger = logger;
            _radioStation = radioStation;
            _disposing = new CancellationTokenSource();

            _radioStation.AddListener(ListenerCallback, out _listenerId);
        }

        public void Dispose()
        {
            _radioStation.RemoveListener(_listenerId);
            _player.Dispose();
        }

        private void ListenerCallback(
            RadioStationActor station, 
            TransceiverStatus status,
            Intent? receivedIntent)
        {
            if (AnythingChanged())
            {
                ApplyChanges();
            }
            
            void ApplyChanges()
            {
                _player.StopAll();

                switch (status)
                {
                    case TransceiverStatus.Silence:
                    case TransceiverStatus.DetectingSilence:
                        _playCancellation?.Cancel();
                        _playTask.SafeContinueWith(() => {
                            _playTask = null;
                            _playCancellation = null;
                        });
                        break;
                    case TransceiverStatus.Transmitting:
                        _playCancellation?.Cancel();
                        _playTask.SafeContinueWith(() => {
                            _playTask = null;
                            _playCancellation = null;
                            _player.StartPttStaticNoise();
                        });
                        break;
                    case TransceiverStatus.ReceivingSingleTransmission:
                        _playCancellation?.Cancel();
                        _playTask.SafeContinueWith(() => {
                            _playCancellation = new CancellationTokenSource();
                            return BeginPlayTransmissionSpeech(_radioStation.SingleIncomingTransmission!, _playCancellation.Token);
                            // _playTask = _player.Play(
                            //     _radioStation.SingleIncomingTransmission!.Wave.Bytes, 
                            //     volume: 1.0f, 
                            //     _playCancellation.Token);
                        });
                        break;
                    case TransceiverStatus.ReceivingMutualCancellation:
                        _playCancellation?.Cancel();
                        _playTask.SafeContinueWith(() => _player.StartMutualCancelationNoise());
                        break;
                }

                _lastStatus = status;
                _lastReceivedTransmissionId = _radioStation.SingleIncomingTransmission?.Id;
            }
            
            bool AnythingChanged()
            {
                if (status != _lastStatus)
                {
                    return true;
                }
                if (status != TransceiverStatus.ReceivingSingleTransmission)
                {
                    return false;
                }
                return _lastReceivedTransmissionId != _radioStation.SingleIncomingTransmission?.Id;
            }
        }

        private async Task BeginPlayTransmissionSpeech(RadioStationActor.TransmissionState transmission, CancellationToken cancellation)
        {
            var shouldSynthesizeSpeech = !transmission.Wave.HasBytes;
            var effectiveWaveBytes = shouldSynthesizeSpeech
                ? await SynthesizeSpeech(transmission, cancellation)
                : transmission.Wave.Bytes;

            if (cancellation.IsCancellationRequested)
            {
                return;
            }
            
            await _player.Play(
                effectiveWaveBytes, 
                volume: 1.0f, 
                cancellation);
        }
        
        private async Task<byte[]> SynthesizeSpeech(RadioStationActor.TransmissionState transmission, CancellationToken cancellation)
        {
            var intent = transmission.Wave.GetIntentOrThrow();

            using var logSpan = _logger.SynthesizingSpeech(
                transmissionId: transmission.Id, 
                intentType: intent.Header.ToString(),
                fromCallsign: intent.Header.OriginatorCallsign, 
                toCallsign: intent.Header.RecipientCallsign);

            try
            {
                var partyActor = _supervisor.GetActorByIdOrThrow<IStatefulActor>(intent.Header.OriginatorUniqueId);
                var party = ((IHaveParty) partyActor.Get()).Party;
                var verbalizer = _verbalization.GetVerbalizer(party, transmission.Wave.Language);
                var utterance = verbalizer.VerbalizeIntent(party, intent);
                //TODO: why DefaultVoice?
                //TODO: remember AssignedPlatformVoiceId 
                var result = await _synthesizer.SynthesizeUtteranceWave(utterance, party.DefaultVoice);
                return result.Wave;
            }
            catch (Exception e)
            {
                logSpan.Fail(e);
                throw;
            }
        }
    }
}
