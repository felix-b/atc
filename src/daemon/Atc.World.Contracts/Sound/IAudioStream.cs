using System.Threading.Channels;

namespace Atc.World.Contracts.Sound;

public interface IAudioStream
{
    void NotifyWriteCompleted(); // use Data.Writer.TryComplete, it's idempotent
    ulong Id { get; }
    SoundFormat Format { get; }
    TimeSpan? Duration { get; }
    Channel<byte[]> Data { get; }
    event Action? DurationReady;
}

public interface IAudioStreamChunk
{
    ref Span<byte> GetBuffer();
    TimeSpan Duration { get; }
}
