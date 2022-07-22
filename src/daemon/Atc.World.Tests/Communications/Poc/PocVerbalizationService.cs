using Atc.World.Contracts.Communications;
using static Atc.World.Contracts.Communications.UtteranceDescription.PartFactory;

namespace Atc.World.Tests.Communications.Poc;

public class PocVerbalizationService : IVerbalizationService
{
    private readonly EnglishVerbalizer _english = new();
    
    public IVerbalizer GetVerbalizer(SpeechSynthesisRequest request)
    {
        if (request.Language == _english.Language)
        {
            return _english;
        }

        throw new NotSupportedException($"POC verbalizer does not support language '{request.Language}'");
    }

    public class EnglishVerbalizer : IVerbalizer
    {
        private readonly Dictionary<string, string> _phoneticAlphabet = new Dictionary<string, string> {
            {"A", "Alpha"},
            {"B", "Bravo"},
            {"C", "Charlie"},
            {"D", "Delta"},
            {"Q", "Quebec"},
        };

        public UtteranceDescription VerbalizeIntent(SpeechSynthesisRequest request)
        {
            var intent = 
                request.Intent as PocIntent 
                ?? throw new NotSupportedException($"POC verbalizer does not support intent of type '{request.Intent.GetType().Name}'");

            var utterance = new UtteranceDescription(
                Language,
                new UtteranceDescription.Part[] {
                    CallsignPart(_phoneticAlphabet[intent.Header.Callee!.Full]),
                    PunctuationPart(),
                    CallsignPart(_phoneticAlphabet[intent.Header.Caller!.Full]),
                    PunctuationPart(),
                    TextPart($"Intent number {(int)intent.PocType}")
                },
                estimatedDuration: intent.PocType.GetTransmissionDuration()
            );

            return utterance;
        }

        public LanguageCode Language { get; } = new LanguageCode("en-US");
    }
}