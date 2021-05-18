using System;

namespace Atc.Data.Airports
{
    public enum ParkingStandType
    {
        Gate,
        Hangar,
        Remote,
        Unknown
    }

    public enum TaxiEdgeType
    {
        Groundway,
        Taxiway,
        Runway
    }

    [Flags]
    public enum ActiveZoneTypes
    {
        None = 0,
        Departure = 0x01,
        Arrival = 0x02,
        Traffic = Departure | Arrival,
        Ils = 0x04,
        Any = Departure | Arrival | Ils
    }
}
