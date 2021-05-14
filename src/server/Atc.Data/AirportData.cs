using Atc.Data.Buffers;
using Atc.Data.Buffers.Impl;

namespace Atc.Data
{
    public struct AirportData
    {
        public StringRef Icao { get; init; }
        public GeoPoint Datum { get; init; }
    }
}
