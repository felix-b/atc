using Zero.Serialization.Buffers;

namespace Atc.Data.Navigation
{
    public struct ControlledAirspaceData
    {
        public int Id;
        public ZStringRef AreaCode;
        public ZStringRef IcaoCode;
        public ZStringRef Name;
        public ZStringRef? CenterName;
        public AirspaceType Type;
        public AirspaceClass Class;
        public AirspaceGeometry Geometry;
        public ZRefAny? ControlFacility;
        public ZRefAny? Airport;
    }
}
