using System;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Redux;

namespace Atc.World.Comms
{
    public partial class RuntimeRadioStationFactory : IHaveRuntimeState<RuntimeRadioStationFactory.RuntimeState>
    {
        private RuntimeState _state = new(
            LastStationId: 0
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
                case UpdateLastStationId updateId:
                    return currentState with {
                        LastStationId = updateId.Value
                    };
            }
            
            return currentState;
        }
        
        public record RuntimeState(
            ulong LastStationId
        );

        public record UpdateLastStationId(
            ulong Value
        ) : IRuntimeStateEvent;
    }
}
