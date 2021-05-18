using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct TaxiNodeData
    {
        public int Id;
        public GeoPoint Location;
        public ZStringRef Name;
        public ZVectorRef<ZRef<TaxiEdgeData>> EdgesIn;
        public ZVectorRef<ZRef<TaxiEdgeData>> EdgesOut;
    }
}