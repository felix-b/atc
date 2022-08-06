using System.Collections.Immutable;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public abstract class AIOperatorBrain<TState> 
    where TState : AIOperatorBrainState // use C# record type
{
    [NotEventSourced]
    private readonly IMyTelemetryBase _telemetry;
    [NotEventSourced]
    private Func<ulong> _onTakeNextIntentId = () => 0;

    protected AIOperatorBrain(Callsign callsign, IMyTelemetryBase telemetry)
    {
        _telemetry = telemetry;
        MyCallsign = callsign;
    }

    public abstract TState CreateInitialState();
    
    public BrainOutput Process(BrainInput input)
    {
        using var traceSpan = _telemetry.SpanProcess(
            hasIncomingIntent: input.IncomingIntent != null,
            stateOutgoingIntents: input.State.OutgoingIntents.Length,
            stateConversations: input.State.ConversationPerCallsign.Count);
        
        if (input.IncomingIntent != null)
        {
            var header = input.IncomingIntent.Intent.Header;
            _telemetry.DebugProcessIncomingIntent(
                id: header.Id, 
                caller: header.Caller, 
                wellKnownType: header.WellKnownType,
                type: input.IncomingIntent.Intent.GetType().Name);
        }

        try
        {
            OnBeforeProcess(input);
            var output = OnProcess(input);
            OnAfterProcess(input, output);            
            
            return output;
        }
        catch (Exception e)
        {
            throw _telemetry.ExceptionFailedToProcess(exception: e);
        }
    }

    public void OnTakeNextIntentId(Func<ulong> callback)
    {
        _onTakeNextIntentId = callback;
    }

    public Callsign MyCallsign { get; }

    protected abstract BrainOutput OnProcess(BrainInput input);

    protected virtual void OnBeforeProcess(BrainInput input)
    {
    }

    protected virtual void OnAfterProcess(BrainInput input, BrainOutput output)
    {
    }

    protected ulong TakeNextIntentId()
    {
        return _onTakeNextIntentId();
    }

    public record BrainInput(
        DateTime UtcNow,
        TState State,
        IntentTuple? IncomingIntent
    );

    public record BrainOutput(
        TState State,
        DateTime? WakeUpAtUtc = null
    ); 

    public interface IMyTelemetryBase : ITelemetry
    {
        void DebugProcessIncomingIntent(ulong id, Callsign caller, WellKnownIntentType wellKnownType, string type);
        ITraceSpan SpanProcess(bool hasIncomingIntent, int stateOutgoingIntents, int stateConversations);
        Exception ExceptionFailedToProcess(Exception exception);
    }
}

public enum OutgoingIntentMergeOption
{
    Add,
    RemoveOtherForCallsign,
    RemoveAllOther
}

public abstract record AIOperatorBrainState(
    ImmutableArray<IntentTuple> OutgoingIntents,
    ImmutableDictionary<Callsign, ConversationToken?> ConversationPerCallsign)
{
    public static void MergeOutgoingIntent(
        AIOperatorBrainState stateBefore,
        IntentTuple newIntent,
        out ImmutableArray<IntentTuple> intentsAfter,
        out ImmutableDictionary<Callsign, ConversationToken?> conversationsAfter)
    {
        var newIntentCallee = newIntent.Intent.Header.Callee;
        ImmutableArray<IntentTuple> effectiveIntentsBefore;
        ImmutableDictionary<Callsign, ConversationToken?> effectiveConversationsBefore;

        switch (newIntent.MergeOption ?? OutgoingIntentMergeOption.Add)
        {
            case OutgoingIntentMergeOption.RemoveOtherForCallsign when newIntentCallee != null:
                effectiveIntentsBefore = stateBefore.OutgoingIntents.RemoveAll(item => item.Intent.Header.Callee == newIntentCallee);
                effectiveConversationsBefore = stateBefore.ConversationPerCallsign.Remove(newIntentCallee);
                break;
            case OutgoingIntentMergeOption.RemoveAllOther:
                effectiveIntentsBefore = ImmutableArray<IntentTuple>.Empty;
                effectiveConversationsBefore = ImmutableDictionary<Callsign, ConversationToken?>.Empty;
                break;
            default:
                effectiveIntentsBefore = stateBefore.OutgoingIntents;
                effectiveConversationsBefore = stateBefore.ConversationPerCallsign;
                break;
        }

        intentsAfter = effectiveIntentsBefore.Add(newIntent);
        conversationsAfter = newIntentCallee != null
            ? effectiveConversationsBefore.SetItem(newIntentCallee, newIntent.ConversationToken)
            : effectiveConversationsBefore;
    }
}

public record IntentTuple(
    Intent Intent,
    ConversationToken? ConversationToken = null,
    AirGroundPriority? Priority = null,
    OutgoingIntentMergeOption? MergeOption = null)
{
    // public static implicit operator IntentTuple(Intent intent)
    // {
    //     return new IntentTuple(intent);
    // }
}

public static class AIOperatorBrainExtensions
{
    public static T WithOutgoingIntent<T>(
        this T state,
        IntentTuple tuple)
        where T : AIOperatorBrainState
    {
        AIOperatorBrainState.MergeOutgoingIntent(
            state, 
            tuple, 
            out var intentsAfter, 
            out var conversationsAfter);

        return state with {
            OutgoingIntents = intentsAfter,
            ConversationPerCallsign = conversationsAfter
        };
    }
}
