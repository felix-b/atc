using System;
using System.Collections.Immutable;
using Zero.Loss.Actors;
using Atc.World.Comms;

namespace Atc.World
{
    public partial class WorldActor : StatefulActor<WorldActor.WorldState>
    {
        public record WorldState(
            ulong Version,
            DateTime StartedAtUtc,
            TimeSpan Timestamp,
            ulong TickCount,
            ImmutableDictionary<string, ActorRef<Traffic.AircraftActor>> AircraftById,
            ImmutableDictionary<int, ImmutableArray<ActorRef<GroundRadioStationAetherActor>>> RadioAetherByKhz
        );

        public record WorldActivationEvent(
            string UniqueId,
            DateTime StartedAtUtc
        ) : IActivationStateEvent<WorldActor>;
        
        public record ProgressLoopUpdateEvent(
            ulong TickCount,
            TimeSpan Timestamp
        ) : IStateEvent;

        public record AircraftAddedEvent(
            ActorRef<Traffic.AircraftActor> Aircraft            
        ) : IStateEvent;

        public record AircraftRemovedEvent(
            ActorRef<Traffic.AircraftActor> Aircraft            
        ) : IStateEvent;

        public record GroundRadioStationAddedEvent(
            ActorRef<GroundRadioStationAetherActor> Aether            
        ) : IStateEvent;

        public record GroundRadioStationRemovedEvent(
            ActorRef<GroundRadioStationAetherActor> Aether            
        ) : IStateEvent;
        
        protected override WorldState Reduce(WorldState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case ProgressLoopUpdateEvent progress:
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        TickCount = progress.TickCount,
                        Timestamp = progress.Timestamp
                    };
                case AircraftAddedEvent aircraftAdded:
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        AircraftById = stateBefore.AircraftById.Add(aircraftAdded.Aircraft.UniqueId, aircraftAdded.Aircraft)
                    };
                case AircraftRemovedEvent aircraftRemoved:
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        AircraftById = stateBefore.AircraftById.Remove(aircraftRemoved.Aircraft.UniqueId)
                    };
                case GroundRadioStationAddedEvent stationAdded:
                    var radioAetherByKhzAfterAdd = AddRadioAether(
                        stateBefore.RadioAetherByKhz, 
                        stationAdded.Aether);
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        RadioAetherByKhz = radioAetherByKhzAfterAdd 
                    };
                case GroundRadioStationRemovedEvent stationRemoved:
                    var radioAetherByKhzAfterRemove = RemoveRadioAether(
                        stateBefore.RadioAetherByKhz, 
                        stationRemoved.Aether);
                    return stateBefore with {
                        Version = stateBefore.Version + 1,
                        RadioAetherByKhz = radioAetherByKhzAfterRemove 
                    };
                default:
                    return stateBefore;
            }

            ImmutableDictionary<int, ImmutableArray<ActorRef<GroundRadioStationAetherActor>>> AddRadioAether(
                ImmutableDictionary<int, ImmutableArray<ActorRef<GroundRadioStationAetherActor>>> aetherByKhzBefore,
                ActorRef<GroundRadioStationAetherActor> aetherToAdd)
            {
                var khz = aetherToAdd.Get().GroundStation.Get().Frequency.Khz;
                var stationArrayBefore = aetherByKhzBefore.TryGetValue(khz, out var existingStationArray)
                    ? existingStationArray
                    : ImmutableArray<ActorRef<GroundRadioStationAetherActor>>.Empty;
                var stationArrayAfter = stationArrayBefore.Add(aetherToAdd);
                var aetherByKhzAfter = aetherByKhzBefore.SetItem(khz, stationArrayAfter);
                return aetherByKhzAfter;
            }   

            ImmutableDictionary<int, ImmutableArray<ActorRef<GroundRadioStationAetherActor>>> RemoveRadioAether(
                ImmutableDictionary<int, ImmutableArray<ActorRef<GroundRadioStationAetherActor>>> aetherByKhzBefore,
                ActorRef<GroundRadioStationAetherActor> aetherToRemove)
            {
                var khz = aetherToRemove.Get().GroundStation.Get().Frequency.Khz;
                if (!aetherByKhzBefore.TryGetValue(khz, out var stationArrayBefore))
                {
                    return aetherByKhzBefore;
                }

                var stationArrayAfter = stationArrayBefore.RemoveAll(x => x.UniqueId == aetherToRemove.UniqueId);
                return stationArrayAfter.Length > 0
                    ? aetherByKhzBefore.SetItem(khz, stationArrayAfter)
                    : aetherByKhzBefore.Remove(khz);
            }   
        }

        private static WorldState CreateInitialState(WorldActivationEvent activation)
        {
            return new WorldState(
                Version: 1,
                StartedAtUtc: activation.StartedAtUtc,
                Timestamp: TimeSpan.Zero,
                TickCount: 0,
                AircraftById: ImmutableDictionary<string, ActorRef<Traffic.AircraftActor>>.Empty,
                RadioAetherByKhz: ImmutableDictionary<int, ImmutableArray<ActorRef<GroundRadioStationAetherActor>>>.Empty
            );
        }


        // public partial record AircraftAddedEvent(
        //     uint Id,
        //     string TypeIcao,
        //     string TailNo,
        //     string Callsign,
        //     uint? ModeS, 
        //     AircraftCategories Category,
        //     OperationTypes Operations,
        //     string? AirlineIcao,
        //     string LiveryId,
        //     GeoPoint Location,
        //     Altitude Altitude,
        //     Angle Pitch,
        //     Angle Roll,
        //     Bearing Heading,
        //     Bearing Track,
        //     Speed GroundSpeed
        // ) : IRuntimeStateEvent;
    }
}