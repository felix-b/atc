using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public interface IRadioStationGrain : IGrainId
{
    void TurnOnMobileStation(Location location, Frequency selectedFrequency);
    void TurnOffMobileStation();
    void TuneMobileStation(Location location, Frequency selectedFrequency);
    void TurnOnGroundStation(Location groundLocation, Frequency fixedFrequency);

    void AddListener(GrainRef<IRadioStationListener> listener, RadioStationListenerMask mask);
    void RemoveListener(GrainRef<IRadioStationListener> listener);

    void BeginTransmission(TransmissionDescription transmission);
    void CompleteTransmission(IntentDescription intent, bool keepPttPressed = false);
    void AbortTransmission();

    void BeginReceiveTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting);

    void EndReceiveCompletedTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting,
        IntentDescription intent);

    void EndReceiveAbortedTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting);
    
    RadioStationType StationType { get; }
    Location Location { get; }
    Frequency Frequency { get; }
    GrainRef<IGroundStationRadioMediumGrain>? GroundStationMedium { get; }
    ITransceiverState TransceiverState { get; }
}

public interface IRadioStationListener : IGrainId
{
    // void TransceiverStateChanged(
    //     // notifying station
    //     GrainRef<IRadioStationGrain> stationTransmitting,
    //     // previous state
    //     TransceiverState oldState,
    //     // new current state
    //     TransceiverState newState,
    //     // passed when switching from transmitting state, after completing a transmission 
    //     IntentDescription? transmittedIntent);

    void NotifyTransmissionStarted(
        GrainRef<IRadioStationGrain> stationTransmitting,
        TransmissionDescription transmission,
        ConversationToken? conversationToken);

    void NotifyTransmissionCompleted(
        GrainRef<IRadioStationGrain> stationTransmitting,
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        IntentDescription transmittedIntent);

    void NotifyTransmissionAborted(
        GrainRef<IRadioStationGrain> stationTransmitting,
        TransmissionDescription transmission, 
        ConversationToken? conversationToken);
}

[Flags]
public enum RadioStationListenerMask
{
    Transmitter = 0x01,
    Receiver = 0x02,
    Transceiver = Transmitter | Receiver,
}

public class RadioStationGrain : 
    AbstractGrain<RadioStationGrain.GrainState>, 
    IRadioStationGrain
{
    public static readonly string TypeString = nameof(RadioStationGrain);

    private readonly ISilo _silo;//TODO: remove when silo is injected to AbstractGrain 

    public RadioStationGrain(
        ISilo silo,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation, silo))
    {
        _silo = silo;
    }

    public void TurnOnMobileStation(Location location, Frequency selectedFrequency)
    {
        if (State.Status != TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Mobile station [{GrainId}] is already powered on.");
        }

        PrivateTuneMobileStation(location, selectedFrequency);
    }

    public void TurnOffMobileStation()
    {
        throw new NotImplementedException();
    }

    public void TuneMobileStation(Location location, Frequency selectedFrequency)
    {
        if (State.Status == TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Cannot tune mobile station [{GrainId}] as it is powered off.");
        }
        
        PrivateTuneMobileStation(location, selectedFrequency);
    }

    public void TurnOnGroundStation(Location groundLocation, Frequency fixedFrequency)
    {
        if (State.StationType != RadioStationType.Ground)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is not of type Ground.");
        }
        if (State.Status != TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Ground station [{GrainId}] is already powered on.");
        }

        var mediumRef = _silo.Grains.CreateGrain<GroundStationRadioMediumGrain>(
            grainId => new GroundStationRadioMediumGrain.GrainActivationEvent(grainId)
        ).As<IGroundStationRadioMediumGrain>();
        
        Dispatch(new InitGroundStationEvent(groundLocation, fixedFrequency, mediumRef));

        mediumRef.Get().InitGroundStation(GetRefToSelfAs<IRadioStationGrain>());
        State.World.Get().AddRadioMedium(mediumRef);
    }

    public void AddListener(GrainRef<IRadioStationListener> listener, RadioStationListenerMask mask)
    {
        Dispatch(new AddListenerEvent(
            new RadioStationListenerEntry(listener, mask)
        ));
    }

    public void RemoveListener(GrainRef<IRadioStationListener> listener)
    {
        throw new NotImplementedException();
    }

    public void BeginTransmission(TransmissionDescription transmission)
    {
        throw new NotImplementedException();
    }

    public void CompleteTransmission(IntentDescription intent, bool keepPttPressed = false)
    {
        throw new NotImplementedException();
    }

    public void AbortTransmission()
    {
        throw new NotImplementedException();
    }

    public void BeginReceiveTransmission(TransmissionDescription transmission, ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting)
    {
        Dispatch(new BeginReceiveTransmissionEvent(transmission, conversationToken, stationTransmitting));        
    }

    public void EndReceiveCompletedTransmission(TransmissionDescription transmission, ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting, IntentDescription intent)
    {
        throw new NotImplementedException();
    }

    public void EndReceiveAbortedTransmission(TransmissionDescription transmission, ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting)
    {
        throw new NotImplementedException();
    }

    public RadioStationType StationType => State.StationType;
    public Location Location => State.LastKnownLocation;
    public Frequency Frequency => State.SelectedFrequency;
    public GrainRef<IGroundStationRadioMediumGrain>? GroundStationMedium => State.GroundStationMedium;
    public ITransceiverState TransceiverState => State;

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
                return stateBefore with {
                    StationType = RadioStationType.Ground,
                    SelectedFrequency = initGround.FixedFrequency,
                    GroundStationMedium = initGround.Medium,
                    PttPressed = false,
                    Status = TransceiverStatus.Silence,
                    CurrentTransmission = null,
                    ConversationToken = null,
                    LastKnownLocation = initGround.GroundLocation,
                    Listeners = ImmutableArray<RadioStationListenerEntry>.Empty
                };
            case TuneMobileStationEvent tuneMobile:
                return stateBefore with {
                    StationType = RadioStationType.Mobile,
                    SelectedFrequency = tuneMobile.SelectedFrequency,
                    GroundStationMedium = tuneMobile.MediumIfFound,
                    PttPressed = false,
                    Status = tuneMobile.MediumIfFound.HasValue 
                        ? TransceiverStatus.Silence
                        : TransceiverStatus.NoMedium,
                    CurrentTransmission = null,
                    ConversationToken = null,
                    LastKnownLocation = tuneMobile.Location,
                    Listeners = ImmutableArray<RadioStationListenerEntry>.Empty
                };
            case AddListenerEvent addListener:
                return stateBefore with {
                    Listeners = stateBefore.Listeners.Add(addListener.Entry)
                };
            case BeginReceiveTransmissionEvent beginReceive:
                return stateBefore with {
                    Status = TransceiverStatus.ReceivingSingleTransmission,
                    CurrentTransmission = beginReceive.Transmission,
                    ConversationToken = beginReceive.ConversationToken 
                };
            default:
                return stateBefore;
        }
    }

    private void PrivateTuneMobileStation(Location location, Frequency selectedFrequency)
    {
        if (State.StationType != RadioStationType.Mobile)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is not of type Mobile.");
        }

        var mediumRef = State.World.Get().TryFindRadioMedium(
            location.Position,
            location.Altitude,
            selectedFrequency);

        Dispatch(new TuneMobileStationEvent(
            Location: location,
            SelectedFrequency: selectedFrequency,
            MediumIfFound: mediumRef));

        if (mediumRef.HasValue)
        {
            mediumRef.Value.Get().AddMobileStation(GetRefToSelfAs<IRadioStationGrain>());
        }
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<RadioStationGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new RadioStationGrain(
                silo: context.Resolve<ISilo>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation, ISilo silo)
    {
        var state = new GrainState(
            World: silo.GetWorld(),
            StationType: activation.StationType,
            SelectedFrequency: Frequency.FromKhz(0),
            GroundStationMedium: null,
            PttPressed: false,
            Status: TransceiverStatus.Off,
            CurrentTransmission: null,
            ConversationToken: null,
            LastKnownLocation: Location.Create(0f, 0f, 0f),
            Listeners: ImmutableArray<RadioStationListenerEntry>.Empty
        );
        return state;
    }
    
    public record GrainState(
        GrainRef<IWorldGrain> World,
        RadioStationType StationType,
        Frequency SelectedFrequency,
        GrainRef<IGroundStationRadioMediumGrain>? GroundStationMedium,
        bool PttPressed,
        TransceiverStatus Status,
        TransmissionDescription? CurrentTransmission,
        ConversationToken? ConversationToken,//TODO: necessary?
        Location LastKnownLocation,
        ImmutableArray<RadioStationListenerEntry> Listeners
    ) : ITransceiverState;

    public record RadioStationListenerEntry(
        GrainRef<IRadioStationListener> Listener,
        RadioStationListenerMask Mask
    );

    public record GrainActivationEvent(
        string GrainId,
        RadioStationType StationType
    ) : IGrainActivationEvent<RadioStationGrain>;

    public record InitGroundStationEvent(
        Location GroundLocation, 
        Frequency FixedFrequency,
        GrainRef<IGroundStationRadioMediumGrain> Medium
    ) : IGrainEvent;

    public record TuneMobileStationEvent(
        Location Location, 
        Frequency SelectedFrequency,
        GrainRef<IGroundStationRadioMediumGrain>? MediumIfFound
    ) : IGrainEvent;

    public record AddListenerEvent(
        RadioStationListenerEntry Entry
    ) : IGrainEvent;

    public record BeginReceiveTransmissionEvent(
        TransmissionDescription Transmission, 
        ConversationToken? ConversationToken,
        GrainRef<IRadioStationGrain> StationTransmitting    
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;
}