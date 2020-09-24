// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include <algorithm>
#include "libworld.h"

using namespace std;

namespace world
{
    shared_ptr<ControllerPosition> ControlFacility::tryFindPosition(ControllerPosition::Type type, const GeoPoint& location) const
    {
        return tryFindFirst<shared_ptr<ControllerPosition>>(m_positions, [this, type](const shared_ptr<ControllerPosition>& position) {
            return (position->type() == type);
        });
    }

    shared_ptr<ControllerPosition> ControlFacility::findPositionOrThrow(ControllerPosition::Type type, const GeoPoint& location) const 
    {
        auto positionOrNull = tryFindPosition(type, location);
        if (positionOrNull)
        {
            return positionOrNull;
        }
        throw runtime_error("Requested controller position not found at this facility.");
    }

    void ControlFacility::progressTo(chrono::microseconds timestamp)
    {
        for (const auto position : m_positions)
        {
            position->progressTo(timestamp);
        }
    }


    // shared_ptr<ControllerPosition> ControlFacility::tryFindPosition(
    //     ControllerPosition::Type type, 
    //     const GeoPoint& location) const
    // {

    // }
}