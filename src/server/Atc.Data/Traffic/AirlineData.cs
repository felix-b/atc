
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public readonly struct AirlineData
    {
        public readonly StringRef Icao;
        public readonly StringRef Name;
        public readonly StringRef Callsign;
        public readonly StringRef RegionIcao;
        public readonly Vector<AirlineRouteData> Routes;
        public readonly Vector<AircraftData> Fleet;
    }
}
