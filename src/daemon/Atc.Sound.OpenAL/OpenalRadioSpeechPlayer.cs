using Atc.Grains;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Sound;

namespace Atc.Sound.OpenAL;

public class OpenalRadioSpeechPlayer : IRadioSpeechPlayer
{
    private readonly ISiloEnvironment _environment;
    private readonly SoundBuffer _staticNoise;
    private readonly SoundBuffer _staticEdgeNoise;
    private readonly SoundBuffer _staticInterferenceNoise;
    private SoundBuffer? _currentSpeech = null;

    public OpenalRadioSpeechPlayer(ISiloEnvironment environment)
    {
        _environment = environment;
        _staticNoise = SoundBuffer.LoadFromFile(environment.GetAssetFilePath("sounds/radio-static-loop-1.wav"), looping: true);
        _staticEdgeNoise = SoundBuffer.LoadFromFile(environment.GetAssetFilePath("sounds/radio-static-edge-m.wav"), looping: false);
        _staticInterferenceNoise = SoundBuffer.LoadFromFile(environment.GetAssetFilePath("sounds/radio-static-loop-1.wav"), looping: true);
    }

    public void Dispose()
    {
        StopAll();
    }

    public void StartPttStaticNoise()
    {
        _staticNoise.AdjustVolume(0.2f);
        _staticNoise.BeginPlay();
    }

    public void StopPttStaticNoise()
    {
        _staticNoise.Stop();
    }

    public void StartInterferenceNoise()
    {
        _staticNoise.AdjustVolume(1.0f);
        _staticInterferenceNoise.BeginPlay();
    }

    public void StopInterferenceNoise()
    {
        _staticInterferenceNoise.Stop();
    }

    public async Task Play(IAudioStream stream, TimeSpan startPoint, float volume, CancellationToken cancellation)
    {
        await PrepareToPlay();

        if (cancellation.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var nonAwaitedTask = _staticNoise.PlayAsyncFor(_currentSpeech!.Length, cancellation);
            _staticEdgeNoise.BeginPlay();
            await _currentSpeech.PlayAsync(cancellation);
            await _staticEdgeNoise.PlayAsync(cancellation);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            StopAll();
        }

        async Task PrepareToPlay()
        {
            var data = await FetchFirstBuffer(stream, cancellation); 
            
            HiLoPassFilter.TransformBuffer(
                data,
                stream.Format,
                HiLoPassFilter.PassType.Highpass,
                cutoffFrequency: 1000,
                resonance: 2.0f);

            _currentSpeech = new SoundBuffer(data, stream.Format, looping: false);
            _currentSpeech.AdjustLengthBy(TimeSpan.FromMilliseconds(-750));
            //_currentSpeech.AdjustPitchBy(0.1f);

            _currentSpeech.AdjustVolume(volume);
            _staticNoise.AdjustVolume(volume * 0.15f);
            _staticEdgeNoise.AdjustVolume(volume * 0.25f);
        }
    }

    public void StopAll()
    {
        _currentSpeech?.Stop();
        _staticNoise.Stop();
        _staticEdgeNoise.Stop();
        _staticInterferenceNoise.Stop();
    }

    public SoundFormat Format => _standardFormat;

    private static readonly SoundFormat _standardFormat = 
        new SoundFormat(bitsPerSample: 16, samplesPerSecond: 11025, channelCount: 1);

    private static async Task<byte[]> FetchFirstBuffer(IAudioStream stream, CancellationToken cancellation)
    {
        await foreach (var buffer in stream.DataChunks.WithCancellation(cancellation))
        {
            return buffer;
        }

        return Array.Empty<byte>();
    }
}