namespace Atc.Maths;

public readonly struct GroundLocation
{
    public readonly GeoPoint Position;
    public readonly Altitude Elevation;

    public GroundLocation(GeoPoint position, Altitude elevation)
    {
        Position = position;
        Elevation = elevation;
    }

    public bool Equals(GroundLocation other)
    {
        return Position.Equals(other.Position) && Elevation.Equals(other.Elevation);
    }

    public override bool Equals(object? obj)
    {
        return obj is GroundLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Elevation);
    }

    public static GroundLocation Create(double lat, double lon, float elevationFeetMsl)
    {
        return new GroundLocation(new GeoPoint(lat, lon), Altitude.FromFeetMsl(elevationFeetMsl));
    }
}
