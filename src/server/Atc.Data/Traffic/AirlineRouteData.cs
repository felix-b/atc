using System;
using Atc.Data.World;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public readonly struct AirlineRouteData
    {
        public readonly StringRef OriginIcao;
        public readonly StringRef DestinationIcao;
        public readonly StringRef FlightNo;
        public readonly BufferPtr<AircraftTypeData> Type;
        public readonly BufferPtr<AircraftData>? Aircraft;
        public readonly WeekDays Days;
        public readonly TimeSpan DepartureTimeUtc;
        public readonly TimeSpan Duration;
    }
}
