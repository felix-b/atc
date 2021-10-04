using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atc.Data.Primitives;
using Zero.Latency.Servers;
using Zero.Loss.Actors;

namespace Atc.World
{
    public partial class WorldActor
    {
        public class TrafficObservableQuery : IObservableQuery<AircraftActor>
        {
            private readonly WorldActor _target;
            private readonly Observer _observer;

            public TrafficObservableQuery(WorldActor target, GeoRect rect)
            {
                _target = target;
                _observer = new(target, rect, name: $"Traffic#{TakeNextObserverId()}");
            }

            public IObserverSubscription Subscribe(QueryObserver<AircraftActor> callback)
            {
                _observer.Callback = callback;
                _target.RegisterObserver(_observer);
                return _observer;
            }

            public IEnumerable<AircraftActor> GetResults()
            {
                return _observer.Run().Keys;
            }

            private static int __nextObserverId;

            private static int TakeNextObserverId()
            {
                return Interlocked.Increment(ref __nextObserverId);
            }
            
            private class Observer : IWorldObserver, IObserverSubscription
            {
                private WorldActor _target;
                private readonly GeoRect _rect;
                private readonly string _name;
                private Dictionary<AircraftActor, int>? _lastResult;

                public Observer(WorldActor target, GeoRect rect, string name)
                {
                    _target = target;
                    _rect = rect;
                    _name = name;

                    target._logger.TrafficQueryObserverCreated(rect.Min.Lat, rect.Min.Lon, rect.Max.Lat, rect.Max.Lon);
                }

                public ValueTask DisposeAsync()
                {
                    _target._logger.TrafficQueryObserverDisposing(_rect.Min.Lat, _rect.Min.Lon, _rect.Max.Lat, _rect.Max.Lon);
                    
                    _target._observers.Remove(this);
                    return ValueTask.CompletedTask;
                }

                public Dictionary<AircraftActor, int> Run()
                {
                    var result = _target.State.AircraftById.Values
                        .Select<ActorRef<AircraftActor>, AircraftActor>(r => r.Get())
                        .Where(IsAircraftInRect)
                        .ToDictionary(
                            ac => ac, 
                            ac => ac.StateVersion
                        );

                    if (_lastResult is null)
                    {
                        _lastResult = result;
                    }
                    
                    return result;
                }

                public void CheckForUpdates()
                {
                    if (Callback is null)
                    {
                        return;
                    }
                    
                    var prevResult = _lastResult;
                    var nextResult = Run();
                    _lastResult = nextResult;

                    CreateObservation(out var observation);
                    Callback(in observation);

                    void CreateObservation(out QueryObservation<AircraftActor> newObservation)
                    {
                        HashSet<AircraftActor> added = new();
                        HashSet<AircraftActor> updated = new();
                        HashSet<AircraftActor> removed = new();

                        if (prevResult != null)
                        {
                            CheckAddedAndUpdated();
                            CheckRemoved();
                        }

                        newObservation = new QueryObservation<AircraftActor>(added, updated, removed);

                        void CheckAddedAndUpdated()
                        {
                            foreach (var pair in nextResult)
                            {
                                var nextAircraft = pair.Key; 

                                if (prevResult != null && prevResult.TryGetValue(nextAircraft, out var prevVersion))
                                {
                                    var nextVersion = pair.Value;
                                    if (prevVersion != nextVersion)
                                    {
                                        updated.Add(nextAircraft);
                                    }
                                }
                                else 
                                {
                                    added.Add(nextAircraft);
                                }
                            }
                        }

                        void CheckRemoved()
                        {
                            foreach (var aircraft in prevResult!.Keys)
                            {
                                if (!nextResult.ContainsKey(aircraft))
                                {
                                    removed.Add(aircraft);
                                }
                            }
                        }
                    }
                }

                public string Name => _name;
                
                public QueryObserver<AircraftActor>? Callback { get; set; }
                
                private bool IsAircraftInRect(AircraftActor aircraft)
                {
                    var location = aircraft.Location;
                    return (
                        location.Lat >= _rect.Min.Lat &&
                        location.Lon >= _rect.Min.Lon &&
                        location.Lat < _rect.Max.Lat &&
                        location.Lon < _rect.Max.Lon);
                }
            }
        }
    }
}