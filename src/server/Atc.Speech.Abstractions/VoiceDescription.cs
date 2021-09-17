using System.Globalization;

namespace Atc.Speech.Abstractions
{
    public record VoiceDescription(
        CultureInfo Culture,
        VoiceGender Gender,
        VoiceType Type,
        VoiceRate Rate,
        VoiceLinkQuality Quality,
        float Volume,
        string? AssignedPlatformVoiceId);

    public enum VoiceGender
    {
        Male,
        Female
    }

    public enum VoiceType
    {
        Unknown = 0,
        Bass = 1,
        Baritone = 2,
        Tenor = 3,
        Countertenor = 4,
        Contralto = 5,
        MezzoSoprano = 6,
        Soprano = 7,
        Treble = 8,
    }

    public enum VoiceRate
    {
        Unknown = 0,
        Slow = 1,
        Medium = 2,
        Fast = 3,
    }

    public enum VoiceLinkQuality
    {
        Unknown = 0,
        Poor = 1,
        Medium = 2,
        Good = 3,
    }
}
