namespace Atc.World.Contracts.Communications; 

public interface IVerbalizationService
{
    IVerbalizer GetVerbalizer(SpeechSynthesisRequest synthesisRequest);
}
    
public interface IVerbalizer
{
    UtteranceDescription VerbalizeIntent(SpeechSynthesisRequest synthesisRequest);
    LanguageCode Language { get; }
}
