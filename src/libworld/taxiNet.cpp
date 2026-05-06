// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <algorithm>
#include <memory>
#include <iostream>
#include <unordered_set>
#include "libworld.h"
#include "stlhelpers.h"

using namespace std;

namespace world
{
//    class ClosestNodeFinder
//    {
//    private:
//        GeoPoint m_location;
//        shared_ptr<TaxiNode> m_closest;
//        double m_minDistanceMetric = -1;
//    public:
//        ClosestNodeFinder(const GeoPoint& _location) :
//            m_location(_location)
//        {
//        }
//    public:
//        void next(const shared_ptr<TaxiNode>& node)
//        {
//            const double distanceMetric =
//                abs(m_location.latitude - node->location().latitude()) +
//                abs(m_location.longitude - node->location().longitude());
//
//            if (m_minDistanceMetric < 0 || distanceMetric < m_minDistanceMetric)
//            {
//                m_minDistanceMetric = distanceMetric;
//                m_closest = node;
//            }
//        }
//    public:
//        const shared_ptr<TaxiNode>& getClosest() const { return m_closest; }
//    };

    shared_ptr<TaxiNode> TaxiNet::findClosestNode(
        const GeoPoint& location, 
        function<bool(shared_ptr<TaxiNode>)> predicate) const
    {
        ClosestItemFinder<TaxiNode> finder(location);

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
        ClosestItemFinder<TaxiNode> finder(location);

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

        ClosestItemFinder<TaxiNode> finder(location);

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

//    shared_ptr<TaxiPath> TaxiNet::tryFindArrivalPathRunwayToGate(
//        shared_ptr<HostServices> host,
//        shared_ptr<Runway> runway,
//        const Runway::End& runwayEnd,
//        shared_ptr<ParkingStand> gate,
//        const GeoPoint &fromPoint)
//    {
//        shared_ptr<TaxiEdge> exitEdge = tryFindExitFromRunway(
//            host, runway, runwayEnd, fromPoint,
//            GeoMath::getTurnDegrees(runwayEnd.heading(), gate->heading()));
//        if (!exitEdge)
//        {
//            return nullptr; // nowhere to go
//        }
//
//        auto path = TaxiPath::tryFind(shared_from_this(), exitEdge->node2()->location().geo(), gate->location().geo());
//        path->edges.insert(path->edges.begin(), exitEdge);
//        path->edges.insert(path->edges.begin(), shared_ptr<TaxiEdge>(new TaxiEdge(
//            fromPoint,
//            exitEdge->node1()->location()
//        )));
//
//        GeoPoint gateLineupPoint = GeoMath::getPointAtDistance(
//            gate->location().geo(),
//            GeoMath::flipHeading(gate->heading()),
//            40);
//
//        GeoPoint fullStopPoint = GeoMath::getPointAtDistance(
//            gate->location().geo(),
//            GeoMath::flipHeading(gate->heading()),
//            13);
//
//        path->appendEdgeTo(gateLineupPoint);
//        path->appendEdgeTo(fullStopPoint);
//
//        return path;
//    }

    shared_ptr<TaxiPath> TaxiNet::tryFindDepartureTaxiPathToRunway(
        const GeoPoint& fromPoint,
        const Runway::End& toRunwayEnd)
    {
        const auto hasRunwayEndEdge = [](shared_ptr<TaxiNode> node) {
            return hasAny<shared_ptr<TaxiEdge>>(node->edges(), [](const shared_ptr<TaxiEdge>& edge) {
                return edge->type() == TaxiEdge::Type::Runway;
            });
        };

        const auto targetRunwayNode = findClosestNode(
            toRunwayEnd.centerlinePoint().geo(),
            hasRunwayEndEdge);

        // Find the hold-short node just before the runway entrance
        // This is the node that has an edge to the runway node, but is not on the runway itself
        const auto findHoldShortNode = [this, &toRunwayEnd, targetRunwayNode, hasRunwayEndEdge]() -> shared_ptr<TaxiNode> {
            if (!targetRunwayNode)
            {
                return nullptr;
            }

            // Find nodes that connect to the runway node but aren't on the runway
            for (const auto& edge : targetRunwayNode->edges())
            {
                // Skip runway edges
                if (edge->type() == TaxiEdge::Type::Runway)
                {
                    continue;
                }

                // Get the node on the other end of this edge
                shared_ptr<TaxiNode> otherNode;
                if (edge->node1() == targetRunwayNode)
                {
                    otherNode = edge->node2();
                }
                else if (edge->node2() == targetRunwayNode)
                {
                    otherNode = edge->node1();
                }

                // Check if this node is NOT on the runway (i.e., it's a taxiway/hold-short node)
                if (otherNode && !hasRunwayEndEdge(otherNode))
                {
                    // Check if the edge has any active zone (departure/arrival/ils) for this runway end
                    // or if it's a hold-short type edge
                    bool hasActiveZone = edge->activeZones().hasAny();
                    bool isHoldShortEdge = edge->type() == TaxiEdge::Type::HoldShort;
                    
                    if (hasActiveZone || isHoldShortEdge)
                    {
                        return otherNode;
                    }
                }
            }

            // Fallback: find any node that connects to the runway node
            for (const auto& edge : targetRunwayNode->edges())
            {
                if (edge->type() == TaxiEdge::Type::Runway)
                {
                    continue;
                }

                if (edge->node1() == targetRunwayNode)
                {
                    return edge->node2();
                }
                else if (edge->node2() == targetRunwayNode)
                {
                    return edge->node1();
                }
            }

            return nullptr;
        };

        const auto holdShortNode = findHoldShortNode();

        const auto appendRunwayBacktrackIfNeeded =
            [this, &toRunwayEnd, &targetRunwayNode, &hasRunwayEndEdge](shared_ptr<TaxiPath> path) {
                if (!path || !targetRunwayNode)
                {
                    return path;
                }

                if (path->toNode == targetRunwayNode)
                {
                    return path;
                }

                if (!hasRunwayEndEdge(path->toNode))
                {
                    return path;
                }

                unordered_set<int> visited;
                auto currentNode = path->toNode;
                int safetyCounter = 0;

                while (currentNode && currentNode != targetRunwayNode && safetyCounter++ < 64)
                {
                    visited.insert(currentNode->id());

                    const float currentDistance = static_cast<float>(GeoMath::getDistanceMeters(
                        currentNode->location().geo(),
                        targetRunwayNode->location().geo()));

                    shared_ptr<TaxiEdge> bestEdge;
                    float bestDistance = currentDistance;

                    for (const auto& edge : currentNode->edges())
                    {
                        if (edge->type() != TaxiEdge::Type::Runway)
                        {
                            continue;
                        }

                        shared_ptr<TaxiEdge> orientedEdge;
                        if (edge->node1() == currentNode)
                        {
                            orientedEdge = edge;
                        }
                        else if (edge->node2() == currentNode)
                        {
                            if (!edge->canFlipOver())
                            {
                                continue;
                            }
                            orientedEdge = TaxiEdge::flipOver(edge);
                        }
                        else
                        {
                            continue;
                        }

                        if (!orientedEdge || visited.count(orientedEdge->node2()->id()) > 0)
                        {
                            continue;
                        }

                        const float candidateDistance = static_cast<float>(GeoMath::getDistanceMeters(
                            orientedEdge->node2()->location().geo(),
                            targetRunwayNode->location().geo()));

                        if (!bestEdge || candidateDistance < bestDistance)
                        {
                            bestEdge = orientedEdge;
                            bestDistance = candidateDistance;
                        }
                    }

                    if (!bestEdge)
                    {
                        break;
                    }

                    path->appendEdge(bestEdge);
                    currentNode = bestEdge->node2();
                }

                return path;
            };

        const TaxiPath::CostFunction costFunc = [](shared_ptr<TaxiEdge> edge) {
            Flight::Phase allocation = edge->flightPhaseAllocation();
            float factor = (allocation == Flight::Phase::Arrival
                ? 5.0f
                : (allocation == Flight::Phase::Departure ? 0.9f : 1.0f));
//            if (factor > 1.5f)
//            {
//                cout << "DEP > " << edge->id() << "/" << edge->name() << " : " << factor << endl;
//            }
            return edge->lengthMeters() * factor;
        };

        // Use hold-short node as destination if available, otherwise fall back to runway centerline
        auto destinationPoint = holdShortNode 
            ? holdShortNode->location().geo() 
            : toRunwayEnd.centerlinePoint().geo();
        auto destinationNode = holdShortNode ? holdShortNode : targetRunwayNode;

        auto path = TaxiPath::tryFind(
            shared_from_this(),
            fromPoint,
            destinationPoint,
            [](shared_ptr<TaxiNode> node) {
                return node->isRouteStart();
            },
            [&destinationNode](shared_ptr<TaxiNode> node) {
                return node == destinationNode;
            },
            costFunc);

        path = appendRunwayBacktrackIfNeeded(path);

        if (!path && targetRunwayNode)
        {
            // Use hold-short node as destination if available
            auto fallbackDestPoint = holdShortNode 
                ? holdShortNode->location().geo() 
                : targetRunwayNode->location().geo();
            auto fallbackDestNode = holdShortNode ? holdShortNode : targetRunwayNode;

            path = TaxiPath::tryFind(
                shared_from_this(),
                fromPoint,
                fallbackDestPoint,
                [](shared_ptr<TaxiNode> node) {
                    return node->isRouteStart();
                },
                [&fallbackDestNode](shared_ptr<TaxiNode> node) {
                    return node == fallbackDestNode;
                },
                costFunc);

            path = appendRunwayBacktrackIfNeeded(path);
        }

        if (path)
        {
            assignFlightPhaseAllocation(path, Flight::Phase::Departure);
        }

        return path;
    }

    shared_ptr<TaxiPath> TaxiNet::tryFindExitPathFromRunway(
        shared_ptr<HostServices> host,
        shared_ptr<Runway> runway,
        const Runway::End& runwayEnd,
        shared_ptr<ParkingStand> gate,
        const GeoPoint &fromPoint)
    {
        float headingToGate = GeoMath::getHeadingFromPoints(fromPoint, gate->location().geo());
        float turnToGateDegrees = GeoMath::getTurnDegrees(runwayEnd.heading(), headingToGate);

        shared_ptr<TaxiEdge> exitEdge = tryFindExitFromRunway(host, runway, runwayEnd, fromPoint,turnToGateDegrees);
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
        while (lastEdge->node2()->edges().size() >= 2 && count++ < 5)
        {
            const auto& nextEdges = lastEdge->node2()->edges();
            // Find the next edge that is not the one we just came from
            shared_ptr<TaxiEdge> nextEdge = nullptr;
            for (const auto& edge : nextEdges)
            {
                if (edge->id() != lastEdge->id())
                {
                    nextEdge = edge;
                    break;
                }
            }
            if (!nextEdge)
            {
                break; // No valid next edge found
            }
            lastEdge = nextEdge->nodeId1() == lastEdge->node2()->id()
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
        const TaxiPath::CostFunction costFunc = [](shared_ptr<TaxiEdge> edge) {
            Flight::Phase allocation = edge->flightPhaseAllocation();
            float factor = (allocation == Flight::Phase::Departure
                ? 5.0f
                : (allocation == Flight::Phase::Arrival ? 0.9f : 1.0f));
//            if (factor > 1.5f)
//            {
//                cout << "ARR > " << edge->id() << "/" << edge->name() << " : " << factor << endl;
//            }
            return edge->lengthMeters() * factor;
        };

        auto path = TaxiPath::tryFind(
            shared_from_this(),
            fromPoint,
            gate->location().geo(),
            function<bool(shared_ptr<TaxiNode>)>(),
            [](shared_ptr<TaxiNode> node) {
                return node->isRouteEnd();
            },
            costFunc);
        if (!path)
        {
            return nullptr;
        }

        assignFlightPhaseAllocation(path, Flight::Phase::Arrival);

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
        shared_ptr<HostServices> host,
        shared_ptr<Runway> runway,
        const Runway::End& runwayEnd,
        const GeoPoint &fromPoint,
        float turnToGateDegrees) const
    {
        const auto isInGateDirection = [&runwayEnd, turnToGateDegrees](shared_ptr<TaxiEdge> edge)->bool {
            float turnToEdgeDegrees = GeoMath::getTurnDegrees(runwayEnd.heading(), edge->heading());
            return turnToEdgeDegrees >= 0
                ? (turnToGateDegrees >= 0)
                : (turnToGateDegrees <= 0);
        };

        auto node = findClosestNodeOnRunway(fromPoint, runway, runwayEnd);
        shared_ptr<TaxiEdge> highSpeedExit;
        shared_ptr<TaxiEdge> regularExit;

        while (node)
        {
            highSpeedExit = node->tryFindEdge([&](shared_ptr<TaxiEdge> e) {
                return e->isHighSpeedExitRunway(runwayEnd.name()) && isInGateDirection(e);
            });
            if (highSpeedExit)
            {
                break;
            }
            if (!regularExit)
            {
                regularExit = node->tryFindEdge([&](shared_ptr<TaxiEdge> e) {
                    return e->type() == TaxiEdge::Type::Taxiway && isInGateDirection(e);
                });
            }
            shared_ptr<TaxiEdge> nextEdge = node->tryFindEdge([&](shared_ptr<TaxiEdge> e) {
                return e->isRunway(runwayEnd.name());
            });
            if (nextEdge)
            {
                // Move in the correct direction along the runway
                if (nextEdge->node1()->id() == node->id())
                {
                    node = nextEdge->node2();
                }
                else if (nextEdge->node2()->id() == node->id())
                {
                    node = nextEdge->node1();
                }
                else
                {
                    node = nullptr; // Edge doesn't connect to current node
                }
            }
            else
            {
                node = nullptr;
            }
        }

        return highSpeedExit ? highSpeedExit : regularExit;
    }

    void TaxiNet::assignFlightPhaseAllocation(shared_ptr<TaxiPath> path, Flight::Phase allocation)
    {
        for (const auto& edge : path->edges)
        {
            if (edge->flightPhaseAllocation() == Flight::Phase::NotAssigned)
            {
                edge->setFlightPhaseAllocation(allocation);
            }
        }
    }
}
