using System.Collections.Immutable;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications.Poc;

public abstract class PocBrain : RadioOperatorBrain<PocBrainState>
{
    protected PocBrain(
        string callsign, 
        IMyTelemetry telemetry) 
        : base(new Callsign(callsign, callsign), telemetry)
    {
        Telemetry = telemetry;
    }

    protected override void OnBeforeProcess(BrainInput input)
    {
        if (input.IncomingIntent != null)
        {
            AllReceivedIntentsLog.Add(input.IncomingIntent.Intent);
            if (input.IncomingIntent.Intent.Header.Callee == MyCallsign)
            {
                TargetedIntentsLog.Add(input.IncomingIntent.Intent);
            }
        }
    }

    protected override void OnAfterProcess(BrainInput input, BrainOutput output)
    {
        foreach (var intent in output.State.OutgoingIntents.Select(tuple => tuple.Intent))
        {
            if (!OutgoingIntentsLog.Contains(intent))
            {
                OutgoingIntentsLog.Add(intent);
            }
        }
    }

    protected BrainOutput WithOutgoingIntent(BrainInput input, Intent intent, DateTime? wakeUpAtUtc = null, int? step = null)
    {
        return WithOutgoingIntent(input, new IntentTuple(intent), wakeUpAtUtc, step);
    }

    protected BrainOutput WithOutgoingIntent(BrainInput input, IntentTuple tuple, DateTime? wakeUpAtUtc = null, int? step = null)
    {
        RadioOperatorBrainState.MergeOutgoingIntent(
            input.State, 
            tuple, 
            out var intentsAfter, 
            out var conversationsAfter);

        return new BrainOutput(
            input.State with {
                OutgoingIntents = intentsAfter,
                ConversationPerCallsign = conversationsAfter,
                Step = step ?? input.State.Step
            },
            wakeUpAtUtc
        );
    }

    protected BrainOutput WithNoOutgoingIntent(BrainInput input, DateTime? wakeUpAtUtc = null, int? step = null)
    {
        return new BrainOutput(
            input.State with {
                Step = step ?? input.State.Step
            },
            wakeUpAtUtc
        );
    }

    public override PocBrainState CreateInitialState()
    {
        return new PocBrainState(
            OutgoingIntents: ImmutableArray<IntentTuple>.Empty,
            ConversationPerCallsign: ImmutableDictionary<Callsign, ConversationToken?>.Empty,
            Step: 0);
    }

    public List<Intent> AllReceivedIntentsLog { get; } = new();
    public List<Intent> TargetedIntentsLog { get; } = new();
    public List<Intent> OutgoingIntentsLog { get; } = new();

    protected IMyTelemetry Telemetry { get; }
    
    public interface IMyTelemetry : IMyTelemetryBase
    {
    }
}

public record PocBrainState(
    ImmutableArray<IntentTuple> OutgoingIntents,
    ImmutableDictionary<Callsign, ConversationToken?> ConversationPerCallsign,
    int Step
) : RadioOperatorBrainState(OutgoingIntents, ConversationPerCallsign);

// public record PocBrainInput(
//     TimeSpan Clock,
//     PocBrainState State,
//     IntentTuple? IncomingIntent
// ) {
//     public PocBrainOutput WithOutgoingIntent(
//         Intent intent, 
//         ConversationToken? conversationToken = null, 
//         bool clearOtherIntents = false, 
//         int? step = null)
//     {
//         var outgoingIntentsBefore = clearOtherIntents
//             ? State.OutgoingIntents.Clear()
//             : State.OutgoingIntents;
//         
//         return new PocBrainOutput(
//             State: new PocBrainState(
//                 OutgoingIntents: outgoingIntentsBefore.Add(
//                     new IntentTuple(intent, ConversationToken: conversationToken)
//                 ),
//                 ConversationPerCallsign: State.ConversationPerCallsign,
//                 Step: step ?? State.Step
//             )
//         );
//     }
// }
//
// public record PocBrainOutput(
//     PocBrainState State,
//     TimeSpan? WakeUpAtClock = null
// ) {
//     public PocBrainOutput WithMemoizedConversationToken(PocBrainInput input)
//     {
//         if (input.IncomingIntent == null)
//         {
//             return this;
//         }
//         return this with {
//             State = State with {
//                 ConversationPerCallsign = State.ConversationPerCallsign.SetItem(
//                     input.IncomingIntent.Intent.Header.Caller.Full, 
//                     input.IncomingIntent.ConversationToken
//                 )
//             }
//         };
//     }
// }

public class PocBrainA : PocBrain
{
    public PocBrainA(IMyTelemetry telemetry) : base("A", telemetry)
    {
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        if (input.State.Step == 0)
        {
            return WithOutgoingIntent(
                input,
                PocIntent.Create(TakeNextIntentId(), "A", "Q", PocIntentType.I1, false),
                step: 1
            );
        }

        return WithNoOutgoingIntent(input);
    }
}

public class PocBrainB : PocBrain
{
    public PocBrainB(IMyTelemetry telemetry) : base("B", telemetry)
    {
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        if (input.State.Step == 0)
        {
            return WithOutgoingIntent(
                input,
                PocIntent.Create(TakeNextIntentId(), "B", "Q", PocIntentType.I3, false),
                step: 1
            );
        }

        if (input.IncomingIntent != null && input.IncomingIntent.Intent is PocIntent intent)
        {
            if (intent.PocType == PocIntentType.I6)
            {
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(PocIntent.Create(TakeNextIntentId(), "B", "Q", PocIntentType.I7, false), input.IncomingIntent.ConversationToken)
                );
            }
        }

        return WithNoOutgoingIntent(input);
    }
}

public class PocBrainC : PocBrain
{
    public PocBrainC(IMyTelemetry telemetry) : base("C", telemetry)
    {
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        if (input.State.Step == 0)
        {
            return new BrainOutput(
                State: input.State with {
                    Step = 1
                },
                WakeUpAtUtc: input.UtcNow.Add(TimeSpan.FromSeconds(5))
            );
        }
        
        if (input.IncomingIntent == null && input.State.Step == 1)
        {
            return WithOutgoingIntent(
                input,
                PocIntent.Create(TakeNextIntentId(), "C", "Q", PocIntentType.I11, false),
                step: 2
            );
        }

        if (input.State.Step == 2 && input.IncomingIntent != null && input.IncomingIntent.Intent is PocIntent intent)
        {
            if (intent.PocType == PocIntentType.I8)
            {
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(
                        PocIntent.Create(TakeNextIntentId(), "C", "Q", PocIntentType.I9, true), 
                        input.IncomingIntent.ConversationToken,
                        MergeOption: OutgoingIntentMergeOption.RemoveAllOther
                    )
                );
            }
        }

        return WithNoOutgoingIntent(input);
    }
}

public class PocBrainD : PocBrain
{
    public PocBrainD(IMyTelemetry telemetry) : base("D", telemetry)
    {
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        if (input.IncomingIntent != null && input.IncomingIntent.Intent is PocIntent intent)
        {
            if (intent.PocType == PocIntentType.I4)
            {
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(PocIntent.Create(TakeNextIntentId(), "D", "Q", PocIntentType.I5, true), input.IncomingIntent.ConversationToken)
                );
            }
        }

        return WithNoOutgoingIntent(input);
    }
}

public class PocBrainQ : PocBrain
{
    public PocBrainQ(IMyTelemetry telemetry) : base("Q", telemetry)
    {
    }

    protected override BrainOutput OnProcess(BrainInput input)
    {
        if (input.IncomingIntent == null || !(input.IncomingIntent.Intent is PocIntent intent))
        {
            return WithNoOutgoingIntent(input);
        }

        switch (intent.PocType)
        {
            case PocIntentType.I1:
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(
                        PocIntent.Create(TakeNextIntentId(), "Q", "A", PocIntentType.I2, concludesConversation: true, priority: AirGroundPriority.GroundToAir),
                        input.IncomingIntent.ConversationToken
                    )
                );
            case PocIntentType.I3:
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(
                        PocIntent.Create(TakeNextIntentId(), "Q", "D", PocIntentType.I4, concludesConversation: false, priority: AirGroundPriority.GroundToAir),
                        input.IncomingIntent.ConversationToken
                    )
                );
            case PocIntentType.I5:
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(
                        PocIntent.Create(TakeNextIntentId(), "Q", "B", PocIntentType.I6, concludesConversation: false, priority: AirGroundPriority.GroundToAir),
                        TryGetMemoizedConversationToken("B")
                    )
                );
            case PocIntentType.I7:
                return WithOutgoingIntent(
                    input,
                    new IntentTuple(
                        PocIntent.Create(TakeNextIntentId(), "Q", "C", PocIntentType.I8, concludesConversation: false, priority: AirGroundPriority.GroundToAir),
                        TryGetMemoizedConversationToken("C")
                    )
                );
            case PocIntentType.I9:
                return WithOutgoingIntent(
                    input, 
                    new IntentTuple(
                        PocIntent.Create(TakeNextIntentId(), "Q", "B", PocIntentType.I10, concludesConversation: true, priority: AirGroundPriority.GroundToAir),
                        TryGetMemoizedConversationToken("B")
                    )
                );
            default:
                return WithNoOutgoingIntent(input);
        }

        ConversationToken? TryGetMemoizedConversationToken(string callsign)
        {
            if (input.IncomingIntent?.Intent.Header.Caller.Full == callsign && input.IncomingIntent.ConversationToken != null)
            {
                return input.IncomingIntent.ConversationToken;
            }
            
            return input.State.ConversationPerCallsign.TryGetValue(new Callsign(callsign, callsign), out var token)
                ? token
                : null;
        }
    }
}
