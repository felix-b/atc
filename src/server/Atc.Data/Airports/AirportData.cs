using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.World.Airports
{
    public struct AirportData
    {
        public StringRef Icao { get; init; }
        public GeoPoint Datum { get; init; }
    }
}
