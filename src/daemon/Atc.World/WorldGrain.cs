using System.Collections.Immutable;
using Atc.Grains;
using Atc.Maths;
using Atc.World.Airports;
using Atc.World.Communications;

namespace Atc.World;

public interface IWorldGrain : IGrainId
{
    void AddRadioMedium(GrainRef<IGroundStationRadioMediumGrain> medium);
    void AddAirport(GrainRef<IAirportGrain> airport);
    GrainRef<IGroundStationRadioMediumGrain>? TryFindRadioMedium(GeoPoint position, Altitude altitude, Frequency frequency);
    GrainRef<IAirportGrain>? TryFindAirportAt(GeoPoint positionWithin);
    GrainRef<IAirportGrain>? TryFindAirportByIcao(string icao);
    ulong TakeNextIntentId();
    ulong TakeNextTransmissionId();

    public GrainRef<IAirportGrain> FindAirportAt(GeoPoint positionWithin) =>
        TryFindAirportAt(positionWithin)
        ?? throw new Exception($"Could not find airport at {positionWithin}");
    
    public GrainRef<IAirportGrain> FindAirportByIcao(string icao) => 
        TryFindAirportByIcao(icao)
        ?? throw new Exception($"Could not find airport '{icao}'");
}

public class WorldGrain : AbstractGrain<WorldGrain.GrainState>, IWorldGrain
{
    public static readonly string TypeString = nameof(WorldGrain);

    public WorldGrain(
        ISiloEventDispatch dispatch,
        GrainActivationEvent activation) :
        base(
            grainId: activation.GrainId,
            grainType: TypeString,
            dispatch: dispatch,
            initialState: CreateInitialState(activation))
    {
    }

    public void AddRadioMedium(GrainRef<IGroundStationRadioMediumGrain> medium)
    {
        Dispatch(new AddRadioMediumEvent(medium));
    }

    public void AddAirport(GrainRef<IAirportGrain> airport)
    {
        Dispatch(new AddAirportEvent(airport));
    }

    public GrainRef<IGroundStationRadioMediumGrain>? TryFindRadioMedium(
        GeoPoint position, 
        Altitude altitude, 
        Frequency frequency)
    {
        var grainRef = State.RadioMediums.FirstOrDefault(m => m.Get().Frequency == frequency);
        return (grainRef.CanGet ? grainRef : null);
    }

    public GrainRef<IAirportGrain>? TryFindAirportAt(GeoPoint positionWithin)
    {
        throw new NotImplementedException();
    }

    public GrainRef<IAirportGrain>? TryFindAirportByIcao(string icao)
    {
        return State.AirportByIcao.TryGetValue(icao, out var airport)
            ? airport
            : null;
    }

    public ulong TakeNextIntentId()
    {
        var result = State.NextIntentId;
        Dispatch(new NextIntentIdTakenEvent());
        return result;
    }

    public ulong TakeNextTransmissionId()
    {
        var result = State.NextTransmissionId;
        Dispatch(new NextTransmissionIdTakenEvent());
        return result;
    }

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
            case AddRadioMediumEvent addMedium:
                return stateBefore with {
                    RadioMediums = stateBefore.RadioMediums.Add(addMedium.Medium)
                };
            case AddAirportEvent addAirport:
                return stateBefore with {
                    AirportByIcao = stateBefore.AirportByIcao.Add(
                        addAirport.Airport.Get().Icao,
                        addAirport.Airport)
                };
            case NextIntentIdTakenEvent:
                return stateBefore with {
                    NextIntentId = stateBefore.NextIntentId + 1
                };
            case NextTransmissionIdTakenEvent:
                return stateBefore with {
                    NextTransmissionId = stateBefore.NextTransmissionId + 1
                };
            default:
                return stateBefore;
        }
    }

    public static void RegisterGrainType(SiloConfigurationBuilder config)
    {
        config.RegisterGrainType<WorldGrain, GrainActivationEvent>(
            TypeString,
            (activation, context) => new WorldGrain(
                dispatch: context.Resolve<ISiloEventDispatch>(),
                activation: activation
            ));
    }

    private static GrainState CreateInitialState(GrainActivationEvent activation)
    {
        return new GrainState(
            RadioMediums: ImmutableArray<GrainRef<IGroundStationRadioMediumGrain>>.Empty,
            AirportByIcao: ImmutableDictionary<string, GrainRef<IAirportGrain>>.Empty, 
            NextIntentId: 1,
            NextTransmissionId: 1
        );
    }

    public record GrainState(
        //TODO: optimize
        ImmutableArray<GrainRef<IGroundStationRadioMediumGrain>> RadioMediums,
        ImmutableDictionary<string, GrainRef<IAirportGrain>> AirportByIcao,
        ulong NextIntentId,
        ulong NextTransmissionId
    );

    public record GrainActivationEvent(
        string GrainId
        //TODO
    ) : IGrainActivationEvent<WorldGrain>;

    public record AddRadioMediumEvent(
        GrainRef<IGroundStationRadioMediumGrain> Medium  
    ) : IGrainEvent;

    public record AddAirportEvent(
        GrainRef<IAirportGrain> Airport  
    ) : IGrainEvent;

    public record NextIntentIdTakenEvent : IGrainEvent;

    public record NextTransmissionIdTakenEvent : IGrainEvent;

    public record SampleWorkItem(
        //TODO
    ) : IGrainWorkItem;

}

