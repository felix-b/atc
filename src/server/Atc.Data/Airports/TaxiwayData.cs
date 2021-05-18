using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct TaxiwayData
    {
        public ZStringRef Name;
        public ZVectorRef<ZRef<TaxiEdgeData>> Edges12;
        public ZVectorRef<ZRef<TaxiEdgeData>> Edges21;
    }
}
