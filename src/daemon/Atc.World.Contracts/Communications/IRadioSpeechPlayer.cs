using Atc.World.Contracts.Sound;

namespace Atc.World.Contracts.Communications;

public interface IRadioSpeechPlayer : IDisposable
{
    Task Play(IAudioStream stream, TimeSpan startPoint, float volume, CancellationToken cancellation);
    
    void StopAll();
    
    void StartPttStaticNoise();
    
    void StopPttStaticNoise();
    
    void StartInterferenceNoise();
    
    void StopInterferenceNoise();
    
    public SoundFormat Format { get; }
}
