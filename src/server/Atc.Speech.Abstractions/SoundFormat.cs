using System;

namespace Atc.Speech.Abstractions
{
    public class SoundFormat
    {
        public SoundFormat(int bitsPerSample, int samplesPerSecond, int channelCount)
        {
            if (bitsPerSample != 8 && bitsPerSample != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bitsPerSample));
            }
            if (samplesPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(samplesPerSecond));
            }
            if (channelCount != 1 && channelCount != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(channelCount));
            }

            this.BitsPerSample = bitsPerSample;
            this.SamplesPerSecond = samplesPerSecond;
            this.ChannelCount = channelCount;
        }

        public TimeSpan GetWaveDuration(int byteLength)
        {
            return TimeSpan.FromSeconds((double) byteLength / (double) ByteRate);
        }

        public int BitsPerSample { get; init; }
        public int SamplesPerSecond { get; init; }
        public int ChannelCount { get; init; }
        public int BytesPerSample => BitsPerSample / 8;
        public int ByteRate => SamplesPerSecond * BytesPerSample * ChannelCount;
    }
}
