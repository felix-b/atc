using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Atc.World.Abstractions;
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
            var shouldSynthesizeSpeech = true; //!transmission.Wave.HasSound;
            var watch = Stopwatch.StartNew();
            var synthesizeResult = await SynthesizeSpeech(transmission, cancellation); 
                // shouldSynthesizeSpeech
                // ? await SynthesizeSpeech(transmission, cancellation)
                // : transmission.Wave.SoundBytes;
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            var synthesisDuration = watch.Elapsed;
            var speechDuration = synthesizeResult.WaveDuration; //TimeSpan.FromMilliseconds(700); // 

            using (var logSpan = _logger.SpeechSynthesisCompletion(synthesisDuration, speechDuration))
            {
                try
                {
                    var aether = _radioStation.Aether?.Get();
                    aether?.OnActualTransmissionDurationAvailable(speechDuration);
                }
                catch (Exception e)
                {
                    logSpan.Fail(e);
                    throw;
                }
            }

            await _player.Play(
                synthesizeResult.Wave, 
                synthesizeResult.Format,
                volume: 1.0f, 
                cancellation);
        }
        
        private async Task<SynthesizeUtteranceWaveResult> SynthesizeSpeech(RadioStationActor.TransmissionState transmission, CancellationToken cancellation)
        {
            if (transmission.Wave.Utterance == null || transmission.Wave.Voice == null)
            {
                throw _logger.CannotSynthesizeSpeechNoUttteranceOrVoice(transmission.Id, transmission.TransmittingStationId);
            }
        
            _logger.SynthesizingSpeech(
                transmissionId: transmission.Id, 
                fromCallsign:_supervisor.GetActorByIdOrThrow<RadioStationActor>(transmission.TransmittingStationId).Get().Callsign, 
                utterance: transmission.Wave.Utterance!.ToString()!);

            try
            {
                //TODO: use cancellation token
                //TODO: remember AssignedPlatformVoiceId 
                var result = await _synthesizer.SynthesizeUtteranceWave(transmission.Wave.Utterance, transmission.Wave.Voice!);
                return result;
            }
            catch (Exception e)
            {
                _logger.FailedToSynthesizeSpeech(transmissionId: transmission.Id, e);
                throw;
            }
        }
    }
}
