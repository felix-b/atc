using Zero.Serialization.Buffers;
using Zero.Serialization.Buffers.Impl;

namespace Atc.Data.Primitives
{
    public struct GeoPolygon
    {
        public ZVectorRef<Edge> Edges;

        public static GeoPolygon FromEdges(params Edge[] edges)
        {
            return new GeoPolygon() {
                Edges = BufferContext.Current.AllocateVector(edges)
            };
        }
        
        public struct Edge 
        {
            public GeoEdgeType Type;
            public GeoPoint? FromPoint;
            public GeoPoint? ArcOrigin;
            public Distance? ArcDistance;
            public Bearing? ArcBearing;
            
            public static Edge FromCircle(GeoPoint arcOrigin, Distance arcDistance)
            {
                return new Edge() {
                    Type = GeoEdgeType.Circle, 
                    ArcOrigin = arcOrigin, 
                    ArcDistance = arcDistance, 
                    ArcBearing = Bearing.FromTrueDegrees(0)
                };
            }
        }
    }
}
