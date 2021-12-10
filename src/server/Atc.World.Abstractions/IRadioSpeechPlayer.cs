using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atc.World.Abstractions
{
    public interface IRadioSpeechPlayer : IDisposable
    {
        Task Play(byte[] wave, float volume, CancellationToken cancellation);
        Task Play(byte[] data, SoundFormat format, float volume, CancellationToken cancellation);
        void StopAll();
        void StartPttStaticNoise();
        void StopPttStaticNoise();
        void StartMutualCancelationNoise();
        void StopMutualCancelationNoise();
        public SoundFormat Format { get; }
    }
}
