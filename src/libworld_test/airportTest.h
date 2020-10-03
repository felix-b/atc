//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include "libworld.h"

using namespace std;
using namespace world;

shared_ptr<Runway> makeRunway(
    shared_ptr<HostServices> host,
    const GeoPoint& p1,
    const GeoPoint& p2,
    const string& name1,
    const string& name2,
    float widthMeters = 50,
    float displacedThresholdMeters = 0);

