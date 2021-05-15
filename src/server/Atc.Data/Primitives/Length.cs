namespace Atc.Data.Primitives
{
    public struct Length
    {
        private readonly float _value; 
        private readonly LengthUnit _unit;
        
        public Length(float value, LengthUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public float Value => _value;
        public LengthUnit Unit => _unit;
    }

    public enum LengthUnit
    {   
        Meter,
        Feet,
        NauticalMile,
        Kilometer
    }
}
