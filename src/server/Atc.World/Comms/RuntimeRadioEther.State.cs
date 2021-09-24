#if false
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioEther : IHaveRuntimeState<RuntimeRadioEther.RuntimeState>
    {
        private RuntimeState _state = new(
            LastTransmissionId: 0
        );
        
        public RuntimeState GetState()
        {
            return _state;
        }

        void IHaveRuntimeState<RuntimeState>.SetState(RuntimeState newState)
        {
            _state = newState;
        }

        RuntimeState IHaveRuntimeState<RuntimeState>.Reduce(RuntimeState currentState, IRuntimeStateEvent stateEvent)
        {
            switch (stateEvent)
            {
                case AircraftStationAddedEvent addedAir:
                    return currentState with {
                        StationById = currentState.RadioStationIds.Add(addedAir.StationId)
                    };
                case GroundStationAddedEvent addedGround:
                    return currentState with {
                        RadioStationIds = currentState.RadioStationIds.Add(addedGround.StationId)
                    };
                case StationRemovedEvent removed:
                    return currentState with {
                        RadioStationIds = currentState.RadioStationIds.Remove(removed.StationId)
                    };
                default:
                    return currentState;
            }
        }

        public record RuntimeState(
            ulong LastTransmissionId
        );
        
        
    }
}
#endif