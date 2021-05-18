
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct AirlineData
    {
        public ZStringRef Icao { get; set; }
        public ZStringRef Name { get; set; }
        public ZStringRef Callsign { get; set; }
        public ZStringRef RegionIcao { get; set; }
        public ZVectorRef<ZRef<AirlineRouteData>> Routes { get; set; }
        public ZVectorRef<ZRef<AircraftData>> Fleet { get; set; }
    }
}
