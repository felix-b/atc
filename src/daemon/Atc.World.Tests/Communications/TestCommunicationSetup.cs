#if false

using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications;

public class TestCommunicationSetup
{
    public TestCommunicationSetup(ISilo silo, MockedGrain<IWorldGrain> world)
    {
        Silo = silo;
        World = world;
    }

    public GrainRef<IGroundStationRadioMediumGrain> TurnOnGroundStation(
        Location location, 
        Frequency frequency, 
        out GrainRef<IRadioStationGrain> groundStationGrain)
    {
        groundStationGrain = Silo.Grains.CreateGrain<RadioStationGrain>(grainId => 
            new RadioStationGrain.GrainActivationEvent(
                grainId, 
                RadioStationType.Ground)
        ).As<IRadioStationGrain>();

        groundStationGrain.Get().TurnOnGroundStation(location, frequency);
        var mediumGrain = groundStationGrain.Get().GroundStationMedium!.Value;
        
        World.Mock.Setup(x => x.TryFindRadioMedium(
            location.Position,
            location.Elevation,
            frequency
        )).Returns(mediumGrain);

        return mediumGrain;
    }
    
    public GrainRef<IRadioStationGrain> TurnOnMobileStation(Location location, Frequency frequency)
    {
        
    }
    
    public ISilo Silo { get; }
    public MockedGrain<IWorldGrain> World { get; }
}

#endif