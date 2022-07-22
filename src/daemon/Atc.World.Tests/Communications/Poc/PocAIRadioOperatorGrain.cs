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
    // [NotEventSourced]
    // private readonly ISpeechService _speechService;
    [NotEventSourced]
    private readonly PocBrain _brain;
    
    public PocAIRadioOperatorGrain(
        ISilo silo,
        //ISpeechService speechService,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation, silo))
    {
        _brain = CreateBrain(activation.Callsign);
        _environment = silo.Environment;
        //_speechService = speechService;
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
        var transmissionId = TestUtility.TakeNextTransmissionId();
        var transmission = new TransmissionDescription(
            Id: transmissionId,
            StartUtc: _environment.UtcNow,
            Volume: State.Party.Voice.Volume,
            Quality: State.Party.Voice.Quality,
            AudioStreamId: null,
            Duration: null,
            SynthesisRequest: new SpeechSynthesisRequest(
                transmissionId, 
                Originator: GetRefToSelfAs<IAIRadioOperatorGrain>(),
                Intent: firstToTransmit.Intent,
                Language: LanguageCode.English //TODO: remove hard-coded value 
            ));
        
        State.Radio.Get().BeginTransmission(transmission, firstToTransmit.ConversationToken);

        var estimatedDuration = firstToTransmit.Intent is PocIntent pocIntent
            ? pocIntent.PocType.GetTransmissionDuration()
            : TimeSpan.FromSeconds(3);

        var utcNow = _environment.UtcNow;
        var finishWorkItemHandle = ScheduleEndOfTransmission(firstToTransmit.Intent, utcNow, estimatedDuration);

        var info = new MyTransmissionInfo(
            StartedAtUtc: utcNow,
            Intent: firstToTransmit.Intent, 
            TransmissionId: transmission.Id, 
            FinishWorkItemHandle: finishWorkItemHandle);

        Dispatch(new StartedMyTransmissionEvent(info));

        return new BeginTransmitNowResponse(
            BeganTransmission: true, 
            firstToTransmit.ConversationToken, 
            transmissionPriority);
    }

    private GrainWorkItemHandle ScheduleEndOfTransmission(Intent intent, DateTime startUtc, TimeSpan duration)
    {
        var finishWorkItemHandle = Defer(
            new CompleteTransmissionWorkItem(intent),
            notEarlierThanUtc: startUtc.Add(duration));
        return finishWorkItemHandle;
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

    public void NotifyTransmissionDurationAvailable(ulong transmissionId, TimeSpan duration)
    {
        var info = State.MyCurrentTransmission;
        
        if (info != null && info.TransmissionId == transmissionId)
        {
            Silo.TaskQueue.CancelWorkItem(info.FinishWorkItemHandle);
            var newWorkItemHandle = ScheduleEndOfTransmission(info.Intent, _environment.UtcNow, duration);
            var newInfo = info with {
                FinishWorkItemHandle = newWorkItemHandle
            };
            Dispatch(new UpdatedMyTransmissionDuration(newInfo));
        }
    }

    public PocBrain Brain => _brain;

    public PartyDescription Party => State.Party;
    
    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            case CompleteTransmissionWorkItem completeTx:
                State.Radio.Get().CompleteTransmission(completeTx.Intent, keepPttPressed: false);
                Dispatch(new FinishedMyTransmissionEvent());
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
            case StartedMyTransmissionEvent startedMyTx:
                return stateBefore with {
                    Brain = stateBefore.Brain with {
                        OutgoingIntents = stateBefore.Brain.OutgoingIntents.RemoveAt(0)
                    },
                    MyCurrentTransmission = startedMyTx.Info
                }; 
            case UpdatedMyTransmissionDuration updatedDuration:
                return stateBefore with {
                    MyCurrentTransmission = updatedDuration.NewInfo
                };
            case FinishedMyTransmissionEvent:
                return stateBefore with {
                    MyCurrentTransmission = null
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

    private static int __nextPartyKey = 0; 
    
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
        var callsign = new Callsign(activation.Callsign, activation.Callsign);
        var party = CreatePartyDescription(activation);

        return new GrainState(
            StartUtc: silo.Environment.UtcNow,
            Party: party,
            Callsign: callsign,
            Radio: activation.Radio,
            Brain: new PocBrainState(
                OutgoingIntents: ImmutableArray<PocIntentTuple>.Empty, 
                ConversationPerCallsign: ImmutableDictionary<string, ConversationToken?>.Empty,
                Step: 0
            ),
            IntentEnqueuedForTransmission: null,
            MyCurrentTransmission: null
        );
    }

    private static PersonPartyDescription CreatePartyDescription(GrainActivationEvent activation)
    {
        var key = __nextPartyKey++;
        var genders = new[] {GenderType.Male, GenderType.Male, GenderType.Male, GenderType.Female, GenderType.Female};
        var voiceTypes = new[] {VoiceType.Bass, VoiceType.Baritone, VoiceType.Tenor, VoiceType.Contralto, VoiceType.Soprano};
        var voiceRates = new[] {VoiceRate.Slow, VoiceRate.Medium, VoiceRate.Slow, VoiceRate.Medium, VoiceRate.Fast};
        var linkQualities = new[] {VoiceLinkQuality.Good, VoiceLinkQuality.Medium, VoiceLinkQuality.Poor, VoiceLinkQuality.Medium, VoiceLinkQuality.Good};
        var volumeLevels = new[] {1.0f, 0.8f, 0.7f, 0.8f, 1.0f};
        var ages = new[] {AgeType.Mature, AgeType.Senior, AgeType.Young, AgeType.Young, AgeType.Mature};
        var seniorities = new[] {SeniorityType.Senior, SeniorityType.Novice, SeniorityType.Senior, SeniorityType.Novice, SeniorityType.Veteran};
        var firstNames = new[] {"Bob", "Michelle", "Peter", "Kelsey", "Kate"};

        var voice = new VoiceDescription(
            Language: LanguageCode.English,
            Gender: genders[key],
            Type: voiceTypes[key],
            Rate: voiceRates[key],
            Quality: linkQualities[key],
            Volume: volumeLevels[key],
            AssignedPlatformVoiceId: null);
        
        return new PersonPartyDescription(
            uniqueId: activation.GrainId,
            NatureType.AI,
            voice,
            genders[key],
            ages[key],
            seniorities[key],
            firstNames[key]
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
        PartyDescription Party,
        Callsign Callsign,
        GrainRef<IRadioStationGrain> Radio,
        PocBrainState Brain,
        Intent? IntentEnqueuedForTransmission,
        MyTransmissionInfo? MyCurrentTransmission
    );

    public record MyTransmissionInfo(
        DateTime StartedAtUtc,
        Intent Intent,
        ulong TransmissionId,
        GrainWorkItemHandle FinishWorkItemHandle
    );

    public record GrainActivationEvent(
        string GrainId,
        string Callsign,
        GrainRef<IRadioStationGrain> Radio
    ) : IGrainActivationEvent<PocAIRadioOperatorGrain>;

    public record UpdateBrainStateEvent(
        PocBrainState BrainState
    ) : IGrainEvent;

    public record StartedMyTransmissionEvent(
        MyTransmissionInfo Info
    ) : IGrainEvent;

    public record FinishedMyTransmissionEvent : IGrainEvent;

    public record UpdatedMyTransmissionDuration(
        MyTransmissionInfo NewInfo
    ) : IGrainEvent;

    public record EnqueuedForTransmissionEvent(
        Intent Intent,
        ConversationToken ConversationToken
    ) : IGrainEvent;

    public record CompleteTransmissionWorkItem(
        Intent Intent
    ) : IGrainWorkItem;

    public record WakeUpWorkItem : IGrainWorkItem;
}
