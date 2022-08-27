using Atc.Grains;
using Atc.World.Airports;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Traffic;

namespace Atc.World.Traffic;

public interface IPilotFlyingGrain : IGrainId
{
    void ReceiveIntent(Intent intent);
    GrainRef<IAircraftGrain> Aircraft { get; }
    FlightPlan FlightPlan { get; }
    GrainRef<IAirportGrain> OriginAirport { get; }
    GrainRef<IAirportGrain> DestinationAirport { get; }
}
