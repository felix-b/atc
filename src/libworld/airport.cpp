// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <sstream>
#include "libworld.h"

namespace world
{
    shared_ptr<Runway> Airport::findLongestRunway()
    {
        const auto compareRunwayLength = [](const shared_ptr<Runway>& r1, const shared_ptr<Runway>& r2) {
            return (r1->lengthMeters() < r2->lengthMeters());
        };

        auto it = max_element(m_runways.begin(), m_runways.end(), compareRunwayLength);
        shared_ptr<Runway> longestRunway = it != m_runways.end()
           ? *it
           : nullptr;

        if (!longestRunway)
        {
            stringstream errorMessage;
            errorMessage
                << "Could not find longest runway at ["
                << m_header.icao()
                << "] are there any runways at this airport??";
            throw runtime_error(errorMessage.str());
        }

        return longestRunway;
    }

    shared_ptr<Runway> Airport::getRunwayOrThrow(const string& name) const
    {
        auto runway = tryFindRunway(name);
        if (!runway)
        {
            stringstream errorMessage;
            errorMessage 
                << "Runway '" << name 
                << "' could not be found at airport '" << m_header.icao() << "'";
            throw runtime_error(errorMessage.str());
        }
        return runway;
    }

    shared_ptr<Runway> Airport::tryFindRunway(const string& name) const
    {
        auto found = m_runwayByName.find(name);
        return found != m_runwayByName.end()
            ? found->second
            : nullptr;
    }

    shared_ptr<ParkingStand> Airport::getParkingStandOrThrow(const string& name) const
    {
        auto parking = tryFindParkingStand(name);
        if (!parking)
        {
            throw runtime_error("Specified parking stand could not be found");
        }
        return parking;
    }

    shared_ptr<ParkingStand> Airport::tryFindParkingStand(const string& name) const
    {
        auto found = m_parkingStandByName.find(name);
        return found != m_parkingStandByName.end()
            ? found->second
            : nullptr;
    }
}
