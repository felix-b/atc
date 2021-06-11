using System;
using Atc.Data.Airports;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public struct FlightRouteData
    {
        public ZRef<AirportData> Origin { get; init; }
        public ZRef<AirportData> Destination { get; init; }
        public ZStringRef FlightNo { get; init; }
        public ZRef<AircraftTypeData>? Type { get; init; }
        public ZRef<AircraftData>? Aircraft { get; init; }
        public ZRef<AirlineData>? Airline { get; init; }
        public OperationTypes Operations { get; init; }
        public WeekDays Days { get; init; }
        public TimeSpan DepartureTimeUtc { get; init; }
        public TimeSpan FlightDuration { get; init; }
    }
}
