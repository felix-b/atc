using Atc.World.Contracts.Communications;
using static Atc.World.Contracts.Communications.UtteranceDescription.PartFactory;

namespace Atc.World.Communications.English;

public class EnglishVerbalizer : IVerbalizer
{
    private readonly Dictionary<string, string> _phoneticAlphabet = new Dictionary<string, string> {
        {"A", "Alpha"},
        {"B", "Bravo"},
        {"C", "Charlie"},
        {"D", "Delta"},
        {"E", "Echo"},
        {"F", "Foxtrot"},
        {"G", "Golf"},
        {"H", "Hotel"},
        {"I", "India"},
        {"J", "Juliet"},
        {"K", "Kilo"},
        {"L", "Lima"},
        {"M", "Mike"},
        {"N", "November"},
        {"O", "Oscar"},
        {"P", "Papa"},
        {"Q", "Quebec"},
        {"R", "Romeo"},
        {"S", "Sierra"},
        {"T", "Tango"},
        {"U", "Uniform"},
        {"V", "Victor"},
        {"W", "Whiskey"},
        {"X", "X-Ray"},
        {"Y", "Yankee"},
        {"Z", "Zulu"},
    };

    public UtteranceDescription VerbalizeIntent(SpeechSynthesisRequest request)
    {
        var intent = request.Intent;
        var utterance = new UtteranceDescription(
            Language,
            new UtteranceDescription.Part[] {
                CallsignPart(_phoneticAlphabet[intent.Header.Callee!.Full]),
                PunctuationPart(),
                CallsignPart(_phoneticAlphabet[intent.Header.Caller!.Full]),
                PunctuationPart(),
                TextPart($"Intent type {intent.Header.WellKnownType.ToString()}")
            },
            estimatedDuration: TimeSpan.FromSeconds(3)
        );

        return utterance;
    }

    public LanguageCode Language => LanguageCode.English;
}
