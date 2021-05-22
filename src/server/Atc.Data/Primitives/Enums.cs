namespace Atc.Data.Primitives
{
    public enum LengthUnit
    {   
        Feet,
        Mile,
        Meter,
        Kilometer,
        NauticalMile,
    }

    public enum AngleUnit
    {
        Degrees,
        Radians
    }

    public enum AltitudeUnit
    {
        Meter,
        Feet,
        Kilometer,
        FlightLevel
    }

    public enum AltitudeType
    {
        Msl,
        Agl
    }

    public enum SpeedUnit
    {   
        Kt,
        Fpm,
        Mph,
        Kmh
    }

    public enum WeightUnit
    {   
        Kg,
        Lbs
    }

    public enum FrequencyUnit
    {
        Hz,
        Khz,
        Mhz
    }

    public enum BearingType
    {
        True,
        Magnetic
    }

    public enum GeoEdgeType
    {
        Unknown = 0,
        ArcByEdge = 1,
        Circle = 2,
        GreatCircle = 3,
        RhumbLine = 4,
        ClockwiseArc = 5,
        CounterClockwiseArc = 6
    }
}
