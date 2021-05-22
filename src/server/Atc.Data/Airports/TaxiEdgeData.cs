using Zero.Serialization.Buffers;
using Atc.Data.Primitives;

namespace Atc.Data.Airports
{
    public struct TaxiEdgeData
    {
        public int Id;
        public ZStringRef Name;
        public TaxiEdgeType Type;
        public char WidthCode;
        public bool IsOneWay;
        public ZRefAny Node1;
        public ZRefAny Node2;
        public ZRefAny? ReverseEdge;
        public Bearing Heading;
        public Distance Length;
        public ActiveZoneData ActiveZones;
    }

    public static class TaxiEdgeDataExtensions
    {
        public static ZRef<TaxiNodeData> Node1Ref(this ref TaxiEdgeData edge)
        {
            return edge.Node1.AsZRef<TaxiNodeData>();
        }

        public static ZRef<TaxiNodeData> Node2Ref(this ref TaxiEdgeData edge)
        {
            return edge.Node2.AsZRef<TaxiNodeData>();
        }
        
        public static ZRef<TaxiEdgeData>? ReverseEdgeRef(this ref TaxiEdgeData edge)
        {
            return edge.ReverseEdge.HasValue 
                ? edge.ReverseEdge.Value.AsZRef<TaxiEdgeData>() 
                : null;
        }
    }
}
