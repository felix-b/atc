namespace Atc.Grains.Tests.Samples;

public interface ISampleGrainTwo : IGrainId
{
    void ChangeValue(decimal newValue);
    void ArmDivideBy10WhenGreaterThan100(DateTime? notLaterThanUtc = null);
    decimal Value { get; }
}

public class SampleGrainTwo : AbstractGrain<SampleGrainTwo.GrainState>, ISampleGrainTwo
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

    public void ChangeValue(decimal newValue)
    {
        Dispatch(new ChangeValueEvent(newValue));
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

    protected override bool ShouldExecuteWorkItem(IGrainWorkItem workItem)
    {
        switch (workItem)
        {
            case DivideValueBy10WorkItem:
                return State.Value >= 100.0m;
            default:
                return base.ShouldExecuteWorkItem(workItem);
        }
    }

    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            case DivideValueBy10WorkItem:
                var newValue = timedOut ? -1.0m : State.Value / 10.0m;
                Dispatch(new ChangeValueEvent(newValue));
                return true;
            default:
                return base.ExecuteWorkItem(workItem, timedOut);
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

