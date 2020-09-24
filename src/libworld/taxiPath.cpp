// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include "libworld.h"
#include <unordered_set>
#include <queue>
#include <iostream>
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
            if (result.size() == 0 || edge->name().compare(result[result.size()-1]) != 0)
            {
                result.push_back(edge->name());
            }
        }

        return result;
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
        const GeoPoint& toPoint)
    {
        const auto isTaxiEdge = [](const shared_ptr<TaxiEdge>& edge) { 
            return (edge->type() == TaxiEdge::Type::Taxiway); 
        };
        const auto hasTaxiEdges = [isTaxiEdge](const shared_ptr<TaxiNode>& node) { 
            return hasAny<shared_ptr<TaxiEdge>>(node->edges(), isTaxiEdge);
        };

        const auto fromNode = taxiNet->findClosestNode(fromPoint, hasTaxiEdges);
        const auto toNode = taxiNet->findClosestNode(toPoint, hasTaxiEdges);
        
        if (fromNode && toNode)
        {
            auto taxiPath = TaxiPath::find(taxiNet, fromNode, toNode);
            return taxiPath;
        }

        return nullptr;
    }

    shared_ptr<TaxiPath> TaxiPath::find(
        shared_ptr<TaxiNet> net, 
        shared_ptr<TaxiNode> from, 
        shared_ptr<TaxiNode> to)
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
                throw runtime_error("Requested taxi path could not be found");
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
                if (edge->type() != TaxiEdge::Type::Taxiway) 
                {
                    continue;
                }
                
                int nextId = edge->node2()->id();
                bool alreadyVisited = (stepDoneById.find(nextId) != stepDoneById.end());
                if (!alreadyVisited)
                {
                    PathStep nextStep = { nextId, edge, tail.lengthToHere + edge->lengthMeters() };
                    //cout << "ALG> pushing step [" << tail.id << "->" << nextId << "] cost[" << nextStep.lengthToHere << "]" << endl;
                    frontier.push(nextStep);
                }
            }
        }

        vector<shared_ptr<TaxiEdge>> solution;
        while (tail.edgeToHere)
        {
            //cout << "TEST> tail = {" << tail.id << "," << tail.edgeToHere->name() << "," << tail.lengthToHere << "}" << endl;
            solution.push_back(tail.edgeToHere);
            //cout << "TEST> prev id = " << tail.edgeToHere->node1()->id() << endl;
            auto prevFind = stepDoneById.find(tail.edgeToHere->node1()->id());
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
