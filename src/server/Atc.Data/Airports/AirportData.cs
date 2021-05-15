using Atc.Data.Buffers;
using Atc.Data.Primitives;

namespace Atc.Data.Airports
{
    public struct AirportData
    {
        public StringRef Icao { get; init; }
        public GeoPoint Datum { get; init; }
    }
}
