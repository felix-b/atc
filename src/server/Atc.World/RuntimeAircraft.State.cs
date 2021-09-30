using Atc.Data.Primitives;
using Atc.World.Redux;

namespace Atc.World
{
    public partial class RuntimeAircraft : 
        IHaveRuntimeState<RuntimeAircraft.RuntimeState>, 
        IObserveRuntimeState<RuntimeAircraft.RuntimeState>
    {
        public record RuntimeState(
            int Version,
            GeoPoint Location,
            Altitude Altitude, 
            Angle Pitch, 
            Angle Roll, 
            Bearing Heading, 
            Bearing Track, 
            Speed GroundSpeed,
            bool AvionicsPoweredOn,
            Frequency Com1Frequency
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
                case MovedEvent moved:
                    return currentState with {
                        Version = currentState.Version + 1,
                        Location = moved.NewLocation
                    };
                case AvionicsPoweredOnEvent avionicsOn:
                    return currentState with {
                        Version = currentState.Version + 1,
                        AvionicsPoweredOn = true,
                        Com1Frequency = avionicsOn.Com1Frequency,
                    };
                case AvionicsPoweredOffEvent:
                    return currentState with {
                        Version = currentState.Version + 1,
                        AvionicsPoweredOn = false,
                    };
                case ComFrequencyChangedEvent frequencyChanged:
                    return currentState with {
                        Version = currentState.Version + 1,
                        Com1Frequency = frequencyChanged.Com1,
                    };
                default:
                    return currentState;
            }
        }

        void IObserveRuntimeState<RuntimeState>.ObserveStateChanges(RuntimeState oldState, RuntimeState newState)
        {
            if (newState.AvionicsPoweredOn != _com1.IsPoweredOn())
            {
                if (newState.AvionicsPoweredOn)
                {
                    _com1.PowerOn();
                }
                else
                {
                    _com1.PowerOff();
                }
            }

            if (_com1.Frequency != newState.Com1Frequency)
            {
                _com1.TuneTo(newState.Com1Frequency);
            }
        }
        
        public record MovedEvent(
            GeoPoint NewLocation
        ) : IRuntimeStateEvent;

        public record AvionicsPoweredOnEvent(
            Frequency Com1Frequency
        ) : IRuntimeStateEvent;

        public record ComFrequencyChangedEvent(
            Frequency Com1
        ) : IRuntimeStateEvent;

        public record AvionicsPoweredOffEvent : IRuntimeStateEvent;
    }
}
