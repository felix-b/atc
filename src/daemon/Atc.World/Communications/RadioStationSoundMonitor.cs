using System.Diagnostics;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Sound;

namespace Atc.World.Communications;

[NotEventSourced]
public class RadioStationSoundMonitor : IDisposable
{
    private readonly ISilo _silo;
    private readonly ISpeechService _speechService;
    private readonly IAudioStreamCache _streamCache;
    private readonly IRadioSpeechPlayer _player;
    private readonly IMyTelemetry _telemetry;
    private readonly GrainRef<IRadioStationGrain> _radioStation;
    private readonly CancellationTokenSource _disposing;
    private TransceiverStatus _lastStatus = TransceiverStatus.Silence;
    private ulong? _lastReceivedTransmissionId = null;
    private Task? _playTask = null;
    private CancellationTokenSource? _playCancellation = null;

    public RadioStationSoundMonitor(
        GrainRef<IRadioStationGrain> radioStation,
        ISiloDependencyContext dependencies
    ) : this(
        silo: dependencies.Resolve<ISilo>(),
        speechService: dependencies.Resolve<ISpeechService>(),
        streamCache: dependencies.Resolve<IAudioStreamCache>(),
        player: dependencies.Resolve<IRadioSpeechPlayer>(),
        telemetry: dependencies.Resolve<ITelemetryProvider>().GetTelemetry<IMyTelemetry>(),
        radioStation)
    {
    }

    public RadioStationSoundMonitor(
        ISilo silo,
        ISpeechService speechService, 
        IAudioStreamCache streamCache,
        IRadioSpeechPlayer player, 
        IMyTelemetry telemetry,
        GrainRef<IRadioStationGrain> radioStation)
    {
        _silo = silo;
        _speechService = speechService;
        _streamCache = streamCache;
        _player = player;
        _telemetry = telemetry;
        _radioStation = radioStation;
        _disposing = new CancellationTokenSource();

        _radioStation.Get().TransceiverStateChanged += ListenerCallback;
        ListenerCallback(_radioStation.Get().TransceiverState);
    }

    public void Dispose()
    {
        _radioStation.Get().TransceiverStateChanged -= ListenerCallback;
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
                    PlayAITransmissionSpeech();
                    // if (state.CurrentTransmission?.AudioStreamId == null)
                    // {
                    //     PlayHumanTransmissionStatic();
                    // }
                    // else
                    // {
                    //     PlayAITransmissionSpeech();
                    // }
                    break;
                case TransceiverStatus.ReceivingSingleTransmission:
                    PlayAITransmissionSpeech();
                    break;
                case TransceiverStatus.ReceivingInterferenceNoise:
                    _playCancellation?.Cancel();
                    _playTask.SafeContinueWith(() => _player.StartInterferenceNoise());
                    break;
            }

            _lastStatus = state.Status;
            _lastReceivedTransmissionId = state.CurrentTransmission?.Id;
        }

        // void PlayHumanTransmissionStatic()
        // {
        //     _playCancellation?.Cancel();
        //     _playTask.SafeContinueWith(() => {
        //         _playTask = null;
        //         _playCancellation = null;
        //         _player.StartPttStaticNoise();
        //     });
        // }

        void PlayAITransmissionSpeech()
        {
            _playCancellation?.Cancel();
            _playTask.SafeContinueWith(() => {
                _playCancellation = new CancellationTokenSource();
                return BeginPlayTransmissionSpeech(state.CurrentTransmission!, _playCancellation.Token);
            });
        }

        bool AnythingChanged()
        {
            if (state.Status != _lastStatus)
            {
                return true;
            }
            if (state.Status != TransceiverStatus.ReceivingSingleTransmission && state.Status != TransceiverStatus.Transmitting)
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
                _silo,
                _speechService,
                cancellation);

            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            var speechStream = await _streamCache.GetStreamById(transmissionWithAudio.AudioStreamId!.Value);
            var startPoint = GetPlayStartPoint();

            _telemetry.VerbosePlayingTransmissionSpeech(transmission.Id, speechStream.Id, startPoint, speechStream.Duration);

            await _player.Play(speechStream, startPoint, volume: transmission.Volume, cancellation);
        }
        catch (Exception e)
        {
            _telemetry.ErrorFailedToPlayTransmissionSpeech(transmission.Id, e);
        }

        TimeSpan GetPlayStartPoint()
        {
            var elapsed = _silo.Environment.UtcNow.Subtract(transmission.StartUtc);
            return elapsed.TotalMilliseconds > 250
                ? elapsed
                : TimeSpan.Zero;
        }
    }
        
    [TelemetryName("RadioStationSoundMonitor")]
    public interface IMyTelemetry : ITelemetry
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
