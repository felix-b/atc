using System.Collections.Immutable;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.LLLL;

public class LlhzPilotBrain : AIOperatorBrain<LlhzPilotBrain.BrainState>
{
    private readonly IMyTelemetry _telemetry;

    public LlhzPilotBrain(
        Callsign callsign, 
        IMyTelemetry telemetry) 
        : base(callsign, telemetry)
    {
        _telemetry = telemetry;
    }

    public override BrainState CreateInitialState()
    {
        return new BrainState(
            OutgoingIntents: ImmutableArray<IntentTuple>.Empty,
            ConversationPerCallsign: ImmutableDictionary<Callsign, ConversationToken?>.Empty);
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        return new BrainOutput(input.State); //TODO
    }

    public record BrainState(
        ImmutableArray<IntentTuple> OutgoingIntents,
        ImmutableDictionary<Callsign, ConversationToken?> ConversationPerCallsign
        //TODO: add more data
    ) : AIOperatorBrainState(OutgoingIntents, ConversationPerCallsign);

    [TelemetryName("LlhzPilotBrain")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
