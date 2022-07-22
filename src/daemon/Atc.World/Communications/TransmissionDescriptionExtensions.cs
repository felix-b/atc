using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public static class TransmissionDescriptionExtensions
{
    public static async Task<TransmissionDescription> WithEnsuredAudioStreamId(
        this TransmissionDescription transmission,
        IVerbalizationService verbalizationService,
        ISpeechSynthesisPlugin speechSynthesisPlugin,
        CancellationToken cancellation)
    {
        //TODO: use cancellation token
        
        if (transmission.AudioStreamId.HasValue)
        {
            return transmission;
        }

        if (transmission.SynthesisRequest == null)
        {
            throw new InvalidOperationException("Transmission has no audio stream and no synthesis request");
        }

        var verbalizer = verbalizationService.GetVerbalizer(transmission.SynthesisRequest);
        var utterance = verbalizer.VerbalizeIntent(transmission.SynthesisRequest);
        var synthesisResult = await speechSynthesisPlugin.SynthesizeSpeech(
            utterance, 
            transmission.SynthesisRequest.Party.Voice, 
            cancellation);

        //TODO: propagate AssignedPlatformVoiceId
        
        return transmission with {
            AudioStreamId = synthesisResult.AudioStreamId,
            SynthesisRequest = null,
            Duration = synthesisResult.Duration
        };
    }
}
