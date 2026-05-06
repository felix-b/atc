// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>

#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "intentFactory.hpp"

using namespace std;
using namespace world;

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

    shared_ptr<Airport> createAirport(
        shared_ptr<HostServices> host,
        const string& icao,
        const GeoPoint& datum,
        int groundKhz,
        int localKhz,
        const string& runwayName1,
        const string& runwayName2)
    {
        Airport::Header header(icao, icao + " Test", datum, 0);
        auto airspace = WorldBuilder::assembleSampleAirportControlZone(header);

        vector<ControllerPosition::Structure> positions = {
            { ControllerPosition::Type::Ground, groundKhz, GeoPolygon::empty(), "" },
            { ControllerPosition::Type::Local, localKhz, GeoPolygon::empty(), "" }
        };

        auto runway = createRunway(host, datum, { datum.latitude, datum.longitude + 0.02 }, runwayName1, runwayName2);
        auto tower = WorldBuilder::assembleAirportTower(host, header, airspace, positions);
        return WorldBuilder::assembleAirport(host, header, { runway }, {}, {}, {}, tower, airspace);
    }
}

TEST(IntentFactoryTest, pilotReportHoldingShortUsesArrivalGroundDuringArrivalTaxi)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);

    auto departureAirport = createAirport(host, "KAAA", { 30.0, 40.0 }, 121900, 118300, "04", "22");
    auto arrivalAirport = createAirport(host, "KBBB", { 31.0, 41.0 }, 121700, 119100, "09", "27");
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);

    auto plan = make_shared<FlightPlan>(0, 3600, "KAAA", "KBBB");
    auto flight = make_shared<Flight>(host, 501, Flight::RulesType::IFR, "DAL", "501", "DAL 501", plan);
    auto aircraft = host->createAIAircraft("B738", "DAL", "N501", Aircraft::Category::Jet);
    flight->setAircraft(aircraft);
    aircraft->setLocation({ 31.0, 41.0 });

    flight->setPhase(Flight::Phase::Arrival);
    auto arrivalIntent = intentFactory->pilotReportHoldingShort(flight, "09", "B1");
    EXPECT_EQ(arrivalIntent->subjectControl()->frequency()->khz(), 121700);

    flight->setPhase(Flight::Phase::Departure);
    auto departureIntent = intentFactory->pilotReportHoldingShort(flight, "04", "A1");
    EXPECT_EQ(departureIntent->subjectControl()->frequency()->khz(), 121900);
}
