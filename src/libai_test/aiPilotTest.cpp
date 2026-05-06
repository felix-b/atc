// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <sstream>
#include <vector>

#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "aiPilot.hpp"
#include "maneuverFactory.hpp"

using namespace std;
using namespace world;
using namespace ai;

namespace
{
    shared_ptr<Airport> createMinimalDepartureAirport(shared_ptr<HostServices> host)
    {
        Airport::Header header("KAAA", "Test Departure", GeoPoint(30.0, 40.0), 0);
        auto airspace = WorldBuilder::assembleSampleAirportControlZone(header);

        vector<ControllerPosition::Structure> positions = {
            { ControllerPosition::Type::Ground, 121900, GeoPolygon::empty(), "" }
        };

        auto tower = WorldBuilder::assembleAirportTower(host, header, airspace, positions);
        return WorldBuilder::assembleAirport(host, header, {}, {}, {}, {}, tower, airspace);
    }

    shared_ptr<Runway> createRunway(
        shared_ptr<HostServices> host,
        const GeoPoint& p1,
        const GeoPoint& p2,
        const string& name1,
        const string& name2,
        float widthMeters = 50,
        float displacedThresholdMeters = 0)
    {
        Runway::End end1(name1, displacedThresholdMeters, 0.0f, UniPoint::fromGeo(host, p1));
        Runway::End end2(name2, displacedThresholdMeters, 10.0f, UniPoint::fromGeo(host, p2));
        return shared_ptr<Runway>(new Runway(end1, end2, widthMeters));
    }

    shared_ptr<Airport> createMinimalArrivalAirport(shared_ptr<HostServices> host)
    {
        Airport::Header header("KBBB", "Test Arrival", GeoPoint(31.0, 41.0), 0);
        auto airspace = WorldBuilder::assembleSampleAirportControlZone(header);

        vector<ControllerPosition::Structure> positions = {
            { ControllerPosition::Type::Local, 118300, GeoPolygon::empty(), "" }
        };

        auto runway = createRunway(host, { 31.00, 41.00 }, { 31.00, 41.02 }, "09L", "27R");
        auto tower = WorldBuilder::assembleAirportTower(host, header, airspace, positions);
        return WorldBuilder::assembleAirport(host, header, { runway }, {}, {}, {}, tower, airspace);
    }

    class ProcedureAwareTestHostServices : public TestHostServices
    {
    public:
        string getHostFilePath(const vector<string>& relativePathParts) override
        {
            return relativePathParts.empty() ? string() : relativePathParts.back();
        }

        shared_ptr<istream> openFileForRead(const string& filePath) override
        {
            if (filePath == "KBBB.dat")
            {
                return make_shared<stringstream>(sampleCifp());
            }

            if (filePath == "earth_fix.dat")
            {
                return make_shared<stringstream>(sampleEarthFix());
            }

            return make_shared<stringstream>(string());
        }

    private:
        static string sampleCifp()
        {
            return
                "STAR:010,4,STAR1,RW09L,STARFIX;\n"
                "STAR:020,4,STAR1,RW09L,APPFIX;\n"
                "APPCH:010,A,I09L,APPFIX,APPFIX;\n"
                "APPCH:020,A,I09L,APPFIX,09L;\n";
        }

        static string sampleEarthFix()
        {
            return
                "I\n"
                "1200 Version - data cycle 2604\n"
                " 31.000000000   40.900000000  STARFIX ENRT KB 0000000 STARFIX\n"
                " 31.000000000   40.950000000  APPFIX ENRT KB 0000000 APPFIX\n";
        }
    };
}

TEST(AIPilotTest, flightCycleIncludesFinalApproachHandoff)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto departureAirport = createMinimalDepartureAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport });
    host->useWorld(world);

    const time_t departureTime = world->currentTime() + 3600;
    const time_t arrivalTime = departureTime + 7200;

    auto plan = make_shared<FlightPlan>(departureTime, arrivalTime, "KAAA", "KBBB");
    auto aircraft = make_shared<AIAircraft>(host, 101, "B738", "DAL", "N101", Aircraft::Category::Jet);
    auto flight = make_shared<Flight>(host, 101, Flight::RulesType::IFR, "DAL", "101", "DAL 101", plan);
    flight->setAircraft(aircraft);

    auto pilot = make_shared<AIPilot>(host, 1, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
    flight->setPilot(pilot);

    auto cycle = pilot->getFlightCycle();
    ASSERT_TRUE(!!cycle);

    shared_ptr<Maneuver> lastChild;
    size_t childCount = 0;
    for (auto child = cycle->firstChild(); child; child = child->nextSibling())
    {
        lastChild = child;
        ++childCount;
    }

    EXPECT_EQ(childCount, 11u);
    ASSERT_TRUE(!!lastChild);
    EXPECT_EQ(lastChild->id(), "enter_final");
    EXPECT_EQ(lastChild->type(), Maneuver::Type::Unspecified);
}

TEST(AIPilotTest, finalToGateStartsWithFinalAndLanding)
{
    auto host = TestHostServices::create();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto departureAirport = createMinimalDepartureAirport(host);
    auto arrivalAirport = createMinimalArrivalAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);

    const time_t departureTime = world->currentTime() + 3600;
    const time_t arrivalTime = departureTime + 7200;

    auto plan = make_shared<FlightPlan>(departureTime, arrivalTime, "KAAA", "KBBB");
    plan->setArrivalRunway("09L");
    auto aircraft = make_shared<AIAircraft>(host, 102, "B738", "DAL", "N102", Aircraft::Category::Jet);
    auto flight = make_shared<Flight>(host, 102, Flight::RulesType::IFR, "DAL", "102", "DAL 102", plan);
    flight->setAircraft(aircraft);

    auto pilot = make_shared<AIPilot>(host, 2, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
    flight->setPilot(pilot);

    auto landingRunwayEnd = arrivalAirport->getRunwayOrThrow("09L")->getEndOrThrow("09L");
    auto finalToGate = pilot->getFinalToGate(landingRunwayEnd);
    ASSERT_TRUE(!!finalToGate);

    auto firstChild = finalToGate->firstChild();
    ASSERT_TRUE(!!firstChild);
    EXPECT_EQ(firstChild->id(), "final");

    auto secondChild = firstChild->nextSibling();
    ASSERT_TRUE(!!secondChild);
    EXPECT_TRUE(secondChild->isProxy());
    EXPECT_TRUE(secondChild->id().empty());
}

TEST(AIPilotTest, finalToGateIncludesStarAndApproachWhenAvailable)
{
    auto host = make_shared<ProcedureAwareTestHostServices>();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto departureAirport = createMinimalDepartureAirport(host);
    auto arrivalAirport = createMinimalArrivalAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);

    const time_t departureTime = world->currentTime() + 3600;
    const time_t arrivalTime = departureTime + 7200;

    auto plan = make_shared<FlightPlan>(departureTime, arrivalTime, "KAAA", "KBBB");
    plan->setStar("STAR1");
    plan->setApproach("I09L");
    plan->setArrivalRunway("09L");
    auto aircraft = make_shared<AIAircraft>(host, 103, "B738", "DAL", "N103", Aircraft::Category::Jet);
    auto flight = make_shared<Flight>(host, 103, Flight::RulesType::IFR, "DAL", "103", "DAL 103", plan);
    flight->setAircraft(aircraft);

    auto pilot = make_shared<AIPilot>(host, 3, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
    flight->setPilot(pilot);

    ASSERT_GE(flight->plan()->legs().size(), 3u);
    EXPECT_TRUE(any_of(flight->plan()->legs().begin(), flight->plan()->legs().end(), [](const shared_ptr<FlightPlan::Leg>& leg) {
        return leg && leg->type() == FlightPlan::LegType::Star;
    }));
    EXPECT_TRUE(any_of(flight->plan()->legs().begin(), flight->plan()->legs().end(), [](const shared_ptr<FlightPlan::Leg>& leg) {
        return leg && leg->type() == FlightPlan::LegType::Approach;
    }));

    auto landingRunwayEnd = arrivalAirport->getRunwayOrThrow("09L")->getEndOrThrow("09L");
    auto finalToGate = pilot->getFinalToGate(landingRunwayEnd);
    ASSERT_TRUE(!!finalToGate);

    auto firstChild = finalToGate->firstChild();
    ASSERT_TRUE(!!firstChild);
    EXPECT_EQ(firstChild->id(), "star_leg");

    auto secondChild = firstChild->nextSibling();
    ASSERT_TRUE(!!secondChild);
    EXPECT_EQ(secondChild->id(), "approach_leg");

    auto thirdChild = secondChild->nextSibling();
    ASSERT_TRUE(!!thirdChild);
    EXPECT_EQ(thirdChild->id(), "final");

    auto fourthChild = thirdChild->nextSibling();
    ASSERT_TRUE(!!fourthChild);
    EXPECT_TRUE(fourthChild->isProxy());
    EXPECT_TRUE(fourthChild->id().empty());
}

TEST(AIPilotTest, setOnFinalStartsOnInboundStarLegHeadingTowardAirport)
{
    auto host = make_shared<ProcedureAwareTestHostServices>();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto departureAirport = createMinimalDepartureAirport(host);
    auto arrivalAirport = createMinimalArrivalAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);

    const time_t departureTime = world->currentTime() + 3600;
    const time_t arrivalTime = departureTime + 7200;

    auto plan = make_shared<FlightPlan>(departureTime, arrivalTime, "KAAA", "KBBB");
    plan->setStar("STAR1");
    plan->setApproach("I09L");
    plan->setArrivalRunway("09L");
    auto aircraft = make_shared<AIAircraft>(host, 105, "B738", "DAL", "N105", Aircraft::Category::Jet);
    auto flight = make_shared<Flight>(host, 105, Flight::RulesType::IFR, "DAL", "105", "DAL 105", plan);
    flight->setAircraft(aircraft);
    world->addFlight(flight);

    auto pilot = make_shared<AIPilot>(host, 4, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
    flight->setPilot(pilot);

    auto landingRunwayEnd = arrivalAirport->getRunwayOrThrow("09L")->getEndOrThrow("09L");
    aircraft->setOnFinal(landingRunwayEnd);

    EXPECT_NEAR(aircraft->location().latitude, 31.0, 1e-9);
    EXPECT_NEAR(aircraft->location().longitude, 40.9, 1e-9);

    const auto expectedHeading = GeoMath::getHeadingFromPoints(
        GeoPoint(31.0, 40.9),
        GeoPoint(31.0, 40.95));
    EXPECT_NEAR(aircraft->attitude().heading(), expectedHeading, 1e-6);
}

TEST(AIPilotTest, setOnFinalMovesLaterArrivalBehindExistingTraffic)
{
    auto host = make_shared<ProcedureAwareTestHostServices>();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto departureAirport = createMinimalDepartureAirport(host);
    auto arrivalAirport = createMinimalArrivalAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);

    auto makeArrival = [&](int id) {
        auto plan = make_shared<FlightPlan>(0, 3600, "KAAA", "KBBB");
        plan->setStar("STAR1");
        plan->setApproach("I09L");
        plan->setArrivalRunway("09L");

        auto aircraft = make_shared<AIAircraft>(host, id, "B738", "DAL", "N" + to_string(id), Aircraft::Category::Jet);
        auto flight = make_shared<Flight>(host, id, Flight::RulesType::IFR, "DAL", to_string(id), "DAL " + to_string(id), plan);
        flight->setAircraft(aircraft);
        world->addFlight(flight);

        auto pilot = make_shared<AIPilot>(host, id, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
        flight->setPilot(pilot);

        return pair<shared_ptr<Flight>, shared_ptr<AIAircraft>>(flight, aircraft);
    };

    auto first = makeArrival(201);
    auto second = makeArrival(202);
    auto landingRunwayEnd = arrivalAirport->getRunwayOrThrow("09L")->getEndOrThrow("09L");

    first.second->setOnFinal(landingRunwayEnd);
    second.second->setOnFinal(landingRunwayEnd);

    const double separationNm = GeoMath::getDistanceMeters(
        first.second->location(),
        second.second->location()) / METERS_IN_1_NAUTICAL_MILE;

    EXPECT_GT(separationNm, 2.5);
    EXPECT_LT(second.second->location().longitude, first.second->location().longitude);
}

TEST(AIPilotTest, approachProcedureSpeedUsesAircraftPerformanceProfile)
{
    auto host = make_shared<ProcedureAwareTestHostServices>();
    auto intentFactory = make_shared<IntentFactory>(host);
    auto maneuverFactory = make_shared<ManeuverFactory>(host);
    host->services().use<IntentFactory>(intentFactory);

    auto departureAirport = createMinimalDepartureAirport(host);
    auto arrivalAirport = createMinimalArrivalAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);

    const auto makeFlight = [&](int id, const string& model, Aircraft::Category category) {
        auto plan = make_shared<FlightPlan>(0, 3600, "KAAA", "KBBB");
        plan->setApproach("I09L");
        plan->setArrivalRunway("09L");

        auto aircraft = make_shared<AIAircraft>(host, id, model, "DAL", "N" + to_string(id), category);
        auto flight = make_shared<Flight>(host, id, Flight::RulesType::IFR, "DAL", to_string(id), "DAL " + to_string(id), plan);
        flight->setAircraft(aircraft);
        world->addFlight(flight);

        auto pilot = make_shared<AIPilot>(host, id, Actor::Gender::Male, flight, maneuverFactory, intentFactory);
        flight->setPilot(pilot);

        auto finalToGate = pilot->getFinalToGate(arrivalAirport->getRunwayOrThrow("09L")->getEndOrThrow("09L"));
        finalToGate->progressTo(chrono::seconds(1));

        return aircraft;
    };

    auto lightAircraft = makeFlight(301, "C172", Aircraft::Category::LightProp);
    auto jetAircraft = makeFlight(302, "B738", Aircraft::Category::Jet);

    EXPECT_LT(lightAircraft->groundSpeedKt(), jetAircraft->groundSpeedKt());
    EXPECT_LT(lightAircraft->groundSpeedKt(), 160.0);
    EXPECT_GT(jetAircraft->groundSpeedKt(), 180.0);
}

TEST(AIPilotTest, arrivalDescentClampsToTerrainBeforePenetratingIt)
{
    auto host = TestHostServices::create();
    auto departureAirport = createMinimalDepartureAirport(host);
    auto arrivalAirport = createMinimalArrivalAirport(host);
    auto world = WorldBuilder::assembleSampleWorld(host, { departureAirport, arrivalAirport });
    host->useWorld(world);
    world->onQueryTerrainElevation([](const GeoPoint&) {
        return 5000.0;
    });

    auto plan = make_shared<FlightPlan>(0, 3600, "KAAA", "KBBB");
    plan->setArrivalRunway("09L");
    auto aircraft = make_shared<AIAircraft>(host, 104, "B738", "DAL", "N104", Aircraft::Category::Jet);
    auto flight = make_shared<Flight>(host, 104, Flight::RulesType::IFR, "DAL", "104", "DAL 104", plan);
    flight->setAircraft(aircraft);
    world->addFlight(flight);
    flight->setPhase(Flight::Phase::Arrival);

    aircraft->setLocation({ 31.0, 41.0 });
    aircraft->setAltitude(Altitude::msl(4200.0f));
    aircraft->setVerticalSpeedFpm(-5000.0);

    aircraft->progressTo(chrono::seconds(10));

    EXPECT_EQ(aircraft->altitude().type(), Altitude::Type::Ground);
}
