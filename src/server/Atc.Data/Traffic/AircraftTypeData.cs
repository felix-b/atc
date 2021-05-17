using Atc.Data.World;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public readonly struct AircraftTypeData
    {
        public readonly StringRef Icao;
        public readonly StringRef Name;
        public readonly StringRef Callsign;
        public readonly AircraftCategories Category;
        public readonly OperationTypes Operations;
        public readonly BufferPtr<FlightModelData> FlightModel;
    }
}
