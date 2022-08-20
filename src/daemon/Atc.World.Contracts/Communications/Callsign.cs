namespace Atc.World.Contracts.Communications;

public record Callsign(
    string Full,
    string Short)
{
    public Callsign(string singleCallsign) : this(singleCallsign, singleCallsign)
    {
    }
    
    public static readonly Callsign Empty = new Callsign(string.Empty, String.Empty);
}
