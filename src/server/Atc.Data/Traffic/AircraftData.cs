using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct AircraftData
    {
        public ZRef<AircraftTypeData> Type { get; init; }
        public ZStringRef TailNo { get; init; }
        public AircraftCategories Category { get; init; }
        public OperationTypes Operations { get; init; }
        public ZStringRef AirlineIcao { get; init; }
        public ZStringRef LiveryId { get; init; }
    }
}
