using Atc.Grains;
using Atc.Maths;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;

namespace Atc.World.Traffic;

public interface IAircraftGrain : IGrainId
{
    IAircraftData AircraftData { get; }
    IAircraftTypeData TypeData { get; }
    GrainRef<IRadioStationGrain> Com1Radio { get; }
    string TailNo { get; }
    Timestamped<Location> LastKnownLocation { get; }
}

public class AircraftGrain : AbstractGrain<AircraftGrain.GrainState>, IAircraftGrain, IStartableGrain
{
    public static readonly string TypeString = nameof(AircraftGrain);
    
    [NotEventSourced]
    private readonly ISilo _silo;

    [NotEventSourced]
    private readonly IAircraftTypeData _typeData;

    [NotEventSourced]
    private readonly IAircraftData _aircraftData;

    public AircraftGrain(
        ISilo silo,
        IAviationDatabase database,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation))
    {
        _silo = silo;
        _aircraftData = database.GetAircraftData(activation.TailNo);
        _typeData = database.GetAircraftTypeData(_aircraftData.TypeIcao);
    }

    public void Start()
    {
        var callsign = new Callsign(
            Full: State.TailNo, 
            Short: State.TailNo.Substring(State.TailNo.Length - 3, 3));
        
        var com1Radio = _silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(grainId, RadioStationType.Mobile, callsign));

        com1Radio.Get().TurnOnMobileStation(Location.At(32.180d, 34.834d, 100.0f), Frequency.FromKhz(0));
        
        Dispatch(new PartsInitializedEvent(
            RadioCom1: com1Radio.As<IRadioStationGrain>()
        ));
    }

    
    public IAircraftData AircraftData => _aircraftData;

    public IAircraftTypeData TypeData => _typeData;
    
    public GrainRef<IRadioStationGrain> Com1Radio => State.RadioCom1;

    public string TailNo => _aircraftData.TailNo;

    public Timestamped<Location> LastKnownLocation => State.LastKnownLocation;

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
            case PartsInitializedEvent parts:
                return stateBefore with {
                    RadioCom1 = parts.RadioCom1
                };
            default:
                return stateBefore;
        }
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<AircraftGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new AircraftGrain(
                silo: context.Resolve<ISilo>(),
                database: context.Resolve<IAviationDatabase>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation)
    {
        return new GrainState(
            TailNo: activation.TailNo,
            RadioCom1: GrainRef<IRadioStationGrain>.NotInitialized,
            LastKnownLocation: Timestamped.Create(
                new Location(activation.ParkedPosition, Altitude.Ground),
                activation.Utc)
        );
    }
    public record GrainState(
        string TailNo,
        GrainRef<IRadioStationGrain> RadioCom1,
        Timestamped<Location> LastKnownLocation
        //TODO
    );

    public record GrainActivationEvent(
        string GrainId,
        string TailNo,
        GeoPoint ParkedPosition,
        Bearing ParkedHeading,
        DateTime Utc
    ) : IGrainActivationEvent<AircraftGrain>;

    public record PartsInitializedEvent(
        GrainRef<IRadioStationGrain> RadioCom1
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;
}
