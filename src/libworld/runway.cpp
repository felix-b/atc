// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include "libworld.h"

using namespace std;

namespace world
{
    Runway::Runway(const Runway::End& end1, const Runway::End& end2, float m_widthMeters) : 
        m_end1(end1), 
        m_end2(end2), 
        m_widthMeters(m_widthMeters), 
        m_lengthMeters(TaxiEdge::calculateTaxiDistance(end1.centerlinePoint(), end2.centerlinePoint()))
    {
    }

    const Runway::End& Runway::getEndOrThrow(const string& name)
    {
        if (name.compare(m_end1.name()) == 0)
        {
            return m_end1;
        }
        if (name.compare(m_end2.name()) == 0)
        {
            return m_end2;
        }

        throw runtime_error("Runway " + m_end1.name() + "/" + m_end2.name() + " has no end named " + name);
    }
}
