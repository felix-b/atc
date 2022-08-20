using System.Collections.Concurrent;
using Atc.Utilities;
using Atc.World.Contracts.Sound;

namespace Atc.Daemon;

public class AtcdOfflineAudioStreamCache : IAudioStreamCache
{
    private readonly ConcurrentDictionary<ulong, IAudioStream> _streamById = new();
    private ulong _nextStreamId = 1;
    
    public IAudioStream CreateStream(SoundFormat format, TimeSpan? duration = null)
    {
        var id = Interlocked.Increment(ref _nextStreamId);
        var stream = new BufferAudioStream(id, format, duration);
        _streamById[id] = stream;
        return stream;
    }

    public Task<IAudioStream> GetStreamById(ulong id)
    {
        if (_streamById.TryGetValue(id, out var stream))
        {
            return Task.FromResult(stream);
        }

        throw new KeyNotFoundException($"Audio stream with id {id} does not exist");
    }

    public class BufferAudioStream : IAudioStream
    {
        private readonly List<byte[]> _chunks = new();

        public BufferAudioStream(ulong id, SoundFormat format, TimeSpan? duration = null)
        {
            Id = id;
            Format = format;
            Duration = duration;
        }

        public Task AddDataChunk(byte[] dataChunk, CancellationToken cancellation)
        {
            _chunks.Add(dataChunk);
            return Task.CompletedTask;
        }

        public void Complete()
        {
            if (Duration.HasValue)
            {
                throw new InvalidOperationException("This stream was already completed");
            }
            
            var totalByteLength = _chunks.Sum(c => c.Length);
            Duration = Format.GetWaveDuration(totalByteLength);
            DurationReady?.Invoke();
        }

        public ulong Id { get; }
        public SoundFormat Format { get; }
        public TimeSpan? Duration { get; private set; }

        public IAsyncEnumerable<byte[]> DataChunks => _chunks.ToAsyncEnumerable(); 
        
        public event Action? DurationReady;
    }
}
