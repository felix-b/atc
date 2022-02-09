using System;
using System.Runtime.InteropServices;
using Atc.Data.Primitives;
using Atc.Math;
using Atc.World.Abstractions;
using Geo.Abstractions;

namespace Atc.World.Traffic.Maneuvers
{
    public record MockupStandManeuver(
        string Name,
        DateTime StartUtc,
        DateTime FinishUtc,
        GeoPoint StartLocation,
        Bearing Heading
    ) : Maneuver(
        Name,
        StartUtc,
        FinishUtc,
        StartLocation,
        StartLocation)
    {
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
                LandingGearExtension: Percentage.Zero, 
                FlapsExtension: Percentage.Zero, 
                AirBrakesExtension: Percentage.Zero, 
                EngineForwardThrust: Percentage.Zero, 
                EngineReverseThrust: Percentage.Zero,
                Track: Bearing.FromTrueDegrees(0),
                GroundSpeed: Speed.FromKnots(0), 
                VerticalSpeed: Speed.FromFpm(0), 
                GroundAcceleration: Acceleration.Zero,
                VerticalAcceleration: Acceleration.Zero);
        }

        public static MockupStandManeuver Create(string name, GeoPoint location, Bearing heading, DateTime startUtc)
        {
            return new MockupStandManeuver(name, startUtc, DateTime.MaxValue, location, heading);
        }

        public static MockupStandManeuver Create(string name, GeoPoint location, Bearing heading, DateTime startUtc, DateTime finishUtc)
        {
            return new MockupStandManeuver(name, startUtc, finishUtc, location, heading);
        }
    }
}