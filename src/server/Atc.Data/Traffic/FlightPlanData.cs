using System;
using Zero.Serialization.Buffers;
using Atc.Data.Airports;
using Atc.Data.Navigation;
using Atc.Data.Primitives;

namespace Atc.Data.Traffic
{
    public struct FlightPlanData
    {
        public ZRef<AirportData> Origin { get; init; }
        public ZRef<AirportData> Destination { get; init; }
        public ZRefAny Aircraft { get; init; }
        public DateTime DepartureUtc { get; init; }
        public Altitude CruizeAltitude { get; init; }
        public ZRef<SidData>? Sid { get; init; }
        public ZRef<NavaidData>? SidTransition { get; init; }
        public ZRef<NavaidData>? StarTransition { get; init; }
        public ZRef<StarData>? Star { get; init; }
        public ZRef<ApproachData>? Approach { get; init; }
        public ZVectorRef<ZRef<NavaidData>> Waypoints { get; init; }
    }
}
