using Zero.Serialization.Buffers;
using Atc.Data.Primitives;

namespace Atc.Data.Traffic
{
    public struct AircraftTypeData
    {
        public ZStringRef Icao { get; init; }
        public ZStringRef Name { get; init; }
        public ZStringRef Callsign { get; init; }
        public AircraftCategories Category { get; init; }
        public OperationTypes Operations { get; init; }
        public ZRef<FlightModelData> FlightModel { get; init; }
        public int PassengerCapacity { get; init; }
        public Weight CargoCapacity { get; init; }
    }
}
