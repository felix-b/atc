using Atc.Data.Airports;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;

namespace Atc.Data
{
    public struct WorldData
    {
        public ZStringMapRef<ZRef<AircraftTypeData>> TypeByIcao { get; init; }
        public ZStringMapRef<ZRef<AirlineData>> AirlineByIcao { get; init; }
        public ZStringMapRef<ZRef<AirportData>> AirportByIcao { get; init; }
    }
}
