using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.Control
{
    public struct ControllerPositionData
    {
        public ControllerPositionType Type;
        public ZRefAny Facility;
        public ZStringRef CallSign;
        public Frequency Frequency;
        public GeoPolygon Boundary;
        public ZVectorRef<ZRefAny> HandoffControllers;
    }
}
