using System.Collections.Immutable;
using Atc.Grains;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;

namespace Atc.World.Tests.Communications.Poc;

public interface IPocAIRadioOperatorGrain : IAIRadioOperatorGrain, IGrainId
{
    //TODO
}

public class PocAIRadioOperatorGrain : 
    AbstractGrain<PocAIRadioOperatorGrain.GrainState>,
    IStartableGrain,
    IPocAIRadioOperatorGrain,
    IRadioStationListener
{
    public static readonly string TypeString = nameof(PocAIRadioOperatorGrain);

    [NotEventSourced]
    private readonly ISiloEnvironment _environment;
    [NotEventSourced]
    private readonly PocBrain _brain;

    public PocAIRadioOperatorGrain(
        ISilo silo,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation, silo))
    {
        _brain = CreateBrain(activation.Callsign);
        _environment = silo.Environment;
    }

    public void Start()
    {
        InvokeBrain(incomingIntent: null);
        State.Radio.Get().AddListener(GetRefToSelfAs<IRadioStationListener>(), RadioStationListenerMask.Receiver);
    }

    public BeginTransmitNowResponse BeginTransmitNow(ConversationToken conversationToken)
    {
        if (State.Brain.OutgoingIntents.IsEmpty)
        {
            return new BeginTransmitNowResponse(BeganTransmission: false);
        }

        var firstToTransmit = State.Brain.OutgoingIntents[0];
        var transmissionPriority = firstToTransmit.Intent.Header.Priority;
        
        State.Radio.Get().BeginTransmission(TestUtility.NewTransmission(), firstToTransmit.ConversationToken);

        var transmissionDuration = firstToTransmit.Intent is PocIntent pocIntent
            ? pocIntent.PocType.GetTransmissionDuration()
            : TimeSpan.FromSeconds(3);

        Dispatch(new PopOutgoingIntentEvent());
        Defer(
            new CompleteTransmissionWorkItem(firstToTransmit.Intent),
            notEarlierThanUtc: _environment.UtcNow.Add(transmissionDuration));
        
        return new BeginTransmitNowResponse(
            BeganTransmission: true, 
            firstToTransmit.ConversationToken, 
            transmissionPriority);
    }

    public void NotifyTransmissionStarted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken)
    {
        // nothing
    }

    public void NotifyTransmissionCompleted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken, 
        Intent transmittedIntent)
    {
        var tuple = new PocIntentTuple(transmittedIntent, conversationToken, transmittedIntent.Header.Priority);
        InvokeBrain(tuple);
    }

    public void NotifyTransmissionAborted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken)
    {
        // nothing
    }

    public PocBrain Brain => _brain;
    
    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            case CompleteTransmissionWorkItem completeTx:
                State.Radio.Get().CompleteTransmission(completeTx.Intent, keepPttPressed: false);
                EnqueueForTransmissionIfNecessary();
                return true;
            case WakeUpWorkItem:
                InvokeBrain(incomingIntent: null);
                EnqueueForTransmissionIfNecessary();
                return true;
            default:
                return base.ExecuteWorkItem(workItem, timedOut);
        }
    }

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case UpdateBrainStateEvent updateBrain:
                return stateBefore with {
                    Brain = updateBrain.BrainState
                };
            case PopOutgoingIntentEvent:
                return stateBefore with {
                    Brain = stateBefore.Brain with {
                        OutgoingIntents = stateBefore.Brain.OutgoingIntents.RemoveAt(0)
                    }
                }; 
            case EnqueuedForTransmissionEvent enqueued:
                return stateBefore with {
                    Brain = stateBefore.Brain with {
                        OutgoingIntents = InjectConversationToken(stateBefore.Brain.OutgoingIntents, enqueued)
                    },
                    IntentEnqueuedForTransmission = enqueued.Intent
                }; 
            default:
                return stateBefore;
        }

        ImmutableArray<PocIntentTuple> InjectConversationToken(ImmutableArray<PocIntentTuple> source, EnqueuedForTransmissionEvent enqueued)
        {
            var intentBefore = source[0];
            var intentAfter = intentBefore.ConversationToken == enqueued.ConversationToken
                ? intentBefore
                : intentBefore with {
                    ConversationToken = enqueued.ConversationToken
                };

            return intentBefore == intentAfter
                ? source
                : source.SetItem(0, intentAfter);
        }
    }

    private void InvokeBrain(PocIntentTuple? incomingIntent)
    {
        var brainInput = new PocBrainInput(
            Clock: _environment.UtcNow.Subtract(State.StartUtc),
            IncomingIntent: incomingIntent,
            State: State.Brain);

        var brainOutput = _brain.Process(brainInput);
        
        HandleOutput();

        void HandleOutput()
        {
            Dispatch(new UpdateBrainStateEvent(brainOutput.State));
            EnqueueForTransmissionIfNecessary();

            if (brainOutput.WakeUpAtClock.HasValue)
            {
                Defer(
                    new WakeUpWorkItem(),
                    notEarlierThanUtc: State.StartUtc.Add(brainOutput.WakeUpAtClock.Value)
                );
            }
        }
    }

    private void EnqueueForTransmissionIfNecessary()
    {
        var shouldEnqueue =
            !State.Brain.OutgoingIntents.IsEmpty &&
            State.Brain.OutgoingIntents[0].Intent != State.IntentEnqueuedForTransmission;
        if (!shouldEnqueue)
        {
            return;
        }
        
        var firstToTransmit = State.Brain.OutgoingIntents.First();
        // var firstToTransmitWithToken = firstToTransmit.ConversationToken != null
        //     ? firstToTransmit
        //     : firstToTransmit with {
        //         ConversationToken = State.Radio.Get().TakeNewAIConversationToken()
        //     };

        var effectivePriority = firstToTransmit.Priority ?? firstToTransmit.Intent.Header.Priority; 
        var conversationToken = State.Radio.Get().EnqueueAIOperatorForTransmission(
            GetRefToSelfAs<IAIRadioOperatorGrain>(),
            effectivePriority,
            firstToTransmit.ConversationToken);

        Console.WriteLine($"{State.Callsign.Full}> enqueue [{firstToTransmit.Intent}] token [{conversationToken}] priority [{effectivePriority}]");

        Dispatch(new EnqueuedForTransmissionEvent(firstToTransmit.Intent, conversationToken));
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<PocAIRadioOperatorGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new PocAIRadioOperatorGrain(
                silo: context.Resolve<ISilo>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation, ISilo silo)
    {
        return new GrainState(
            StartUtc: silo.Environment.UtcNow,
            Callsign: new Callsign(activation.Callsign, activation.Callsign),
            Radio: activation.Radio,
            Brain: new PocBrainState(
                OutgoingIntents: ImmutableArray<PocIntentTuple>.Empty, 
                ConversationPerCallsign: ImmutableDictionary<string, ConversationToken?>.Empty,
                Step: 0
            ),
            IntentEnqueuedForTransmission: null
        );
    }

    private static PocBrain CreateBrain(string callsign)
    {
        switch (callsign)
        {
            case "A": return new PocBrainA();
            case "B": return new PocBrainB();
            case "C": return new PocBrainC();
            case "D": return new PocBrainD();
            case "Q": return new PocBrainQ();
            default: throw new ArgumentException($"Unexpected callsign: '{callsign}'");
        }
    }

    public record GrainState(
        DateTime StartUtc,
        Callsign Callsign,
        GrainRef<IRadioStationGrain> Radio,
        PocBrainState Brain,
        Intent? IntentEnqueuedForTransmission
    );

    public record GrainActivationEvent(
        string GrainId,
        string Callsign,
        GrainRef<IRadioStationGrain> Radio
    ) : IGrainActivationEvent<PocAIRadioOperatorGrain>;

    public record UpdateBrainStateEvent(
        PocBrainState BrainState
    ) : IGrainEvent;

    public record PopOutgoingIntentEvent : IGrainEvent;

    public record EnqueuedForTransmissionEvent(
        Intent Intent,
        ConversationToken ConversationToken
    ) : IGrainEvent;

    public record CompleteTransmissionWorkItem(
        Intent Intent
    ) : IGrainWorkItem;

    public record WakeUpWorkItem : IGrainWorkItem;
}
