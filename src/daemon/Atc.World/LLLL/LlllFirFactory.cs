using Atc.Grains;
using Atc.Maths;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Traffic;
using Atc.World.Traffic;

namespace Atc.World.LLLL;

public static class LlllFirFactory
{
    public static void InitializeFir(
        ISilo silo, 
        GrainRef<IWorldGrain> world, 
        out GrainRef<IAircraftGrain>[] allParkedAircraft)
    {
        var llhzAirport = silo.Grains.CreateGrain<LlhzAirportGrain>(grainId =>
            new LlhzAirportGrain.GrainActivationEvent(grainId, world));

        var aircraft = new[] {
            LlllTrafficFactory.SpawnParkedAircraft(silo, "4XCGK", llhzAirport.Get().Datum, Bearing.FromTrueDegrees(0)),
            LlllTrafficFactory.SpawnParkedAircraft(silo, "4XCDK", llhzAirport.Get().Datum, Bearing.FromTrueDegrees(0)),
            LlllTrafficFactory.SpawnParkedAircraft(silo, "4XCDC", llhzAirport.Get().Datum, Bearing.FromTrueDegrees(0)),
            LlllTrafficFactory.SpawnParkedAircraft(silo, "4XCDT", llhzAirport.Get().Datum, Bearing.FromTrueDegrees(0))
        };

        allParkedAircraft = aircraft
            .Select(a => a.As<IAircraftGrain>())
            .ToArray();
    }

    public static void BeginPatternFlightsAtLlhz(
        ISilo silo, 
        GrainRef<IWorldGrain> world, 
        GrainRef<IAircraftGrain>[] allAircraft,
        TimeSpan duration)
    {
        var nextTakeoffUtc = silo.Environment.UtcNow.AddMinutes(5);
        
        foreach (var aircraft in allAircraft)
        {
            var tailNo = aircraft.Get().TailNo;
            var callsign = new Callsign(Full: tailNo, Short: tailNo.Substring(tailNo.Length - 3, 3));
            var flightPlan = new PatternFlightPlan(
                TailNo: tailNo,
                Callsign: callsign,
                TakeoffTimeUtc: nextTakeoffUtc,
                LandingTimeUtc: nextTakeoffUtc.Add(duration));
            
            silo.Grains.CreateGrain<LlhzAIPilotFlyingGrain>(grainId =>
                new LlhzAIPilotFlyingGrain.GrainActivationEvent(
                    grainId,
                    world,
                    aircraft, 
                    flightPlan)
            );

            nextTakeoffUtc = nextTakeoffUtc.AddMinutes(1);
        }
    }
}