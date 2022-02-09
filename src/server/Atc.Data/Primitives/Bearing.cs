using System;

namespace Atc.Data.Primitives
{
    public readonly struct Bearing
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
        public float Degrees => _value.Degrees;
        public float Radians => _value.Radians;

        public static Bearing FromTrueDegrees(float degrees)
        {
            return new Bearing(Angle.FromDegrees(degrees), BearingType.True);
        }

        public static float NormalizeDegrees(float value)
        {
            var result = Math.Abs(value) > 720
                ? Math.IEEERemainder(value, 360)
                : value;
            
            while (result >= 360.0f)
            {
                result -= 360.0f;
            }
            while (result < 0.0f)
            {
                result += 360.0f;
            }

            return (float)result;
        }
    }
}
