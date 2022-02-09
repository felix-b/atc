using System;
using System.Runtime.InteropServices;
using Atc.Data.Primitives;
using Atc.Math;
using Atc.World.Abstractions;
using Geo.Abstractions;

namespace Atc.World.Traffic.Maneuvers
{
    public record MockupMoveManeuver(
        string Name,
        DateTime StartUtc,
        DateTime FinishUtc,
        GeoPoint StartLocation,
        GeoPoint FinishLocation,
        TimeSpan Duration,
        Distance Distance,
        Bearing Heading,
        Speed GroundSpeed
    ) : Maneuver(
        Name,
        StartUtc,
        FinishUtc,
        StartLocation,
        FinishLocation)
    {
        public override AircraftSituation GetAircraftSituation(DateTime atUtc)
        {
            var elapsed = atUtc - StartUtc;
            var progress = Duration > TimeSpan.Zero
                ? elapsed.TotalSeconds / Duration.TotalSeconds
                : 0.0d;

            var location = new GeoPoint(
                lat: StartLocation.Lat + (FinishLocation.Lat - StartLocation.Lat) * progress,
                lon: StartLocation.Lon + (FinishLocation.Lon - StartLocation.Lon) * progress);
            
            return new AircraftSituation(
                Utc: atUtc,
                Location: location,
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
                GroundSpeed: GroundSpeed,
                VerticalSpeed: Speed.FromFpm(0), 
                GroundAcceleration: Acceleration.Zero,
                VerticalAcceleration: Acceleration.Zero);
        }

        public static MockupMoveManeuver Create(string name, DateTime startUtc, DateTime finishUtc, GeoPoint startPoint, GeoPoint finishPoint)
        {
            var duration = finishUtc - startUtc;
            GeoMath.CalculateGreatCircleLine(startPoint, finishPoint, out var line);

            var distance = line.Length;// GeoMath.QuicklyApproximateDistance(startPoint, finishPoint);
            var heading = line.Bearing12;
            var groundSpeed = duration > TimeSpan.Zero
                ? Speed.FromKnots(distance.NauticalMiles / (float)duration.TotalHours)
                : Speed.FromKnots(0);

            return new MockupMoveManeuver(
                Name: name,
                StartUtc: startUtc,
                FinishUtc: finishUtc,
                StartLocation: startPoint,
                FinishLocation: finishPoint,
                Duration: duration,
                Distance: distance,
                Heading: heading,
                GroundSpeed: groundSpeed);
        }

        public static MockupMoveManeuver Create(string name, DateTime startUtc, GeoPoint startPoint, GeoPoint finishPoint, Speed groundSpeed)
        {
            GeoMath.CalculateGreatCircleLine(startPoint, finishPoint, out var line);

            var distance = line.Length;// GeoMath.QuicklyApproximateDistance(startPoint, finishPoint);
            var heading = line.Bearing12;
            var duration = TimeSpan.FromHours(distance.NauticalMiles / groundSpeed.Knots);
            var finishUtc = startUtc.Add(duration);

            return new MockupMoveManeuver(
                Name: name,
                StartUtc: startUtc,
                FinishUtc: finishUtc,
                StartLocation: startPoint,
                FinishLocation: finishPoint,
                Duration: duration,
                Distance: distance,
                Heading: heading, 
                GroundSpeed: groundSpeed);
        }
    }
}