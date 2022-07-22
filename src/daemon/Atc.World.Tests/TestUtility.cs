using Atc.Grains;
using Atc.World.Contracts.Communications;
using Moq;

namespace Atc.World.Tests;

public record MockedGrain<T>(
    GrainRef<T> Grain,
    Mock<T> Mock
) where T : class, IGrainId;

public static class TestUtility
{
    private static int __nextMockId = 1;
    private static ulong __nextTransmissionId = 1;
    
    public static MockedGrain<T> MockGrain<T>(string? grainId = null, ISilo? injectTo = null) where T : class, IGrainId
    {
        var mock = new Mock<T>();
        var grainType = $"MockOf{typeof(T).Name}";

        mock.SetupGet(x => x.GrainType).Returns(grainType);
        mock.SetupGet(x => x.GrainId).Returns(grainId ?? MakeNewGrainId());

        if (injectTo != null)
        {
            SiloTestDoubles.InjectGrainMockToSilo(mock.As<IGrain>().Object, injectTo);
        }
        
        var grainRef = SiloTestDoubles.MockGrainRef(mock.Object);
        return new MockedGrain<T>(grainRef, mock);

        string MakeNewGrainId()
        {
            var mockId = TakeNextMockId();
            return $"{grainType}/#{mockId}";
        }
    }

    public static MockedGrain<IWorldGrain> MockWorldGrain(ISilo silo)
    {
        return MockGrain<IWorldGrain>(grainId: SiloExtensions.WorldGrainId, injectTo: silo);
    }

    public static ulong TakeNextTransmissionId()
    {
        return Interlocked.Increment(ref __nextTransmissionId);
    }

    public static TransmissionDescription NewTransmission(
        ISiloEnvironment? environment = null,
        ulong? id = null,
        GrainRef<IAIRadioOperatorGrain>? originator = null,
        Intent? intent = null,
        LanguageCode? language = null,
        ulong? audioStreamId = null,
        TimeSpan? duration = null)
    {
        var effectiveStartUtc = environment?.UtcNow ?? DateTime.UtcNow;
        var effectiveId = id ?? TakeNextTransmissionId(); 
        var effectiveDuration = duration ?? TimeSpan.FromSeconds(3);
        var effectiveLanguage = language ?? LanguageCode.English;
        var effectiveSynthesisRequest = intent != null
            ? new SpeechSynthesisRequest(effectiveId, originator!.Value, intent, effectiveLanguage)
            : null;
         
        return new TransmissionDescription(
            Id: effectiveId,
            StartUtc: effectiveStartUtc,
            Volume: 1.0f,
            Quality: VoiceLinkQuality.Good,
            AudioStreamId: effectiveSynthesisRequest != null 
                ? null 
                : (audioStreamId ?? 0),
            SynthesisRequest: effectiveSynthesisRequest,
            Duration: effectiveDuration);
    }
    
    private static int TakeNextMockId()
    {
        return Interlocked.Increment(ref __nextMockId);
    }

    private static PartyDescription MakePartyDescription(Callsign? callsign = null)
    {
        return new PersonPartyDescription(
            "test",
            NatureType.AI,
            VoiceDescription.Default,
            GenderType.Male,
            AgeType.Mature,
            SeniorityType.Senior,
            "Bob");
    }
}
