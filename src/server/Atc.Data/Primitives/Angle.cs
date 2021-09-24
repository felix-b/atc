using System;
using System.Collections.Generic;
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

        public float GetValueInUnit(AngleUnit unit)
        {
            return _unit == unit 
                ? _value 
                : _value * _unitConversionMatrix[_unit][unit];
        }
        
        public float Value => _value;
        public AngleUnit Unit => _unit;
        public float Degrees => GetValueInUnit(AngleUnit.Degrees);
        public float Radians => GetValueInUnit(AngleUnit.Radians);

        public static Angle FromDegrees(float value)
        {
            return new Angle(value, AngleUnit.Degrees);
        }

        public static Angle FromRadians(float value)
        {
            return new Angle(value, AngleUnit.Radians);
        }

        private static readonly Dictionary<AngleUnit, Dictionary<AngleUnit, float>> _unitConversionMatrix = new() {
            [AngleUnit.Degrees] = new() {
                [AngleUnit.Degrees] = 1,
                [AngleUnit.Radians] = 0.01745329252f,
            },
            [AngleUnit.Radians] = new() {
                [AngleUnit.Degrees] = 57.295779513f,
                [AngleUnit.Radians] = 1,
            },
        };        
    }
}