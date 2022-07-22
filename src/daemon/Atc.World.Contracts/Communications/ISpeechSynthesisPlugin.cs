using Atc.Grains;
using Atc.World.Contracts.Sound;

namespace Atc.World.Contracts.Communications;

public interface ISpeechSynthesisPlugin
{
    Task<SpeechSynthesisResult> SynthesizeSpeech(
        UtteranceDescription utterance, 
        VoiceDescription voice,
        CancellationToken cancellation);
}

public record SpeechSynthesisRequest(
    ulong TransmissionId,
    GrainRef<IAIRadioOperatorGrain> Originator,
    Intent Intent,
    LanguageCode Language
);

public record SpeechSynthesisResult(
    // access audio stream through IAudioStreamCache
    ulong AudioStreamId,
    // reuse on subsequent calls for same party
    string? AssignedPlatformVoiceId,
    // duration of the audio, if known (it's unknown for human transmissions in progress) 
    TimeSpan? Duration
);
