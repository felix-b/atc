using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.World.Abstractions;

namespace Atc.World.Traffic.Maneuvers
{
    public class ManeuverBuilder
    {
        private readonly string _name;
        private readonly GeoPoint _startLocation;
        private readonly DateTime _startUtc;
        private readonly ImmutableList<Maneuver>.Builder _parts;

        public ManeuverBuilder(string name, GeoPoint startLocation, DateTime startUtc)
        {
            _name = name;
            _startLocation = startLocation;
            _startUtc = startUtc;
            _parts = ImmutableList.CreateBuilder<Maneuver>();
        }

        public void MoveTo(string name, GeoPoint location, Speed speed)
        {
            var maneuver = MockupMoveManeuver.Create(
                name,
                startUtc: FinishUtc, 
                startPoint: FinishLocation, 
                finishPoint: location, 
                groundSpeed: speed);
            _parts.Add(maneuver);
        }

        public void Wait(string name, TimeSpan time)
        {
            var maneuver = MockupStandManeuver.Create(
                name,
                startUtc: FinishUtc, 
                location: FinishLocation,
                heading: FinishHeading);
            _parts.Add(maneuver);
        }

        public Maneuver GetManeuver()
        {
            return new CompositeManeuver(_name, _parts.ToImmutable());
        }

        public GeoPoint FinishLocation => _parts.Count > 0
            ? _parts[^1].FinishLocation
            : _startLocation;

        public DateTime FinishUtc => _parts.Count > 0
            ? _parts[^1].FinishUtc
            : _startUtc;

        public Bearing FinishHeading => GetFinishHeading();

        private Bearing GetFinishHeading()
        {
            if (_parts.Count == 0)
            {
                return Bearing.FromTrueDegrees(0);
            }

            var lastManeuver = _parts[^1];
            var finishSituation = lastManeuver.GetAircraftSituation(lastManeuver.FinishUtc);
            return finishSituation.Heading;
        }
    }
}
