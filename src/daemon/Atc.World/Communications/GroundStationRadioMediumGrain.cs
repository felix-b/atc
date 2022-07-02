using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public interface IGroundStationRadioMediumGrain : IGrainId
{
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

public record ConversationToken(
    ulong Id,
    AirGroundPriority Priority
);

public class GroundStationRadioMediumGrain : 
    AbstractGrain<GroundStationRadioMediumGrain.GrainState>,
    IGroundStationRadioMediumGrain
{
    public static readonly string TypeString = nameof(GroundStationRadioMediumGrain);

    public record GrainState(
        GrainRef<IRadioStationGrain> GroundStation,
        ImmutableDictionary<string, GrainRef<IRadioStationGrain>> MobileStationById,
        ImmutableSortedSet<PendingTransmissionQueueEntry> PendingTransmissionQueue,
        ImmutableHashSet<ConversationToken> ConversationsInProgress,
        ImmutableHashSet<string> TransmittingStationIds,
        bool IsSilent,
        DateTime SilenceSinceUtc
    );

    public record PendingTransmissionQueueEntry(
        ConversationToken Token,
        GrainRef<IAIRadioOperatorGrain> Operator
    );

    public record GrainActivationEvent(
        string GrainId,
        GrainRef<IRadioStationGrain> GroundStation
    ) : IGrainActivationEvent<GroundStationRadioMediumGrain>;

    public record SampleEvent(
        //TODO
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;

    private readonly ISiloEnvironment _environment;

    public GroundStationRadioMediumGrain(
        ISiloEventDispatch dispatch,
        ISiloEnvironment environment,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: dispatch,
            initialState: CreateInitialState(activation, environment.UtcNow))
    {
        _environment = environment;
    }

    public void AddMobileStation(GrainRef<IRadioStationGrain> station)
    {
        throw new NotImplementedException();
    }

    public void RemoveMobileStation(GrainRef<IRadioStationGrain> station)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void CancelPendingConversation(ConversationToken token)
    {
        throw new NotImplementedException();
    }

    public GrainRef<IRadioStationGrain> GroundStation => State.GroundStation;
    public GroundLocation AntennaLocation { get; }
    public Frequency Frequency { get; }

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
            default:
                return stateBefore;
        }
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

    private static GrainState CreateInitialState(GrainActivationEvent activation, DateTime utcNow)
    {
        return new GrainState(
            GroundStation: activation.GroundStation,
            MobileStationById: ImmutableDictionary<string, GrainRef<IRadioStationGrain>>.Empty,
            PendingTransmissionQueue: ImmutableSortedSet<PendingTransmissionQueueEntry>.Empty,
            ConversationsInProgress: ImmutableHashSet<ConversationToken>.Empty,
            TransmittingStationIds: ImmutableHashSet<string>.Empty,
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
}
