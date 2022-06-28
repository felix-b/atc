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

    public record DivideValueBy10WorkItem() : IGrainWorkItem;

    public SampleGrainTwo(
        string grainId, 
        ISiloEventDispatch dispatch, 
        GrainState initialState) : 
        base(grainId, TypeString, dispatch, initialState)
    {
    }

    public Task ChangeValue(decimal newValue)
    {
        return Dispatch(new ChangeValueEvent(newValue));
    }

    public void ArmDivideBy10WhenGreaterThan100(DateTime? notLaterThanUtc = null)
    {
        Defer(
            new DivideValueBy10WorkItem(), 
            withPredicate: true, 
            notLaterThanUtc: notLaterThanUtc);
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

    protected override async Task<bool> ShouldExecuteWorkItem(IGrainWorkItem workItem)
    {
        switch (workItem)
        {
            case DivideValueBy10WorkItem:
                return State.Value >= 100.0m;
            default:
                return await base.ShouldExecuteWorkItem(workItem);
        }
    }

    protected override async Task<bool> ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            case DivideValueBy10WorkItem:
                var newValue = timedOut ? -1.0m : State.Value / 10.0m;
                await Dispatch(new ChangeValueEvent(newValue));
                return true;
            default:
                return await base.ExecuteWorkItem(workItem, timedOut);
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

