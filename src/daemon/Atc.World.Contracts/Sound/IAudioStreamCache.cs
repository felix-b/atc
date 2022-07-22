namespace Atc.World.Contracts.Sound;

public interface IAudioStreamCache
{
    IAudioStream CreateStream(SoundFormat format, TimeSpan? duration = null);
    Task<IAudioStream> GetStreamById(ulong id);
}
