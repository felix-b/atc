using Atc.Data.Primitives;

namespace Atc.Data.Navigation
{
    public struct AirspaceGeometry
    {
        public GeoPolygon LateralBounds;
        public Altitude? LowerBound;
        public Altitude? UpperBound;
    }
}
