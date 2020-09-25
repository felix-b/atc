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
    shared_ptr<TaxiNode> TaxiNet::findClosestNode(
        const GeoPoint& location, 
        function<bool(shared_ptr<TaxiNode>)> predicate) const
    {
        shared_ptr<TaxiNode> closest;
        double minDistanceMetric = -1;

        for (const auto node : m_nodes)
        {
            if (!predicate(node))
            {
                continue;
            }
            
            const double distanceMetric = 
                abs(location.latitude - node->location().latitude()) + 
                abs(location.longitude - node->location().longitude());

            if (minDistanceMetric < 0 || distanceMetric < minDistanceMetric)
            {
                minDistanceMetric = distanceMetric;
                closest = node;
            }
        }

        return closest;
    }
}