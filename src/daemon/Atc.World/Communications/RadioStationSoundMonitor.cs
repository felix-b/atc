using System.Diagnostics;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Sound;

namespace Atc.World.Communications;

[NotEventSourced]
public class RadioStationSoundMonitor : IDisposable
{
    private readonly ISiloEnvironment _environment;
    private readonly IVerbalizationService _verbalization;
    private readonly ISpeechSynthesisPlugin _synthesizer;
    private readonly IAudioStreamCache _streamCache;
    private readonly IRadioSpeechPlayer _player;
    private readonly IThisTelemetry _telemetry;
    private readonly GrainRef<IRadioStationGrain> _radioStation;
    private readonly CancellationTokenSource _disposing;
    private TransceiverStatus _lastStatus = TransceiverStatus.Silence;
    private ulong? _lastReceivedTransmissionId = null;
    private Task? _playTask = null;
    private CancellationTokenSource? _playCancellation = null;
        
    public RadioStationSoundMonitor(
        ISiloEnvironment environment,
        IVerbalizationService verbalization,
        ISpeechSynthesisPlugin synthesizer, 
        IAudioStreamCache streamCache,
        IRadioSpeechPlayer player, 
        IThisTelemetry telemetry,
        GrainRef<IRadioStationGrain> radioStation)
    {
        _environment = environment;
        _verbalization = verbalization;
        _synthesizer = synthesizer;
        _streamCache = streamCache;
        _player = player;
        _telemetry = telemetry;
        _radioStation = radioStation;
        _disposing = new CancellationTokenSource();

        _radioStation.Get().OnTransceiverStateChanged += ListenerCallback;
    }

    public void Dispose()
    {
        _radioStation.Get().OnTransceiverStateChanged -= ListenerCallback;
        _disposing.Cancel();
        _playCancellation?.Cancel();
        _player.Dispose();
    }

    private void ListenerCallback(ITransceiverState state)
    {
        if (AnythingChanged())
        {
            ApplyChanges();
        }
            
        void ApplyChanges()
        {
            _player.StopAll();

            switch (state.Status)
            {
                case TransceiverStatus.Silence:
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
                        return BeginPlayTransmissionSpeech(state.CurrentTransmission!, _playCancellation.Token);
                    });
                    break;
                case TransceiverStatus.ReceivingInterferenceNoise:
                    _playCancellation?.Cancel();
                    _playTask.SafeContinueWith(() => _player.StartInterferenceNoise());
                    break;
            }

            _lastStatus = state.Status;
            _lastReceivedTransmissionId = state.CurrentTransmission?.Id;
        }
            
        bool AnythingChanged()
        {
            if (state.Status != _lastStatus)
            {
                return true;
            }
            if (state.Status != TransceiverStatus.ReceivingSingleTransmission)
            {
                return false;
            }

            return _lastReceivedTransmissionId != state.CurrentTransmission?.Id;
        }
    }

    private async Task BeginPlayTransmissionSpeech(TransmissionDescription transmission, CancellationToken cancellation)
    {
        _telemetry.DebugPreparingToPlayTransmissionSpeech(transmission.Id);
        
        try
        {
            var transmissionWithAudio = await transmission.WithEnsuredAudioStreamId(
                _verbalization, 
                _synthesizer,
                cancellation);

            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            var speechStream = await _streamCache.GetStreamById(transmissionWithAudio.AudioStreamId!.Value);
            var startPoint = GetPlayStartPoint();

            _telemetry.VerbosePlayingTransmissionSpeech(transmission.Id, speechStream.Id, startPoint, speechStream.Duration);

            await _player.Play(speechStream, startPoint, volume: 1.0f, cancellation);
        }
        catch (Exception e)
        {
            _telemetry.ErrorFailedToPlayTransmissionSpeech(transmission.Id, e);
        }

        TimeSpan GetPlayStartPoint()
        {
            var elapsed = _environment.UtcNow.Subtract(transmission.StartUtc);
            return elapsed.TotalMilliseconds > 250
                ? elapsed
                : TimeSpan.Zero;
        }
    }
        
    public interface IThisTelemetry : ITelemetry
    {
        void DebugPreparingToPlayTransmissionSpeech(ulong transmissionId);
        void VerbosePlayingTransmissionSpeech(ulong transmissionId, ulong audioStreamId, TimeSpan startPoint, TimeSpan? duration);
        void ErrorFailedToPlayTransmissionSpeech(ulong transmissionId, Exception exception);
    }
}

public static class AsyncExtensions
{
    public static Task SafeContinueWith(this Task? source, Func<Task> next)
    {
        return source?.ContinueWith(t => next()) ?? next();
    }

    public static Task SafeContinueWith(this Task? source, Action next)
    {
        if (source != null)
        {
            return source.ContinueWith(t => next());
        }

        next();
        return Task.CompletedTask;
    }
}
