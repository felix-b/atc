using System;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.AI;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Zero.Loss.Actors;
using Zero.Loss.Actors.Impl;

namespace Atc.World.Tests.Separation
{
    [TestFixture]
    public class RouteAllocationGraphTests
    {
        [Test]
        public void CanBuildGraph()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Vertex("C", new GeoPoint(20, 20), Altitude.FromFeetMsl(2000))
                .Vertex("D", new GeoPoint(10, 20), Altitude.FromFeetMsl(1000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Edge("BC", from: "B", to: "C", directed: true)
                .Edge("CD", from: "C", to: "D", directed: true)
                .Edge("BD", from: "B", to: "D", directed: false)
                .Build();

            graph.VertexByName.Values.Select(v => v.Name).Should().BeEquivalentTo(new[] {"A", "B", "C", "D"});
            graph.EdgeByName.Values.Select(e => e.Name).Should().BeEquivalentTo(new[] {"AB", "BC", "CD", "BD"});

            var vertexA = graph.VertexByName["A"];
            vertexA.Altitude.Should().Be(Altitude.FromFeetMsl(1000));
            vertexA.Location.Should().Be(new GeoPoint(10, 10));
            vertexA.EdgesOut.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "AB[A->B]"
            });
            vertexA.EdgesIn.Should().BeEmpty();
            
            var vertexB = graph.VertexByName["B"];
            vertexB.Location.Should().Be(new GeoPoint(20, 10));
            vertexB.Altitude.Should().Be(Altitude.FromFeetMsl(2000));
            vertexB.EdgesOut.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "BC[B->C]", "BD[B<->D]"
            });
            vertexB.EdgesIn.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "AB[A->B]", "BD[B<->D]"
            });

            var vertexC = graph.VertexByName["C"];
            vertexC.EdgesOut.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "CD[C->D]"
            });
            vertexC.EdgesIn.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "BC[B->C]"
            });

            var vertexD = graph.VertexByName["D"];
            vertexD.EdgesOut.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "BD[B<->D]"
            });
            vertexD.EdgesIn.Select(e => e.ToString()).Should().BeEquivalentTo(new[] {
                "CD[C->D]", "BD[B<->D]"
            });
        }
        
        [Test]
        public void CanFindEdgeToVertex()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Vertex("C", new GeoPoint(20, 20), Altitude.FromFeetMsl(2000))
                .Vertex("D", new GeoPoint(10, 20), Altitude.FromFeetMsl(1000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Edge("BC", from: "B", to: "C", directed: true)
                .Edge("CD", from: "C", to: "D", directed: true)
                .Edge("BD", from: "B", to: "D", directed: false)
                .Build();

            graph.VertexByName["A"].TryFindEdgeTo("B")!.Name.Should().Be("AB");
            graph.VertexByName["B"].TryFindEdgeTo("D")!.Name.Should().Be("BD");
            graph.VertexByName["D"].TryFindEdgeTo("B")!.Name.Should().Be("BD");
            graph.VertexByName["B"].TryFindEdgeTo("A").Should().BeNull();
            graph.VertexByName["A"].TryFindEdgeTo("D").Should().BeNull();
        }

        [Test]
        public void CanCalculateEdgeDistance()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Vertex("C", new GeoPoint(20, 20), Altitude.FromFeetMsl(2000))
                .Vertex("D", new GeoPoint(10, 20), Altitude.FromFeetMsl(1000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Edge("BC", from: "B", to: "C", directed: true)
                .Edge("CD", from: "C", to: "D", directed: true)
                .Edge("BD", from: "B", to: "D", directed: false)
                .Build();

            graph.EdgeByName["AB"].Distance.Kilometers.Should().BeApproximately(1106f, precision: 1.0f);
            graph.EdgeByName["BD"].Distance.Kilometers.Should().BeApproximately(1541f, precision: 1.0f);
        }

        [Test]
        public void CanCheckForAllocationOverlap()
        {
            var time0 = new DateTime(2021, 12, 21, 0, 0, 0, DateTimeKind.Utc);
            var allocation = new RouteAllocationGraph.Allocation(
                MakeAircraftRef("#1"), 
                "wpt1",
                FromUtc: time0 + TimeSpan.FromMinutes(7),
                UntilUtc: time0 + TimeSpan.FromMinutes(8));

            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(7), time0 + TimeSpan.FromMinutes(8)).Should().BeTrue();
            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(6.9), time0 + TimeSpan.FromMinutes(7.1)).Should().BeTrue();
            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(7.9), time0 + TimeSpan.FromMinutes(8.1)).Should().BeTrue();
            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(7.5), time0 + TimeSpan.FromMinutes(7.6)).Should().BeTrue();
            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(6), time0 + TimeSpan.FromMinutes(9)).Should().BeTrue();

            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(5), time0 + TimeSpan.FromMinutes(6)).Should().BeFalse();
            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(6), time0 + TimeSpan.FromMinutes(7)).Should().BeFalse();
            allocation.OverlapsWith(time0 + TimeSpan.FromMinutes(8), time0 + TimeSpan.FromMinutes(9)).Should().BeFalse();
        }
        
        [Test]
        public void CanAllocateVertex()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var time1 = new DateTime(2021, 12, 21, 12, 31, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");

            var graph1 = graph0.WithWaypointAllocation(owner1, "A", time0, TimeSpan.FromMinutes(1), out var allocation);

            allocation.Should().NotBeNull();
            allocation.WaypointName.Should().Be("A");
            allocation.FromUtc.Should().Be(time0);
            allocation.UntilUtc.Should().Be(time1);
            allocation.Owner.UniqueId.Should().Be("#1");

            graph0.AllocationListByName.Should().BeEmpty();
            graph1.AllocationListByName.Should().NotBeEmpty();
            graph1.AllocationListByName["A"].Count.Should().Be(1);
            graph1.AllocationListByName["A"][0].Should().BeSameAs(allocation);
        }

        [Test]
        public void CanHaveConcurrentAllocationsOnDifferentVertices()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var time1 = new DateTime(2021, 12, 21, 12, 31, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");

            var graph1 = graph0.WithWaypointAllocation(owner1, "A", time0, TimeSpan.FromMinutes(1), out var allocation1);

            allocation1.Should().NotBeNull();
            allocation1.FromUtc.Should().Be(time0);
            allocation1.UntilUtc.Should().Be(time1);
            allocation1.Owner.UniqueId.Should().Be("#1");

            var graph2 = graph1.WithWaypointAllocation(owner1, "B", time0, TimeSpan.FromMinutes(1), out var allocation2);

            allocation2.Should().NotBeNull();
            allocation2.FromUtc.Should().Be(time0);
            allocation2.UntilUtc.Should().Be(time1);
            allocation2.Owner.UniqueId.Should().Be("#1");

            graph0.AllocationListByName.Should().BeEmpty();
            graph2.AllocationListByName.Count.Should().Be(2);
            graph2.AllocationListByName["A"].Count.Should().Be(1);
            graph2.AllocationListByName["A"][0].Should().BeSameAs(allocation1);
            graph2.AllocationListByName["B"].Count.Should().Be(1);
            graph2.AllocationListByName["B"][0].Should().BeSameAs(allocation2);
        }
        
        [Test]
        public void CanHaveNonConflictingAllocationsOnSameVertex()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");
            var owner2 = MakeAircraftRef("#2");
            var owner3 = MakeAircraftRef("#3");
            var owner4 = MakeAircraftRef("#4");
            var owner5 = MakeAircraftRef("#5");

            var graph1 = graph0.WithWaypointAllocation(
                owner1, "A", time0 + TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), out var allocation1);
            
            allocation1.Should().NotBeNull();
            allocation1.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(2));
            allocation1.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(3));
            allocation1.Owner.UniqueId.Should().Be("#1");

            var graph2 = graph1.WithWaypointAllocation(
                owner2, "A", time0 + TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1), out var allocation2);
            
            allocation2.Should().NotBeNull();
            allocation2.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(10));
            allocation2.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(11));
            allocation2.Owner.UniqueId.Should().Be("#2");

            var graph3 = graph2.WithWaypointAllocation(
                owner3, "A", time0 + TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), out var allocation3);
            
            allocation3.Should().NotBeNull();
            allocation3.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(5));
            allocation3.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(6));
            allocation3.Owner.UniqueId.Should().Be("#3");

            var graph4 = graph3.WithWaypointAllocation(
                owner4, "A", time0 + TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), out var allocation4);
            
            allocation4.Should().NotBeNull();
            allocation4.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(1));
            allocation4.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(2));
            allocation4.Owner.UniqueId.Should().Be("#4");

            var graph5 = graph4.WithWaypointAllocation(
                owner5, "A", time0 + TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(1), out var allocation5);
            
            allocation5.Should().NotBeNull();
            allocation5.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(15));
            allocation5.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(16));
            allocation5.Owner.UniqueId.Should().Be("#5");
            
            graph5.AllocationListByName["A"].Should().BeEquivalentTo(
                new[] { allocation4, allocation1, allocation3, allocation2, allocation5 }, 
                x => x.WithStrictOrdering());
        }

        [Test]
        public void CanResolveAllocationConflict_1()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");
            var owner2 = MakeAircraftRef("#2");

            var graph1 = graph0.WithWaypointAllocation(
                owner1, "A", time0 + TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), out var allocation1);
            
            var graph2 = graph1.WithWaypointAllocation(
                owner2, "A", time0 + TimeSpan.FromMinutes(1.5), TimeSpan.FromMinutes(1), out var allocation2);
            
            allocation2.Should().NotBeNull();
            allocation2.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(3));
            allocation2.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(4));
            allocation2.Owner.UniqueId.Should().Be("#2");
           
            graph2.AllocationListByName["A"].Should().BeEquivalentTo(
                new[] { allocation1, allocation2 }, 
                x => x.WithStrictOrdering());
        }

        [Test]
        public void CanResolveAllocationConflict_2()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");
            var owner2 = MakeAircraftRef("#2");
            var owner3 = MakeAircraftRef("#3");

            var graph1 = graph0.WithWaypointAllocation(
                owner1, "A", time0 + TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), out var allocation1);
            
            var graph2 = graph1.WithWaypointAllocation(
                owner2, "A", time0 + TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1), out var allocation2);
            
            var graph3 = graph2.WithWaypointAllocation(
                owner3, "A", time0 + TimeSpan.FromMinutes(1.5), TimeSpan.FromMinutes(1.5), out var allocation3);
            
            allocation3.Should().NotBeNull();
            allocation3.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(5));
            allocation3.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(6.5));
            allocation3.Owner.UniqueId.Should().Be("#3");

            graph3.AllocationListByName["A"].Should().BeEquivalentTo(
                new[] { allocation1, allocation2, allocation3 }, 
                x => x.WithStrictOrdering());
        }

        [Test]
        public void CanResolveAllocationConflict_3()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");
            var owner2 = MakeAircraftRef("#2");
            var owner3 = MakeAircraftRef("#3");
            var owner4 = MakeAircraftRef("#4");

            var graph1 = graph0.WithWaypointAllocation(
                owner1, "A", time0 + TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), out var allocation1);
            
            var graph2 = graph1.WithWaypointAllocation(
                owner2, "A", time0 + TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1), out var allocation2);
            
            var graph3 = graph2.WithWaypointAllocation(
                owner3, "A", time0 + TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(1), out var allocation3);
            
            var graph4 = graph3.WithWaypointAllocation(
                owner4, "A", time0 + TimeSpan.FromMinutes(1.5), TimeSpan.FromMinutes(1.5), out var allocation4);

            allocation4.Should().NotBeNull();
            allocation4.WaypointName.Should().Be("A");
            allocation4.FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(5));
            allocation4.UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(6.5));
            allocation4.Owner.UniqueId.Should().Be("#4");

            graph4.AllocationListByName["A"].Should().BeEquivalentTo(
                new[] { allocation1, allocation2, allocation4, allocation3 }, 
                x => x.WithStrictOrdering());
        }

        [Test]
        public void CanAllocateRouteWithoutConflicts()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Vertex("C", new GeoPoint(20, 20), Altitude.FromFeetMsl(2000))
                .Vertex("D", new GeoPoint(10, 20), Altitude.FromFeetMsl(1000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Edge("BC", from: "B", to: "C", directed: true)
                .Edge("CD", from: "C", to: "D", directed: true)
                .Edge("BD", from: "B", to: "D", directed: false)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");
            var owner2 = MakeAircraftRef("#2");
            var owner3 = MakeAircraftRef("#3");

            var graph1 = graph0
                .WithWaypointAllocation(owner1, "A", time0 + TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner1, "B", time0 + TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner1, "C", time0 + TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner1, "D", time0 + TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner2, "A", time0 + TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner2, "B", time0 + TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner2, "C", time0 + TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner2, "D", time0 + TimeSpan.FromMinutes(6), TimeSpan.FromMinutes(1), out _);

            var graph2 = graph1.WithRouteAllocation(
                owner3,
                time0 + TimeSpan.FromMinutes(5),
                new[] {
                    ("A", TimeSpan.FromMinutes(1)),
                    ("B", TimeSpan.FromMinutes(1)),
                    ("C", TimeSpan.FromMinutes(1)),
                    ("D", TimeSpan.FromMinutes(1)),
                },
                out var allocationList
            );

            allocationList.Count.Should().Be(4);
            graph2.AllocationListByName["A"].Last().Should().BeSameAs(allocationList[0]);
            graph2.AllocationListByName["B"].Last().Should().BeSameAs(allocationList[1]);
            graph2.AllocationListByName["C"].Last().Should().BeSameAs(allocationList[2]);
            graph2.AllocationListByName["D"].Last().Should().BeSameAs(allocationList[3]);
            
            allocationList[0].Owner.Should().Be(owner3);
            allocationList[0].WaypointName.Should().Be("A");
            allocationList[0].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(5));
            allocationList[0].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(6));

            allocationList[1].Owner.Should().Be(owner3);
            allocationList[1].WaypointName.Should().Be("B");
            allocationList[1].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(6));
            allocationList[1].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(7));
            
            allocationList[2].Owner.Should().Be(owner3);
            allocationList[2].WaypointName.Should().Be("C");
            allocationList[2].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(7));
            allocationList[2].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(8));

            allocationList[3].Owner.Should().Be(owner3);
            allocationList[3].WaypointName.Should().Be("D");
            allocationList[3].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(8));
            allocationList[3].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(9));
        }
        
        [Test]
        public void CanAllocateRouteWithConflictResolutions()
        {
            var builder = new RouteAllocationGraph.Builder();
            var graph0 = builder
                .Vertex("A", new GeoPoint(10, 10), Altitude.FromFeetMsl(1000))
                .Vertex("B", new GeoPoint(20, 10), Altitude.FromFeetMsl(2000))
                .Vertex("C", new GeoPoint(20, 20), Altitude.FromFeetMsl(2000))
                .Vertex("D", new GeoPoint(10, 20), Altitude.FromFeetMsl(1000))
                .Edge("AB", from: "A", to: "B", directed: true)
                .Edge("BC", from: "B", to: "C", directed: true)
                .Edge("CD", from: "C", to: "D", directed: true)
                .Edge("BD", from: "B", to: "D", directed: false)
                .Build();

            var time0 = new DateTime(2021, 12, 21, 12, 30, 0, DateTimeKind.Utc);
            var owner1 = MakeAircraftRef("#1");
            var owner2 = MakeAircraftRef("#2");
            var owner3 = MakeAircraftRef("#3");
            var owner4 = MakeAircraftRef("#4");
            var owner5 = MakeAircraftRef("#5");

            var graph1 = graph0
                .WithWaypointAllocation(owner1, "A", time0 + TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner1, "B", time0 + TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner1, "C", time0 + TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner1, "D", time0 + TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner2, "A", time0 + TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1), out _)
                .WithWaypointAllocation(owner2, "B", time0 + TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner2, "C", time0 + TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner2, "D", time0 + TimeSpan.FromMinutes(11), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner3, "A", time0 + TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner3, "B", time0 + TimeSpan.FromMinutes(8), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner3, "C", time0 + TimeSpan.FromMinutes(11), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner3, "D", time0 + TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(3), out _)
                .WithWaypointAllocation(owner4, "A", time0 + TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(2), out _);

            var graph2 = graph1.WithRouteAllocation(
                owner5,
                time0 + TimeSpan.FromMinutes(5),
                new[] {
                    ("A", TimeSpan.FromMinutes(1)),
                    ("B", TimeSpan.FromMinutes(1)),
                    ("C", TimeSpan.FromMinutes(1)),
                    ("D", TimeSpan.FromMinutes(1)),
                },
                out var allocationList
            );
            
            allocationList.Count.Should().Be(4);
            graph2.AllocationListByName["A"].Should().Contain(allocationList[0]);
            graph2.AllocationListByName["B"].Should().Contain(allocationList[1]);
            graph2.AllocationListByName["C"].Should().Contain(allocationList[2]);
            graph2.AllocationListByName["D"].Should().Contain(allocationList[3]);
            
            allocationList[0].Owner.Should().Be(owner5);
            allocationList[0].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(14));
            allocationList[0].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(15));

            allocationList[1].Owner.Should().Be(owner5);
            allocationList[1].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(15));
            allocationList[1].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(16));
            
            allocationList[2].Owner.Should().Be(owner5);
            allocationList[2].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(16));
            allocationList[2].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(17));

            allocationList[3].Owner.Should().Be(owner5);
            allocationList[3].FromUtc.Should().Be(time0 + TimeSpan.FromMinutes(17));
            allocationList[3].UntilUtc.Should().Be(time0 + TimeSpan.FromMinutes(18));
        }

        private ActorRef<Traffic.AircraftActor> MakeAircraftRef(string uniqueId)
        {
            return new ActorRef<Traffic.AircraftActor>(Mock.Of<IInternalSupervisorActor>(), uniqueId);
        }
    }
}