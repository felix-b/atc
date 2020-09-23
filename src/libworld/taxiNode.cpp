// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <algorithm>
#include "libworld.h"

using namespace std;

namespace world
{
    shared_ptr<TaxiEdge> TaxiNode::getEdgeTo(shared_ptr<TaxiNode> node)
    {
        const auto found = find_if(m_edges.begin(), m_edges.end(), [&](const shared_ptr<TaxiEdge>& edge) {
            return edge->node2() == node;
        });
        if (found == m_edges.end())
        {
            throw runtime_error("Edge to specified node could not be found");
        }
        return *found;
    }
}