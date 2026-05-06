// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
// Regression tests for stale RunwayCrossClearance bug:
// After a first runway crossing, the clearance was left in the flight and would
// bypass the hold-short protocol for subsequent runway crossings during the same taxi.
//
#include <memory>
#include <vector>

#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "intentFactory.hpp"
#include "maneuverFactory.hpp"

#define private public
#include "aiPilot.hpp"
#undef private

using namespace std;
using namespace world;
using namespace ai;

namespace
{
    shared_ptr<Airport> createAirportForCrossTest(shared_ptr<HostServices> host)
    {
        Airport::Header header("KXCR", "Cross Test Airport", GeoPoint(35.0, 45.0), 0);
        auto airspace = WorldBuilder::assembleSampleAirportControlZone(header);

        vector<ControllerPosition::Structure> positions = {
            { ControllerPosition::Type::Ground, 121900, GeoPolygon::empty(), "" }
        };

        auto tower = WorldBuilder::assembleAirportTower(host, header, airspace, positions);
        return WorldBuilder::assembleAirport(host, header, {}, {}, {}, {}, tower, airspace);
    }
}

// Regression test: a stale RunwayCrossClearance remaining in the flight from a first
// crossing must be removed at the start of maneuverAwaitCrossRunway so that the second
// crossing correctly waits for a new clearance rather than immediately bypassing hold-short.
TEST(StaleCrossClearanceTest, staleClearanceIsRemovedAtStartOfSecondCrossing)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto airport = createAirportForCrossTest(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { airport });
    host->useWorld(world);

    const time_t departureTime = world->currentTime() + 3600;
    const time_t arrivalTime = departureTime + 7200;

    auto plan = make_shared<FlightPlan>(departureTime, arrivalTime, "KXCR", "KJFK");
    auto aircraft = make_shared<AIAircraft>(host, 201, "B738", "TES", "201", Aircraft::Category::Jet);
    auto flight = make_shared<Flight>(host, 201, Flight::RulesType::IFR, "TES", "201", "TES 201", plan);
    flight->setAircraft(aircraft);
    world->addFlight(flight);

    auto pilot = make_shared<AIPilot>(host, 5, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
    flight->setPilot(pilot);

    // Simulate a completed first runway crossing: leave a stale RunwayCrossClearance in the flight
    Clearance::Header clrHdr;
    clrHdr.id = 42;
    clrHdr.type = Clearance::Type::RunwayCrossClearance;
    clrHdr.issuedTimestamp = chrono::seconds(0);
    clrHdr.issuedBy = nullptr;
    clrHdr.issuedTo = nullptr;
    auto staleClearance = make_shared<RunwayCrossClearance>(clrHdr, "06L");
    flight->addClearance(staleClearance);

    ASSERT_TRUE(!!flight->tryFindClearance<RunwayCrossClearance>(Clearance::Type::RunwayCrossClearance));

    // Create a minimal TaxiEdge (no active zones — simulates a non-matched crossing edge)
    auto edge = make_shared<TaxiEdge>(
        UniPoint::fromGeo(host, airport->header().datum()),
        UniPoint::fromGeo(host, GeoPoint(airport->header().datum().latitude,
                                         airport->header().datum().longitude + 0.001)));

    // Get the second crossing maneuver
    auto crossManeuver = pilot->maneuverAwaitCrossRunway(airport, edge);

    // Progress once — the first instantAction must remove the stale clearance
    crossManeuver->progressTo(world->timestamp() + chrono::seconds(1));

    // The stale clearance must be gone; the await step will now block until a new clearance arrives
    EXPECT_FALSE(!!flight->tryFindClearance<RunwayCrossClearance>(Clearance::Type::RunwayCrossClearance));
}
