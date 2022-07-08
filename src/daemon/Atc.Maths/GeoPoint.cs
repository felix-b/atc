namespace Atc.Maths;

public readonly struct GeoPoint : IComparable<GeoPoint>
{
    public GeoPoint(double lat, double lon)
    {
        Lat = lat;
        Lon = lon;
    }

    public readonly double Lat;
    public readonly double Lon;

    public int CompareTo(GeoPoint other)
    {
        var latComparison = Lat.CompareTo(other.Lat);
        if (latComparison != 0) return latComparison;
        return Lon.CompareTo(other.Lon);
    }

    public void ToRadians(out float latRadians, out float lonRadians)
    {
        latRadians = Angle.FromDegrees((float) Lat).Radians;
        lonRadians = Angle.FromDegrees((float) Lon).Radians;
    }

    public static bool operator <(GeoPoint left, GeoPoint right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(GeoPoint left, GeoPoint right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(GeoPoint left, GeoPoint right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(GeoPoint left, GeoPoint right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static GeoPoint LatLon(double lat, double lon)
    {
        return new GeoPoint(lat, lon);
    }
}
