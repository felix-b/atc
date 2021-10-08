using System;
using System.Threading;
using System.Threading.Tasks;
using Atc.Speech.Abstractions;
using Just.Utility;

namespace Atc.Sound
{
    public class RadioSpeechPlayer : IRadioSpeechPlayer
    {
        private readonly SoundBuffer _staticNoise;
        private readonly SoundBuffer _staticEdgeNoise;
        private readonly SoundBuffer _staticMutualCancellationNoise;
        private SoundBuffer? _currentSpeech = null;

        public RadioSpeechPlayer()
        {
            _staticNoise = SoundBuffer.LoadFromFile(@"D:\TnC\atc2\assets\sounds\radio-static-loop-1.wav", looping: true);
            _staticEdgeNoise = SoundBuffer.LoadFromFile(@"D:\TnC\atc2\assets\sounds\radio-static-edge-m.wav", looping: false);
            _staticMutualCancellationNoise = SoundBuffer.LoadFromFile(@"D:\TnC\atc2\assets\sounds\radio-static-loop-1.wav", looping: true);
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

        public void StartMutualCancelationNoise()
        {
            _staticNoise.AdjustVolume(1.0f);
            _staticMutualCancellationNoise.BeginPlay();
        }

        public void StopMutualCancelationNoise()
        {
            _staticMutualCancellationNoise.Stop();
        }

        public Task Play(byte[] data, float volume, CancellationToken cancellation)
        {
            return Play(data, _standardFormat, volume, cancellation);
        }

        public async Task Play(byte[] data, SoundFormat format, float volume, CancellationToken cancellation)
        {
            PrepareToPlay();

            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            Console.WriteLine("Playing OpenAL buffer");

            try
            {
                var nonAwaitedTask = _staticNoise.PlayAsyncFor(_currentSpeech.Length, cancellation);
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

            void PrepareToPlay()
            {
                HiLoPassFilter.TransformBuffer(
                    data,
                    format,
                    HiLoPassFilter.PassType.Highpass,
                    cutoffFrequency: 1500,
                    resonance: 1.0f);

                _currentSpeech = new SoundBuffer(data, format, looping: false);
                _currentSpeech.AdjustLengthBy(TimeSpan.FromMilliseconds(-750));
                _currentSpeech.AdjustPitchBy(0.1f);

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
            _staticMutualCancellationNoise.Stop();
        }

        public SoundFormat Format => _standardFormat;

        private static readonly SoundFormat _standardFormat = 
            new SoundFormat(bitsPerSample: 16, samplesPerSecond: 11025, channelCount: 1);
    }
}