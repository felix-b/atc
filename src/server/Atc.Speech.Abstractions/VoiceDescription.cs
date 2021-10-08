using System.Globalization;
using Atc.Data.Primitives;

namespace Atc.Speech.Abstractions
{
    public record VoiceDescription(
        LanguageCode Language,
        VoiceGender Gender,
        VoiceType Type,
        VoiceRate Rate,
        VoiceLinkQuality Quality,
        float Volume,
        string? AssignedPlatformVoiceId)
    {
        public static readonly VoiceDescription Default = new VoiceDescription(
            Language: "en-US",
            VoiceGender.Male,
            VoiceType.Bass,
            VoiceRate.Medium,
            VoiceLinkQuality.Good,
            Volume: 1.0f,
            AssignedPlatformVoiceId: null);
    }

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
