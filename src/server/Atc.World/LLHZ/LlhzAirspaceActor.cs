using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.AI;
using ProtoBuf.WellKnownTypes;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzAirspaceActor : StatefulActor<LlhzAirspaceActor.AirspaceState>
    {
        public const string TypeString = "airspace/ctr/LLHZ";
        
        public record AirspaceState(
            RouteAllocationGraph RouteGraph
        );

        public record ActivationEvent(
            string UniqueId
        ) : IActivationStateEvent<LlhzAirspaceActor>;

        public record AllocateRouteEvent(
            ActorRef<AircraftActor> Owner,
            DateTime StartAtUtc,
            WaypointAllocationRequest[] Waypoints
        ) : IStateEvent;

        public record WaypointAllocationRequest(
            string WaypointName,
            TimeSpan Duration
        );

        private readonly IStateStore _store; 

        [NotEventSourced]
        private IReadOnlyList<RouteAllocationGraph.Allocation>? _lastRouteAllocations = null;
        
            
        public LlhzAirspaceActor(IStateStore store, ActivationEvent activation)
            : base(TypeString, activation.UniqueId, new AirspaceState(BuildRouteGraph()))
        {
            _store = store;
        }

        public IReadOnlyList<RouteAllocationGraph.Allocation> AllocatePatternRoute(ActorRef<AircraftActor> aircraft, DateTime utc)
        {
            var request = new AllocateRouteEvent(aircraft, utc, new [] {
                new WaypointAllocationRequest("v_RWY29", TimeSpan.FromSeconds(30)),
                new WaypointAllocationRequest("v_RWY11", TimeSpan.FromSeconds(30)),
                new WaypointAllocationRequest("v_R29UW", TimeSpan.FromSeconds(30)),
                new WaypointAllocationRequest("v_R29DW", TimeSpan.FromSeconds(30)),
                new WaypointAllocationRequest("v_R29B", TimeSpan.FromSeconds(30)),
                new WaypointAllocationRequest("v_29F", TimeSpan.FromSeconds(30)),
                new WaypointAllocationRequest("v_RWY29", TimeSpan.FromSeconds(30))
            });

            _store.Dispatch(this, request);
            return TakeLastRouteAllocations();
        } 

        protected override AirspaceState Reduce(AirspaceState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case AllocateRouteEvent allocateRoute:
                    var waypointsArray = allocateRoute.Waypoints
                        .Select(w => (name: w.WaypointName, duration: w.Duration))
                        .ToArray(); 
                    return stateBefore with {
                        RouteGraph = stateBefore.RouteGraph.WithRouteAllocation(
                            allocateRoute.Owner,
                            allocateRoute.StartAtUtc,
                            waypointsArray,
                            out _lastRouteAllocations
                        )
                    };
                default:
                    return stateBefore;
            }
        }

        private IReadOnlyList<RouteAllocationGraph.Allocation> TakeLastRouteAllocations()
        {
            var result = _lastRouteAllocations 
                ?? throw new InvalidOperationException("No last route allocations");

            _lastRouteAllocations = null;
            return result;
        }
        
        private static RouteAllocationGraph BuildRouteGraph()
        {
            var builder = new RouteAllocationGraph.Builder();

            builder
                .Vertex("v_RWY29", new GeoPoint(32.179256, 34.838035), Altitude.FromFeetMsl(110))
                .Vertex("v_RWY11", new GeoPoint(32.181418, 34.830673), Altitude.FromFeetMsl(90))
                .Vertex("v_R29UW", new GeoPoint(32.183481, 34.824624), Altitude.FromFeetMsl(400))
                .Vertex("v_R29DW", new GeoPoint(32.193079, 34.828169), Altitude.FromFeetMsl(800))
                .Vertex("v_R29B", new GeoPoint(32.187821, 34.854620), Altitude.FromFeetMsl(800))
                .Vertex("v_29F", new GeoPoint(32.176325, 34.851372), Altitude.FromFeetMsl(600))
                .Vertex("v_BATZRA_800", new GeoPoint(32.219004, 34.883246), Altitude.FromFeetMsl(800))
                .Vertex("v_BATZRA_1200", new GeoPoint(32.219004, 34.883246), Altitude.FromFeetMsl(1200))
                .Vertex("v_BATZRA_2000", new GeoPoint(32.219004, 34.883246), Altitude.FromFeetMsl(2000))
                .Vertex("v_BNEY_DROR_1500", new GeoPoint(32.255206, 34.891128), Altitude.FromFeetMsl(1500))
                .Vertex("v_BNEY_DROR_2000", new GeoPoint(32.255206, 34.891128), Altitude.FromFeetMsl(2000))
                .Vertex("v_TZOMET_HASHARON_1500", new GeoPoint(32.323442, 34.904178), Altitude.FromFeetMsl(1500))
                .Vertex("v_TZOMET_HASHARON_2000", new GeoPoint(32.323442, 34.904178), Altitude.FromFeetMsl(2000));

            builder
                .Vertex("v_TZ_3", new GeoPoint(0, 0), Altitude.FromFeetMsl(1500))
                .Vertex("v_TZ_8", new GeoPoint(0, 0), Altitude.FromFeetMsl(1500))
                .Vertex("v_TZ_9", new GeoPoint(0, 0), Altitude.FromFeetMsl(1500))
                .Vertex("v_TZ_12", new GeoPoint(0, 0), Altitude.FromFeetMsl(1500))
                .Vertex("v_TZ_13", new GeoPoint(0, 0), Altitude.FromFeetMsl(1500))
                .Vertex("v_TZ_11", new GeoPoint(0, 0), Altitude.FromFeetMsl(1500));
            
            builder
                .Edge("e_RWY", "v_RWY29", "v_RWY11", directed: true)
                .Edge("e_29_UPWND", "v_RWY11", "v_R29UW", directed: true)
                .Edge("e_29_RXWND", "v_R29UW", "v_R29DW", directed: true)
                .Edge("e_29_RDWND", "v_R29DW", "v_R29B", directed: true)
                .Edge("e_29_RBASE", "v_R29B", "v_29F", directed: true)
                .Edge("e_29_FINAL", "v_29F", "v_RWY29", directed: true);

            builder
                .Edge("e_TO29_BATZRA800", "v_R29DW", "v_BATZRA_800", directed: true)
                .Edge("e_BATZRA800_TZ3", "v_BATZRA_800", "v_TZ_3", directed: true)
                .Edge("e_BATZRA800_TZ8", "v_BATZRA_800", "v_TZ_8", directed: true)
                .Edge("e_BATZRA800_TZ9", "v_BATZRA_800", "v_TZ_9", directed: true)
                .Edge("e_BATZRA800_BNEYDROR1500", "v_BATZRA_800", "v_BNEY_DROR_1500", directed: true)
                .Edge("e_BNEYDROR1500_HASHARON1500", "v_BNEY_DROR_1500", "v_TZOMET_HASHARON_1500", directed: true)
                .Edge("e_HASHARON1500_TZ12", "v_TZOMET_HASHARON_1500", "v_TZ_12", directed: true)
                .Edge("e_HASHARON1500_TZ13", "v_TZOMET_HASHARON_1500", "v_TZ_13", directed: true);

            builder
                .Edge("e_TZ12_HASHARON2000", "v_TZ_12", "v_TZOMET_HASHARON_2000", directed: true)
                .Edge("e_TZ13_HASHARON2000", "v_TZ_13", "v_TZOMET_HASHARON_2000", directed: true)
                .Edge("e_HASHARON2000_BNEYDROR2000", "v_TZOMET_HASHARON_2000", "v_BNEY_DROR_2000", directed: true)
                .Edge("e_BNEYDROR2000_BATZRA2000", "v_BNEY_DROR_2000", "v_BATZRA_2000", directed: true)
                .Edge("e_BATZRA2000_RB29", "v_BATZRA_2000", "v_R29B", directed: true)
                .Edge("e_TZ3_BATZRA1200", "v_TZ_3", "v_BATZRA_1200", directed: true)
                .Edge("e_TZ8_BATZRA1200", "v_TZ_8", "v_BATZRA_1200", directed: true)
                .Edge("e_TZ9_BATZRA1200", "v_TZ_9", "v_BATZRA_1200", directed: true)
                .Edge("e_BATZRA1200_RB29", "v_BATZRA_1200", "v_R29B", directed: true);

            return builder.Build();
        }
    }
}