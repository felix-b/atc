using Atc.Telemetry;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public class SpeechService : ISpeechService
{
    private readonly IVerbalizationService _verbalizationService;
    private readonly ISpeechSynthesisPlugin _synthesisPlugin;
    private readonly IMyTelemetry _telemetry;

    public SpeechService(
        IVerbalizationService verbalizationService,
        ISpeechSynthesisPlugin synthesisPlugin,
        IMyTelemetry telemetry)
    {
        _verbalizationService = verbalizationService;
        _synthesisPlugin = synthesisPlugin;
        _telemetry = telemetry;
    }
    
    public async Task<SpeechSynthesisResult> SynthesizeSpeech(
        SpeechSynthesisRequest request, 
        CancellationToken cancellation)
    {
        var party = request.Originator.Get().Party;
        var verbalizer = _verbalizationService.GetVerbalizer(request);
        var utterance = verbalizer.VerbalizeIntent(request);
        var result = await _synthesisPlugin.SynthesizeSpeech(
            utterance, 
            party.Voice, 
            cancellation);

        return result;
    }

    public interface IMyTelemetry : ITelemetry
    {
    }
}
