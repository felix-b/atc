using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;

namespace Atc.World;

public interface IWorldGrain : IGrainId
{
    void AddRadioMedium(GrainRef<IGroundStationRadioMediumGrain> medium);
    GrainRef<IGroundStationRadioMediumGrain>? TryFindRadioMedium(GeoPoint position, Altitude altitude, Frequency frequency);
}

public class WorldGrain : AbstractGrain<WorldGrain.GrainState>, IWorldGrain
{
    public static readonly string TypeString = nameof(WorldGrain);

    public WorldGrain(
        ISiloEventDispatch dispatch,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: dispatch,
            initialState: CreateInitialState(activation))
    {
    }

    public void AddRadioMedium(GrainRef<IGroundStationRadioMediumGrain> medium)
    {
        Dispatch(new AddRadioMediumEvent(medium));
    }

    public GrainRef<IGroundStationRadioMediumGrain>? TryFindRadioMedium(
        GeoPoint position, 
        Altitude altitude, 
        Frequency frequency)
    {
        return State.RadioMediums.First();//TODO
    }

    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            default:
                return base.ExecuteWorkItem(workItem, timedOut);
        }
    }

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case AddRadioMediumEvent addMedium:
                return stateBefore with {
                    RadioMediums = stateBefore.RadioMediums.Add(addMedium.Medium)
                };
            default:
                return stateBefore;
        }
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<WorldGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new WorldGrain(
                dispatch: context.Resolve<ISiloEventDispatch>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation)
    {
        return new GrainState(
            RadioMediums: ImmutableArray<GrainRef<IGroundStationRadioMediumGrain>>.Empty
        );
    }

    public record GrainState(
        //TODO: optimize
        ImmutableArray<GrainRef<IGroundStationRadioMediumGrain>> RadioMediums  
    );

    public record GrainActivationEvent(
        string GrainId
        //TODO
    ) : IGrainActivationEvent<WorldGrain>;

    public record AddRadioMediumEvent(
        GrainRef<IGroundStationRadioMediumGrain> Medium  
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;

}

