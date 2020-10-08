// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <algorithm>
#include <memory>
#include "libworld.h"
#include "stlhelpers.h"

using namespace std;

namespace world
{
    class ClosestNodeFinder
    {
    private:
        GeoPoint m_location;
        shared_ptr<TaxiNode> m_closest;
        double m_minDistanceMetric = -1;
    public:
        ClosestNodeFinder(const GeoPoint& _location) :
            m_location(_location)
        {
        }
    public:
        void next(const shared_ptr<TaxiNode>& node)
        {
            const double distanceMetric =
                abs(m_location.latitude - node->location().latitude()) +
                abs(m_location.longitude - node->location().longitude());

            if (m_minDistanceMetric < 0 || distanceMetric < m_minDistanceMetric)
            {
                m_minDistanceMetric = distanceMetric;
                m_closest = node;
            }
        }
    public:
        const shared_ptr<TaxiNode>& getClosest() const { return m_closest; }
    };

    shared_ptr<TaxiNode> TaxiNet::findClosestNode(
        const GeoPoint& location, 
        function<bool(shared_ptr<TaxiNode>)> predicate) const
    {
        ClosestNodeFinder finder(location);

        for (const auto node : m_nodes)
        {
            if (predicate(node))
            {
                finder.next(node);
            }
        }

        return finder.getClosest();
    }

    shared_ptr<TaxiNode> TaxiNet::findClosestNode(
        const GeoPoint &location,
        const vector<shared_ptr<TaxiNode>>& possibleNodes) const
    {
        ClosestNodeFinder finder(location);

        for (const auto node : possibleNodes)
        {
            finder.next(node);
        }

        return finder.getClosest();
    }

//    void TaxiNet::findNodesAheadOnRunway(
//        const GeoPoint& location,
//        const shared_ptr<Runway>& runway,
//        const Runway::End& runwayEnd,
//        vector<shared_ptr<TaxiNode>>& nodesAhead) const
//    {
//        const auto isNodeAhead = [&](const shared_ptr<TaxiNode>& node) {
//            float headingToNode = GeoMath::getHeadingFromPoints(location, node->location().geo());
//            float turnToNodeDegrees = GeoMath::getTurnDegrees(runwayEnd.heading(), headingToNode);
//            return (abs(turnToNodeDegrees) < 45);
//        };
//
//        for (const auto& edge : runway->edges())
//        {
//            if (isNodeAhead(edge->node1()))
//            {
//                nodesAhead.push_back(edge->node1());
//            }
//
//            if (isNodeAhead(edge->node2()))
//            {
//                nodesAhead.push_back(edge->node2());
//            }
//        }
//    }

    shared_ptr<TaxiNode> TaxiNet::findClosestNodeOnRunway(
        const GeoPoint &location,
        const shared_ptr<Runway>& runway,
        const Runway::End &runwayEnd) const
    {
        const auto isNodeAhead = [&](const shared_ptr<TaxiNode>& node) {
            float headingToNode = GeoMath::getHeadingFromPoints(location, node->location().geo());
            float turnToNodeDegrees = GeoMath::getTurnDegrees(runwayEnd.heading(), headingToNode);
            return (abs(turnToNodeDegrees) < 45);
        };

        ClosestNodeFinder finder(location);

        for (const auto& edge : runway->edges())
        {
            const auto& effectiveEdge = edge->isRunway(runwayEnd.name())
                ? edge
                : TaxiEdge::flipOver(edge);

            if (isNodeAhead(effectiveEdge->node1()))
            {
                finder.next(effectiveEdge->node1());
            }

            if (isNodeAhead(effectiveEdge->node2()))
            {
                finder.next(effectiveEdge->node2());
            }
        }

        return finder.getClosest();
    }

    shared_ptr<TaxiPath> TaxiNet::tryFindArrivalPathRunwayToGate(
        shared_ptr<Runway> runway,
        const Runway::End& runwayEnd,
        shared_ptr<ParkingStand> gate,
        const GeoPoint &fromPoint)
    {
        shared_ptr<TaxiEdge> exitEdge = tryFindExitFromRunway(runway, runwayEnd, fromPoint);
        if (!exitEdge)
        {
            return nullptr; // nowhere to go
        }

        auto path = TaxiPath::tryFind(shared_from_this(), exitEdge->node2()->location().geo(), gate->location().geo());
        path->edges.insert(path->edges.begin(), exitEdge);
        path->edges.insert(path->edges.begin(), shared_ptr<TaxiEdge>(new TaxiEdge(
            fromPoint,
            exitEdge->node1()->location()
        )));

        GeoPoint gateLineupPoint = GeoMath::getPointAtDistance(
            gate->location().geo(),
            GeoMath::flipHeading(gate->heading()),
            40);

        GeoPoint fullStopPoint = GeoMath::getPointAtDistance(
            gate->location().geo(),
            GeoMath::flipHeading(gate->heading()),
            13);

        path->appendEdgeTo(gateLineupPoint);
        path->appendEdgeTo(fullStopPoint);

        return path;
    }

    shared_ptr<TaxiPath> TaxiNet::tryFindExitPathFromRunway(
        shared_ptr<Runway> runway,
        const Runway::End& runwayEnd,
        shared_ptr<ParkingStand> gate,
        const GeoPoint &fromPoint)
    {
        shared_ptr<TaxiEdge> exitEdge = tryFindExitFromRunway(runway, runwayEnd, fromPoint);
        if (!exitEdge)
        {
            return nullptr; // nowhere to go
        }

        auto preExitEdge = shared_ptr<TaxiEdge>(new TaxiEdge(
            fromPoint,
            exitEdge->node1()->location()
        ));

        auto path = shared_ptr<TaxiPath>(new TaxiPath(preExitEdge->node1(), exitEdge->node2(), {
            preExitEdge,
            exitEdge
        }));

        auto lastEdge = exitEdge;
        int count = 0;
        while (lastEdge->node2()->edges().size() == 2 && count++ < 5)
        {
            const auto& nextEdges = lastEdge->node2()->edges();
            auto nextEdge = (nextEdges[0]->id() == lastEdge->id() ? nextEdges[1] : nextEdges[0]);
            lastEdge = nextEdge->nodeId1() == lastEdge->nodeId2()
                ? nextEdge
                : TaxiEdge::flipOver(nextEdge);
            path->appendEdge(lastEdge);
        }

        return path;
    }

    shared_ptr<TaxiPath> TaxiNet::tryFindTaxiPathToGate(
        shared_ptr<ParkingStand> gate,
        const GeoPoint &fromPoint)
    {
        auto path = TaxiPath::tryFind(shared_from_this(), fromPoint, gate->location().geo());

        GeoPoint gateLineupPoint = GeoMath::getPointAtDistance(
            gate->location().geo(),
            GeoMath::flipHeading(gate->heading()),
            40);

        GeoPoint fullStopPoint = GeoMath::getPointAtDistance(
            gate->location().geo(),
            GeoMath::flipHeading(gate->heading()),
            13);

        path->appendEdgeTo(gateLineupPoint);
        path->appendEdgeTo(fullStopPoint);

        return path;
    }

    shared_ptr<TaxiEdge> TaxiNet::tryFindExitFromRunway(
        shared_ptr<Runway> runway,
        const Runway::End& runwayEnd,
        const GeoPoint &fromPoint) const
    {
        auto node = findClosestNodeOnRunway(fromPoint, runway, runwayEnd);
        shared_ptr<TaxiEdge> highSpeedExit;
        shared_ptr<TaxiEdge> regularExit;

        while (node)
        {
            highSpeedExit = node->tryFindEdge([&](shared_ptr<TaxiEdge> e) {
                return e->isHighSpeedExitRunway(runwayEnd.name());
            });
            if (highSpeedExit)
            {
                break;
            }
            if (!regularExit)
            {
                regularExit = node->tryFindEdge([&](shared_ptr<TaxiEdge> e) {
                    return e->type() == TaxiEdge::Type::Taxiway;
                });
            }
            shared_ptr<TaxiEdge> nextEdge = node->tryFindEdge([&](shared_ptr<TaxiEdge> e) {
                return e->isRunway(runwayEnd.name());
            });
            node = nextEdge ? nextEdge->node2() : nullptr;
        }

        return highSpeedExit ? highSpeedExit : regularExit;
    }
}