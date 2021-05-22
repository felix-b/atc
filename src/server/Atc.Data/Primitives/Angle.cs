using System;
using System.Runtime.InteropServices.ComTypes;

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
        public float Degrees => _unit == AngleUnit.Degrees ? _value : throw new NotImplementedException();
        public float Radians => _unit == AngleUnit.Radians ? _value : throw new NotImplementedException();

        public static Angle FromDegrees(float value)
        {
            return new Angle(value, AngleUnit.Degrees);
        }
    }
}