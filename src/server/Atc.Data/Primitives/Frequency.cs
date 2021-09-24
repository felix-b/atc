using System.Collections.Generic;

namespace Atc.Data.Primitives
{
    public readonly struct Frequency
    {
        private readonly int _value; 
        private readonly FrequencyUnit _unit;
        
        public Frequency(int value, FrequencyUnit unit) 
        {
            _value = value;
            _unit = unit;
        }

        public decimal GetValueInUnit(FrequencyUnit unit)
        {
            return _unit == unit 
                ? _value 
                : _value * _unitConversionMatrix[_unit][unit];
        }

        public bool Equals(Frequency other)
        {
            return (this.Khz == other.Khz);
        }

        public override bool Equals(object? obj)
        {
            return obj is Frequency other && Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Khz;
        }

        public override string ToString()
        {
            return $"{Khz} KHz";
        }

        public int Value => _value;
        public FrequencyUnit Unit => _unit;
        public int Hz => (int)GetValueInUnit(FrequencyUnit.Hz);
        public int Khz => (int)GetValueInUnit(FrequencyUnit.Khz);
        public decimal Mhz => GetValueInUnit(FrequencyUnit.Mhz);

        public static Frequency FromKhz(int value)
        {
            return new Frequency(value, FrequencyUnit.Khz);
        }

        public static bool operator ==(Frequency left, Frequency right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Frequency left, Frequency right)
        {
            return !left.Equals(right);
        }

        private static readonly Dictionary<FrequencyUnit, Dictionary<FrequencyUnit, decimal>> _unitConversionMatrix = new() {
            [FrequencyUnit.Hz] = new() {
                [FrequencyUnit.Hz] = 1,
                [FrequencyUnit.Khz] = 0.001m,
                [FrequencyUnit.Mhz] = 0.000001m,
            },
            [FrequencyUnit.Khz] = new() {
                [FrequencyUnit.Hz] = 1000,
                [FrequencyUnit.Khz] = 1,
                [FrequencyUnit.Mhz] = 0.001m,
            },
            [FrequencyUnit.Mhz] = new() {
                [FrequencyUnit.Hz] = 1000000,
                [FrequencyUnit.Khz] = 1000,
                [FrequencyUnit.Mhz] = 1,
            },
        };        
        
    }
}
