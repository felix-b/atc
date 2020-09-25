// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include "libworld.h"

using namespace std;

namespace world
{
    Maneuver::Maneuver(Type _type, const string& _id, const vector<shared_ptr<Maneuver>>& children) :
        m_type(_type),
        m_id(_id),
        m_startTimestamp(0),
        m_finishTimestamp(0),
        m_state(State::NotStarted)
    {
        insertChildren(children);
    }

    void Maneuver::insertChildren(const vector<shared_ptr<Maneuver>>& children)
    {
        if (children.size() == 0)
        {
            return;
        }

        shared_ptr<Maneuver> lastChild = nullptr;

        for (const auto& child : children)
        {
            if (lastChild)
            {
                lastChild->m_nextSibling = child;
            }
            else
            {
                m_firstChild = child;
            }
            
            lastChild = child;
        }
    }

    shared_ptr<Maneuver> Maneuver::unProxy(shared_ptr<Maneuver> source)
    {
        return source->isProxy() ? source->unProxy() : source;
    }
}
