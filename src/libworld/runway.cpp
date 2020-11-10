// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <cctype>
#include "libworld.h"

using namespace std;

namespace world
{
    Runway::Runway(const Runway::End& _end1, const Runway::End& _end2, float _widthMeters) :
        m_name(_end1.name() + "/" + _end2.name()),
        m_end1(_end1),
        m_end2(_end2),
        m_widthMeters(_widthMeters),
        m_lengthMeters(TaxiEdge::calculateTaxiDistance(_end1.centerlinePoint(), _end2.centerlinePoint()))
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

    void Runway::calculateBounds()
    {
        float diagonal = (float)GeoMath::hypotenuse(30 + m_widthMeters / 2);
        m_bounds.A = GeoMath::getPointAtDistance(
            m_end1.centerlinePoint().geo(),
            GeoMath::addTurnToHeading(m_end1.heading(), 135),
            diagonal);
        m_bounds.B = GeoMath::getPointAtDistance(
            m_end1.centerlinePoint().geo(),
            GeoMath::addTurnToHeading(m_end1.heading(), -135),
            diagonal);
        m_bounds.C = GeoMath::getPointAtDistance(
            m_end2.centerlinePoint().geo(),
            GeoMath::addTurnToHeading(m_end2.heading(), 135),
            diagonal);
        m_bounds.D = GeoMath::getPointAtDistance(
            m_end2.centerlinePoint().geo(),
            GeoMath::addTurnToHeading(m_end2.heading(), -135),
            diagonal);
        m_bounds.minLatitude = min(m_bounds.A.latitude, min(m_bounds.B.latitude, min(m_bounds.C.latitude, m_bounds.D.latitude)));
        m_bounds.minLongitude = min(m_bounds.A.longitude, min(m_bounds.B.longitude, min(m_bounds.C.longitude, m_bounds.D.longitude)));
        m_bounds.maxLatitude = max(m_bounds.A.latitude, max(m_bounds.B.latitude, max(m_bounds.C.latitude, m_bounds.D.latitude)));
        m_bounds.maxLongitude = max(m_bounds.A.longitude, max(m_bounds.B.longitude, max(m_bounds.C.longitude, m_bounds.D.longitude)));
    }

    bool Runway::Bounds::contains(const GeoPoint& p) const
    {
        if (p.longitude < minLongitude || p.longitude > maxLongitude || p.latitude < minLatitude || p.latitude > maxLatitude)
        {
            return false;
        }

        return GeoMath::isPointInRectangle(p, A, B, C, D);
    }

    int Runway::getRunwayEndNumber(const string& name)
    {
        char digits[3] = { 0 };

        for (int i = 0 ; i < name.length() && i < 2 ; i++)
        {
            char c = name.at(i);
            if (isdigit(c))
            {
                digits[i] = c;
            }
        }

        return atoi(digits);
    }

    char Runway::getRunwayEndSuffix(const string& name)
    {
        char c = name.at(name.length() - 1);
        return !isdigit(c) ? c : 0;
    }
}
