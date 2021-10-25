using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Primitives;
using Atc.Math;
using Zero.Loss.Actors;

namespace Atc.World.AI
{
    public class RouteAllocationGraph
    {
        public RouteAllocationGraph(
            ImmutableDictionary<string, Edge> edgeByName, 
            ImmutableDictionary<string, Vertex> vertexByName)
            : this(edgeByName, vertexByName, ImmutableDictionary<string, ImmutableSortedSet<Allocation>>.Empty)
        {
        }

        private RouteAllocationGraph(
            ImmutableDictionary<string, Edge> edgeByName, 
            ImmutableDictionary<string, Vertex> vertexByName,
            ImmutableDictionary<string, ImmutableSortedSet<Allocation>> allocationListByName)
        {
            EdgeByName = edgeByName;
            VertexByName = vertexByName;
            AllocationListByName = allocationListByName;
        }

        public RouteAllocationGraph WithWaypointAllocation(
            ActorRef<AircraftActor> owner, 
            string name, 
            DateTime fromUtc, 
            TimeSpan duration, 
            out Allocation allocation)
        {
            return WithWaypointAllocation(owner, name, fromUtc, fromUtc + duration, out allocation);
        }

        public RouteAllocationGraph WithWaypointAllocation(
            ActorRef<AircraftActor> owner, 
            string name, 
            Allocation previous, 
            TimeSpan duration, 
            out Allocation allocation)
        {
            return WithWaypointAllocation(owner, name, previous.UntilUtc, previous.UntilUtc + duration, out allocation);
        }

        public RouteAllocationGraph WithWaypointAllocation(
            ActorRef<AircraftActor> owner, 
            string waypointName, 
            DateTime fromUtc, 
            DateTime untilUtc,
            out Allocation allocation)
        {
            var existingAllocations = AllocationListByName.TryGetValue(waypointName, out var existingEntry)
                ? existingEntry
                : ImmutableSortedSet<Allocation>.Empty;

            var duration = untilUtc - fromUtc;
            var effectiveFromUtc = fromUtc;
            var effectiveUntilUtc = untilUtc;
            
            foreach (var existingAllocation in existingAllocations)
            {
                if (effectiveUntilUtc <= existingAllocation.FromUtc)
                {
                    return WithAllocationAdded(out allocation);
                }
                
                if (existingAllocation.OverlapsWith(effectiveFromUtc, effectiveUntilUtc))
                {
                    effectiveFromUtc = existingAllocation.UntilUtc;
                    effectiveUntilUtc = effectiveFromUtc + duration;
                }
            }

            return WithAllocationAdded(out allocation);

            RouteAllocationGraph WithAllocationAdded(out Allocation newAllocation)
            {
                newAllocation = new Allocation(owner, waypointName, effectiveFromUtc, effectiveUntilUtc);
                return new RouteAllocationGraph(
                    EdgeByName,
                    VertexByName,
                    AllocationListByName.SetItem(waypointName, existingAllocations.Add(newAllocation)));
            }
        }

        public RouteAllocationGraph WithRouteAllocation(
            ActorRef<AircraftActor> owner, 
            DateTime utc, 
            (string name, TimeSpan duration)[] waypoints, 
            out IReadOnlyList<Allocation> allocations)
        {
            var allocationTimes = new List<DateTime?>(Enumerable.Repeat<DateTime?>(null, waypoints.Length));
            var waypointIndex = 0;
            var firstWaypointExpectedTime = utc;
            var nextWaypointExpectedTime = utc;

            while (waypointIndex < waypoints.Length)
            {
                var (name, duration) = waypoints[waypointIndex];
                var asRequested = CanAllocateAsRequested(name, nextWaypointExpectedTime, duration, out var actualTime); 
                allocationTimes[waypointIndex] = actualTime;

                if (asRequested || waypointIndex == 0)
                {
                    waypointIndex++;
                    nextWaypointExpectedTime = actualTime + duration;
                }
                else
                {
                    waypointIndex = 0;
                    firstWaypointExpectedTime += (actualTime - nextWaypointExpectedTime);
                    nextWaypointExpectedTime = firstWaypointExpectedTime;
                }
            }

            var allocationListByNameBuilder = AllocationListByName.ToBuilder()!;
            var allocationsArray = new Allocation[waypoints.Length];

            for (int i = 0; i < waypoints.Length; i++)
            {
                AddAllocation(i);
            }

            allocations = allocationsArray;
            return new RouteAllocationGraph(EdgeByName, VertexByName, allocationListByNameBuilder.ToImmutable());

            void AddAllocation(int index)
            {
                var (name, duration) = waypoints[index];
                var allocationList = allocationListByNameBuilder.TryGetValue(name, out var existingEntry)
                    ? existingEntry
                    : ImmutableSortedSet<Allocation>.Empty;
                var allocation = new Allocation(owner, name, allocationTimes[index]!.Value, allocationTimes[index]!.Value + duration);
                allocationsArray[index] = allocation;
                allocationListByNameBuilder[name] = allocationList.Add(allocation);
            }
        }

        private bool CanAllocateAsRequested(
            string wayPointName, 
            DateTime requestedTimeUtc, 
            TimeSpan requestedDuration, 
            out DateTime actualTimeUtc)
        {
            var existingAllocations = AllocationListByName.TryGetValue(wayPointName, out var existingEntry)
                ? existingEntry
                : ImmutableSortedSet<Allocation>.Empty;

            var effectiveFromUtc = requestedTimeUtc;
            var effectiveUntilUtc = requestedTimeUtc + requestedDuration;
            
            foreach (var existingAllocation in existingAllocations)
            {
                if (effectiveUntilUtc <= existingAllocation.FromUtc)
                {
                    actualTimeUtc = effectiveFromUtc;
                    return (actualTimeUtc == requestedTimeUtc);
                }
                
                if (existingAllocation.OverlapsWith(effectiveFromUtc, effectiveUntilUtc))
                {
                    effectiveFromUtc = existingAllocation.UntilUtc;
                    effectiveUntilUtc = effectiveFromUtc + requestedDuration;
                }
            }

            actualTimeUtc = effectiveFromUtc;
            return (actualTimeUtc == requestedTimeUtc);
        }

        public ImmutableDictionary<string, Edge> EdgeByName { get; }
        public ImmutableDictionary<string, Vertex> VertexByName { get; }
        public ImmutableDictionary<string, ImmutableSortedSet<Allocation>> AllocationListByName { get; }
    
        public record Edge (
            string Name,
            string Vertex1Name,
            string Vertex2Name,
            bool Directed,
            Distance Distance)
        {
            public override string ToString()
            {
                return $"{Name}[{Vertex1Name}{(Directed ? "->" : "<->")}{Vertex2Name}]";
            }
        }

        public record Vertex (
            string Name,
            GeoPoint Location,
            Altitude Altitude,
            ImmutableArray<Edge> EdgesIn,
            ImmutableArray<Edge> EdgesOut)
        {
            public Edge? TryFindEdgeTo(string vertexName)
            {
                return EdgesOut.FirstOrDefault(e => e.Vertex1Name == vertexName || e.Vertex2Name == vertexName);
            }
        }

        public record Allocation (
            ActorRef<AircraftActor> Owner,
            string WaypointName,
            DateTime FromUtc,
            DateTime UntilUtc) : IComparable<Allocation>
        {
            public bool OverlapsWith(DateTime fromUtc, DateTime untilUtc)
            {
                return (
                    fromUtc < this.UntilUtc &&
                    untilUtc > this.FromUtc);
            }

            public int CompareTo(Allocation? other)
            {
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                if (ReferenceEquals(null, other))
                {
                    return 1;
                }

                var fromUtcComparison = FromUtc.CompareTo(other.FromUtc);
                if (fromUtcComparison != 0)
                {
                    return fromUtcComparison;
                }
                
                return UntilUtc.CompareTo(other.UntilUtc);
            }
        }

        public class Builder
        {
            private ImmutableDictionary<string, Edge>.Builder _edgeByName = 
                ImmutableDictionary.CreateBuilder<string, Edge>();

            private ImmutableDictionary<string, Vertex>.Builder _vertexByName = 
                ImmutableDictionary.CreateBuilder<string, Vertex>();

            public Builder Vertex(string name, GeoPoint location, Altitude altitude)
            {
                _vertexByName.Add(
                    name,
                    new Vertex(name, location, altitude, ImmutableArray<Edge>.Empty, ImmutableArray<Edge>.Empty)); 
                return this;
            }
            
            public Builder Edge(string name, string from, string to, bool directed)
            {
                var vertex1 = _vertexByName[from];
                var vertex2 = _vertexByName[to];
                GeoMath.CalculateGreatCircleLine(vertex1.Location, vertex2.Location, out var line);
                var edge = new Edge(name, from, to, directed, line.Length);
                
                _edgeByName.Add(name, edge);
                _vertexByName[from] = vertex1 with {
                    EdgesOut = vertex1.EdgesOut.Add(edge),
                    EdgesIn = directed ? vertex1.EdgesIn : vertex1.EdgesIn.Add(edge)
                } ;
                _vertexByName[to] = vertex2 with {
                    EdgesIn = vertex2.EdgesIn.Add(edge),
                    EdgesOut = directed ? vertex2.EdgesOut : vertex2.EdgesOut.Add(edge)
                };

                return this;
            }

            public RouteAllocationGraph Build()
            {
                return new RouteAllocationGraph(_edgeByName.ToImmutable(), _vertexByName.ToImmutable());
            }
        }

    }
}
