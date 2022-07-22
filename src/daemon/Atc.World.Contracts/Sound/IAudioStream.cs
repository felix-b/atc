using System.Threading.Channels;

namespace Atc.World.Contracts.Sound;

public interface IAudioStream
{
    Task AddDataChunk(byte[] dataChunk, CancellationToken cancellation);
    void Complete();
    ulong Id { get; }
    SoundFormat Format { get; }
    TimeSpan? Duration { get; }
    IAsyncEnumerable<byte[]> DataChunks { get; }
    event Action? DurationReady;
}

public interface IAudioStreamChunk
{
    ref Span<byte> GetBuffer();
    TimeSpan Duration { get; }
}
