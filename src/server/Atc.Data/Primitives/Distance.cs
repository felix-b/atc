namespace Atc.Data.Primitives
{
    public struct Distance
    {
        private readonly float _value; 
        private readonly LengthUnit _unit;
        
        public Distance(float value, LengthUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public float Value => _value;
        public LengthUnit Unit => _unit;

        public static Distance FromMeters(float value)
        {
            return new Distance(value, LengthUnit.Meter);
        }

        public static Distance FromFeet(float value)
        {
            return new Distance(value, LengthUnit.Feet);
        }

        public static Distance FromNauticalMiles(float value)
        {
            return new Distance(value, LengthUnit.NauticalMile);
        }
    }

    public enum LengthUnit
    {   
        Meter,
        Feet,
        NauticalMile,
        Kilometer
    }
}
