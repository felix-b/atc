// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"

using namespace std;

namespace world
{
    shared_ptr<Actor> Intent::getSpeakingActor() const
    {
        switch (m_direction)
        {
        case Direction::PilotToController:
            return m_subjectFlight->pilot();
        case Direction::ControllerToPilot:
            return m_subjectControl->controller();
        default:
            return nullptr;
        }
    }
}
