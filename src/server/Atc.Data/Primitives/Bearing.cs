namespace Atc.Data.Primitives
{
    public struct Bearing
    {
        private readonly Angle _value;
        private readonly BearingType _type;

        public Bearing(Angle value, BearingType type)
        {
            _value = value;
            _type = type;
        }

        public Angle Value => _value;
        public BearingType Type => _type;
    }

    public enum BearingType
    {
        Magnetic,
        True
    }
}
