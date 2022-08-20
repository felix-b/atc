using Atc.Maths;

namespace Atc.World.Contracts.Data;

public interface IAirportData
{
    string Icao { get; }
    GeoPoint Datum { get; }
}
