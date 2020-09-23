// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"

namespace world
{
    shared_ptr<Runway> Airport::getRunwayOrThrow(const string& name) const
    {
        auto runway = tryFindRunway(name);
        if (!runway)
        {
            throw runtime_error("Specified runway could not be found");
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
