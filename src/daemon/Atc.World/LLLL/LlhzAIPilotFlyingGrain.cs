using Atc.Grains;
using Atc.World.Airports;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Traffic;
using Atc.World.Traffic;

namespace Atc.World.LLLL;

public class LlhzAIPilotFlyingGrain : 
    AbstractGrain<LlhzAIPilotFlyingGrain.GrainState>, 
    IPilotFlyingGrain,
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
                Aircraft: State.Aircraft,
                PilotFlying: GetRefToSelfAs<IPilotFlyingGrain>(),
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

    public void ReceiveIntent(Intent intent)
    {
        //
    }

    public GrainRef<IAircraftGrain> Aircraft => State.Aircraft;
    public FlightPlan FlightPlan => State.FlightPlan;
    public GrainRef<IAirportGrain> OriginAirport => State.OriginAirport;
    public GrainRef<IAirportGrain> DestinationAirport => State.DestinationAirport;

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
        var worldObject = activation.World.Get();
        var originAirport = worldObject.FindAirportByIcao(activation.FlightPlan.OriginIcao);
        var destinationAirport = worldObject.FindAirportByIcao(activation.FlightPlan.DestinationIcao);
        
        return new GrainState(
            World: activation.World,
            Aircraft: activation.Aircraft,
            FlightPlan: activation.FlightPlan,
            PilotMonitoring: GrainRef<ILlhzAIPilotMonitoringGrain>.NotInitialized,
            OriginAirport: originAirport,
            DestinationAirport: destinationAirport
        );
    }
    
    public record GrainState(
        GrainRef<IWorldGrain> World,
        GrainRef<IAircraftGrain> Aircraft,
        GrainRef<ILlhzAIPilotMonitoringGrain> PilotMonitoring,
        FlightPlan FlightPlan,
        GrainRef<IAirportGrain> OriginAirport,
        GrainRef<IAirportGrain> DestinationAirport
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
