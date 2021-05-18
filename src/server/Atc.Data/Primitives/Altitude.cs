namespace Atc.Data.Primitives
{
    public readonly struct Altitude
    {
        private readonly float _value; 
        private readonly AltitudeUnit _unit;
        private readonly AltitudeType _type;
        
        public Altitude(float value, AltitudeUnit unit, AltitudeType type) 
        {
            _value = value;
            _unit = unit;
            _type = type;
        }

        public float Value => _value;
        public AltitudeUnit Unit => _unit;
        public AltitudeType Type => _type;

        public static Altitude FromFeetMsl(float value)
        {
            return new Altitude(value, AltitudeUnit.Feet, AltitudeType.Msl);
        }
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
}
