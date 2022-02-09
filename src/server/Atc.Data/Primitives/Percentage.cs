using System;

namespace Atc.Data.Primitives
{
    public struct Percentage : IEquatable<Percentage>
    {
        private readonly float _unitValue;

        public Percentage(float unitValue)
        {
            if (unitValue < 0.0f || unitValue > 1.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unitValue),"Value must be between 0 and 1");
            }
            
            _unitValue = unitValue;
        }

        public bool Equals(Percentage other)
        {
            return _unitValue.Equals(other._unitValue);
        }

        public override bool Equals(object? obj)
        {
            return obj is Percentage other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _unitValue.GetHashCode();
        }

        public float UnitValue => _unitValue;

        public float PercentValue => _unitValue * 100.0f;

        public static readonly Percentage Zero = new Percentage(0.0f);

        public static readonly Percentage Hundred = new Percentage(1.0f);

        public static bool operator ==(Percentage left, Percentage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Percentage left, Percentage right)
        {
            return !left.Equals(right);
        }
    }
}