using System.Collections.Immutable;
using System.Data;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Airports;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Traffic;
using Atc.World.Traffic;

namespace Atc.World.LLLL;

public class LlhzPilotMonitoringBrain : RadioOperatorBrain<LlhzPilotMonitoringBrain.BrainState>
{
    [NotEventSourced]
    private readonly IMyTelemetry _telemetry;
    [NotEventSourced]
    private readonly ConversationHandler<BrainInput, BrainOutput> _conversationHandler;
 
    public LlhzPilotMonitoringBrain(
        Callsign callsign, 
        IMyTelemetry telemetry) 
        : base(callsign, telemetry)
    {
        _telemetry = telemetry;
        _conversationHandler = BuildConversationHandler();
    }

    public override BrainState CreateInitialState(BrainActivationData activation)
    {
        var ownerActivation = (LlhzAIPilotMonitoringGrain.GrainActivationEvent)activation.OwnerActivation;
        var pilotFlyingObject = ownerActivation.PilotFlying.Get();
        
        return new BrainState(
            OutgoingIntents: ImmutableArray<IntentTuple>.Empty,
            ConversationPerCallsign: ImmutableDictionary<Callsign, ConversationToken?>.Empty,
            ConversationHandler: ConversationHandler.CreateInitialState(nameof(WhenBeforeRequestStartup)),
            PilotFlying: ownerActivation.PilotFlying,
            Aircraft: ownerActivation.Aircraft, 
            World: ownerActivation.World,
            OriginAirport: pilotFlyingObject.OriginAirport,
            DestinationAirport: pilotFlyingObject.DestinationAirport,
            FlightPlan: pilotFlyingObject.FlightPlan
        );
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        var processed = _conversationHandler.TryProcessInput(
            input.State.ConversationHandler,
            input,
            out var newHandlerState,
            out var contextualOutput);

        var output = contextualOutput ?? new BrainOutput(input.State);
        var conversationHandlerStateChanged = !ReferenceEquals(newHandlerState, input.State.ConversationHandler);

        if (!processed)
        {
            //TODO: log warning?
        }
        
        return !conversationHandlerStateChanged
            ? output 
            : output with {
                State = output.State with {
                    ConversationHandler = newHandlerState
                }
            };
    }

    private ConversationHandler<BrainInput, BrainOutput> BuildConversationHandler()
    {
        var builder = ConversationHandler.CreateBuilder<BrainInput, BrainOutput>();
        builder.Add(nameof(WhenBeforeRequestStartup), WhenBeforeRequestStartup);
        builder.Add(nameof(WhenRequestingStartup), WhenRequestingStartup);
        builder.Add(nameof(WhenGotStartupApproval), WhenGotStartupApproval);
        builder.Add(nameof(WhenRequestingTaxi), WhenRequestingTaxi);
        builder.Add(nameof(WhenTaxiingToRunway), WhenTaxiingToRunway);
        builder.Add(nameof(WhenGotTaxiClearance), WhenGotTaxiClearance);
        builder.Add(nameof(WhenReportingReadyForDeparture), WhenReportingReadyForDeparture);
        builder.Add(nameof(WhenAwaitingLineupApproval), WhenAwaitingLineupApproval);
        builder.Add(nameof(WhenAwaitingTakeoffClearance), WhenAwaitingTakeoffClearance);
        builder.Add(nameof(WhenGotTakeoffClearance), WhenGotTakeoffClearance);
        builder.Add(nameof(WhenTookOff), WhenTookOff);
        builder.Add(nameof(WhenTakeoffAborted), WhenTakeoffAborted);
        builder.Add(nameof(WhenReportingDownwind), WhenReportingDownwind);
        builder.Add(nameof(WhenGotLandingNumber), WhenGotLandingNumber);
        builder.Add(nameof(WhenReportingCircuitsRemaining), WhenReportingCircuitsRemaining);
        builder.Add(nameof(WhenReportingFinal), WhenReportingFinal);
        builder.Add(nameof(WhenAwaitingLandingClearance), WhenAwaitingLandingClearance);
        builder.Add(nameof(WhenGotLandingClearance), WhenGotLandingClearance);
        builder.Add(nameof(WhenGoingAround), WhenGoingAround);
        builder.Add(nameof(WhenTaxiingToParking), WhenTaxiingToParking);
        builder.Add(nameof(WhenCheckingOut), WhenCheckingOut);
        return builder.Build();
    }

    private IEnumerable<ConversationContext.Result> WhenBeforeRequestStartup(BrainInput input)
    {
        var clearance = input.State.OriginAirport.Get().Clearance;

        var aircraftObject = input.State.Aircraft.Get(); 
        aircraftObject.Com1Radio.Get().TuneMobileStation(aircraftObject.LastKnownLocation.Value, clearance.Get().Frequency);

        var newState = input.State.WithOutgoingIntent(new IntentTuple(
            Intent: new ParkedCheckInIntent(
                Header: new IntentHeader(
                    Id: TakeNextIntentId(),
                    WellKnownType: WellKnownIntentType.CheckIn,
                    Priority: AirGroundPriority.FlightSafetyNormal,
                    Caller: MyCallsign,
                    Callee: clearance.Get().Callsign,
                    IntentFlags.HasGreeting,
                    ToneTraits.Neutral
                ) 
            )
        ));
        
        yield return ConversationContext.ProvideOutput(new BrainOutput(newState));
        yield return ConversationContext.RemoveContext(nameof(WhenBeforeRequestStartup));
        yield return ConversationContext.AddContext(nameof(WhenRequestingStartup), ConversationContextRelevance.Foreground);
    }

    private IEnumerable<ConversationContext.Result> WhenRequestingStartup(BrainInput input)
    {
        if (input.IncomingIntent?.Intent is GoAheadIntent goAhead)
        {
            
        }
        else
        {
            yield return ConversationContext.GiveUp();
        }
    }

    private IEnumerable<ConversationContext.Result> WhenGotStartupApproval(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenRequestingTaxi(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenTaxiingToRunway(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenGotTaxiClearance(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenReportingReadyForDeparture(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenAwaitingLineupApproval(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenAwaitingTakeoffClearance(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenGotTakeoffClearance(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenTookOff(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenTakeoffAborted(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenReportingDownwind(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenGotLandingNumber(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenReportingCircuitsRemaining(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenReportingFinal(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenAwaitingLandingClearance(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenGotLandingClearance(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenGoingAround(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }

    private IEnumerable<ConversationContext.Result> WhenTaxiingToParking(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }
    
    private IEnumerable<ConversationContext.Result> WhenCheckingOut(BrainInput input)
    {
        yield return ConversationContext.GiveUp();
    }
    
    public record BrainState(
        ImmutableArray<IntentTuple> OutgoingIntents,
        ImmutableDictionary<Callsign, ConversationToken?> ConversationPerCallsign,
        ConversationHandler.ConversationState ConversationHandler,
        GrainRef<IWorldGrain> World,
        GrainRef<IAirportGrain> OriginAirport,
        GrainRef<IAirportGrain> DestinationAirport,
        GrainRef<IAircraftGrain> Aircraft,
        GrainRef<IPilotFlyingGrain> PilotFlying,
        FlightPlan FlightPlan
    ) : RadioOperatorBrainState(OutgoingIntents, ConversationPerCallsign);

    [TelemetryName("LlhzPilotBrain")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
