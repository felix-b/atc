using System;
using System.Collections.Immutable;
using Atc.Data.Control;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public interface ILlhzControllerBrain
    {
        void Initialize(ILlhzControllerBrainState state);
        void ObserveSituation(ILlhzControllerBrainState state, ILlhzControllerBrainActions output);
        void HandleIncomingTransmission(Intent intent, ILlhzControllerBrainState state, ILlhzControllerBrainActions output);
        ControllerPositionType PositionType { get; }
    }
    
    public interface ILlhzControllerBrainState
    {
        ControllerPositionData Position { get; }
        ImmutableDictionary<string, LlhzFlightStrip> StripBoard { get; }
        RadioStationActor Radio { get; }
        LlhzAirportActor Airport { get; }
        DateTime UtcNow { get; }
    }

    public interface ILlhzControllerBrainActions
    {
        void Transmit(Intent intent);
        void AddFlightStrip(LlhzFlightStrip flightStrip);
        void UpdateFlightStrip(LlhzFlightStrip flightStrip);
        void HandoffFlightStrip(LlhzFlightStrip flightStrip, ActorRef<LlhzControllerActor> toController);
        void RemoveFlightStrip(LlhzFlightStrip flightStrip);
        T CreateIntent<T>(ActorRef<AircraftActor> recipient, WellKnownIntentType type, Func<IntentHeader, T> factory) where T : Intent;
    }
}
