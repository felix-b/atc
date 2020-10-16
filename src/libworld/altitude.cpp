//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include "libworld.h"

using namespace std;

namespace world
{
    string Altitude::toString() const
    {
        switch (m_type)
        {
        case Altitude::Type::Ground:
            return "";
        case Altitude::Type::AGL:
            return to_string((int)m_feet) + " AGL";
        case Altitude::Type::MSL:
            return to_string((int)m_feet) + " MSL";
        default:
            return to_string((int)m_feet) + " ???";
        }
    }
}
