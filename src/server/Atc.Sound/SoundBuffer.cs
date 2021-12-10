using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Atc.World.Abstractions;
using OpenTK.Audio.OpenAL;

namespace Atc.Sound
{
    public class SoundBuffer : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly SoundFormat _format;
        private readonly bool _looping;
        private readonly int _bufferId;
        private readonly int _sourceId;
        private readonly CancellationTokenSource _disposing = new CancellationTokenSource();
        private TimeSpan _length;
        private bool _disposed = false;

        public SoundBuffer(byte[] data, SoundFormat format, bool looping)
        {
            _format = format;
            _looping = looping;
            _bufferId = AL.GenBuffer();
            _sourceId = AL.GenSource();

            var seconds = (double)data.Length / (double)format.ByteRate;
            _length = TimeSpan.FromSeconds(seconds);

            AL.BufferData(_bufferId, ToALFormat(format), data.AsSpan(), format.SamplesPerSecond);
            AL.Source(_sourceId, ALSourcei.Buffer, _bufferId);
            AL.Source(_sourceId, ALSourceb.Looping, _looping);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                AL.SourceStop(_sourceId);
                AL.DeleteSource(_sourceId);
                AL.DeleteBuffer(_bufferId);
            }
        }

        public void BeginPlay()
        {
            AL.SourceStop(_sourceId);
            AL.SourceRewind(_sourceId);
            AL.SourcePlay(_sourceId);
        }

        public Task PlayAsync(CancellationToken cancellation)
        {
            using var anyReasonCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _disposing.Token);
            
            AL.SourceStop(_sourceId);
            AL.SourceRewind(_sourceId);
            AL.SourcePlay(_sourceId);

            return Task.Delay(_length, anyReasonCancellation.Token);
        }

        public async Task PlayAsyncFor(TimeSpan duration, CancellationToken cancellation)
        {
            using var anyReasonCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _disposing.Token);

            AL.SourceRewind(_sourceId);
            AL.SourcePlay(_sourceId);

            try
            {
                await Task.Delay(duration, anyReasonCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                AL.SourceStop(_sourceId);
            }
        }

        public void Stop()
        {
            AL.SourceStop(_sourceId);
            AL.SourceRewind(_sourceId);
        }

        public void AdjustVolume(float value)
        {
            AL.Source(_sourceId, ALSourcef.Gain, value);
        }

        public void AdjustPitchBy(float value)
        {
            AL.Source(_sourceId, ALSourcef.Pitch, 1.0f + value);
            _length = new TimeSpan((long)(_length.Ticks * (1.0f - value)));
        }

        public void AdjustLengthBy(TimeSpan delta)
        {
            _length = _length.Add(delta);
        }

        public TimeSpan Length => _length;

        public ALSourceState State
        {
            get
            {
                AL.GetSource(_sourceId, ALGetSourcei.SourceState, out var value);
                return (ALSourceState)value;
            }
        }
        
        public static SoundBuffer LoadFromFile(string fileName, bool looping)
        {
            using var file = File.OpenRead(fileName);
            return LoadFromFile(file, looping);
        }

        public static SoundBuffer LoadFromFile(Stream input, bool looping)
        {
            var data = LoadWave(input, out var channels, out var bitsPerSample, out var sampleRate);
            var format = new SoundFormat(bitsPerSample, sampleRate, channels);
            return new SoundBuffer(data, format, looping);
        }

        // Loads a wave/riff audio file.
        public static ALFormat ToALFormat(SoundFormat format)
        {
            switch (format.ChannelCount)
            {
                case 1: return format.BitsPerSample == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                case 2: return format.BitsPerSample == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                default: throw new NotSupportedException("The specified sound format is not supported.");
            }
        }

        private static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            using (BinaryReader reader = new BinaryReader(stream))
            {
                // RIFF header
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                int riff_chunck_size = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("Specified stream is not a wave file.");

                // WAVE header
                string format_signature = new string(reader.ReadChars(4));
                if (format_signature != "fmt ")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int format_chunk_size = reader.ReadInt32();
                int audio_format = reader.ReadInt16();
                int num_channels = reader.ReadInt16();
                int sample_rate = reader.ReadInt32();
                int byte_rate = reader.ReadInt32();
                int block_align = reader.ReadInt16();
                int bits_per_sample = reader.ReadInt16();

                string data_signature = new string(reader.ReadChars(4));
                if (data_signature != "data")
                    throw new NotSupportedException("Specified wave file is not supported.");

                int data_chunk_size = reader.ReadInt32();

                channels = num_channels;
                bits = bits_per_sample;
                rate = sample_rate;

                return reader.ReadBytes((int)reader.BaseStream.Length);
            }
        }
    }
}
