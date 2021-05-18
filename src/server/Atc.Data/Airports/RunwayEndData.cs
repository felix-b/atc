using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct RunwayEndData
    {
        public ZStringRef Name;
        public Bearing Heading;
        public GeoPoint CenterlinePoint;
        public Distance DisplacedThresholdLength;
        public Distance OverrunAreaLength;
        public ZVectorRef<ZRef<TaxiEdgeData>> Edges;
    }
}
