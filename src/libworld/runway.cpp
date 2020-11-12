// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <cctype>
#include "libworld.h"
#include <cmath>
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
        m_bounds.calculate(m_end1.centerlinePoint().geo(),
            m_end2.centerlinePoint().geo(),
                            m_end1.heading(),
                            m_widthMeters,
                            5);
        // Generate bounds for the taxiedges with active zones associated with this runway
        for (auto edge : m_activeZones)
        {
            Bounds& bounds = m_activeBounds[edge];
            if ((bounds.minLatitude == 0) && (bounds.maxLatitude == 0))
            {
                bounds.calculate(
                    edge->node1()->location().geo(),
                    edge->node2()->location().geo(),
                    edge->heading(),
                    edge->widthHint(),
                    0
                    );
            }
        }
    }

    void Runway::Bounds::calculate(const GeoPoint& end1, const GeoPoint& end2, float heading1_2, float widthMeters, float marginMeters)
    {
        GeoPoint Temp;
        marginMeters = sqrt(2 * (marginMeters * marginMeters) );
        Temp = GeoMath::getPointAtDistance(end1, GeoMath::addTurnToHeading(heading1_2, 90), widthMeters/2.);
        A = GeoMath::getPointAtDistance(Temp, GeoMath::addTurnToHeading(heading1_2 , 135), marginMeters);
        Temp = GeoMath::getPointAtDistance(end1, GeoMath::addTurnToHeading(heading1_2, -90), widthMeters/2.);
        B = GeoMath::getPointAtDistance(Temp, GeoMath::addTurnToHeading(heading1_2 , -135), marginMeters);
        Temp = GeoMath::getPointAtDistance(end2, GeoMath::addTurnToHeading(heading1_2, -90), widthMeters/2.);
        C = GeoMath::getPointAtDistance(Temp, GeoMath::addTurnToHeading(heading1_2 , -45), marginMeters);
        Temp = GeoMath::getPointAtDistance(end2, GeoMath::addTurnToHeading(heading1_2, 90), widthMeters/2.);
        D = GeoMath::getPointAtDistance(Temp, GeoMath::addTurnToHeading(heading1_2 , 45), marginMeters);

        minLatitude = min(A.latitude, min(B.latitude, min(C.latitude, D.latitude)));
        minLongitude = min(A.longitude, min(B.longitude, min(C.longitude, D.longitude)));
        maxLatitude = max(A.latitude, max(B.latitude, max(C.latitude, D.latitude)));
        maxLongitude = max(A.longitude, max(B.longitude, max(C.longitude, D.longitude)));

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
    
    void Runway::appendActiveZone(shared_ptr<TaxiEdge> edge)
    {
        if (std::find(m_activeZones.begin(), m_activeZones.end(), edge) == m_activeZones.end())
        {
            m_activeZones.push_back(edge);
        }
    }

    bool Runway::activeZonesContains(const GeoPoint location)
    {
        for (auto edge : m_activeZones)
        {
            Bounds& bounds = m_activeBounds[edge];

            if (bounds.contains(location))
            {
                return true;
            }
        }
        return false;
    }
}
