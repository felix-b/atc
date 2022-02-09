using System;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.Data.Traffic;

namespace Atc.World.Abstractions
{
    public abstract record Maneuver(
        string Name,
        DateTime StartUtc,
        DateTime FinishUtc,
        GeoPoint StartLocation,
        GeoPoint FinishLocation
    )
    {
        public abstract AircraftSituation GetAircraftSituation(DateTime atUtc);
        public virtual Maneuver GetCurrentManeuver(DateTime atUtc) => this;
    }

    public record AircraftSituation(
        DateTime Utc,
        GeoPoint Location,
        Altitude Altitude,
        Bearing Heading,
        Angle Pitch,
        Angle Roll,
        AircraftLights Lights, 
        Percentage LandingGearExtension,
        Percentage FlapsExtension,
        Percentage AirBrakesExtension,
        Percentage EngineForwardThrust,
        Percentage EngineReverseThrust,
        Bearing Track,
        Speed GroundSpeed,
        Speed VerticalSpeed,
        Acceleration GroundAcceleration,
        Acceleration VerticalAcceleration
    );

    [Flags]
    public enum AircraftLights
    {
        None = 0,
        Cabin = 0x01,
        Nav = 0x02,
        Taxi = 0x04,
        Landing = 0x08,
        Strobe = 0x10,
    }
}
