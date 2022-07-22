namespace Atc.World.Contracts.Communications;

[Flags]
public enum ToneTraits
{
    Neutral = 0,
    Affirmation = 0x0001,
    Negation = 0x0002,
    Distress = 0x0004,
    ConfidenceLow = 0x0010,
    ConfidenceHigh = 0x0020,
    ToneCheerful = 0x0100,
    ToneEducative = 0x0200,
    ToneImpatient = 0x0400,
}
