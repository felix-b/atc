using Atc.Grains;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.LLLL;

public static class LlhzPartyDescriptionFactory
{
    private static int __nextPartyKey = 0;

    public static PersonPartyDescription CreateParty(IGrainActivationEvent activation)
    {
        var key = __nextPartyKey++;
        var genders = new[] {GenderType.Male, GenderType.Male, GenderType.Male, GenderType.Female, GenderType.Female};
        var voiceTypes = new[] {VoiceType.Bass, VoiceType.Baritone, VoiceType.Tenor, VoiceType.Contralto, VoiceType.Soprano};
        var voiceRates = new[] {VoiceRate.Slow, VoiceRate.Medium, VoiceRate.Slow, VoiceRate.Medium, VoiceRate.Fast};
        var linkQualities = new[] {VoiceLinkQuality.Good, VoiceLinkQuality.Medium, VoiceLinkQuality.Poor, VoiceLinkQuality.Medium, VoiceLinkQuality.Good};
        var volumeLevels = new[] {1.0f, 0.8f, 0.7f, 0.8f, 1.0f};
        var ages = new[] {AgeType.Mature, AgeType.Senior, AgeType.Young, AgeType.Young, AgeType.Mature};
        var seniorities = new[] {SeniorityType.Senior, SeniorityType.Novice, SeniorityType.Senior, SeniorityType.Novice, SeniorityType.Veteran};
        var firstNames = new[] {"Bob", "Michelle", "Peter", "Kelsey", "Kate"};

        var voice = new VoiceDescription(
            Language: LanguageCode.English,
            Gender: genders[key % genders.Length],
            Type: voiceTypes[key % voiceTypes.Length],
            Rate: voiceRates[key % voiceRates.Length],
            Quality: linkQualities[key % linkQualities.Length],
            Volume: volumeLevels[key % volumeLevels.Length],
            AssignedPlatformVoiceId: null);
        
        return new PersonPartyDescription(
            uniqueId: activation.GrainId,
            NatureType.AI,
            voice,
            genders[key % genders.Length],
            ages[key % ages.Length],
            seniorities[key % seniorities.Length],
            firstNames[key % firstNames.Length]
        );
    }
}