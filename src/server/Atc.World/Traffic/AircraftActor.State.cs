using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Traffic
{
    public partial class AircraftActor 
    {
        public record AircraftState(
            int Version,
            AircraftData Data,
            string Callsign,
            ActorRef<RadioStationActor> Com1Radio,
            AircraftSituation LastKnownSituation,
            uint LastKnownSituationVersion,
            uint LastFetchedSituationVersion,
            Maneuver CurrentManeuver
        );

        public record ActivationEvent(
            string UniqueId,
            AircraftState InitialState
        ) : IActivationStateEvent<Traffic.AircraftActor>;
        
        public record MovedEvent(
            GeoPoint NewLocation
        ) : IStateEvent;

        public record ReplaceManeuverEvent(
            Maneuver NewManeuver
        ) : IStateEvent;

        public record UpdateLastKnownSituationEvent(
            AircraftSituation NewSituation
        ) : IStateEvent;

        protected override AircraftState Reduce(AircraftState stateBefore, IStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                // case MovedEvent moved:
                //     return stateBefore with {
                //         Version = stateBefore.Version + 1,
                //         Location = moved.NewLocation
                //     };
                case ReplaceManeuverEvent replaceManeuver:
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        CurrentManeuver = replaceManeuver.NewManeuver
                    };
                case UpdateLastKnownSituationEvent updateSituation:
                    var newSituationVersion = stateBefore.LastFetchedSituationVersion + 1;
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        LastKnownSituation = updateSituation.NewSituation,
                        LastKnownSituationVersion = newSituationVersion,
                        LastFetchedSituationVersion = newSituationVersion,
                    };
                default:
                    return stateBefore;
            }
        }
    }
}
