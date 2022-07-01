using System.Security.AccessControl;

namespace Atc.Grains.Tests.Samples;

public class SampleGrainOne : AbstractGrain<SampleGrainOne.GrainState>
{
    public static readonly string TypeString = nameof(SampleGrainOne);

    public record GrainState(
        int Num, 
        string Str
    );

    public record GrainActivationEvent(
        string GrainId,
        int Num,
        string Str
    ) : IGrainActivationEvent<SampleGrainOne>;

    public record ChangeStrEvent(
        string NewStr
    ) : IGrainEvent;

    public record ChangeNumEvent(
        int NewNum
    ) : IGrainEvent;

    public record MultiplyNumWorkItem(
        int Times
    ) : IGrainWorkItem;

    public record DuplicateStrWorkItem() : IGrainWorkItem;

    public SampleGrainOne(
        string grainId, 
        ISiloEventDispatch dispatch, 
        GrainState initialState) : 
        base(grainId, TypeString, dispatch, initialState)
    {
    }

    public void ChangeStr(string newStr)
    {
        Dispatch(new ChangeStrEvent(newStr));
    }

    public GrainWorkItemHandle RequestMultiplyNum(int times)
    {
        return Defer(new MultiplyNumWorkItem(times));
    }

    public GrainWorkItemHandle DeferredDuplicateStr(DateTime atUtc)
    {
        return Defer(new DuplicateStrWorkItem(), notEarlierThanUtc: atUtc);
    }

    public int Num => State.Num;
    public string Str => State.Str;

    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            case MultiplyNumWorkItem multiplyNum:
                Dispatch(new ChangeNumEvent(NewNum: State.Num * multiplyNum.Times));
                return true;
            case DuplicateStrWorkItem duplicateStr:
                Dispatch(new ChangeStrEvent(NewStr: $"{State.Str}|{State.Str}"));
                return true;
            default:
                return base.ExecuteWorkItem(workItem, timedOut);
        }
    }

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case ChangeStrEvent changeStr:
                return stateBefore with {
                    Str = changeStr.NewStr
                };
            case ChangeNumEvent changeNum:
                return stateBefore with {
                    Num = changeNum.NewNum
                };
            default:
                return stateBefore;
        }
    }
    
    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<SampleGrainOne, GrainActivationEvent>(
            TypeString, 
            (activation, context) => new SampleGrainOne(
                grainId: activation.GrainId,
                dispatch: context.Resolve<ISiloEventDispatch>(),
                initialState: new GrainState(activation.Num, activation.Str)
            ));
    }
}

