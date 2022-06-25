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

    public record ChangeValueEvent(
        decimal NewValue
    ) : IGrainEvent;

    public SampleGrainTwo(
        string grainId, 
        ISiloEventDispatch dispatch, 
        GrainState initialState) : 
        base(grainId, TypeString, dispatch, initialState)
    {
    }

    public void ChangeValue(decimal newValue)
    {
        Dispatch(new ChangeValueEvent(newValue));
    }

    public decimal Value => State.Value;

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case ChangeValueEvent changeValue:
                return stateBefore with {
                    Value = changeValue.NewValue
                };
            default:
                return stateBefore;
        }
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

