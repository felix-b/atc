using System;
using Atc.Data.Primitives;
using Atc.World.Abstractions;

namespace Atc.World.Traffic.Maneuvers
{
    public record ParkedColdAndDarkManeuver(
        DateTime StartUtc,
        DateTime FinishUtc,
        GeoPoint StartLocation,
        GeoPoint FinishLocation,
        Bearing Heading
    ) : Maneuver(
        Name: __name,
        StartUtc,
        FinishUtc,
        StartLocation,
        FinishLocation) 
    {
        private static readonly string __name = "parked-cold-and-dark"; 

        public override AircraftSituation GetAircraftSituation(DateTime atUtc)
        {
            return new AircraftSituation(
                Utc: atUtc,
                Location: StartLocation,
                Altitude: Altitude.Ground,
                Heading: Heading,
                Pitch: Angle.FromDegrees(0),
                Roll: Angle.FromDegrees(0),
                Lights: AircraftLights.None,
                LandingGearExtension: Percentage.Hundred,
                FlapsExtension: Percentage.Zero,
                AirBrakesExtension: Percentage.Zero,
                EngineForwardThrust: Percentage.Zero,
                EngineReverseThrust: Percentage.Zero,
                Track: Heading,
                GroundSpeed: Speed.FromKnots(0),
                VerticalSpeed: Speed.FromFpm(0),
                GroundAcceleration: Acceleration.Zero,
                VerticalAcceleration: Acceleration.Zero);
        }
        
        public static ParkedColdAndDarkManeuver Create(DateTime startUtc, DateTime finishUtc, GeoPoint location, Bearing heading)
        {
            return new ParkedColdAndDarkManeuver(
                StartUtc: startUtc,
                FinishUtc: finishUtc,
                StartLocation: location,
                FinishLocation: location,
                Heading: heading);
        }
    }
}
