using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;

namespace Atc.World.Communications;

public interface IRadioStationGrain : IGrainId
{
    void InitGroundMedium(GrainRef<IGroundStationRadioMediumGrain> medium);
    void TuneMobileStation(GeoPoint position, Altitude altitude, Frequency frequency);

    void AddListener(GrainRef<IRadioStationListener> listener, RadioStationListenerMask mask);
    void RemoveListener(GrainRef<IRadioStationListener> listener);

    void BeginTransmission(TransmissionDescription transmission);
    void CompleteTransmission(IntentDescription intent, bool keepPttPressed = false);
    void AbortTransmission();

    void NotifyTransmissionStarted(
        TransmissionDescription transmission, 
        ConversationToken conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting);

    void NotifyTransmissionCompleted(
        TransmissionDescription transmission, 
        ConversationToken conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting,
        IntentDescription intent);

    void NotifyTransmissionAborted(
        TransmissionDescription transmission, 
        ConversationToken conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting);
    
    RadioStationType StationType { get; }
    GroundLocation? GroundLocation { get; }
    
    Frequency TunedFrequency { get; }
    GrainRef<IGroundStationRadioMediumGrain>? Medium { get; }
}

public enum RadioStationType
{
    Ground,
    Mobile
}

public interface IRadioStationListener : IGrainId
{
    void TransceiverStateChanged(
        GrainRef<IRadioStationGrain> station, 
        TransceiverState oldState, 
        TransceiverState newState);
}

[Flags]
public enum RadioStationListenerMask
{
    Transmitter = 0x01,
    Receiver = 0x02,
    Transceiver = Transmitter | Receiver
}

public class RadioStationGrain : 
    AbstractGrain<RadioStationGrain.GrainState>, 
    IRadioStationGrain
{
    public static readonly string TypeString = nameof(RadioStationGrain);

    public record GrainState(
        //TODO
    );

    public record GrainActivationEvent(
        string GrainId
        //TODO
    ) : IGrainActivationEvent<RadioStationGrain>;

    public record SampleEvent(
        //TODO
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;

    public RadioStationGrain(
        ISiloEventDispatch dispatch,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: dispatch,
            initialState: CreateInitialState(activation))
    {
    }

    public void InitGroundMedium(GrainRef<IGroundStationRadioMediumGrain> medium)
    {
        throw new NotImplementedException();
    }

    public void TuneMobileStation(GeoPoint position, Altitude altitude, Frequency frequency)
    {
        throw new NotImplementedException();
    }

    public void AddListener(GrainRef<IRadioStationListener> listener, RadioStationListenerMask mask)
    {
        throw new NotImplementedException();
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

    public void NotifyTransmissionStarted(TransmissionDescription transmission, ConversationToken conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting)
    {
        throw new NotImplementedException();
    }

    public void NotifyTransmissionCompleted(TransmissionDescription transmission, ConversationToken conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting, IntentDescription intent)
    {
        throw new NotImplementedException();
    }

    public void NotifyTransmissionAborted(TransmissionDescription transmission, ConversationToken conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting)
    {
        throw new NotImplementedException();
    }

    public RadioStationType StationType => throw new NotImplementedException();
    public GroundLocation? GroundLocation => throw new NotImplementedException();
    public Frequency TunedFrequency => throw new NotImplementedException();
    public GrainRef<IGroundStationRadioMediumGrain>? Medium => throw new NotImplementedException();

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
        config.RegisterGrainType<RadioStationGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new RadioStationGrain(
                dispatch: context.Resolve<ISiloEventDispatch>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation)
    {
        return new GrainState(
            //TODO
        );
    }
}