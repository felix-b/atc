using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.Data.Control;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzControllerBrainContext : ILlhzControllerBrainState, ILlhzControllerBrainActions
    {
        private ImmutableList<Intent>.Builder? _transmittedIntents = null;
        private ImmutableList<LlhzFlightStrip>.Builder? _addedFlightStrips = null;
        private ImmutableList<LlhzFlightStrip>.Builder? _updatedFlightStrips = null;
        private ImmutableList<LlhzFlightStrip>.Builder? _removedFlightStrips = null;
        private ImmutableList<LlhzControllerActor.LlhzFlightStripHandoff>.Builder? _handedOffFlightStrips = null;
        
        private ControllerPositionData _position;
        private ImmutableDictionary<string, LlhzFlightStrip> _stripBoard;
        private ActorRef<RadioStationActor> _radio;
        private ActorRef<LlhzAirportActor> _airport;
        private readonly IWorldContext _world;

        public LlhzControllerBrainContext(
            ControllerPositionData position, 
            ImmutableDictionary<string, LlhzFlightStrip> stripBoard, 
            ActorRef<RadioStationActor> radio, 
            ActorRef<LlhzAirportActor> airport,
            IWorldContext world)
        {
            _position = position;
            _stripBoard = stripBoard;
            _radio = radio;
            _airport = airport;
            _world = world;
        }

        ControllerPositionData ILlhzControllerBrainState.Position => _position;

        ImmutableDictionary<string, LlhzFlightStrip> ILlhzControllerBrainState.StripBoard => _stripBoard;

        RadioStationActor ILlhzControllerBrainState.Radio => _radio.Get();

        LlhzAirportActor ILlhzControllerBrainState.Airport => _airport.Get();

        DateTime ILlhzControllerBrainState.UtcNow => _world.UtcNow();

        void ILlhzControllerBrainActions.Transmit(Intent intent)
        {
            SafeAddItem(ref _transmittedIntents, intent);
        }

        void ILlhzControllerBrainActions.AddFlightStrip(LlhzFlightStrip flightStrip)
        {
            SafeAddItem(ref _addedFlightStrips, flightStrip);
        }

        void ILlhzControllerBrainActions.UpdateFlightStrip(LlhzFlightStrip flightStrip)
        {
            SafeAddItem(ref _updatedFlightStrips, flightStrip);
        }

        void ILlhzControllerBrainActions.HandoffFlightStrip(LlhzFlightStrip flightStrip, ActorRef<LlhzControllerActor> toController)
        {
            SafeAddItem(ref _handedOffFlightStrips, new LlhzControllerActor.LlhzFlightStripHandoff(flightStrip, toController));
        }

        void ILlhzControllerBrainActions.RemoveFlightStrip(LlhzFlightStrip flightStrip)
        {
            SafeAddItem(ref _removedFlightStrips, flightStrip);
        }

        T ILlhzControllerBrainActions.CreateIntent<T>(ActorRef<Traffic.AircraftActor> recipient, WellKnownIntentType type, Func<IntentHeader, T> factory)
        {
            var radioInstance = _radio.Get();
            var header = new IntentHeader(
                type,
                CustomCode: 0,
                OriginatorUniqueId: radioInstance.UniqueId,
                OriginatorCallsign: radioInstance.Callsign,
                OriginatorPosition: radioInstance.Location,
                RecipientUniqueId: recipient.UniqueId,
                RecipientCallsign: recipient.Get().Callsign,
                CreatedAtUtc: _world.UtcNow());

            T intent = factory(header);
            return intent;
        }

        public void Clear()
        {
            _transmittedIntents = null;
            _addedFlightStrips = null;
            _updatedFlightStrips = null;
            _removedFlightStrips = null;
            _handedOffFlightStrips = null;
        }
        
        public ImmutableList<Intent>? GetTransmittedIntents() => _transmittedIntents?.ToImmutable();
        public ImmutableList<LlhzFlightStrip>? GetAddedFlightStrips() => _addedFlightStrips?.ToImmutable();
        public ImmutableList<LlhzFlightStrip>? GetUpdatedFlightStrips() => _updatedFlightStrips?.ToImmutable();
        public ImmutableList<LlhzFlightStrip>? GetRemovedFlightStrips() => _removedFlightStrips?.ToImmutable();
        public ImmutableList<LlhzControllerActor.LlhzFlightStripHandoff>? GetHandedOffFlightStrips() => _handedOffFlightStrips?.ToImmutable();

        public bool HasOutputs()
        {
            return (
                _transmittedIntents != null ||
                _addedFlightStrips != null ||
                _updatedFlightStrips != null ||
                _removedFlightStrips != null ||
                _handedOffFlightStrips != null);
        }
        
        public LlhzControllerActor.ApplyBrainOutputEvent TakeOutputEventAndClear(bool consumedIncomingIntent)
        {
            var outputEvent = new LlhzControllerActor.ApplyBrainOutputEvent(
                ConsumedIncomingIntent: consumedIncomingIntent,
                TransmittedIntents: GetTransmittedIntents(),
                AddedFlightStrips: GetAddedFlightStrips(),
                UpdatedFlightStrips: GetUpdatedFlightStrips(),
                RemovedFlightStrips: GetRemovedFlightStrips(),
                HandedOffFlightStrips: GetHandedOffFlightStrips());

            Clear();
            return outputEvent;
        }
        
        public ILlhzControllerBrainState AsState => this;
        public ILlhzControllerBrainActions AsActions => this;

        private void SafeAddItem<T>(ref ImmutableList<T>.Builder? builder, T item)
        {
            if (builder == null)
            {
                builder = ImmutableList.CreateBuilder<T>();
            }
            builder.Add(item);
        }
    }
}
