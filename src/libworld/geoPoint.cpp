// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"

namespace world
{
    const GeoPoint GeoPoint::empty(0, 0);

    bool operator== (const GeoPoint& p1, const GeoPoint& p2)
    {
        return (p1.latitude == p2.latitude && p1.longitude == p2.longitude);
    }
    
    bool operator!= (const GeoPoint& p1, const GeoPoint& p2)
    {
        return !(p1 == p2);
    }
}

