namespace Atc.Data.Primitives
{
    public readonly struct Angle
    {
        private readonly float _value; 
        private readonly AngleUnit _unit;
        
        public Angle(float value, AngleUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public float Value => _value;
        public AngleUnit Unit => _unit;

        public static Angle FromDegrees(float value)
        {
            return new Angle(value, AngleUnit.Degrees);
        }
    }

    public enum AngleUnit
    {
        Degrees,
        Radians
    }
}