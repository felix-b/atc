using FluentAssertions;

namespace Atc.Grains.Tests.Samples;

public class SampleGrainThree : AbstractGrain<SampleGrainThree.GrainState>
{
    public static readonly string TypeString = nameof(SampleGrainThree);
    
    public record GrainState(
        GrainRef<ISampleGrainOne> One,
        GrainRef<ISampleGrainTwo> Two
    );

    public record GrainActivationEvent(
        string GrainId,
        GrainRef<ISampleGrainOne> One,
        GrainRef<ISampleGrainTwo> Two
    ) : IGrainActivationEvent<SampleGrainThree>;

    public SampleGrainThree(
        GrainActivationEvent activation, 
        ISiloEventDispatch dispatch) 
        : base(
            activation.GrainId, 
            TypeString, 
            dispatch, 
            new GrainState(activation.One, activation.Two))
    {
    }

    public string Str => State.One.Get().Str;
    public int Num => State.One.Get().Num;
    public decimal Value => State.Two.Get().Value;

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        return stateBefore;
    }
    
    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<SampleGrainThree, GrainActivationEvent>(
            TypeString, 
            (activation, context) => new SampleGrainThree(
                activation,
                dispatch: context.Resolve<ISiloEventDispatch>()
            ));
    }
}
