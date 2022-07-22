using System.Collections.Immutable;
using Atc.World.Contracts.Communications;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Atc.World.Tests.Communications.Poc;

public abstract class PocBrain
{
    protected PocBrain(string callsign)
    {
        MyCallsign = new Callsign(callsign, callsign);
    }

    public PocBrainOutput Process(PocBrainInput input)
    {
        //Console.WriteLine($"--- {this.GetType().Name}.Process @ clock={input.Clock.TotalMilliseconds}ms ---");

        if (input.IncomingIntent != null)
        {
            AllReceivedIntentsLog.Add(input.IncomingIntent.Intent);
            if (input.IncomingIntent.Intent.Header.Callee == MyCallsign)
            {
                TargetedIntentsLog.Add(input.IncomingIntent.Intent);
            }
        }
        
        var output = OnProcess(input);

        foreach (var intent in output.State.OutgoingIntents.Select(tuple => tuple.Intent))
        {
            if (!OutgoingIntentsLog.Contains(intent))
            {
                OutgoingIntentsLog.Add(intent);
            }
        }
        
        return output;
    }

    public Callsign MyCallsign { get; }
    public List<Intent> AllReceivedIntentsLog { get; } = new();
    public List<Intent> TargetedIntentsLog { get; } = new();
    public List<Intent> OutgoingIntentsLog { get; } = new();

    protected abstract PocBrainOutput OnProcess(PocBrainInput input);
}

public record PocBrainState(
    ImmutableArray<PocIntentTuple> OutgoingIntents,
    ImmutableDictionary<string, ConversationToken?> ConversationPerCallsign,
    int Step
);

public record PocBrainInput(
    TimeSpan Clock,
    PocBrainState State,
    PocIntentTuple? IncomingIntent
) {
    public PocBrainOutput WithOutgoingIntent(
        Intent intent, 
        ConversationToken? conversationToken = null, 
        bool clearOtherIntents = false, 
        int? step = null)
    {
        var outgoingIntentsBefore = clearOtherIntents
            ? State.OutgoingIntents.Clear()
            : State.OutgoingIntents;
        
        return new PocBrainOutput(
            State: new PocBrainState(
                OutgoingIntents: outgoingIntentsBefore.Add(
                    new PocIntentTuple(intent, ConversationToken: conversationToken)
                ),
                ConversationPerCallsign: State.ConversationPerCallsign,
                Step: step ?? State.Step
            )
        );
    }
}

public record PocBrainOutput(
    PocBrainState State,
    TimeSpan? WakeUpAtClock = null
) {
    public PocBrainOutput WithMemoizedConversationToken(PocBrainInput input)
    {
        if (input.IncomingIntent == null)
        {
            return this;
        }
        return this with {
            State = State with {
                ConversationPerCallsign = State.ConversationPerCallsign.SetItem(
                    input.IncomingIntent.Intent.Header.Caller.Full, 
                    input.IncomingIntent.ConversationToken
                )
            }
        };
    }
}

public record PocIntentTuple(
    Intent Intent,
    ConversationToken? ConversationToken,
    AirGroundPriority? Priority = null
);

public class PocBrainA : PocBrain
{
    public PocBrainA() : base("A")
    {
    }

    protected override PocBrainOutput OnProcess(PocBrainInput input)
    {
        if (input.State.Step == 0)
        {
            return input.WithOutgoingIntent(
                PocIntent.Create(0, "A", "Q", PocIntentType.I1, false),
                step: 1
            );
        }

        return new PocBrainOutput(input.State);
    }
}

public class PocBrainB : PocBrain
{
    public PocBrainB() : base("B")
    {
    }

    protected override PocBrainOutput OnProcess(PocBrainInput input)
    {
        if (input.State.Step == 0)
        {
            return input.WithOutgoingIntent(
                PocIntent.Create(0, "B", "Q", PocIntentType.I3, false),
                step: 1
            );
        }

        if (input.IncomingIntent != null && input.IncomingIntent.Intent is PocIntent intent)
        {
            if (intent.PocType == PocIntentType.I6)
            {
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "B", "Q", PocIntentType.I7, false),
                    input.IncomingIntent.ConversationToken
                );
            }
        }
        
        return new PocBrainOutput(input.State);
    }
}

public class PocBrainC : PocBrain
{
    public PocBrainC() : base("C")
    {
    }

    protected override PocBrainOutput OnProcess(PocBrainInput input)
    {
        if (input.State.Step == 0)
        {
            return new PocBrainOutput(
                State: input.State with {
                    Step = 1
                },
                WakeUpAtClock: TimeSpan.FromSeconds(5)
            );
        }
        
        if (input.IncomingIntent == null && input.State.Step == 1)
        {
            return input.WithOutgoingIntent(
                PocIntent.Create(0, "C", "Q", PocIntentType.I11, false),
                step: 2
            );
        }

        if (input.State.Step == 2 && input.IncomingIntent != null && input.IncomingIntent.Intent is PocIntent intent)
        {
            if (intent.PocType == PocIntentType.I8)
            {
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "C", "Q", PocIntentType.I9, true),
                    input.IncomingIntent.ConversationToken,
                    clearOtherIntents: true
                );
            }
        }
        
        return new PocBrainOutput(input.State);
    }
}

public class PocBrainD : PocBrain
{
    public PocBrainD() : base("D")
    {
    }

    protected override PocBrainOutput OnProcess(PocBrainInput input)
    {
        if (input.IncomingIntent != null && input.IncomingIntent.Intent is PocIntent intent)
        {
            if (intent.PocType == PocIntentType.I4)
            {
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "D", "Q", PocIntentType.I5, true),
                    input.IncomingIntent.ConversationToken
                );
            }
        }
        
        return new PocBrainOutput(input.State);
    }
}

public class PocBrainQ : PocBrain
{
    public PocBrainQ() : base("Q")
    {
    }

    protected override PocBrainOutput OnProcess(PocBrainInput input)
    {
        if (input.IncomingIntent == null || !(input.IncomingIntent.Intent is PocIntent intent))
        {
            return new PocBrainOutput(input.State);
        }

        switch (intent.PocType)
        {
            case PocIntentType.I1:
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "Q", "A", PocIntentType.I2, concludesConversation: true, priority: AirGroundPriority.GroundToAir),
                    input.IncomingIntent.ConversationToken
                ).WithMemoizedConversationToken(input);
            case PocIntentType.I3:
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "Q", "D", PocIntentType.I4, concludesConversation: false, priority: AirGroundPriority.GroundToAir),
                    conversationToken: null
                ).WithMemoizedConversationToken(input);
            case PocIntentType.I5:
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "Q", "B", PocIntentType.I6, concludesConversation: false, priority: AirGroundPriority.GroundToAir),
                    TryGetMemoizedConversationToken("B")
                ).WithMemoizedConversationToken(input);
            case PocIntentType.I7:
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "Q", "C", PocIntentType.I8, concludesConversation: false, priority: AirGroundPriority.GroundToAir),
                    TryGetMemoizedConversationToken("C")
                ).WithMemoizedConversationToken(input);
            case PocIntentType.I9:
                return input.WithOutgoingIntent(
                    PocIntent.Create(0, "Q", "B", PocIntentType.I10, concludesConversation: true, priority: AirGroundPriority.GroundToAir),
                    TryGetMemoizedConversationToken("B")
                ).WithMemoizedConversationToken(input);
            default:
                return new PocBrainOutput(input.State);
        }

        ConversationToken? TryGetMemoizedConversationToken(string callsign)
        {
            return input.State.ConversationPerCallsign.TryGetValue(callsign, out var token)
                ? token
                : null;
        }
    }
}
