namespace Atc.Maths;

public static class Timestamped
{
    public static Timestamped<T> Create<T>(T value, DateTime utc)
    {
        return new Timestamped<T>(value, utc);
    }
}

public readonly struct Timestamped<T>
{
    public readonly T Value;
    public readonly DateTime Utc;

    public Timestamped(T value, DateTime utc)
    {
        Value = value;
        Utc = utc;
    }

    public bool Equals(Timestamped<T> other)
    {
        return Utc.Equals(other.Utc) && EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is Timestamped<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Utc);
    }

    public static bool operator ==(Timestamped<T> x, Timestamped<T> y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(Timestamped<T> x, Timestamped<T> y)
    {
        return !(x == y);
    }
}
