namespace Atc.Grains.Tests.Samples;

public class SampleGrainTwo : AbstractGrain<SampleGrainTwo.GrainState>
{
    public static readonly string TypeString = nameof(SampleGrainTwo);

    public record GrainState(
        decimal Value 
    );

    public record GrainActivationEvent(
        string GrainId,
        decimal Value
    ) : IGrainActivationEvent<SampleGrainTwo>;

    public SampleGrainTwo(
        string grainId, 
        ISiloEventDispatch dispatch, 
        GrainState initialState) : 
        base(grainId, TypeString, dispatch, initialState)
    {
    }

    public decimal Value => State.Value;

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        return stateBefore;
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<SampleGrainTwo, GrainActivationEvent>(
            TypeString, 
            (activation, context) => new SampleGrainTwo(
                grainId: activation.GrainId,
                dispatch: context.Resolve<ISiloEventDispatch>(),
                initialState: new GrainState(Value: activation.Value)
            ));
    }
}

