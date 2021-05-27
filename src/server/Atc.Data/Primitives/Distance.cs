using System;
using System.Collections.Generic;

namespace Atc.Data.Primitives
{
    public readonly struct Distance : IEquatable<Distance>
    {
        public const float Precision = 0.0001f;

        private readonly float _value; 
        private readonly LengthUnit _unit;
        
        public Distance(float value, LengthUnit unit) 
        {
            _value = value;
            _unit = unit;
        }
        
        public float GetValueInUnit(LengthUnit unit)
        {
            return _unit == unit 
                ? _value 
                : _value * _unitConversionMatrix[_unit][unit];
        }

        public float Value => _value;
        public LengthUnit Unit => _unit;

        public float Feet => GetValueInUnit(LengthUnit.Feet); 

        public float Miles => GetValueInUnit(LengthUnit.Mile);

        public float Meters => GetValueInUnit(LengthUnit.Meter);

        public float Kilometers => GetValueInUnit(LengthUnit.Kilometer); 

        public float NauticalMiles => GetValueInUnit(LengthUnit.NauticalMile); 
        
        public bool Equals(Distance other)
        {
            return Math.Abs(_value - other.GetValueInUnit(_unit)) < Precision;
        }

        public override bool Equals(object? obj)
        {
            return obj is Distance other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) this.Feet;
        }

        public override string ToString()
        {
            return $"{_value} {_unit}";
        }

        private static readonly Dictionary<LengthUnit, Dictionary<LengthUnit, float>> _unitConversionMatrix = new() {
            [LengthUnit.Feet] = new() {
                [LengthUnit.Feet] = 1,
                [LengthUnit.Mile] = 0.000189394f,
                [LengthUnit.Meter] = 0.3048f,
                [LengthUnit.Kilometer] = 0.0003048f,
                [LengthUnit.NauticalMile] = 0.000164579f,
            },
            [LengthUnit.Mile] = new() {
                [LengthUnit.Feet] = 5280,
                [LengthUnit.Mile] = 1,
                [LengthUnit.Meter] = 1609.34f,
                [LengthUnit.Kilometer] = 1.60934f,
                [LengthUnit.NauticalMile] = 0.868976f,
            },
            [LengthUnit.Meter] = new() {
                [LengthUnit.Feet] = 3.28084f,
                [LengthUnit.Mile] = 0.000621371f,
                [LengthUnit.Meter] = 1,
                [LengthUnit.Kilometer] = 0.001f,
                [LengthUnit.NauticalMile] = 0.000539957f
            },
            [LengthUnit.Kilometer] = new() {
                [LengthUnit.Feet] = 3280.84f,
                [LengthUnit.Mile] = 0.621371f,
                [LengthUnit.Meter] = 1000,
                [LengthUnit.Kilometer] = 1,
                [LengthUnit.NauticalMile] = 0.539957f,
            },
            [LengthUnit.NauticalMile] = new() {
                [LengthUnit.Feet] = 6076.12f,
                [LengthUnit.Mile] = 1.15078f,
                [LengthUnit.Meter] = 1852,
                [LengthUnit.Kilometer] = 1.852f,
                [LengthUnit.NauticalMile] = 1,
            },
        };
        
        public static Distance FromMeters(float value)
        {
            return new Distance(value, LengthUnit.Meter);
        }

        public static Distance FromKilometers(float value)
        {
            return new Distance(value, LengthUnit.Kilometer);
        }

        public static Distance FromFeet(float value)
        {
            return new Distance(value, LengthUnit.Feet);
        }

        public static Distance FromMiles(float value)
        {
            return new Distance(value, LengthUnit.Mile);
        }

        public static Distance FromNauticalMiles(float value)
        {
            return new Distance(value, LengthUnit.NauticalMile);
        }

        public static Distance FromNauticalMiles(double value)
        {
            return new Distance((float)value, LengthUnit.NauticalMile);
        }

        public static bool operator ==(Distance left, Distance right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Distance left, Distance right)
        {
            return !left.Equals(right);
        }
    }
}
