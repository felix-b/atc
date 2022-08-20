using Atc.Grains;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Traffic;
using Atc.World.Traffic;

namespace Atc.World.LLLL;

public interface ILlhzAIPilotFlyingGrain : IGrainId
{
    //TODO
}

public class LlhzAIPilotFlyingGrain : 
    AbstractGrain<LlhzAIPilotFlyingGrain.GrainState>, 
    ILlhzAIPilotFlyingGrain,
    IStartableGrain
{
    public static readonly string TypeString = nameof(LlhzAIPilotFlyingGrain);
    
    [NotEventSourced]
    private readonly ISilo _silo;

    public LlhzAIPilotFlyingGrain(
        ISilo silo,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation))
    {
        _silo = silo;
    }

    public void Start()
    {
        var pilotMonitoring = _silo.Grains.CreateGrain<LlhzAIPilotMonitoringGrain>(
            grainId => new LlhzAIPilotMonitoringGrain.GrainActivationEvent(
                grainId,
                Callsign: State.FlightPlan.Callsign,
                World: State.World,
                Radio: State.Aircraft.Get().Com1Radio)
        );

        Dispatch(new PartsInitializedEvent(
            PilotMonitoring: pilotMonitoring.As<ILlhzAIPilotMonitoringGrain>()
        ));

        if (State.FlightPlan is CrossCountryFlightPlan xcPlan)
        {
            if (xcPlan.FlightRules == FlightRules.Ifr)
            {
                
            }
        }
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
            case PartsInitializedEvent parts:
                return stateBefore with {
                    PilotMonitoring = parts.PilotMonitoring
                };
            default:
                return stateBefore;
        }
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<LlhzAIPilotFlyingGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new LlhzAIPilotFlyingGrain(
                silo: context.Resolve<ISilo>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation)
    {
        return new GrainState(
            World: activation.World,
            Aircraft: activation.Aircraft,
            FlightPlan: activation.FlightPlan,
            PilotMonitoring: GrainRef<ILlhzAIPilotMonitoringGrain>.NotInitialized
        );
    }
    
    public record GrainState(
        GrainRef<IWorldGrain> World,
        GrainRef<IAircraftGrain> Aircraft,
        GrainRef<ILlhzAIPilotMonitoringGrain> PilotMonitoring,
        FlightPlan FlightPlan
    );

    public record GrainActivationEvent(
        string GrainId,
        GrainRef<IWorldGrain> World,
        GrainRef<IAircraftGrain> Aircraft,
        FlightPlan FlightPlan
    ) : IGrainActivationEvent<LlhzAIPilotFlyingGrain>;

    public record PartsInitializedEvent(
        GrainRef<ILlhzAIPilotMonitoringGrain> PilotMonitoring
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;
}
