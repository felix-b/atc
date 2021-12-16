using System;
using System.Threading.Tasks;

namespace Atc.World.Abstractions
{
    public interface ISpeechSynthesisPlugin
    {
        Task<SynthesizeUtteranceWaveResult> SynthesizeUtteranceWave(UtteranceDescription utterance, VoiceDescription voice);
    }

    public record SynthesizeUtteranceWaveResult(
        byte[] Wave,
        string? AssignedPlatformVoiceId,
        SoundFormat Format)
    {
        public TimeSpan WaveDuration => Format.GetWaveDuration(Wave.Length);
    }
}
