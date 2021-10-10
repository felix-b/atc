using System;
using System.Collections;
using System.Collections.Generic;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.World.Comms;
using Microsoft.AspNetCore.Http;
using Zero.Latency.Servers;
using Zero.Loss.Actors;
using Zero.Serialization.Buffers;

namespace Atc.World
{
    public partial class WorldActor : IWorldContext
    {
        public static readonly string TypeString = "world";
        
        private readonly IStateStore _store;
        private readonly ILogger _logger;
        private readonly ISupervisorActor _supervisor;
        
        [NotEventSourced]        
        private readonly DeferredTaskQueue _taskQueue;

        [NotEventSourced]        
        private readonly HashSet<IWorldObserver> _observers = new();

        public WorldActor(
            IStateStore store, 
            ILogger logger,
            ISupervisorActor supervisor,
            WorldActivationEvent activation)
            : base(TypeString, activation.UniqueId, CreateInitialState(activation))
        {
            _store = store;
            _logger = logger;
            _supervisor = supervisor;
            _taskQueue = new DeferredTaskQueue(logger);

            InitializeMockWorldObjects();
        }

        public IObservableQuery<AircraftActor> QueryTraffic(in GeoRect rect)
        {
            return new TrafficObservableQuery(this, rect);
        }

        
        public void ProgressBy(TimeSpan delta)
        {
            _store.Dispatch(this, new ProgressLoopUpdateEvent(
                TickCount: State.TickCount + 1,
                Timestamp: State.Timestamp + delta));

            using var logSpan = _logger.ProgressBy((int) delta.TotalMilliseconds, (int) State.Timestamp.TotalMilliseconds, State.TickCount);
            using var lifecycle = new OperationLifecycle(this, nameof(ProgressBy));

            // 1. create new empty ChangeSet and make it current in the context
            // 2. call ProgressBy on all parties involved   
            //    ?> abstract parties subscribing to ProgressTo?
            //    ?> pipeline?
            // 3. every party updates its state Redux-style:
            //    - an event is dispatched to a store
            //    - a reducer processes the event and produces new state
            //    - the event is published to the current ChangeSet
            //    - changes to state are published to the current ChangeSet 
            //    - existing subscriptions are run against the ChangeSet and the observers are invoked as necessary 
            // 4.

            foreach (var aircraft in State.AircraftById.Values)
            {
                using (_logger.ProgressByAircraft(aircraft.UniqueId))
                {
                    aircraft.Get().ProgressBy(delta);
                }
            }

            ExecuteDeferredTasks();
        }

        public void RegisterObserver(IWorldObserver observer)
        {
            _logger.RegisteringObserver(observer.Name);
            _observers.Add(observer);
        }

        public ulong CreateUniqueId(ulong lastId)
        {
            //TODO: apply shard ranges one day
            return lastId + 1;
        }

        public ActorRef<GroundRadioStationAetherActor>? TryFindRadioAether(ActorRef<RadioStationActor> fromStationActor)
        {
            var fromStation = fromStationActor.Get();
            
            if (State.RadioAetherByKhz.TryGetValue(fromStation.Frequency.Khz, out var aetherList))
            {
                foreach (var aether in aetherList)
                {
                    if (aether.Get().IsReachableBy(fromStationActor))
                    {
                        _logger.FoundRadioAether(
                            fromStation: fromStationActor.ToString(),
                            groundStation: aether.Get().GroundStation.ToString(),
                            khz: fromStation.Frequency.Khz, 
                            lat: fromStation.Location.Lat, 
                            lon: fromStation.Location.Lon, 
                            feet: fromStation.Elevation.Feet);
                        return aether;
                    }
                }
            }

            _logger.FailedToFindRadioAether(
                fromStation: fromStationActor.ToString(),
                khz: fromStation.Frequency.Khz, 
                lat: fromStation.Location.Lat, 
                lon: fromStation.Location.Lon, 
                feet: fromStation.Elevation.Feet);
            return null;
        }

        //TODO: fix this
        [NotEventSourced]
        private uint _nextAircraftId = 1;
        
        public ActorRef<AircraftActor> SpawnNewAircraft(
            string typeIcao,
            string tailNo,
            string? callsign,
            string? airlineIcao,
            AircraftCategories category,
            OperationTypes operations,
            GeoPoint location,
            Altitude altitude,
            Bearing heading,
            Bearing? track = null,
            Speed? groundSpeed = null,
            Angle? pitch = null,
            Angle? roll = null)
        {
            var aircraft = AircraftActor.SpawnNewAircraft(
                _supervisor,
                _nextAircraftId++,
                typeIcao,
                tailNo,
                callsign,
                airlineIcao,
                liveryId: null,
                modeS: null,
                category,
                operations,
                location,
                altitude,
                heading,
                track,
                groundSpeed,
                pitch, 
                roll);
            
            _store.Dispatch(this, new AircraftAddedEvent(aircraft));
            return aircraft;
        }

        public ActorRef<GroundRadioStationAetherActor> AddGroundStation(ActorRef<RadioStationActor> station)
        {
            var aether = _supervisor.CreateActor<GroundRadioStationAetherActor>(uniqueId => new GroundRadioStationAetherActor.ActivationEvent( 
                uniqueId, 
                UtcNow(), 
                station));
            _store.Dispatch(this, new GroundRadioStationAddedEvent(aether));
            return aether;
        }
        
        public IDeferHandle Defer(Action action)
        {
            return _taskQueue.EnqueueWorkItem(
                predicate: __alwaysTruePredicate, 
                deadlineUtc: null, 
                onPredicateTrue: action, 
                onTimeout: null);
        }

        public IDeferHandle DeferBy(TimeSpan time, Action action)
        {
            return _taskQueue.EnqueueWorkItem(
                predicate: __alwaysFalsePredicate, 
                deadlineUtc: UtcNow() + time, 
                onPredicateTrue: null, 
                onTimeout: action);
        }

        public IDeferHandle DeferUntil(DateTime utc, Action action)
        {
            return _taskQueue.EnqueueWorkItem(
                predicate: __alwaysFalsePredicate, 
                deadlineUtc: utc, 
                onPredicateTrue: null, 
                onTimeout: action);
        }

        public IDeferHandle DeferUntil(Func<bool> predicate, DateTime deadlineUtc, Action onPredicateTrue, Action onTimeout)
        {
            return _taskQueue.EnqueueWorkItem(
                predicate, 
                deadlineUtc, 
                onPredicateTrue, 
                onTimeout);
        }

        public void ExecuteDeferredTasks()
        {
            using var logSpan = _logger.ExecutingDeferredTasks();
            
            try
            {
                _taskQueue.RunToCompletion(UtcNow());
            }
            catch (Exception e)
            {
                logSpan.Fail(e);
            }
        }

        public DateTime UtcNow()
        {
            return State.StartedAtUtc + State.Timestamp;
        }

        public TimeSpan Timestamp => State.Timestamp;

        public ILogger Logger => _logger;

        // TODO: remove
        private void InitializeMockWorldObjects()
        {
            // var stationLlhzClrDel = _radioStationFactory().CreateGroundStation(
            //     new GeoPoint(32.179766d, 34.834404d),
            //     Altitude.FromFeetMsl(100),
            //     Frequency.FromKhz(130850),
            //     "Hertzliya Clearance",
            //     "Hertzliya Clearance",
            //     out var llhzClrDelAether);  
            // var stationLlhzTwrPrimary = _radioStationFactory().CreateGroundStation(
            //     new GeoPoint(32.179766d, 34.834404d),
            //     Altitude.FromFeetMsl(100),
            //     Frequency.FromKhz(122200),
            //     "Hertzliya Tower",
            //     "Hertzliya",
            //     out var llhzTwr1Aether);  
            // var stationLlhzTwrSecondary = _radioStationFactory().CreateGroundStation(
            //     new GeoPoint(32.179766d, 34.834404d),
            //     Altitude.FromFeetMsl(100),
            //     Frequency.FromKhz(129400),
            //     "Hertzliya Tower",
            //     "Hertzliya",
            //     out var llhzTwr2Aether);
            // var stationPlutoPrimary = _radioStationFactory().CreateGroundStation(
            //     new GeoPoint(32.179766d, 34.834404d),
            //     Altitude.FromFeetMsl(100),
            //     Frequency.FromKhz(118400),
            //     "Pluto",
            //     "Pluto",
            //     out var pluto1Aether);
            // var stationPlutoSecondary = _radioStationFactory().CreateGroundStation(
            //     new GeoPoint(32.179766d, 34.834404d),
            //     Altitude.FromFeetMsl(100),
            //     Frequency.FromKhz(119150),
            //     "Pluto",
            //     "Pluto",
            //     out var pluto2Aether);
            //     
            // _radioAethersByKhz.Add(130850, new List<GroundRadioStationAether>() { llhzClrDelAether });
            // _radioAethersByKhz.Add(122200, new List<GroundRadioStationAether>() { llhzTwr1Aether });
            // _radioAethersByKhz.Add(129400, new List<GroundRadioStationAether>() { llhzTwr2Aether });
            // _radioAethersByKhz.Add(118400, new List<GroundRadioStationAether>() { pluto1Aether });
            // _radioAethersByKhz.Add(119150, new List<GroundRadioStationAether>() { pluto2Aether });
        }

        private static readonly Func<bool> __alwaysTruePredicate = () => true; 
        private static readonly Func<bool> __alwaysFalsePredicate = () => false; 
        
        public static ActorRef<WorldActor> Create(ISupervisorActor supervisor, DateTime startAtUtc)
        {
            var world = supervisor.CreateActor<WorldActor>(uniqueId => new WorldActor.WorldActivationEvent(
                uniqueId, 
                startAtUtc));
            return world;
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<WorldActor, WorldActivationEvent>(
                TypeString, 
                (activation, dependencies) => new WorldActor(
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<ILogger>(),
                    dependencies.Resolve<ISupervisorActor>(),
                    activation
                ));
        }

        internal class DeferredTaskQueue
        {
            private readonly ILogger _logger;
            private readonly LinkedList<WorkItem> _workItems = new();

            public DeferredTaskQueue(ILogger logger)
            {
                _logger = logger;
            }

            public IDeferHandle EnqueueWorkItem(Func<bool>? predicate, DateTime? deadlineUtc, Action? onPredicateTrue, Action? onTimeout)
            {
                var node = _workItems.AddLast(new WorkItem(0, onPredicateTrue, onTimeout, predicate, deadlineUtc));
                return new DeferredTaskQueueItemHandle(_workItems, node);
            }

            public void RunToCompletion(DateTime timestampUtc)
            {
                const int maxAllowedCycles = 10;
                
                for (int cycleCount = 0; cycleCount < maxAllowedCycles; cycleCount++)
                {
                    RunOnce(timestampUtc, out var processedItemCount);
                
                    if (processedItemCount == 0)
                    {
                        return;
                    }
                }
                
                _logger.EndlessLoopInDeferredTaskQueue();
            }

            public void RunOnce(DateTime timestampUtc, out int processedItemCount)
            {
                processedItemCount = 0;
                
                for (var node = _workItems.First ; node != null ; )
                {
                    var currentNode = node;
                    var nextNode = currentNode.Next;
                    
                    if (ShouldRunItem(node.Value, out var actionToRun))
                    {
                        processedItemCount++;
                        _workItems.Remove(node);
                        node.Value.Removed = true;
                        actionToRun?.Invoke();
                    }

                    node = nextNode;
                }

                bool ShouldRunItem(WorkItem item, out Action? actionToRun)
                {
                    if (item.Predicate == null || item.Predicate())
                    {
                        actionToRun = item.OnPredicateTrue;
                        return true;
                    }

                    if (item.DeadlineUtc.HasValue && timestampUtc >= item.DeadlineUtc)
                    {
                        actionToRun = item.OnTimeout;
                        return true;
                    }

                    actionToRun = null;
                    return false;
                }
            }
            
            private class DeferredTaskQueueItemHandle : IDeferHandle
            {
                private readonly LinkedList<WorkItem> _list;
                private readonly LinkedListNode<WorkItem> _node;

                public DeferredTaskQueueItemHandle(LinkedList<WorkItem> list, LinkedListNode<WorkItem> node)
                {
                    _list = list;
                    _node = node;
                }

                public void UpdateDeadline(DateTime newDeadlineUtc)
                {
                    _node.Value = _node.Value with {
                        DeadlineUtc = newDeadlineUtc
                    };
                }

                public void Cancel()
                {
                    if (!_node.Value.Removed)
                    {
                        _list.Remove(_node);
                        _node.Value.Removed = true;
                    }
                }
            }

            public record WorkItem(
                ulong Id,
                Action? OnPredicateTrue,
                Action? OnTimeout,
                Func<bool>? Predicate,
                DateTime? DeadlineUtc)
            {
                public bool Removed { get; set; } = false;
            }
        }
    }
}
