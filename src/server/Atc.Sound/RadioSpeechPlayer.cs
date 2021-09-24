using System;
using System.Threading;
using System.Threading.Tasks;
using Atc.Speech.Abstractions;

namespace Atc.Sound
{
    public class RadioSpeechPlayer : IRadioSpeechPlayer
    {
        private readonly SoundBuffer _staticNoise;
        private readonly SoundBuffer _staticEdgeNoise;

        public RadioSpeechPlayer()
        {
            _staticNoise = SoundBuffer.LoadFromFile(@"D:\TnC\atc2\assets\sounds\radio-static-loop-1.wav", looping: true);
            _staticEdgeNoise = SoundBuffer.LoadFromFile(@"D:\TnC\atc2\assets\sounds\radio-static-edge-m.wav", looping: false);
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

        public Task Play(byte[] data, float volume)
        {
            return Play(data, _standardFormat, volume);
        }

        public async Task Play(byte[] data, SoundFormat format, float volume)
        {
            HiLoPassFilter.TransformBuffer(
                data,
                format,
                HiLoPassFilter.PassType.Highpass,
                cutoffFrequency: 1500,
                resonance: 1.0f);

            var buffer = new SoundBuffer(data, format, looping: false);
            buffer.AdjustLengthBy(TimeSpan.FromMilliseconds(-750));
            buffer.AdjustPitchBy(0.1f);

            buffer.AdjustVolume(volume);
            _staticNoise.AdjustVolume(volume * 0.15f);
            _staticEdgeNoise.AdjustVolume(volume * 0.25f);
            
            Console.WriteLine("Playing OpenAL buffer");

            var nonAwaitedTask = _staticNoise.PlayAsyncFor(buffer.Length);
            _staticEdgeNoise.BeginPlay();
            await buffer.PlayAsync();
            await _staticEdgeNoise.PlayAsync();
            buffer.Stop();
        }

        public SoundFormat Format => _standardFormat;

        private static readonly SoundFormat _standardFormat = 
            new SoundFormat(bitsPerSample: 16, samplesPerSecond: 11025, channelCount: 1);
    }
}