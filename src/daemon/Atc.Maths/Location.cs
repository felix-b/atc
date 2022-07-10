namespace Atc.Maths;

public readonly struct Location
{
    public readonly GeoPoint Position;
    public readonly Altitude Altitude;
    public Altitude Elevation => Altitude;

    public Location(GeoPoint position, Altitude elevationOrAltitude)
    {
        Position = position;
        Altitude = elevationOrAltitude;
    }

    public bool Equals(Location other)
    {
        return Position.Equals(other.Position) && Altitude.Equals(other.Altitude);
    }

    public override bool Equals(object? obj)
    {
        return obj is Location other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Altitude);
    }

    public static Location At(double lat, double lon, float elevationFt)
    {
        return new Location(new GeoPoint(lat, lon), Altitude.FromFeetMsl(elevationFt));
    }
}
