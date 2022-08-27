using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Airports;
using Atc.World.Communications;
using Atc.World.Contracts.Airports;
using Atc.World.Contracts.Communications;
using Atc.World.Control;

namespace Atc.World.LLLL;

public class LlhzAirportGrain : 
    AbstractGrain<LlhzAirportGrain.GrainState>, 
    IAirportGrain,
    IStartableGrain
{
    public static readonly string TypeString = nameof(LlhzAirportGrain);

    [NotEventSourced]
    private readonly ISilo _silo;
    
    public LlhzAirportGrain(
        ISilo silo,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: silo.Dispatch,
            initialState: CreateInitialState(activation))
    {
        _silo = silo;
    }

    public void Start()
    {
        var clearanceCallsign = new Callsign("Hertzlia Clearance");
        var clearanceRadio = _silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Ground,
                clearanceCallsign));
        clearanceRadio.Get().TurnOnGroundStation(Location.At(32.180d, 34.834d, 100.0f), Frequency.FromKhz(130850));
        var clearanceController = _silo.Grains.CreateGrain<LlhzAIClearanceControllerGrain>(grainId =>
            new LlhzAIClearanceControllerGrain.GrainActivationEvent(
                grainId,
                clearanceCallsign,
                State.World,
                clearanceRadio.As<IRadioStationGrain>()));

        var towerCallsign = new Callsign("Hertzlia");
        var towerRadio = _silo.Grains.CreateGrain<RadioStationGrain>(grainId =>
            new RadioStationGrain.GrainActivationEvent(
                grainId,
                RadioStationType.Ground,
                towerCallsign));
        towerRadio.Get().TurnOnGroundStation(Location.At(32.180d, 34.834d, 100.0f), Frequency.FromKhz(122200));
        var towerController = _silo.Grains.CreateGrain<LlhzAIClearanceControllerGrain>(grainId =>
            new LlhzAIClearanceControllerGrain.GrainActivationEvent(
                grainId,
                towerCallsign,
                State.World,
                towerRadio.As<IRadioStationGrain>()));
        
        Dispatch(new PartsInitializedEvent(
            clearanceController.As<ILlhzControllerGrain>(),
            towerController.As<ILlhzControllerGrain>()
        ));
    }

    public string Icao => "LLHZ";
    public GeoPoint Datum => new GeoPoint(lat: 32.180d, lon: 34.834d);
    public TerminalInformation CurrentAtis => State.Atis;
    public GrainRef<IControllerGrain> Clearance => State.ClearanceController.As<IControllerGrain>();
    public GrainRef<IControllerGrain> Tower => State.TowerController.As<IControllerGrain>();

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
                    ClearanceController = parts.ClearanceController,
                    TowerController = parts.TowerController
                };
            default:
                return stateBefore;
        }
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<LlhzAirportGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new LlhzAirportGrain(
                silo: context.Resolve<ISilo>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation)
    {
        return new GrainState(
            Atis: MakeAtis(),
            World: activation.World,
            ClearanceController: new GrainRef<ILlhzControllerGrain>(),
            TowerController: new GrainRef<ILlhzControllerGrain>()
        );
    }

    private static TerminalInformation MakeAtis() => new TerminalInformation(
        Icao: "LLHZ",
        Designator: "Q",
        Wind: new Wind(Bearing.FromTrueDegrees(290.0f), Speed.FromKnots(10), gust: null),
        Qnh: Pressure.FromInHg(29.81f),
        ActiveRunwaysDeparture: ImmutableList<string>.Empty.Add("29"),
        ActiveRunwaysArrival: ImmutableList<string>.Empty.Add("29"));

    public record GrainState(
        TerminalInformation Atis,
        GrainRef<IWorldGrain> World,
        GrainRef<ILlhzControllerGrain> ClearanceController,
        GrainRef<ILlhzControllerGrain> TowerController
    );

    public record GrainActivationEvent(
        string GrainId,
        GrainRef<IWorldGrain> World
        //TODO
    ) : IGrainActivationEvent<LlhzAirportGrain>;

    public record PartsInitializedEvent(
        GrainRef<ILlhzControllerGrain> ClearanceController,
        GrainRef<ILlhzControllerGrain> TowerController
    ) : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;
}
