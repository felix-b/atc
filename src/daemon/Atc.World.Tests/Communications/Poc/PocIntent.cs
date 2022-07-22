using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications.Poc;

public record PocIntent(
    IntentHeader Header,
    PocIntentType PocType
) : Intent(Header)
{

    public override string ToString() => $"{Header.Caller.Full}->{Header.Callee?.Full ?? "*"} : {PocType}";

    public static PocIntent Create(
        ulong id, 
        string from, 
        string to, 
        PocIntentType pocType, 
        bool concludesConversation,
        AirGroundPriority? priority = null)
    {
        var header = new IntentHeader(
            id, 
            WellKnownType: WellKnownIntentType.None, 
            Priority: priority ?? AirGroundPriority.FlightSafetyNormal,
            Caller: new Callsign(from, from), 
            Callee: new Callsign(to, to),
            Flags: concludesConversation ? IntentFlags.ConcludesConversation : IntentFlags.None,
            Tone: ToneTraits.Neutral);

        return new PocIntent(Header: header, PocType: pocType);
    }
}

public enum PocIntentType
{
    I1 = 1,
    I2 = 2,
    I3 = 3,
    I4 = 4,
    I5 = 5,
    I6 = 6,
    I7 = 7,
    I8 = 8,
    I9 = 9,
    I10 = 10,
    I11 = 11
}

public static class PocIntentTypeExtensions
{
    public static TimeSpan GetTransmissionDuration(this PocIntentType value)
    {
        switch (value)
        {
            case PocIntentType.I1: return TimeSpan.FromSeconds(3);
            case PocIntentType.I2: return TimeSpan.FromSeconds(4);
            case PocIntentType.I3: return TimeSpan.FromSeconds(5);
            case PocIntentType.I4: return TimeSpan.FromSeconds(4);
            case PocIntentType.I5: return TimeSpan.FromSeconds(3);
            case PocIntentType.I6: return TimeSpan.FromSeconds(2);
            case PocIntentType.I7: return TimeSpan.FromSeconds(3);
            case PocIntentType.I8: return TimeSpan.FromSeconds(4);
            case PocIntentType.I9: return TimeSpan.FromSeconds(5);
            case PocIntentType.I10: return TimeSpan.FromSeconds(4);
            case PocIntentType.I11: return TimeSpan.FromSeconds(3);
        }
        
        return TimeSpan.FromSeconds(3);
    }
}