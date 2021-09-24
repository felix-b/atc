using System.Threading.Tasks;

namespace Atc.Speech.Abstractions
{
    public interface IRadioSpeechPlayer
    {
        Task Play(byte[] wave, float volume);
        public SoundFormat Format { get; }
    }
}
