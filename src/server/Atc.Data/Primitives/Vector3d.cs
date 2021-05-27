using System;

namespace Atc.Data.Primitives
{
    public readonly struct Vector3d : IEquatable<Vector3d>
    {
        private const double Epsilon = 0.00000001;
        
        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(Vector3d other)
        {
            return (
                Math.Abs(X - other.X) < Epsilon && 
                Math.Abs(Y - other.Y) < Epsilon && 
                Math.Abs(Z - other.Z) < Epsilon);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3d other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public readonly double X;
        public readonly double Y;
        public readonly double Z;
    }
}