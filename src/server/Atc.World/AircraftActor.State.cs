using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.World.Comms;
using Zero.Loss.Actors;
using Zero.Serialization.Buffers;

namespace Atc.World
{
    public partial class AircraftActor 
    {
        public record AircraftState(
            int Version,
            AircraftData Data,
            GeoPoint Location,
            Altitude Altitude, 
            Angle Pitch, 
            Angle Roll, 
            Bearing Heading, 
            Bearing Track, 
            Speed GroundSpeed,
            ActorRef<RadioStationActor> Com1Radio
        );

        public record ActivationEvent(
            string UniqueId,
            AircraftState InitialState
        ) : IActivationStateEvent<AircraftActor>;
        
        public record MovedEvent(
            GeoPoint NewLocation
        ) : IStateEvent;

        protected override AircraftState Reduce(AircraftState stateBefore, IStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case MovedEvent moved:
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        Location = moved.NewLocation
                    };
                default:
                    return stateBefore;
            }
        }
    }
}
