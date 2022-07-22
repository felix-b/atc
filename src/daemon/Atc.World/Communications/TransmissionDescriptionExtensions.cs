using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public static class TransmissionDescriptionExtensions
{
    public static async Task<TransmissionDescription> WithEnsuredAudioStreamId(
        this TransmissionDescription transmission,
        ISpeechService speechService,
        CancellationToken cancellation)
    {
        //TODO: use cancellation token
        
        if (transmission.AudioStreamId.HasValue)
        {
            return transmission;
        }

        var request = 
            transmission.SynthesisRequest
            ?? throw new InvalidOperationException("Transmission has no audio stream and no synthesis request");

        var synthesisResult = await speechService.SynthesizeSpeech(request, cancellation);
        if (synthesisResult.Duration.HasValue)
        {
            request.Originator.Get().NotifyTransmissionDurationAvailable(
                transmission.Id, 
                synthesisResult.Duration.Value);
        }
        
        //TODO: propagate AssignedPlatformVoiceId
        
        return transmission with {
            AudioStreamId = synthesisResult.AudioStreamId,
            SynthesisRequest = null,
            Duration = synthesisResult.Duration
        };
    }
}
