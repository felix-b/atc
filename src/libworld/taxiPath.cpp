// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"
#include <unordered_set>
#include <queue>
#include <iostream>
#include <sstream>
#include <iomanip>
#include <algorithm>

namespace world
{
    struct PathStep
    {
    public:
        int id;
        shared_ptr<TaxiEdge> edgeToHere;
        float lengthToHere;
    public:
        static bool compare(const PathStep& left, const PathStep& right) {
            return (left.lengthToHere > right.lengthToHere);
        };
    };

    typedef priority_queue<
        PathStep, 
        vector<PathStep>, 
        function<bool(const PathStep&, const PathStep&)>
    > PathStepPriorityQueue;

    TaxiPath::TaxiPath(
        const shared_ptr<TaxiNode> _fromNode,
        const shared_ptr<TaxiNode> _toNode,
        const vector<shared_ptr<TaxiEdge>>& _edges) : 
        fromNode(_fromNode),
        toNode(_toNode),
        edges(_edges)
    {
    }

    vector<string> TaxiPath::toHumanFriendlySteps()
    {
        vector<string> result;

        for (const auto& edge : edges)
        {
            if (edge->name().length() > 0)
            {
                if (result.size() == 0 || edge->name().compare(result[result.size() - 1]) != 0)
                {
                    result.push_back(edge->name());
                }
            }
        }

        return result;
    }

    string TaxiPath::toHumanFriendlyString()
    {
        auto steps = toHumanFriendlySteps();
        stringstream text;
        bool first = true;

        for (const auto& step : steps)
        {
            if (!first)
            {
                text << ",";
            }
            text << step;
            first = false;
        }

        return text.str();
    }

    void TaxiPath::appendEdge(shared_ptr<TaxiEdge> edge)
    {
        edges.push_back(edge);
        toNode = edge->node2();
    }

    void TaxiPath::appendEdgeTo(const UniPoint& destination)
    {
        auto newEdge = shared_ptr<TaxiEdge>(new TaxiEdge(
            toNode->location(),
            destination
        ));

        edges.push_back(newEdge);
        toNode = newEdge->node2();
    }

    shared_ptr<TaxiPath> TaxiPath::tryFind(
        shared_ptr<TaxiNet> taxiNet, 
        const GeoPoint& fromPoint, 
        const GeoPoint& toPoint,
        CostFunction costFunction)
    {
        return TaxiPath::tryFind(
            taxiNet,
            fromPoint,
            toPoint,
            function<bool(shared_ptr<TaxiNode>)>(),
            function<bool(shared_ptr<TaxiNode>)>(),
            costFunction);
    }

    shared_ptr<TaxiPath> TaxiPath::tryFind(
        shared_ptr<TaxiNet> taxiNet,
        const GeoPoint& fromPoint,
        const GeoPoint& toPoint,
        function<bool(shared_ptr<TaxiNode>)> fromNodePredicate,
        function<bool(shared_ptr<TaxiNode>)> toNodePredicate,
        CostFunction costFunction)
    {
        const auto isTaxiEdge = [](const shared_ptr<TaxiEdge>& edge) { 
            return (edge->type() == TaxiEdge::Type::Taxiway || 
                    edge->type() == TaxiEdge::Type::HoldShort);
        };
        const auto hasTaxiEdges = [isTaxiEdge](const shared_ptr<TaxiNode>& node) { 
            return hasAny<shared_ptr<TaxiEdge>>(node->edges(), isTaxiEdge);
        };

        const auto hasPreferredPredicates = static_cast<bool>(fromNodePredicate) || static_cast<bool>(toNodePredicate);

        const auto selectNode = [taxiNet, &hasTaxiEdges](
            const GeoPoint& location,
            const function<bool(shared_ptr<TaxiNode>)>& preferredPredicate,
            bool allowExactMatchWithoutTaxiEdges = false)
        {
            if (preferredPredicate)
            {
                auto preferredNode = taxiNet->findClosestNode(location, [&](shared_ptr<TaxiNode> node) {
                    return hasTaxiEdges(node) && preferredPredicate(node);
                });
                if (preferredNode)
                {
                    return preferredNode;
                }

                // For destination nodes (like runway nodes), also try to find a node that 
                // matches just the predicate even if it doesn't have taxi edges
                if (allowExactMatchWithoutTaxiEdges)
                {
                    auto exactNode = taxiNet->findClosestNode(location, preferredPredicate);
                    if (exactNode)
                    {
                        return exactNode;
                    }
                }

                auto junctionNode = taxiNet->findClosestNode(location, [&](shared_ptr<TaxiNode> node) {
                    return hasTaxiEdges(node) && node->isJunction();
                });
                if (junctionNode)
                {
                    return junctionNode;
                }
            }

            return taxiNet->findClosestNode(location, hasTaxiEdges);
        };

        const auto fromNode = selectNode(fromPoint, fromNodePredicate, false);
        const auto toNode = selectNode(toPoint, toNodePredicate, true);
        
        if (fromNode && toNode)
        {
            try
            {
                return TaxiPath::find(taxiNet, fromNode, toNode, costFunction);
            }
            catch (const runtime_error&)
            {
                if (hasPreferredPredicates)
                {
                    try
                    {
                        return TaxiPath::tryFind(taxiNet, fromPoint, toPoint, costFunction);
                    }
                    catch (const runtime_error&)
                    {
                        return nullptr;
                    }
                }
                return nullptr;
            }
        }

        return nullptr;
    }

    shared_ptr<TaxiPath> TaxiPath::find(
        shared_ptr<TaxiNet> net, 
        shared_ptr<TaxiNode> from, 
        shared_ptr<TaxiNode> to,
        CostFunction costFunction)
    {
        // uniform cost search

        unordered_map<int, PathStep> stepDoneById;
        PathStepPriorityQueue frontier(PathStep::compare);

        PathStep tail = { from->id(), nullptr, 0 };
        frontier.push(tail);

        while (true)
        {
            if (frontier.size() == 0)
            {
                stringstream errorMessage;
                errorMessage << setprecision(11)
                             << "Unable to find taxi path! From ["
                             << from->id() << "|" << from->location().geo().latitude << "," << from->location().geo().longitude
                             << "] to ["
                             << to->id() << "|" << to->location().geo().latitude << "," << to->location().geo().longitude << "]";
                throw runtime_error(errorMessage.str());
            }

            tail = frontier.top();
            frontier.pop();

            //cout << "ALG> frontier size[" << frontier.size() << "] min tail node[" << tail.id << "] cost[" << tail.lengthToHere << "]";
            if (tail.edgeToHere)
            {
                //cout << " from node[" << tail.edgeToHere->node1()->id() << "]" << endl;
            }
            else 
            {
                //cout << " (origin)" << endl;
            }

            if (tail.id == to->id())
            {
                //cout << "ALG> done: soultion found.";
                break;
            }

            stepDoneById.insert({ tail.id, tail });
            //cout << "ALG> stepDoneById = { ";
            // for (const auto& pair : stepDoneById)
            // {
            //     cout << "(" << pair.first << "<-" << (pair.second.edgeToHere ? pair.second.edgeToHere->node1()->id() : -1) << ":cost[" << pair.second.lengthToHere << "]) ";
            // }
            // cout << " }" << endl;

            auto tailNode = net->getNodeById(tail.id);
            for (const auto& edge : tailNode->edges())
            {
                // Allow routing over taxiways, hold short edges, and runway edges
                if (edge->type() != TaxiEdge::Type::Taxiway && 
                    edge->type() != TaxiEdge::Type::HoldShort &&
                    edge->type() != TaxiEdge::Type::Runway) 
                {
                    continue;
                }
                
                // Check both directions for bidirectional taxiways
                int nextId = -1;
                shared_ptr<TaxiEdge> traversalEdge = edge;
                
                if (edge->node1()->id() == tail.id)
                {
                    nextId = edge->node2()->id();
                }
                else if (edge->node2()->id() == tail.id && !edge->isOneWay())
                {
                    nextId = edge->node1()->id();
                    traversalEdge = TaxiEdge::flipOver(edge);
                }
                
                if (nextId != -1)
                {
                    bool alreadyVisited = (stepDoneById.find(nextId) != stepDoneById.end());
                    if (!alreadyVisited)
                    {
                        PathStep nextStep = { nextId, traversalEdge, tail.lengthToHere + costFunction(traversalEdge) };
                        //cout << "ALG> pushing step [" << tail.id << "->" << nextId << "] cost[" << nextStep.lengthToHere << "]" << endl;
                        frontier.push(nextStep);
                    }
                }
            }
        }

        vector<shared_ptr<TaxiEdge>> solution;
        while (tail.edgeToHere)
        {
            //cout << "TEST> tail = {" << tail.id << "," << tail.edgeToHere->name() << "," << tail.lengthToHere << "}" << endl;
            solution.push_back(tail.edgeToHere);
            //cout << "TEST> prev id = " << tail.edgeToHere->node1()->id() << endl;
            // Find the correct previous node based on traversal direction
            int prevNodeId = (tail.edgeToHere->node2()->id() == tail.id) 
                ? tail.edgeToHere->node1()->id() 
                : tail.edgeToHere->node2()->id();
            auto prevFind = stepDoneById.find(prevNodeId);
            if (prevFind == stepDoneById.end())
            {
                throw runtime_error("Failed to reverse construct taxi solution");
            }
            tail = prevFind->second;
        }

        reverse(solution.begin(), solution.end());
        return shared_ptr<TaxiPath>(new TaxiPath(from, to, solution));
    }
}
