using System;
using System.Collections.Immutable;
using System.IO;
using Atc.Data.Primitives;
using Atc.World.Abstractions;

namespace Atc.World.Traffic.Maneuvers
{
    public record CompositeManeuver(
        string Name,
        ImmutableList<Maneuver> Parts    
    ) : Maneuver(
        Name,
        StartUtc: Parts[0].StartUtc,
        FinishUtc: Parts[^1].FinishUtc,
        StartLocation: Parts[0].StartLocation,
        FinishLocation: Parts[^1].FinishLocation)
    {
        public override AircraftSituation GetAircraftSituation(DateTime atUtc)
        {
            var currentManeuver = GetCurrentManeuver(atUtc);
            return currentManeuver.GetAircraftSituation(atUtc);
        }

        public override Maneuver GetCurrentManeuver(DateTime atUtc)
        {
            if (atUtc < StartUtc || atUtc >= FinishUtc)
            {
                throw new ArgumentOutOfRangeException(nameof(atUtc),"Specified time is outside of current maneuver time span");
            }
            
            var currentPart = 
                BinarySearch(atUtc, 0, Parts.Count)
                ?? throw new InvalidDataException("Failed to find maneuver part at specified time");

            return currentPart.GetCurrentManeuver(atUtc);
        }

        private Maneuver? BinarySearch(DateTime atUtc, int fromIndex, int toIndex)
        {
            if (fromIndex >= toIndex)
            {
                return null;
            }
            
            var midIndex = fromIndex + ((toIndex - fromIndex) >> 1);
            var midPart = Parts[midIndex];
            if (midPart.FinishUtc < atUtc)
            {
                return BinarySearch(atUtc, midIndex + 1, toIndex);
            }
            if (midPart.StartUtc > atUtc)
            {
                return BinarySearch(atUtc, fromIndex, midIndex);
            }

            return midPart;
        }
    }
}
