using System.Collections.Immutable;
using Atc.Telemetry;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.LLLL;

public class LlhzTowerControllerBrain : AIOperatorBrain<LlhzTowerControllerBrain.BrainState>
{
    public LlhzTowerControllerBrain(Callsign callsign, IMyTelemetryBase telemetry) 
        : base(callsign, telemetry)
    {
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

    [TelemetryName("LlhzTowerControllerBrain")]
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}
