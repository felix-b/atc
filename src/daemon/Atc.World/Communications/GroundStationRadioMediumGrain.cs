using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public interface IGroundStationRadioMediumGrain : IGrainId
{
    void InitGroundStation(GrainRef<IRadioStationGrain> groundStation);
    
    void AddMobileStation(GrainRef<IRadioStationGrain> station);
    void RemoveMobileStation(GrainRef<IRadioStationGrain> station);
    void HasLineOfSightTo(GeoPoint position, Altitude altitude);

    // Before an AI radio operator can transmit, it must register with GroundStationRadioMediumGrain, 
    // which manages a priority queue of transmissions. When it's the operator's turn to transmit,
    // it will be called BeginTransmitNow.
    // - aiOperator - the operator being registered
    // - conversationToken - when transmitting as part of existing conversation; obtained from BeginTransmittingNow call
    // - priority - when registering to start a new conversation, or to amend priority of the conversation that continues.
    ConversationToken EnqueueAIOperatorForTransmission(
        GrainRef<IAIRadioOperatorGrain> aiOperator,
        ConversationToken? conversationToken = null,
        AirGroundPriority? priority = null);

    // Remove registration previously created with EnqueueAIOperatorForTransmission
    void CancelPendingConversation(ConversationToken token);
        
    GrainRef<IRadioStationGrain> GroundStation { get; }
    GroundLocation AntennaLocation { get; }
    Frequency Frequency { get; }
}

public interface IAIRadioOperatorGrain : IGrainId
{
    // Called when it's the AI operator's turn to transmit,
    // according to priority queue managed by GroundStationRadioMediumGrain.
    // At this moment the AI operator can either start transmitting
    // by invoking associated IRadioStationGrain.BeginTransmission,
    // or give up the transmission. The returned response must match the action taken.
    // If ActionTaken == None is returned, he AI operator is removed from the queue,
    // and it has to call EnqueueAIOperatorForTransmission again.
    BeginTransmitNowResponse BeginTransmitNow(ConversationToken conversationToken);
}

public record BeginTransmitNowResponse(
    // reflects the action taken by the AI operator
    TransmitNowAction ActionTaken,
    // updates priority of existing conversation, or specifies priority of the new conversation
    AirGroundPriority? NewPriority = null
);

public enum TransmitNowAction
{
    // starting a transmission as part of conversation received in the token
    ContinueConversation,
    // starting a transmission which is a part of a new conversation 
    StartNewConversation,
    // giving up the turn to transmit
    None
}

public class GroundStationRadioMediumGrain : 
    AbstractGrain<GroundStationRadioMediumGrain.GrainState>,
    IGroundStationRadioMediumGrain,
    IRadioStationListener
{
    public static readonly string TypeString = nameof(GroundStationRadioMediumGrain);

    [NotEventSourced]
    private readonly ISiloEnvironment _environment;

    public GroundStationRadioMediumGrain(
        ISiloEventDispatch dispatch,
        ISiloEnvironment environment,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: dispatch,
            initialState: CreateInitialState(groundStation: default, environment.UtcNow))
    {
        _environment = environment;
    }

    public void InitGroundStation(GrainRef<IRadioStationGrain> groundStation)
    {
        Dispatch(new InitGroundStationEvent(groundStation));

        groundStation.Get().AddListener(
            GetRefToSelfAs<IRadioStationListener>(), 
            RadioStationListenerMask.Transmitter);
    }

    public void AddMobileStation(GrainRef<IRadioStationGrain> station)
    {
        Dispatch(new AddMobileStationEvent(station));
        station.Get().AddListener(
            GetRefToSelfAs<IRadioStationListener>(), 
            RadioStationListenerMask.Transmitter);
        NotifyCurrentTransmissionsStarted();

        void NotifyCurrentTransmissionsStarted()
        {
            foreach (var entry in State.InProgressTransmissionByStationId.Values)
            {
                station.Get().BeginReceiveTransmission(
                    transmission: entry.Transmission,
                    conversationToken: entry.ConversationToken,
                    stationTransmitting: entry.StationTransmitting);
            }
        }
    }

    public void RemoveMobileStation(GrainRef<IRadioStationGrain> station)
    {
        NotifyCurrentTransmissionsAborted();
        Dispatch(new RemoveMobileStationEvent(station));
        station.Get().RemoveListener(GetRefToSelfAs<IRadioStationListener>());

        void NotifyCurrentTransmissionsAborted()
        {
            foreach (var entry in State.InProgressTransmissionByStationId.Values)
            {
                if (entry.StationTransmitting == station)
                {
                    station.Get().AbortTransmission();
                }
                else
                {
                    station.Get().EndReceiveAbortedTransmission(
                        transmission: entry.Transmission,
                        conversationToken: entry.ConversationToken,
                        stationTransmitting: entry.StationTransmitting);
                }
            }
        }
    }

    public void HasLineOfSightTo(GeoPoint position, Altitude altitude)
    {
        throw new NotImplementedException();
    }

    public ConversationToken EnqueueAIOperatorForTransmission(
        GrainRef<IAIRadioOperatorGrain> aiOperator, 
        ConversationToken? conversationToken = null,
        AirGroundPriority? priority = null)
    {
        var token = 
            conversationToken 
            ?? new ConversationToken(State.NextConversationTokenId, priority ?? AirGroundPriority.None);
        var entry = new PendingTransmissionQueueEntry(token, aiOperator);
        Dispatch(new EnqueuePendingTransmissionEvent(entry));
        return token;
    }

    public void CancelPendingConversation(ConversationToken token)
    {
        throw new NotImplementedException();
    }

    public void NotifyTransmissionStarted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken)
    {
        Dispatch(new TransmissionStartedEvent(
            StationTransmitting: stationTransmitting,
            Transmission: transmission, 
            ConversationToken: conversationToken
        ));

        foreach (var station in GetAllStations(except: stationTransmitting))
        {
            station.Get().BeginReceiveTransmission(transmission, conversationToken, stationTransmitting);
        }
    }

    public void NotifyTransmissionCompleted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken, 
        IntentDescription transmittedIntent)
    {
        // check before dispatching TransmissionEndedEvent, as the event may reset the flag
        var transmissionWasInterfered = State.TransmissionWasInterfered; 

        Dispatch(new TransmissionEndedEvent(
            StationTransmitting: stationTransmitting,
            Transmission: transmission, 
            ConversationToken: conversationToken,
            ConversationConcluded: transmittedIntent.ConcludesConversation && !transmissionWasInterfered,
            Utc: _environment.UtcNow
        ));

        foreach (var station in GetAllStations(except: stationTransmitting))
        {
            if (!transmissionWasInterfered)
            {
                station.Get().EndReceiveCompletedTransmission(transmission, conversationToken, stationTransmitting, transmittedIntent);
            }
            else
            {
                station.Get().EndReceiveAbortedTransmission(transmission, conversationToken, stationTransmitting);
            }
        }
    }

    public void NotifyTransmissionAborted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken)
    {
        Dispatch(new TransmissionEndedEvent(
            StationTransmitting: stationTransmitting,
            Transmission: transmission, 
            ConversationToken: conversationToken,
            ConversationConcluded: true,
            Utc: _environment.UtcNow
        ));

        foreach (var station in GetAllStations(except: stationTransmitting))
        {
            station.Get().EndReceiveAbortedTransmission(
                transmission, 
                conversationToken, 
                stationTransmitting);
        }
    }

    public GrainRef<IRadioStationGrain> GroundStation => State.GroundStation;
    public GroundLocation AntennaLocation => GroundStation.Get().GroundLocation.GetValueOrDefault();
    public Frequency Frequency => GroundStation.Get().TunedFrequency;

    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            default:
                return base.ExecuteWorkItem(workItem, timedOut);
        }
    }

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        switch (@event)
        {
            case InitGroundStationEvent initGround:
                return CreateInitialState(initGround.GroundStation, _environment.UtcNow);
            case AddMobileStationEvent addMobile:
                return stateBefore with {
                    MobileStationById = stateBefore.MobileStationById.Add(addMobile.MobileStation.GrainId, addMobile.MobileStation) 
                };
            case RemoveMobileStationEvent removeMobile:
                return stateBefore with {
                    MobileStationById = stateBefore.MobileStationById.Remove(removeMobile.MobileStation.GrainId) 
                };
            case EnqueuePendingTransmissionEvent enqueueTx:
                return stateBefore with {
                    PendingTransmissionQueue = stateBefore.PendingTransmissionQueue.Add(enqueueTx.Entry),
                    NextConversationTokenId = stateBefore.NextConversationTokenId + 1
                };
            case TransmissionStartedEvent txStarted:
                var pendingEntry = TryFindTransmissionQueueEntry(stateBefore, txStarted.ConversationToken);
                return stateBefore with {
                    IsSilent = false,
                    TransmissionWasInterfered = 
                        stateBefore.TransmissionWasInterfered || 
                        !stateBefore.InProgressTransmissionByStationId.IsEmpty, 
                    InProgressTransmissionByStationId = stateBefore.InProgressTransmissionByStationId.Add(
                        txStarted.StationTransmitting.GrainId,
                        new InProgressTransmissionEntry(
                            txStarted.StationTransmitting,
                            txStarted.ConversationToken!,
                            txStarted.Transmission)),
                    PendingTransmissionQueue = pendingEntry != null
                        ? stateBefore.PendingTransmissionQueue.Remove(pendingEntry)
                        : stateBefore.PendingTransmissionQueue,
                    ConversationsInProgress = stateBefore.ConversationsInProgress.Add(txStarted.ConversationToken!) 
                };
            case TransmissionEndedEvent txEnded:
                var newTransmittingStationIds = 
                    stateBefore.InProgressTransmissionByStationId.Remove(txEnded.StationTransmitting.GrainId); 
                return stateBefore with {
                    IsSilent = newTransmittingStationIds.IsEmpty,
                    TransmissionWasInterfered = 
                        stateBefore.TransmissionWasInterfered && 
                        !newTransmittingStationIds.IsEmpty,
                    InProgressTransmissionByStationId = newTransmittingStationIds,
                    ConversationsInProgress = txEnded.ConversationConcluded 
                        ? stateBefore.ConversationsInProgress.Remove(txEnded.ConversationToken!)
                        : stateBefore.ConversationsInProgress,
                    SilenceSinceUtc = txEnded.Utc,
                };
            default:
                return stateBefore;
        }

        static PendingTransmissionQueueEntry? TryFindTransmissionQueueEntry(GrainState state, ConversationToken? token)
        {
            return token != null
                ? state.PendingTransmissionQueue.FirstOrDefault(e => e.Token.Id == token.Id)
                : null;
        }
    }

    private IEnumerable<GrainRef<IRadioStationGrain>> GetAllStations(GrainRef<IRadioStationGrain>? except = null)
    {
        var result = State.MobileStationById.Values.Append(State.GroundStation);
        return except == null
            ? result
            : result.Where(s => s != except.Value);
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<GroundStationRadioMediumGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new GroundStationRadioMediumGrain(
                dispatch: context.Resolve<ISiloEventDispatch>(),
                environment: context.Resolve<ISiloEnvironment>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainRef<IRadioStationGrain> groundStation, DateTime utcNow)
    {
        return new GrainState(
            GroundStation: groundStation,
            MobileStationById: ImmutableDictionary<string, GrainRef<IRadioStationGrain>>.Empty,
            PendingTransmissionQueue: ImmutableSortedSet<PendingTransmissionQueueEntry>.Empty,
            ConversationsInProgress: ImmutableHashSet<ConversationToken>.Empty,
            InProgressTransmissionByStationId: ImmutableDictionary<string, InProgressTransmissionEntry>.Empty, 
            TransmissionWasInterfered: false,
            NextConversationTokenId: 1,
            IsSilent: true,
            SilenceSinceUtc: utcNow);
    }
    
    //------- Pending transmission queue priority order ---------
    // (1) Ground station? goes first
    // (2) Transmission related to an in-progress conversation? goes first
    // (3) Compare AirGroundPriority
    // (4) Compare Ids (chronological order)

    //------- Silence duration rules ---------
    // (1) ground station? no delay
    // (2) transmission related to an in-progress conversation? no delay
    // (3) delay according to AirGroundPriority
    public record GrainState(
        GrainRef<IRadioStationGrain> GroundStation,
        ImmutableDictionary<string, GrainRef<IRadioStationGrain>> MobileStationById,
        ImmutableSortedSet<PendingTransmissionQueueEntry> PendingTransmissionQueue,
        ImmutableHashSet<ConversationToken> ConversationsInProgress,
        ImmutableDictionary<string, InProgressTransmissionEntry> InProgressTransmissionByStationId,
        bool TransmissionWasInterfered,
        ulong NextConversationTokenId,
        bool IsSilent,
        DateTime SilenceSinceUtc
    );

    public record PendingTransmissionQueueEntry(
        ConversationToken Token,
        GrainRef<IAIRadioOperatorGrain> Operator
    ) : IComparable<PendingTransmissionQueueEntry>, IComparable
    {
        public int CompareTo(PendingTransmissionQueueEntry? other)
        {
            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            return this.Token.Id.CompareTo(other.Token.Id);//TODO
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is PendingTransmissionQueueEntry other 
                ? CompareTo(other) 
                : throw new ArgumentException($"Object must be of type {nameof(PendingTransmissionQueueEntry)}");
        }
    }

    public record InProgressTransmissionEntry(
        GrainRef<IRadioStationGrain> StationTransmitting,
        ConversationToken? ConversationToken,
        TransmissionDescription Transmission
    );

    public record GrainActivationEvent(
        string GrainId
    ) : IGrainActivationEvent<GroundStationRadioMediumGrain>;

    // should be dispatched as part of initialization
    // re-initializes the entire state
    public record InitGroundStationEvent(
        GrainRef<IRadioStationGrain> GroundStation
    ) : IGrainEvent;

    public record AddMobileStationEvent(
        GrainRef<IRadioStationGrain> MobileStation
    ) : IGrainEvent;

    public record RemoveMobileStationEvent(
        GrainRef<IRadioStationGrain> MobileStation
    ) : IGrainEvent;

    public record EnqueuePendingTransmissionEvent(
        PendingTransmissionQueueEntry Entry
    ) : IGrainEvent;

    public record TransmissionStartedEvent(
        GrainRef<IRadioStationGrain> StationTransmitting,
        TransmissionDescription Transmission,
        ConversationToken? ConversationToken
    ) : IGrainEvent;

    public record TransmissionEndedEvent(
        GrainRef<IRadioStationGrain> StationTransmitting,
        TransmissionDescription Transmission,
        ConversationToken? ConversationToken,
        bool ConversationConcluded,
        DateTime Utc
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;
}
