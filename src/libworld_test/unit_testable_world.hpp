//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#if 0
#pragma once

#include <memory>
#include <functional>
#include "libworld.h"

using namespace std;

namespace world
{
    class TestHostServices : public HostServices
    {
    };

    class TestSetup
    {
    public:
        shared_ptr<TestHostServices> host();
        shared_ptr<World> world();
    };

    class TestSetupBuilder
    {
    public:
        void addAirport(const string& icao);
        shared_ptr<TestSetup> getTestSetup();
    };
}

//    ControllerTestBed testBed;
//    testBed.addAirport(AirportReporitory::EFGH);
//    testBed.addArrivalFlightOnFinal("TES101", "ABCD", "EFGH");
//    testBed.addControllerUnderTest(ControllerPosition::Type::Local);
//
//    auto test = testBed.createTest();

#endif