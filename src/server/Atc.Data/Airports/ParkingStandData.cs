using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct ParkingStandData
    {
        public int Id;
        public ZStringRef Name;
        public GeoPoint Location;
        public Bearing Direction;
        public ParkingStandType Type;
        public AircraftCategories Categories;
        public OperationTypes Operations;
        public char WidthCode;
        public ZVectorRef<ZRef<AirlineData>> Airlines;
    }
}
