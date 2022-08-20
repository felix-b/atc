using Atc.World.Communications.English;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public class VerbalizationService : IVerbalizationService
{
    private readonly Dictionary<LanguageCode, IVerbalizer> _verbalizerByLanguageCode = new() {
        { LanguageCode.English, new EnglishVerbalizer() },
    };

    public IVerbalizer GetVerbalizer(SpeechSynthesisRequest request)
    {
        if (_verbalizerByLanguageCode.TryGetValue(request.Language, out var verbalizer))
        {
            return verbalizer;
        }

        throw new NotSupportedException($"Verbalizer does not support language '{request.Language}'");
    }
}