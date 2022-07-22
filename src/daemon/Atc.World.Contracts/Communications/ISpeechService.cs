namespace Atc.World.Contracts.Communications;

public interface ISpeechService
{
    Task<SpeechSynthesisResult> SynthesizeSpeech(SpeechSynthesisRequest request, CancellationToken cancellation);
}
