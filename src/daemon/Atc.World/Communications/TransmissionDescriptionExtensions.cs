using Atc.Grains;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public static class TransmissionDescriptionExtensions
{
    public static async Task<TransmissionDescription> WithEnsuredAudioStreamId(
        this TransmissionDescription transmission,
        ISilo silo,
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
            silo.PostAsyncAction(transmission.Id, () => {
                request.Originator.Get().NotifyTransmissionDurationAvailable(
                    transmission.Id, 
                    transmission.StartUtc,
                    synthesisResult.Duration.Value);
            });  
        }
        
        //TODO: propagate AssignedPlatformVoiceId
        
        return transmission with {
            AudioStreamId = synthesisResult.AudioStreamId,
            SynthesisRequest = null,
            Duration = synthesisResult.Duration
        };
    }
}
