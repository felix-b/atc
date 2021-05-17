using Atc.Data.World;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public readonly struct AircraftData
    {
        public readonly BufferPtr<AircraftTypeData> Type;
        public readonly StringRef TailNo;
        public readonly AircraftCategories Category;
        public readonly OperationTypes Operations;
        public readonly BufferPtr<AirlineData>? Airline;
        public readonly StringRef LiveryId;
    }
}
