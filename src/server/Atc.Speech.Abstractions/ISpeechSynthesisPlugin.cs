using System;
using System.IO;
using System.Threading.Tasks;

namespace Atc.Speech.Abstractions
{
    public interface ISpeechSynthesisPlugin
    {
        Task<SynthesizeUtteranceWaveResult> SynthesizeUtteranceWave(UtteranceDescription utterance, VoiceDescription voice);
    }

    public record SynthesizeUtteranceWaveResult(byte[] Wave, string? AssignedPlatformVoiceId);
}
