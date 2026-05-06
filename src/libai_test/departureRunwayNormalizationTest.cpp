//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <vector>

#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "worldHelper.hpp"
#include "simplePhraseologyService.hpp"

#define private public
#include "aiControllerBase.hpp"
#include "localController.hpp"
#undef private

using namespace std;
using namespace world;
using namespace ai;

namespace
{
    shared_ptr<Runway> createRunway(
        shared_ptr<HostServices> host,
        const GeoPoint& p1,
        const GeoPoint& p2,
        const string& name1,
        const string& name2)
    {
        Runway::End end1(name1, 0.0f, 0.0f, UniPoint::fromGeo(host, p1));
        Runway::End end2(name2, 0.0f, 10.0f, UniPoint::fromGeo(host, p2));
        return shared_ptr<Runway>(new Runway(end1, end2, 50.0f));
    }

    shared_ptr<Airport> createControlledAirport(shared_ptr<HostServices> host)
    {
        Airport::Header header("LEBL", "Barcelona Test", GeoPoint(41.2971, 2.0785), 0);
        auto airspace = WorldBuilder::assembleSampleAirportControlZone(header);

        vector<ControllerPosition::Structure> positions = {
            { ControllerPosition::Type::Ground, 121700, GeoPolygon::empty(), "" },
            { ControllerPosition::Type::Local, 118100, GeoPolygon::empty(), "" }
        };

        auto runway = createRunway(
            host,
            header.datum(),
            { header.datum().latitude, header.datum().longitude + 0.02 },
            "06L",
            "24R");
        auto tower = WorldBuilder::assembleAirportTower(host, header, airspace, positions);
        return WorldBuilder::assembleAirport(host, header, { runway }, {}, {}, {}, tower, airspace);
    }

    shared_ptr<Airport> createDepartureOnlyControlledAirport(shared_ptr<HostServices> host)
    {
        Airport::Header header("LEMD", "Madrid Departure Test", GeoPoint(40.4983, -3.5676), 0);
        auto airspace = WorldBuilder::assembleSampleAirportControlZone(header);

        vector<ControllerPosition::Structure> positions = {
            { ControllerPosition::Type::Ground, 121900, GeoPolygon::empty(), "" },
            { ControllerPosition::Type::Departure, 119100, GeoPolygon::empty(), "" }
        };

        auto runway = createRunway(
            host,
            header.datum(),
            { header.datum().latitude, header.datum().longitude + 0.02 },
            "14L",
            "32R");
        auto tower = WorldBuilder::assembleAirportTower(host, header, airspace, positions);
        return WorldBuilder::assembleAirport(host, header, { runway }, {}, {}, {}, tower, airspace);
    }

}

TEST(DepartureRunwayNormalizationTest, groundControllerHandsOffUnpaddedDepartureRunway)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto clearanceFactory = make_shared<ClearanceFactory>(host);
    host->services().use<IntentFactory>(intentFactory);
    host->services().use<ClearanceFactory>(clearanceFactory);

    auto airport = createControlledAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { airport });
    host->useWorld(world);

    auto testFlight = host->addIfrFlight(
        601,
        airport->header().icao(),
        "KJFK",
        airport->header().datum(),
        Altitude::ground());
    auto flight = testFlight.ptr;
    flight->setPhase(Flight::Phase::Departure);
    flight->plan()->setDepartureRunway("6L");

    auto ground = airport->groundAt(flight->aircraft()->location());
    AIControllerBase groundController(host, 1, Actor::Gender::Male, ground);
    groundController.m_clearedForDepartureTaxi.insert(flight);

    auto intent = intentFactory->pilotReportHoldingShort(flight, airport, "6L", "A1");
    groundController.receiveIntent(intent);

    EXPECT_EQ(groundController.m_clearedForDepartureTaxi.count(flight), 0u);
    EXPECT_EQ(groundController.m_departureTaxiHandedOffToTower.count(flight), 1u);
}

TEST(DepartureRunwayNormalizationTest, localControllerResolvesUnpaddedRunwayMutex)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto clearanceFactory = make_shared<ClearanceFactory>(host);
    host->services().use<IntentFactory>(intentFactory);
    host->services().use<ClearanceFactory>(clearanceFactory);

    auto airport = createControlledAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { airport });
    host->useWorld(world);

    airport->selectActiveRunways();

    auto local = airport->localAt(airport->header().datum());
    LocalController localController(host, 2, Actor::Gender::Female, local);

    vector<string> departureRunways;
    vector<string> arrivalRunways;
    localController.selectActiveRunways(departureRunways, arrivalRunways);

    shared_ptr<SimpleRunwayMutex> mutex;
    EXPECT_NO_THROW({
        mutex = localController.getRunwayMutex("6L");
    });
    EXPECT_TRUE(!!mutex);
}

TEST(DepartureRunwayNormalizationTest, departureTowerLookupFallsBackWhenLocalMissing)
{
    auto host = TestHostServices::create();
    auto airport = createDepartureOnlyControlledAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { airport });
    host->useWorld(world);

    auto testFlight = host->addIfrFlight(
        602,
        airport->header().icao(),
        "KJFK",
        airport->header().datum(),
        Altitude::ground());

    WorldHelper helper(host);
    auto tower = helper.getDepartureTower(testFlight.ptr);

    ASSERT_TRUE(!!tower);
    EXPECT_EQ(tower->type(), ControllerPosition::Type::Departure);
}

TEST(DepartureRunwayNormalizationTest, takeoffClearanceFallsBackWhenLocalMissing)
{
    auto host = TestHostServices::create();
    auto clearanceFactory = make_shared<ClearanceFactory>(host);
    host->services().use<ClearanceFactory>(clearanceFactory);

    auto airport = createDepartureOnlyControlledAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { airport });
    host->useWorld(world);

    auto testFlight = host->addIfrFlight(
        603,
        airport->header().icao(),
        "KJFK",
        airport->header().datum(),
        Altitude::ground());
    auto flight = testFlight.ptr;
    flight->setPhase(Flight::Phase::Departure);
    flight->plan()->setDepartureRunway("14L");

    auto ifrClearance = clearanceFactory->ifrClearance(flight, 2345);
    ASSERT_TRUE(!!ifrClearance);
    flight->addClearance(ifrClearance);

    auto takeoffClearance = clearanceFactory->takeoffClearance(flight, 140.0f, false);
    ASSERT_TRUE(!!takeoffClearance);
    ASSERT_TRUE(!!takeoffClearance->header().issuedBy);
    EXPECT_EQ(takeoffClearance->header().issuedBy->type(), ControllerPosition::Type::Departure);
}

TEST(DepartureRunwayNormalizationTest, automaticHandoffIsNotQueuedTwice)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto clearanceFactory = make_shared<ClearanceFactory>(host);
    host->services().use<IntentFactory>(intentFactory);
    host->services().use<ClearanceFactory>(clearanceFactory);
    host->services().use<PhraseologyService>(make_shared<SimplePhraseologyService>(host));

    auto airport = createControlledAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { airport });
    host->useWorld(world);
    airport->selectActiveRunways();

    auto testFlight = host->addIfrFlight(
        604,
        airport->header().icao(),
        "KJFK",
        airport->header().datum(),
        Altitude::ground());
    auto flight = testFlight.ptr;
    flight->setPhase(Flight::Phase::Departure);
    flight->plan()->setDepartureRunway("06L");

    auto ground = airport->groundAt(flight->aircraft()->location());
    AIControllerBase groundController(host, 3, Actor::Gender::Male, ground);
    groundController.m_clearedForDepartureTaxi.insert(flight);

    world->progressTo(chrono::seconds(1));
    groundController.progressTo(world->timestamp());
    groundController.progressTo(world->timestamp());

    world->progressTo(chrono::seconds(2));
    world->progressTo(chrono::seconds(3));
    world->progressTo(chrono::seconds(4));

    int handoffTransmissions = 0;
    for (const auto& transmission : host->textToSpeechService()->transmissionHistory())
    {
        if (transmission && transmission->intent() && transmission->intent()->code() == GroundSwitchToTowerIntent::IntentCode)
        {
            handoffTransmissions++;
        }
    }

    EXPECT_EQ(handoffTransmissions, 1);
    EXPECT_EQ(groundController.m_departureTaxiHandedOffToTower.count(flight), 1u);
}

