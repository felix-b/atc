using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;

namespace Atc.Data.Primitives
{
    public readonly struct Altitude : IEquatable<Altitude>
    {
        public const float Precision = 0.0001f;
        
        private readonly float _value; 
        private readonly AltitudeUnit _unit;
        private readonly AltitudeType _type;
        
        public Altitude(float value, AltitudeUnit unit, AltitudeType type) 
        {
            _value = value;
            _unit = unit;
            _type = type;
        }

        public float GetValueInUnit(AltitudeUnit unit)
        {
            if (_type == AltitudeType.Agl && unit == AltitudeUnit.FlightLevel)
            {
                throw new InvalidOperationException("Cannot convert AGL altitude to FL unit");
            }

            return _unit == unit 
                ? _value 
                : _value * _unitConversionMatrix[_unit][unit];
        }
        
        public float Value => _value;
        public AltitudeUnit Unit => _unit;
        public AltitudeType Type => _type;

        public bool IsGround => Ground.Equals(this);

        public float Feet => GetValueInUnit(AltitudeUnit.Feet); 

        public float Meters => GetValueInUnit(AltitudeUnit.Meter);

        public float Kilometers => GetValueInUnit(AltitudeUnit.Kilometer); 

        public int FlightLevel => (int)GetValueInUnit(AltitudeUnit.FlightLevel); 

        public bool Equals(Altitude other)
        {
            return (_type == other._type && Math.Abs(_value - other.GetValueInUnit(_unit)) < Precision);
        }

        public override bool Equals(object? obj)
        {
            return obj is Altitude other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = (_type == AltitudeType.Msl ? 0b01000000_00000000_00000000_00000000 : 0);
            hashCode |= (int)Feet;
            return hashCode;
        }
        
        private static readonly Dictionary<AltitudeUnit, Dictionary<AltitudeUnit, float>> _unitConversionMatrix = new() {
            [AltitudeUnit.Feet] = new() {
                [AltitudeUnit.Feet] = 1,
                [AltitudeUnit.Meter] = 0.3048f,
                [AltitudeUnit.Kilometer] = 0.0003048f,
                [AltitudeUnit.FlightLevel] = 0.01f,
            },
            [AltitudeUnit.Meter] = new() {
                [AltitudeUnit.Feet] = 3.28084f,
                [AltitudeUnit.Meter] = 1,
                [AltitudeUnit.Kilometer] = 0.001f,
                [AltitudeUnit.FlightLevel] = 0.0328084f
            },
            [AltitudeUnit.Kilometer] = new() {
                [AltitudeUnit.Feet] = 3280.84f,
                [AltitudeUnit.Meter] = 1000,
                [AltitudeUnit.Kilometer] = 1,
                [AltitudeUnit.FlightLevel] = 32.8084f,
            },
            [AltitudeUnit.FlightLevel] = new() {
                [AltitudeUnit.Feet] = 100,
                [AltitudeUnit.Meter] = 30.48f,
                [AltitudeUnit.Kilometer] = 0.03048f,
                [AltitudeUnit.FlightLevel] = 1,
            },
        };

        public static readonly Altitude Ground = new Altitude(0.0f, AltitudeUnit.Feet, AltitudeType.Agl); 

        public static Altitude FromFeetMsl(float value)
        {
            return new Altitude(value, AltitudeUnit.Feet, AltitudeType.Msl);
        }

        public static Altitude FromFeetAgl(float value)
        {
            return new Altitude(value, AltitudeUnit.Feet, AltitudeType.Agl);
        }

        public static Altitude FromFlightLevel(int value)
        {
            return new Altitude(value, AltitudeUnit.FlightLevel, AltitudeType.Msl);
        }

        public static bool operator ==(Altitude left, Altitude right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Altitude left, Altitude right)
        {
            return !left.Equals(right);
        }
    }
}
