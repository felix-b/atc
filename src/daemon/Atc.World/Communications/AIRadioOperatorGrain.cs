using System.Collections.Immutable;
using Atc.Grains;
using Atc.Telemetry;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public abstract class AIRadioOperatorGrain<TBrainState> : 
    AbstractGrain<AIRadioOperatorGrain<TBrainState>.GrainState>,
    IStartableGrain,
    IAIRadioOperatorGrain,
    IRadioStationListener
    where TBrainState : AIOperatorBrainState // use C# record type
{
    [NotEventSourced]
    private readonly ISilo _silo;
    [NotEventSourced]
    private readonly IMyTelemetryBase _telemetry;
    [NotEventSourced]
    private readonly AIOperatorBrain<TBrainState> _brain;
    
    protected AIRadioOperatorGrain(
        ISilo silo,
        IMyTelemetryBase telemetry,
        string grainType,
        AIOperatorBrain<TBrainState> brain,
        PartyDescription party,
        GrainActivationEventBase activation) :
        base(
            grainId: activation.GrainId,
            grainType,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation, silo, brain, party))
    {
        _silo = silo;
        _telemetry = telemetry;
        _brain = brain;
        _brain.OnTakeNextIntentId(() => {
            return State.World.Get().TakeNextIntentId();
        });
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
        var transmissionId = State.World.Get().TakeNextTransmissionId();
        var transmission = new TransmissionDescription(
            Id: transmissionId,
            StartUtc: _silo.Environment.UtcNow,
            Volume: State.Party.Voice.Volume,
            Quality: State.Party.Voice.Quality,
            AudioStreamId: null,
            Duration: null,
            SynthesisRequest: new SpeechSynthesisRequest(
                transmissionId, 
                Originator: GetRefToSelfAs<IAIRadioOperatorGrain>(),
                Intent: firstToTransmit.Intent,
                Language: State.Language 
            ));
        
        State.Radio.Get().BeginTransmission(transmission, firstToTransmit.ConversationToken);

        var estimatedDuration = TimeSpan.FromSeconds(4); //TODO: avoid hard coded values

        var utcNow = _silo.Environment.UtcNow;
        var finishWorkItemHandle = ScheduleEndOfTransmission(transmission.Id, firstToTransmit.Intent, utcNow, estimatedDuration);

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

    private GrainWorkItemHandle ScheduleEndOfTransmission(
        ulong transmissionId, 
        Intent intent, 
        DateTime startUtc, 
        TimeSpan duration)
    {
        var utcNow = _silo.Environment.UtcNow;
        var endUtc = utcNow.Add(duration); 
        var finishWorkItemHandle = Defer(
            new CompleteTransmissionWorkItem(intent),
            notEarlierThanUtc: endUtc);

        _telemetry.ScheduledEndOfTransmission(transmissionId, intentId: intent.Header.Id, duration, startUtc, endUtc);
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
        var tuple = new IntentTuple(transmittedIntent, conversationToken, transmittedIntent.Header.Priority);
        InvokeBrain(tuple);
    }

    public void NotifyTransmissionAborted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken)
    {
        // nothing
    }

    public void NotifyTransmissionDurationAvailable(ulong transmissionId, DateTime startUtc, TimeSpan duration)
    {
        var info = State.MyCurrentTransmission;
        
        if (info != null && info.TransmissionId == transmissionId)
        {
            Silo.TaskQueue.CancelWorkItem(info.FinishWorkItemHandle);
            var newWorkItemHandle = ScheduleEndOfTransmission(transmissionId, info.Intent, startUtc, duration);
            var newInfo = info with {
                FinishWorkItemHandle = newWorkItemHandle
            };
            Dispatch(new UpdatedMyTransmissionDuration(newInfo));
        }
    }

    public AIOperatorBrain<TBrainState> Brain => _brain;
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
                        OutgoingIntents = InjectConversationToken(stateBefore.Brain.OutgoingIntents, enqueued),
                        ConversationPerCallsign = enqueued.Intent.Header.Callee == null
                            ? stateBefore.Brain.ConversationPerCallsign
                            : stateBefore.Brain.ConversationPerCallsign.SetItem(
                                enqueued.Intent.Header.Callee, 
                                enqueued.ConversationToken)
                    },
                    IntentEnqueuedForTransmission = enqueued.Intent
                }; 
            default:
                return stateBefore;
        }

        ImmutableArray<IntentTuple> InjectConversationToken(ImmutableArray<IntentTuple> source, EnqueuedForTransmissionEvent enqueued)
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

    protected void InvokeBrain(IntentTuple? incomingIntent)
    {
        var brainInput = new AIOperatorBrain<TBrainState>.BrainInput(
            UtcNow: _silo.Environment.UtcNow,
            IncomingIntent: incomingIntent,
            State: State.Brain);

        var brainOutput = _brain.Process(brainInput);
        
        HandleOutput();

        void HandleOutput()
        {
            Dispatch(new UpdateBrainStateEvent(brainOutput.State));
            EnqueueForTransmissionIfNecessary();

            if (brainOutput.WakeUpAtUtc.HasValue)
            {
                Defer(
                    new WakeUpWorkItem(),
                    notEarlierThanUtc: brainOutput.WakeUpAtUtc.Value);
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

        _telemetry.EnqueueForTransmission(
            intentId: firstToTransmit.Intent.Header.Id,
            callsignCalled: firstToTransmit.Intent.Header.Callee ?? Callsign.Empty,
            conversationToken: conversationToken,
            priority: effectivePriority);
            
        Dispatch(new EnqueuedForTransmissionEvent(firstToTransmit.Intent, conversationToken));
    }

    private static GrainState CreateInitialState(
        GrainActivationEventBase activation, 
        ISilo silo, 
        AIOperatorBrain<TBrainState> brain,
        PartyDescription party)
    {
        return new GrainState(
            StartUtc: silo.Environment.UtcNow,
            Party: party,
            Callsign: activation.Callsign,
            World: activation.World,
            Radio: activation.Radio,
            Brain: brain.CreateInitialState(),
            IntentEnqueuedForTransmission: null,
            MyCurrentTransmission: null,
            Language: activation.Language
        );
    }

    // private static PersonPartyDescription CreatePartyDescription(GrainActivationEvent activation)
    // {
    //     var key = __nextPartyKey++;
    //     var genders = new[] {GenderType.Male, GenderType.Male, GenderType.Male, GenderType.Female, GenderType.Female};
    //     var voiceTypes = new[] {VoiceType.Bass, VoiceType.Baritone, VoiceType.Tenor, VoiceType.Contralto, VoiceType.Soprano};
    //     var voiceRates = new[] {VoiceRate.Slow, VoiceRate.Medium, VoiceRate.Slow, VoiceRate.Medium, VoiceRate.Fast};
    //     var linkQualities = new[] {VoiceLinkQuality.Good, VoiceLinkQuality.Medium, VoiceLinkQuality.Poor, VoiceLinkQuality.Medium, VoiceLinkQuality.Good};
    //     var volumeLevels = new[] {1.0f, 0.8f, 0.7f, 0.8f, 1.0f};
    //     var ages = new[] {AgeType.Mature, AgeType.Senior, AgeType.Young, AgeType.Young, AgeType.Mature};
    //     var seniorities = new[] {SeniorityType.Senior, SeniorityType.Novice, SeniorityType.Senior, SeniorityType.Novice, SeniorityType.Veteran};
    //     var firstNames = new[] {"Bob", "Michelle", "Peter", "Kelsey", "Kate"};
    //
    //     var voice = new VoiceDescription(
    //         Language: LanguageCode.English,
    //         Gender: genders[key],
    //         Type: voiceTypes[key],
    //         Rate: voiceRates[key],
    //         Quality: linkQualities[key],
    //         Volume: volumeLevels[key],
    //         AssignedPlatformVoiceId: null);
    //     
    //     return new PersonPartyDescription(
    //         uniqueId: activation.GrainId,
    //         NatureType.AI,
    //         voice,
    //         genders[key],
    //         ages[key],
    //         seniorities[key],
    //         firstNames[key]
    //     );
    // }

    public record GrainState(
        DateTime StartUtc,
        PartyDescription Party,
        LanguageCode Language,
        Callsign Callsign,
        GrainRef<IWorldGrain> World,
        GrainRef<IRadioStationGrain> Radio,
        TBrainState Brain,
        Intent? IntentEnqueuedForTransmission,
        MyTransmissionInfo? MyCurrentTransmission
    );

    public record MyTransmissionInfo(
        DateTime StartedAtUtc,
        Intent Intent,
        ulong TransmissionId,
        GrainWorkItemHandle FinishWorkItemHandle
    );

    public record GrainActivationEventBase(
        string GrainId,
        Callsign Callsign,
        GrainRef<IWorldGrain> World,
        GrainRef<IRadioStationGrain> Radio,
        LanguageCode Language
    ) : IGrainActivationEvent<AIRadioOperatorGrain<TBrainState>>;

    public record UpdateBrainStateEvent(
        TBrainState BrainState
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

    public interface IMyTelemetryBase : ITelemetry
    {
        void ScheduledEndOfTransmission(ulong transmissionId, ulong intentId, TimeSpan duration, DateTime startUtc, DateTime endUtc);
        void EnqueueForTransmission(ulong intentId, Callsign callsignCalled, ConversationToken conversationToken, AirGroundPriority priority);
    }
}
