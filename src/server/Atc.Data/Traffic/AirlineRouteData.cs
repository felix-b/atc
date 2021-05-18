using System;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct AirlineRouteData
    {
        public ZStringRef OriginIcao { get; init; }
        public ZStringRef DestinationIcao { get; init; }
        public ZStringRef FlightNo { get; init; }
        public ZRef<AircraftTypeData>? Type { get; init; }
        public ZRef<AircraftData>? Aircraft { get; init; }
        public OperationTypes Operations { get; init; }
        public WeekDays Days { get; init; }
        public TimeSpan DepartureTimeUtc { get; init; }
        public TimeSpan FlightDuration { get; init; }
    }
}
