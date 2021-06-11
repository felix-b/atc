using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atc.Data.Primitives;
using Zero.Latency.Servers;

namespace Atc.World
{
    public partial class RuntimeWorld
    {
        public class TrafficObservableQuery : IObservableQuery<RuntimeAircraft>
        {
            private readonly RuntimeWorld _target;
            private readonly Observer _observer;

            public TrafficObservableQuery(RuntimeWorld target, GeoRect rect)
            {
                _target = target;
                _observer = new(target, rect);
            }

            public IObserverSubscription Subscribe(QueryObserver<RuntimeAircraft> callback)
            {
                _observer.Callback = callback;
                _target.RegisterObserver(_observer);
                return _observer;
            }

            public IEnumerable<RuntimeAircraft> GetResults()
            {
                return _observer.Run().Keys;
            }

            private class Observer : IWorldObserver, IObserverSubscription
            {
                private RuntimeWorld _target;
                private readonly GeoRect _rect;
                private Dictionary<RuntimeAircraft, int>? _lastResult;

                public Observer(RuntimeWorld target, GeoRect rect)
                {
                    _target = target;
                    _rect = rect;

                    target._logger.TrafficQueryObserverCreated(rect.Min.Lat, rect.Min.Lon, rect.Max.Lat, rect.Max.Lon);
                }

                public ValueTask DisposeAsync()
                {
                    _target._logger.TrafficQueryObserverDisposing(_rect.Min.Lat, _rect.Min.Lon, _rect.Max.Lat, _rect.Max.Lon);
                    
                    _target._observers.Remove(this);
                    return ValueTask.CompletedTask;
                }

                public Dictionary<RuntimeAircraft, int> Run()
                {
                    var result = _target._state.AircraftById.Values
                        .Where(IsAircraftInRect)
                        .ToDictionary(
                            ac => ac, 
                            ac => ac.GetState().Version
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

                    void CreateObservation(out QueryObservation<RuntimeAircraft> newObservation)
                    {
                        HashSet<RuntimeAircraft> added = new();
                        HashSet<RuntimeAircraft> updated = new();
                        HashSet<RuntimeAircraft> removed = new();

                        if (prevResult != null)
                        {
                            CheckAddedAndUpdated();
                            CheckRemoved();
                        }

                        newObservation = new QueryObservation<RuntimeAircraft>(added, updated, removed);

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

                public QueryObserver<RuntimeAircraft>? Callback { get; set; }
                
                private bool IsAircraftInRect(RuntimeAircraft aircraft)
                {
                    var location = aircraft.GetState().Location;
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