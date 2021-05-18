using System;
using Zero.Serialization.Buffers;

namespace Atc.Data.Airports
{
    public struct ActiveZoneData
    {
        public UInt64 ArrivalRunwayMask;
        public UInt64 DepartureRunwayMask;
        public UInt64 IlsRunwayMask;

        public void AddZoneTypes(ZRef<RunwayData> runway, ActiveZoneTypes types)
        {
            var runwayFlag = runway.Get().BitmaskFlag;

            if ((types & ActiveZoneTypes.Arrival) != 0)
            {
                ArrivalRunwayMask |= runwayFlag;
            }
            if ((types & ActiveZoneTypes.Departure) != 0)
            {
                DepartureRunwayMask |= runwayFlag;
            }
            if ((types & ActiveZoneTypes.Ils) != 0)
            {
                IlsRunwayMask |= runwayFlag;
            }
        }

        public ActiveZoneTypes GetZoneTypes(ZRef<RunwayData> runway)
        {
            var runwayFlag = runway.Get().BitmaskFlag;
            var result = ActiveZoneTypes.None;

            if ((ArrivalRunwayMask & runwayFlag) != 0)
            {
                result |= ActiveZoneTypes.Arrival;
            }

            if ((DepartureRunwayMask & runwayFlag) != 0)
            {
                result |= ActiveZoneTypes.Departure;
            }

            if ((IlsRunwayMask & runwayFlag) != 0)
            {
                result |= ActiveZoneTypes.Ils;
            }

            return result;
        }
    }
}
