using Atc.Speech.Abstractions;

namespace Atc.World.Abstractions
{
    public interface IVerbalizer
    {
        void VerbalizeIntent(Intent intent, out UtteranceDescription utterance, out VoiceDescription voice);
    }
}
