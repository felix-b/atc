namespace Atc.World.Contracts.Communications;

public record TransmissionDescription(
    ulong Id,
    DateTime StartUtc,
    ulong? AudioStreamId,
    SpeechSynthesisRequest? SynthesisRequest,
    TimeSpan? Duration
) {
    public void ValidateOrThrow(string paramName)
    {
        if ((AudioStreamId == null) == (SynthesisRequest == null))
        {
            throw new ArgumentException(
                "Transmission must specify exactly one of AudioStreamId or SynthesisRequest", 
                paramName: paramName);
        }
    }
}
