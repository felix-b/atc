
using Atc.Data.Navigation;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct AirlineData
    {
        public ZStringRef Icao { get; set; }
        public ZStringRef Name { get; set; }
        public ZStringRef Callsign { get; set; }
        public ZRef<IcaoRegionData>? Region { get; set; }
    }
}
