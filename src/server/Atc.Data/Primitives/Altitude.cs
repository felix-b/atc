namespace Atc.Data.Primitives
{
    public readonly struct Altitude
    {
        public float Value { get; init; }
        public AltitudeUnit Unit { get; init; }
    }
    
    public enum AltitudeUnit
    {
        Meter,
        Feet,
        Kilometer,
        FlightLevel
    }
}
