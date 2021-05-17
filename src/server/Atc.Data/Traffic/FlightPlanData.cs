using System;
using Atc.Data.Primitives;
using Atc.Data.World;
using Zero.Serialization.Buffers;

namespace Atc.Data.Traffic
{
    public readonly struct FlightPlanData
    {
        public readonly StringRef OriginIcao;
        public readonly StringRef DestinationIcao;
        public readonly StringRef TailNo;
        public readonly DateTime DepartureUtc;
        public readonly Altitude CruizeAltitude;
        public readonly StringRef Sid;
        public readonly StringRef SidTransition;
        
        public readonly StringRef StarTransition;
        public readonly StringRef Star;
        public readonly StringRef Approach;
    }
}
