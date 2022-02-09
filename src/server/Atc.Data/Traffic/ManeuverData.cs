using System;
using Atc.Data.Primitives;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct ManeuverData
    {
        public ManeuverType Type { get; set; }
        public TimeSpan StartUtc { get; init; }
        public TimeSpan FinishUtc { get; init; }
        public GeoPoint StartPoint { get; init; }
        public GeoPoint FinishPoint { get; init; }
        // TODO: add many other parameters
        public ZRef<ManeuverData>? Next { get; init; } // for composite maneuvers
    }

    public enum ManeuverType
    {
        Mockup = 0,
        Composite = 100
    }
}
