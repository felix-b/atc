using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public interface IGroundStationRadioMediumGrain : IGrainId, IRadioStationListener
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
        GrainRef<IRadioStationGrain> station,
        GrainRef<IAIRadioOperatorGrain> aiOperator,
        ConversationToken? conversationToken = null,
        AirGroundPriority? priority = null);

    // Remove registration previously created with EnqueueAIOperatorForTransmission
    void CancelPendingTransmission(GrainRef<IRadioStationGrain> station);

    // Used by AI operator when it decides to transmit a different conversation when its turn arrives 
    ConversationToken TakeNewAIConversationToken();

    // Start next transmission if pending and possible
    void CheckPendingTransmissions();
    
    GrainRef<IRadioStationGrain> GroundStation { get; }
    Location AntennaLocation { get; }
    Frequency Frequency { get; }
    int TransmittingStationsCount { get; }
    TransmissionDescription? SingleTransmission { get; }
}

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
            initialState: CreateInitialState(ownerGrain: null, groundStation: default, environment.UtcNow))
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
                    stationTransmitting: entry.StationTransmitting,
                    transmittingStationsCount: State.InProgressTransmissionByStationId.Count);
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
                        stationTransmitting: entry.StationTransmitting,
                        transmittingStationsCount: 0);
                }
            }
        }
    }

    public void HasLineOfSightTo(GeoPoint position, Altitude altitude)
    {
        throw new NotImplementedException();
    }

    public ConversationToken EnqueueAIOperatorForTransmission(
        GrainRef<IRadioStationGrain> station, 
        GrainRef<IAIRadioOperatorGrain> aiOperator, 
        ConversationToken? conversationToken = null,
        AirGroundPriority? priority = null)
    {
        var effectivePriority = priority ?? AirGroundPriority.FlightSafetyNormal;
        var effectiveToken =
            conversationToken
            ?? new ConversationToken(State.NextConversationTokenId);
        var tookNextConversationId = effectiveToken.Id == State.NextConversationTokenId;

        var entry = new PendingTransmissionQueueEntry(effectiveToken, station, aiOperator, effectivePriority);
        Dispatch(new EnqueuePendingTransmissionEvent(entry, tookNextConversationId));
        SchedulePendingCheckWorkItem();

        return effectiveToken;
    }

    
    public void CancelPendingTransmission(GrainRef<IRadioStationGrain> station)
    {
        Dispatch(new RemovePendingTransmissionEvent(station));
    }

    public ConversationToken TakeNewAIConversationToken()
    {
        var token = new ConversationToken(State.NextConversationTokenId);
        Dispatch(new NextConversationTokenIdTakenEvent());
        return token;
    }

    public void CheckPendingTransmissions()
    {
        var utcNow = _environment.UtcNow;
        
        while (State.IsSilent && !State.PendingTransmissionQueue.IsEmpty)
        {
            var entryToTalk = State.PendingTransmissionQueue.First();
            var requiredSilence = GetRequiredSilenceBeforeTransmission(entryToTalk);
            if (State.SilenceSinceUtc.Add(requiredSilence) > utcNow)
            {
                break;
            }
            
            var response = entryToTalk.Operator.Get().BeginTransmitNow(entryToTalk.Token);
            
            Dispatch(new RemovePendingTransmissionEvent(
                Station: entryToTalk.Station,
                AlsoRemoveInProgressConversation: !response.BeganTransmission));

            if (response.BeganTransmission)
            {
                break;
            }
        }
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
            station.Get().BeginReceiveTransmission(
                transmission, 
                conversationToken, 
                stationTransmitting,
                transmittingStationsCount: State.InProgressTransmissionByStationId.Count);
        }
    }

    public void NotifyTransmissionCompleted(
        GrainRef<IRadioStationGrain> stationTransmitting, 
        TransmissionDescription transmission,
        ConversationToken? conversationToken, 
        Intent transmittedIntent)
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
        
        SchedulePendingCheckWorkItem();

        foreach (var station in GetAllStations(except: stationTransmitting))
        {
            if (!transmissionWasInterfered)
            {
                station.Get().EndReceiveCompletedTransmission(
                    transmission, 
                    conversationToken, 
                    stationTransmitting, 
                    transmittedIntent);
            }
            else
            {
                station.Get().EndReceiveAbortedTransmission(
                    transmission, 
                    conversationToken, 
                    stationTransmitting,
                    transmittingStationsCount: State.InProgressTransmissionByStationId.Count);
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
            
        SchedulePendingCheckWorkItem();

        foreach (var station in GetAllStations(except: stationTransmitting))
        {
            station.Get().EndReceiveAbortedTransmission(
                transmission, 
                conversationToken, 
                stationTransmitting,
                transmittingStationsCount: State.InProgressTransmissionByStationId.Count);
        }
    }

    public GrainRef<IRadioStationGrain> GroundStation => State.GroundStation;
    public Location AntennaLocation => GroundStation.Get().Location;
    public Frequency Frequency => GroundStation.Get().Frequency;
    public int TransmittingStationsCount => State.InProgressTransmissionByStationId.Count;

    public TransmissionDescription? SingleTransmission =>
        State.InProgressTransmissionByStationId.Count == 1
            ? State.InProgressTransmissionByStationId.First().Value.Transmission
            : null;

    protected override bool ExecuteWorkItem(IGrainWorkItem workItem, bool timedOut)
    {
        switch (workItem)
        {
            case CheckPendingTransmissionsWorkItem:
                Dispatch(new PendingCheckScheduleChangedEvent(WorkItemHandle: null));
                CheckPendingTransmissions();
                return true;
            default:
                return base.ExecuteWorkItem(workItem, timedOut);
        }
    }

    protected override GrainState Reduce(GrainState stateBefore, IGrainEvent @event)
    {
        PendingTransmissionQueueEntry? pendingEntry;
        
        switch (@event)
        {
            case InitGroundStationEvent initGround:
                return CreateInitialState(ownerGrain: this, initGround.GroundStation, _environment.UtcNow);
            case AddMobileStationEvent addMobile:
                return stateBefore with {
                    MobileStationById = stateBefore.MobileStationById.Add(addMobile.MobileStation.GrainId, addMobile.MobileStation) 
                };
            case RemoveMobileStationEvent removeMobile:
                //TODO: remove pending 
                return stateBefore with {
                    MobileStationById = stateBefore.MobileStationById.Remove(removeMobile.MobileStation.GrainId),
                    PendingTransmissionQueue = ImmutableSortedSet<PendingTransmissionQueueEntry>.Empty.Union(
                        stateBefore.PendingTransmissionQueue.Where(
                            e => e.Station != removeMobile.MobileStation))
                };
            case EnqueuePendingTransmissionEvent enqueuePending:
                pendingEntry = TryFindTransmissionQueueEntry(stateBefore, enqueuePending.Entry.Station);
                var stateAfter = stateBefore with {
                    PendingTransmissionQueue = pendingEntry != null 
                        ? stateBefore.PendingTransmissionQueue.Remove(pendingEntry).Add(enqueuePending.Entry)
                        : stateBefore.PendingTransmissionQueue.Add(enqueuePending.Entry), 
                    NextConversationTokenId = enqueuePending.TookNextConversationId
                        ? stateBefore.NextConversationTokenId + 1
                        : stateBefore.NextConversationTokenId
                };
                return stateAfter;
            case RemovePendingTransmissionEvent removePending:
                pendingEntry = TryFindTransmissionQueueEntry(stateBefore, removePending.Station);
                return pendingEntry == null
                    ? stateBefore
                    : stateBefore with {
                        PendingTransmissionQueue = stateBefore.PendingTransmissionQueue.Remove(pendingEntry),
                        ConversationsInProgress = removePending.AlsoRemoveInProgressConversation
                            ? stateBefore.ConversationsInProgress.Remove(pendingEntry.Token)
                            : stateBefore.ConversationsInProgress
                    };
            case NextConversationTokenIdTakenEvent:
                return stateBefore with {
                    NextConversationTokenId = stateBefore.NextConversationTokenId + 1
                };
            case PendingCheckScheduleChangedEvent checkScheduled:
                return stateBefore with {
                    PendingCheckWorkItemHandle = checkScheduled.WorkItemHandle
                };
            case TransmissionStartedEvent txStarted:
                pendingEntry = txStarted.ConversationToken != null
                    ? TryFindTransmissionQueueEntry(stateBefore, txStarted.StationTransmitting)
                    : null;
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
                    ConversationsInProgress = txStarted.ConversationToken != null
                        ? stateBefore.ConversationsInProgress.Add(txStarted.ConversationToken)
                        : stateBefore.ConversationsInProgress
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

        static PendingTransmissionQueueEntry? TryFindTransmissionQueueEntry(
            GrainState state, 
            GrainRef<IRadioStationGrain> station)
        {
            return state.PendingTransmissionQueue.FirstOrDefault(e => e.Station == station);
        }
    }

    private void SchedulePendingCheckWorkItem()
    {
        if (State.PendingCheckWorkItemHandle.HasValue)
        {
            Silo.TaskQueue.CancelWorkItem(State.PendingCheckWorkItemHandle.Value);
        }

        if (State.PendingTransmissionQueue.IsEmpty || !State.IsSilent)
        {
            Dispatch(new PendingCheckScheduleChangedEvent(WorkItemHandle: null));
            return;
        }

        var firstToTransmit = State.PendingTransmissionQueue[0];
        var silenceRequirement = GetRequiredSilenceBeforeTransmission(firstToTransmit);
        var currentSilenceDuration = _environment.UtcNow.Subtract(State.SilenceSinceUtc);
        var remainingSilenceDuration = silenceRequirement > currentSilenceDuration
            ? silenceRequirement.Subtract(currentSilenceDuration)
            : TimeSpan.Zero;

        var silenceUntil = _environment.UtcNow.Add(remainingSilenceDuration);
        var newHandle = Defer(new CheckPendingTransmissionsWorkItem(), notEarlierThanUtc: silenceUntil);
        
        Dispatch(new PendingCheckScheduleChangedEvent(newHandle));
    }

    private IEnumerable<GrainRef<IRadioStationGrain>> GetAllStations(GrainRef<IRadioStationGrain>? except = null)
    {
        var result = State.MobileStationById.Values.Append(State.GroundStation);
        return except == null
            ? result
            : result.Where(s => s != except.Value);
    }

    private TimeSpan GetRequiredSilenceBeforeTransmission(PendingTransmissionQueueEntry entry)
    {
        return State.ConversationsInProgress.Contains(entry.Token)
            ? TimeSpan.Zero
            : entry.Priority.RequiredSilenceBeforeNewConversation();
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

    private static GrainState CreateInitialState(
        GroundStationRadioMediumGrain? ownerGrain,
        GrainRef<IRadioStationGrain> groundStation, 
        DateTime utcNow)
    {
        var pendingTransmissionComparer = ownerGrain != null
            ? new PendingTransmissionQueueEntryComparer(ownerGrain)
            : null;
        
        return new GrainState(
            GroundStation: groundStation,
            MobileStationById: ImmutableDictionary<string, GrainRef<IRadioStationGrain>>.Empty,
            PendingTransmissionQueue: ImmutableSortedSet.Create<PendingTransmissionQueueEntry>(pendingTransmissionComparer),
            PendingCheckWorkItemHandle: null,
            ConversationsInProgress: ImmutableHashSet.Create<ConversationToken>(ConversationTokenEqualityComparer.Instance),
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
        GrainWorkItemHandle? PendingCheckWorkItemHandle, 
        ImmutableHashSet<ConversationToken> ConversationsInProgress,
        ImmutableDictionary<string, InProgressTransmissionEntry> InProgressTransmissionByStationId,
        bool TransmissionWasInterfered,
        ulong NextConversationTokenId,
        bool IsSilent,
        DateTime SilenceSinceUtc
    );

    public record PendingTransmissionQueueEntry(
        ConversationToken Token,
        GrainRef<IRadioStationGrain> Station,
        GrainRef<IAIRadioOperatorGrain> Operator,
        AirGroundPriority Priority
    );

    public class ConversationTokenEqualityComparer : IEqualityComparer<ConversationToken>
    {
        public bool Equals(ConversationToken? x, ConversationToken? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(ConversationToken obj)
        {
            return obj.Id.GetHashCode();
        }

        public static readonly ConversationTokenEqualityComparer Instance = new ConversationTokenEqualityComparer();
    }

    public class PendingTransmissionQueueEntryComparer : IComparer<PendingTransmissionQueueEntry>
    {
        private readonly GroundStationRadioMediumGrain _ownerGrain;

        public PendingTransmissionQueueEntryComparer(GroundStationRadioMediumGrain ownerGrain)
        {
            _ownerGrain = ownerGrain;
        }

        public int Compare(PendingTransmissionQueueEntry? x, PendingTransmissionQueueEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            var xPriority = (int)x.Priority;
            var yPriority = (int)y.Priority;
            var xIsGround = xPriority == (int)AirGroundPriority.GroundToAir;
            var yIsGround = yPriority == (int)AirGroundPriority.GroundToAir;
            if (xIsGround != yIsGround)
            {
                return xIsGround ? -1 : 1;
            }
            
            var ownerState = _ownerGrain.State;
            var xInProgress = ownerState.ConversationsInProgress.Contains(x.Token);
            var yInProgress = ownerState.ConversationsInProgress.Contains(y.Token);
            if (xInProgress != yInProgress)
            {
                return xInProgress ? -1 : 1;
            }

            if (xPriority != yPriority)
            {
                return xPriority < yPriority ? -1 : 1;
            }

            return x.Token.Id < y.Token.Id ? -1 : 1;
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
        PendingTransmissionQueueEntry Entry,
        bool TookNextConversationId
    ) : IGrainEvent;

    public record NextConversationTokenIdTakenEvent : IGrainEvent;

    public record PendingCheckScheduleChangedEvent(
        GrainWorkItemHandle? WorkItemHandle
    ) : IGrainEvent;

    public record RemovePendingTransmissionEvent(
        GrainRef<IRadioStationGrain> Station,
        bool AlsoRemoveInProgressConversation = false
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

    public record CheckPendingTransmissionsWorkItem : IGrainWorkItem;
}