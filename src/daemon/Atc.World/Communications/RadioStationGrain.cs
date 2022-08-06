using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.Telemetry;
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

    ConversationToken EnqueueAIOperatorForTransmission(
        GrainRef<IAIRadioOperatorGrain> operatorGrain, 
        AirGroundPriority priority,
        ConversationToken? conversationToken = null);

    ConversationToken TakeNewAIConversationToken();

    void BeginTransmission(TransmissionDescription transmission, ConversationToken? conversationToken);
    void CompleteTransmission(Intent intent, bool keepPttPressed = false);
    void AbortTransmission();

    void BeginReceiveTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting, 
        int transmittingStationsCount);

    void EndReceiveCompletedTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting,
        Intent intent);

    void EndReceiveAbortedTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting,
        int transmittingStationsCount);
    
    RadioStationType StationType { get; }
    Location Location { get; }
    Frequency Frequency { get; }
    Callsign Callsign { get; }
    GrainRef<IGroundStationRadioMediumGrain>? GroundStationMedium { get; }
    ITransceiverState TransceiverState { get; }
    bool HasMonitor { get; }

    event Action<ITransceiverState>? TransceiverStateChanged;
    event Action<Intent>? IntentCaptured;
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
        Intent transmittedIntent);

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
    private readonly IMyTelemetry _telemetry;

    public RadioStationGrain(
        ISilo silo,
        IMyTelemetry telemetry,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation, silo))
    {
        _telemetry = telemetry;
        _silo = silo;
    }

    public void TurnOnMobileStation(Location location, Frequency selectedFrequency)
    {
        if (State.Status != TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Mobile station [{GrainId}] is already powered on.");
        }

        PrivateTuneMobileStation(location, selectedFrequency);

        _telemetry.InfoMobileStationTurnedOn(callsign: State.Callsign.Full, grainId: GrainId);
    }

    public void TurnOffMobileStation()
    {
        if (State.StationType != RadioStationType.Mobile)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is not of type Mobile.");
        }
        if (State.Status == TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is already powered off.");
        }

        var refToSelf = GetRefToSelfAs<IRadioStationGrain>();

        if (State.GroundStationMedium.HasValue)
        {
            State.GroundStationMedium.Value.Get().RemoveMobileStation(refToSelf);
        }
        
        Dispatch(new PowerOffEvent());

        _telemetry.InfoMobileStationTurnedOff(callsign: State.Callsign.Full, grainId: GrainId);
    }

    public void TuneMobileStation(Location location, Frequency selectedFrequency)
    {
        if (State.Status == TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Cannot tune mobile station [{GrainId}] as it is powered off.");
        }
        
        PrivateTuneMobileStation(location, selectedFrequency);

        _telemetry.InfoMobileStationTuned(callsign: State.Callsign.Full, frequency: selectedFrequency.Khz, grainId: GrainId);
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

        _telemetry.InfoGroundStationTurnedOn(callsign: State.Callsign.Full, frequency: fixedFrequency.Khz, grainId: GrainId, mediumGrainId: mediumRef.GrainId);

        mediumRef.Get().InitGroundStation(GetRefToSelfAs<IRadioStationGrain>());
        State.World.Get().AddRadioMedium(mediumRef);
    }

    public void AddListener(GrainRef<IRadioStationListener> listener, RadioStationListenerMask mask)
    {
        Dispatch(new AddListenerEvent(
            new RadioStationListenerEntry(listener, mask)
        ));

        _telemetry.VerboseAddedListener(callsign: State.Callsign.Full, listenerGrainId: listener.GrainId, (int)mask);
    }

    public void RemoveListener(GrainRef<IRadioStationListener> listener)
    {
        Dispatch(new RemoveListenerEvent(listener));

        _telemetry.VerboseRemovedListener(callsign: State.Callsign.Full, listenerGrainId: listener.GrainId);
    }

    public ConversationToken EnqueueAIOperatorForTransmission(
        GrainRef<IAIRadioOperatorGrain> operatorGrain, 
        AirGroundPriority priority,
        ConversationToken? conversationToken = null)
    {
        ValidatePoweredOnAndTuned();
        
        _telemetry.VerboseEnqueueAIForTransmission(
            callsign: State.Callsign.Full, 
            priority: (int)priority, 
            conversationTokenId: conversationToken?.Id ?? 0,
            operatorGrainId: operatorGrain.GrainId);

        return State.GroundStationMedium!.Value.Get().EnqueueAIOperatorForTransmission(
            station: GetRefToSelfAs<IRadioStationGrain>(),
            aiOperator: operatorGrain,
            conversationToken: conversationToken,
            priority: priority);
    }

    public ConversationToken TakeNewAIConversationToken()
    {
        ValidatePoweredOnAndTuned();
        return State.GroundStationMedium!.Value.Get().TakeNewAIConversationToken();
    }

    public void BeginTransmission(TransmissionDescription transmission, ConversationToken? conversationToken)
    {
        ValidatePoweredOnAndTuned();
        transmission.ValidateOrThrow(paramName: nameof(transmission));
        
        Dispatch(new BeginTransmissionEvent(transmission, conversationToken));

        _telemetry.VerboseBeginTransmission(
            callsign: State.Callsign.Full, 
            transmissionId: transmission.Id, 
            conversationTokenId: conversationToken?.Id ?? 0);
        
        NotifyListeners(
            RadioStationListenerMask.Transmitter,
            entry => entry.Listener.Get().NotifyTransmissionStarted(
                GetRefToSelfAs<IRadioStationGrain>(), 
                transmission, 
                conversationToken));
    }

    public void CompleteTransmission(Intent intent, bool keepPttPressed = false)
    {
        var transmission = GetCurrentTransmissionOrThrow();
        var conversationToken = State.ConversationToken;
        var transmittingStationsCount = State.GroundStationMedium!.Value.Get().TransmittingStationsCount - 1;
        
        Dispatch(new EndTransmissionEvent(keepPttPressed, transmittingStationsCount));

        _telemetry.VerboseCompleteTransmission(
            callsign: State.Callsign.Full, 
            transmissionId: transmission.Id, 
            conversationTokenId: conversationToken?.Id ?? 0,
            intentId: intent.Header.Id,  
            transmittingStationsCount: transmittingStationsCount,
            keepPttPressed: keepPttPressed);

        NotifyListeners(
            RadioStationListenerMask.Transmitter,
            entry => entry.Listener.Get().NotifyTransmissionCompleted(
                GetRefToSelfAs<IRadioStationGrain>(), 
                transmission, 
                conversationToken,
                intent));
        
        OnIntentCaptured(intent);
    }

    public void AbortTransmission()
    {
        var transmission = GetCurrentTransmissionOrThrow();
        var conversationToken = State.ConversationToken;
        var transmittingStationsCount = State.GroundStationMedium!.Value.Get().TransmittingStationsCount - 1;
        
        Dispatch(new EndTransmissionEvent(KeepPttPressed: false, transmittingStationsCount));

        _telemetry.VerboseAbortTransmission(
            callsign: State.Callsign.Full, 
            transmissionId: transmission.Id, 
            conversationTokenId: conversationToken?.Id ?? 0,
            transmittingStationsCount: transmittingStationsCount);

        NotifyListeners(
            RadioStationListenerMask.Transmitter,
            entry => entry.Listener.Get().NotifyTransmissionAborted(
                GetRefToSelfAs<IRadioStationGrain>(), 
                transmission, 
                conversationToken));
    }

    public void BeginReceiveTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting, 
        int transmittingStationsCount)
    {
        ValidatePoweredOnAndTuned();
        
        Dispatch(new BeginReceiveTransmissionEvent(
            transmission, 
            conversationToken, 
            stationTransmitting,
            transmittingStationsCount));

        _telemetry.VerboseBeginReceiveTransmission(
            callsign: State.Callsign.Full, 
            transmissionId: transmission.Id, 
            stationTransmitting: stationTransmitting.Get().Callsign?.Full ?? string.Empty,
            conversationTokenId: conversationToken?.Id ?? 0,
            transmittingStationsCount: transmittingStationsCount);

        NotifyListeners(
            RadioStationListenerMask.Receiver,
            entry => entry.Listener.Get().NotifyTransmissionStarted(
                stationTransmitting, 
                transmission, 
                conversationToken));
    }

    public void EndReceiveCompletedTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting, 
        Intent intent)
    {
        Dispatch(new EndReceiveTransmissionEvent(
            transmission, 
            conversationToken, 
            stationTransmitting,
            AnotherStationTransmission: null,
            TransmittingStationsCount: 0));

        _telemetry.VerboseEndReceiveCompletedTransmission(
            callsign: State.Callsign.Full,
            transmissionId: transmission.Id,
            conversationTokenId: conversationToken?.Id ?? 0,
            caller: intent.Header.Caller.Full,
            callee: intent.Header.Callee?.Full ?? string.Empty,
            intentId: intent.Header.Id,
            isRecipient: intent.Header.Callee == State.Callsign);

        NotifyListeners(
            RadioStationListenerMask.Receiver,
            entry => entry.Listener.Get().NotifyTransmissionCompleted(
                stationTransmitting, 
                transmission, 
                conversationToken,
                intent));
        
        OnIntentCaptured(intent);
    }

    public void EndReceiveAbortedTransmission(
        TransmissionDescription transmission, 
        ConversationToken? conversationToken,
        GrainRef<IRadioStationGrain> stationTransmitting,
        int transmittingStationsCount)
    {
        var anotherTransmissionInProgress = transmittingStationsCount == 1
            ? State.GroundStationMedium!.Value.Get().SingleTransmission
            : null;
        
        Dispatch(new EndReceiveTransmissionEvent(
            transmission, 
            conversationToken, 
            stationTransmitting, 
            transmittingStationsCount,
            anotherTransmissionInProgress));

        _telemetry.VerboseEndReceiveAbortedTransmission(
            callsign: State.Callsign.Full,
            transmissionId: transmission.Id,
            conversationTokenId: conversationToken?.Id ?? 0,
            stationTransmitting: stationTransmitting.Get().Callsign.Full);

        NotifyListeners(
            RadioStationListenerMask.Receiver,
            entry => entry.Listener.Get().NotifyTransmissionAborted(
                stationTransmitting, 
                transmission, 
                conversationToken));
    }

    public RadioStationType StationType => State.StationType;
    public Location Location => State.LastKnownLocation;
    public Frequency Frequency => State.SelectedFrequency;
    public Callsign Callsign => State.Callsign;
    public GrainRef<IGroundStationRadioMediumGrain>? GroundStationMedium => State.GroundStationMedium;
    public ITransceiverState TransceiverState => State;
    public bool HasMonitor => TransceiverStateChanged != null;
    public event Action<ITransceiverState>? TransceiverStateChanged;
    public event Action<Intent>? IntentCaptured;

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
        TransceiverStatus newStatus;
        
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
                    //Listeners = ImmutableArray<RadioStationListenerEntry>.Empty
                };
            case AddListenerEvent addListener:
                return stateBefore with {
                    Listeners = stateBefore.Listeners.Add(addListener.Entry)
                };
            case RemoveListenerEvent removeListener:
                return stateBefore with {
                    Listeners = stateBefore.Listeners.RemoveAll(entry => entry.Listener == removeListener.Listener)
                };
            case BeginReceiveTransmissionEvent beginReceive:
                newStatus = ReduceTransceiverStatus(
                    oldStatus: stateBefore.Status, 
                    newStatus: TransceiverStatus.ReceivingSingleTransmission, 
                    transmittingStationsCount: beginReceive.TransmittingStationsCount); 
                return stateBefore with {
                    Status = newStatus,
                    CurrentTransmission = newStatus == TransceiverStatus.ReceivingSingleTransmission
                        ? beginReceive.Transmission
                        : null,
                    ConversationToken = newStatus == TransceiverStatus.ReceivingSingleTransmission
                        ? beginReceive.ConversationToken
                        : null, 
                };
            case EndReceiveTransmissionEvent endReceive:
                newStatus = ReduceTransceiverStatus(
                    oldStatus: stateBefore.Status, 
                    newStatus: TransceiverStatus.Silence, 
                    transmittingStationsCount: endReceive.TransmittingStationsCount); 
                return stateBefore with {
                    Status = newStatus,
                    CurrentTransmission = endReceive.AnotherStationTransmission,
                    ConversationToken = endReceive.ConversationToken 
                };
            case BeginTransmissionEvent beginTransmit:
                return stateBefore with {
                    Status = TransceiverStatus.Transmitting,
                    PttPressed = true,
                    CurrentTransmission = beginTransmit.Transmission,
                    ConversationToken = beginTransmit.ConversationToken
                };
            case EndTransmissionEvent endTransmit:
                newStatus = ReduceTransceiverStatus(
                    oldStatus: stateBefore.Status, 
                    newStatus: TransceiverStatus.Silence, 
                    transmittingStationsCount: endTransmit.TransmittingStationsCount); 
                return stateBefore with {
                    Status = newStatus,
                    PttPressed = endTransmit.KeepPttPressed,
                    CurrentTransmission = null
                };
            case PowerOffEvent:
                return stateBefore with {
                    Status = TransceiverStatus.Off,
                    GroundStationMedium = null,
                    PttPressed = false,
                    CurrentTransmission = null,
                    ConversationToken = null,
                    Listeners = ImmutableArray<RadioStationListenerEntry>.Empty
                };
            default:
                return stateBefore;
        }
    }

    protected override void ObserveChanges(GrainState oldState, GrainState newState)
    {
        if (WasTransceiverStateAffected())
        {
            TransceiverStateChanged?.Invoke(newState);
        }

        bool WasTransceiverStateAffected()
        {
            return (
                newState.Status != oldState.Status ||
                newState.SelectedFrequency != oldState.SelectedFrequency ||
                newState.GroundStationMedium != oldState.GroundStationMedium ||
                newState.ConversationToken != oldState.ConversationToken ||
                !ReferenceEquals(newState.CurrentTransmission, oldState.CurrentTransmission));
        }
    }

    private void PrivateTuneMobileStation(Location location, Frequency selectedFrequency)
    {
        if (State.StationType != RadioStationType.Mobile)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is not of type Mobile.");
        }

        var refToSelf = GetRefToSelfAs<IRadioStationGrain>();

        if (State.GroundStationMedium.HasValue)
        {
            State.GroundStationMedium.Value.Get().RemoveMobileStation(refToSelf);
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
            mediumRef.Value.Get().AddMobileStation(refToSelf);
        }
    }

    private void NotifyListeners(RadioStationListenerMask maskFilter, Action<RadioStationListenerEntry> action)
    {
        foreach (var listener in State.Listeners)
        {
            if ((listener.Mask & maskFilter) != maskFilter)
            {
                continue;
            }
            
            //TODO: open telemetry span
            try
            {
                action(listener);
            }
            catch (Exception e)
            {
                //TODO: report to telemetry
                Console.WriteLine(e);
            }
        }
    }

    private TransmissionDescription GetCurrentTransmissionOrThrow()
    {
        var transmission =
            State.CurrentTransmission
            ?? throw new InvalidOperationException("Internal error: State.CurrentTransmission is null");
        return transmission;
    }

    private void ValidatePoweredOnAndTuned()
    {
        if (State.Status == TransceiverStatus.Off)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is powered off");
        }
        if (!State.GroundStationMedium.HasValue)
        {
            throw new InvalidOperationException($"Station [{GrainId}] is not tuned to a medium");
        }
    }

    private TransceiverStatus ReduceTransceiverStatus(
        TransceiverStatus oldStatus, 
        TransceiverStatus newStatus,
        int transmittingStationsCount)
    {
        if (oldStatus == TransceiverStatus.Transmitting && newStatus != TransceiverStatus.Silence)
        {
            return oldStatus;
        }
        
        return transmittingStationsCount > 1 && newStatus != TransceiverStatus.Transmitting
            ? TransceiverStatus.ReceivingInterferenceNoise
            : newStatus;
    }

    private void OnIntentCaptured(Intent intent)
    {
        _telemetry.InfoIntentCaptured(
            callsign: State.Callsign.Full,
            intentId: intent.Header.Id,
            isRecipient: intent.Header.Callee == State.Callsign,
            caller: intent.Header.Caller.Full,
            callee: intent.Header.Callee?.Full ?? string.Empty,
            wellKnownType: intent.Header.WellKnownType.ToString(),
            priority: intent.Header.Priority.ToString());

        IntentCaptured?.Invoke(intent);
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<RadioStationGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new RadioStationGrain(
                silo: context.Resolve<ISilo>(),
                telemetry: context.Resolve<ITelemetryProvider>().GetTelemetry<IMyTelemetry>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation, ISilo silo)
    {
        var state = new GrainState(
            World: silo.GetWorld(),
            StationType: activation.StationType,
            Callsign: activation.Callsign,
            SelectedFrequency: Frequency.FromKhz(0),
            GroundStationMedium: null,
            PttPressed: false,
            Status: TransceiverStatus.Off,
            CurrentTransmission: null,
            ConversationToken: null,
            LastKnownLocation: Location.At(0f, 0f, 0f),
            Listeners: ImmutableArray<RadioStationListenerEntry>.Empty
        );
        return state;
    }

    public record GrainState(
        GrainRef<IWorldGrain> World,
        RadioStationType StationType,
        Callsign Callsign,
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
        RadioStationType StationType,
        Callsign Callsign
    ) : IGrainActivationEvent<RadioStationGrain>;

    public record InitGroundStationEvent(
        Location GroundLocation, 
        Frequency FixedFrequency,
        GrainRef<IGroundStationRadioMediumGrain> Medium
    ) : IGrainEvent;

    public record PowerOffEvent : IGrainEvent;

    public record TuneMobileStationEvent(
        Location Location, 
        Frequency SelectedFrequency,
        GrainRef<IGroundStationRadioMediumGrain>? MediumIfFound
    ) : IGrainEvent;

    public record AddListenerEvent(
        RadioStationListenerEntry Entry
    ) : IGrainEvent;

    public record RemoveListenerEvent(
        GrainRef<IRadioStationListener> Listener
    ) : IGrainEvent;

    public record BeginReceiveTransmissionEvent(
        TransmissionDescription Transmission, 
        ConversationToken? ConversationToken,
        GrainRef<IRadioStationGrain> StationTransmitting,
        int TransmittingStationsCount
    ) : IGrainEvent;

    public record EndReceiveTransmissionEvent(
        TransmissionDescription Transmission, 
        ConversationToken? ConversationToken,
        GrainRef<IRadioStationGrain> StationTransmitting,
        int TransmittingStationsCount,
        TransmissionDescription? AnotherStationTransmission
    ) : IGrainEvent;

    public record BeginTransmissionEvent(
        TransmissionDescription Transmission, 
        ConversationToken? ConversationToken
    ) : IGrainEvent;

    public record EndTransmissionEvent(
        bool KeepPttPressed,
        int TransmittingStationsCount
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;

    [TelemetryName("RadioStation")]
    public interface IMyTelemetry : ITelemetry
    {
        void InfoMobileStationTurnedOn(string callsign, string grainId);
        void InfoMobileStationTurnedOff(string callsign, string grainId);
        void InfoMobileStationTuned(string callsign, int frequency, string grainId);
        void InfoGroundStationTurnedOn(string callsign, int frequency, string grainId, string mediumGrainId);
        void VerboseAddedListener(string callsign, string listenerGrainId, int mask);
        void VerboseRemovedListener(string callsign, string listenerGrainId);
        void VerboseEnqueueAIForTransmission(string callsign, int priority, ulong conversationTokenId, string operatorGrainId);
        void VerboseBeginTransmission(string callsign, ulong transmissionId, ulong conversationTokenId);
        void VerboseCompleteTransmission(string callsign, ulong transmissionId, ulong conversationTokenId, ulong intentId, int transmittingStationsCount, bool keepPttPressed);
        void VerboseAbortTransmission(string callsign, ulong transmissionId, ulong conversationTokenId, int transmittingStationsCount);
        void VerboseBeginReceiveTransmission(string callsign, ulong transmissionId, string stationTransmitting, ulong conversationTokenId, int transmittingStationsCount);
        void VerboseEndReceiveCompletedTransmission(string callsign, ulong transmissionId, ulong conversationTokenId, string caller, string callee, ulong intentId, bool isRecipient);
        void VerboseEndReceiveAbortedTransmission(string callsign, ulong transmissionId, ulong conversationTokenId, string stationTransmitting);
        void InfoIntentCaptured(string callsign, ulong intentId, bool isRecipient, string caller, string callee, string wellKnownType, string priority);
    }
}