using System.Collections.Immutable;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.LLLL;

public class LlhzPilotMonitoringBrain : RadioOperatorBrain<LlhzPilotMonitoringBrain.BrainState>
{
    private readonly IMyTelemetry _telemetry;

    public LlhzPilotMonitoringBrain(
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
    ) : RadioOperatorBrainState(OutgoingIntents, ConversationPerCallsign);

    [TelemetryName("LlhzPilotBrain")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
