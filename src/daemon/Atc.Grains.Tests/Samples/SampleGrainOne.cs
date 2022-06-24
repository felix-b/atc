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

    public int Num => State.Num;
    public string Str => State.Str;

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case ChangeStrEvent changeStr:
                return stateBefore with {
                    Str = changeStr.NewStr
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

