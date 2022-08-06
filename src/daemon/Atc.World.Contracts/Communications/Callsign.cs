namespace Atc.World.Contracts.Communications;

public record Callsign(
    string Full,
    string Short)
{
    public static readonly Callsign Empty = new Callsign(string.Empty, String.Empty);
}
