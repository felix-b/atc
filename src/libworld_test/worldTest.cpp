// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <sstream>
#include <string>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

namespace
{
    class ProcedureTestHostServices : public TestHostServices
    {
    public:
        string getHostFilePath(const vector<string>& relativePathParts) override
        {
            if (relativePathParts.empty())
            {
                return "";
            }

            return relativePathParts.back();
        }

        shared_ptr<istream> openFileForRead(const string& filePath) override
        {
            if (filePath == "KJFK.dat")
            {
                return make_shared<stringstream>(sampleDepartureCifp());
            }

            if (filePath == "KMIA.dat")
            {
                return make_shared<stringstream>(sampleArrivalCifp());
            }

            return make_shared<stringstream>(string());
        }

    private:
        static string sampleDepartureCifp()
        {
            return
                "SID:010,4,GREKI6,RW04L,RW04L;\n"
                "SID:020,4,GREKI6,RW04L,YNKEE;\n";
        }

        static string sampleArrivalCifp()
        {
            return
                "APPCH:010,A,I09L,I09L,APPFIX;\n"
                "APPCH:020,A,I09L,I09L,FINALF;\n";
        }
    };
}

TEST(FlightPlanTest, rebuildsProcedureLegsAndCursorAdvancesThroughStoredProcedures)
{
    auto host = make_shared<ProcedureTestHostServices>();

    auto plan = make_shared<FlightPlan>(0, 3600, "KJFK", "KMIA");
    plan->setDepartureRunway("04L");
    plan->setSid("GREKI6");
    plan->setSidTransition("YNKEE");
    plan->setApproach("I09L");
    plan->setArrivalRunway("09L");

    auto flight = make_shared<Flight>(host, 901, Flight::RulesType::IFR, "DAL", "901", "DAL 901", plan);
    auto cursor = flight->planCursor();

    ASSERT_TRUE(!!cursor);
    ASSERT_EQ(flight->plan()->legs().size(), 6);
    EXPECT_EQ(flight->plan()->legs()[0]->type(), FlightPlan::LegType::TakeOff);
    EXPECT_EQ(flight->plan()->legs()[1]->type(), FlightPlan::LegType::Sid);
    EXPECT_EQ(flight->plan()->legs()[2]->type(), FlightPlan::LegType::EnRoute);
    EXPECT_EQ(flight->plan()->legs()[3]->type(), FlightPlan::LegType::Approach);
    EXPECT_EQ(flight->plan()->legs()[4]->type(), FlightPlan::LegType::Approach);
    EXPECT_EQ(flight->plan()->legs()[5]->type(), FlightPlan::LegType::Landing);

    ASSERT_TRUE(cursor->activateNextLegOfType(FlightPlan::LegType::Sid));
    ASSERT_TRUE(!!cursor->activeLeg());
    EXPECT_EQ(cursor->activeLeg()->type(), FlightPlan::LegType::Sid);
    EXPECT_EQ(cursor->activeLeg()->fromNavaid(), "RW04L");
    EXPECT_EQ(cursor->activeLeg()->toNavaid(), "YNKEE");

    ASSERT_TRUE(cursor->activateNextLegOfType(FlightPlan::LegType::Approach));
    ASSERT_TRUE(!!cursor->activeLeg());
    EXPECT_EQ(cursor->activeLeg()->type(), FlightPlan::LegType::Approach);
    EXPECT_EQ(cursor->activeLeg()->fromNavaid(), "I09L");
    EXPECT_EQ(cursor->activeLeg()->toNavaid(), "APPFIX");
}

shared_ptr<Flight> makeFlight(shared_ptr<HostServices> host, int id, const string& fromIcao, const string& toIcao)
{
    auto plan = make_shared<FlightPlan>(0, 3600, fromIcao, toIcao);
    auto flight = make_shared<Flight>(host, id, Flight::RulesType::IFR, "DAL", to_string(id), "DAL " + to_string(id), plan);
    auto aircraft = host->createAIAircraft("B738", "DAL", "T" + to_string(id), Aircraft::Category::Jet);
    flight->setAircraft(aircraft);
    return flight;
}

TEST(WorldTest, canAddFlights)
{
    auto host = TestHostServices::create();
    auto world = make_shared<World>(host, 0);
    host->useWorld(world);

    auto flight1 = makeFlight(host, 101, "KJFK", "KMIA");
    auto flight2 = makeFlight(host, 102, "KMIA", "KJFK");

    world->addFlight(flight1);
    world->addFlight(flight2);

    ASSERT_EQ(world->flights().size(), 2);

    EXPECT_EQ(world->flights()[0], flight1);
    EXPECT_EQ(world->flights()[1], flight2);

    EXPECT_EQ(world->getFlightById(101), flight1);
    EXPECT_EQ(world->getFlightById(102), flight2);
}

TEST(WorldTest, canAddAirport)
{
    auto host = TestHostServices::create();
    auto world = make_shared<World>(host, 0);

    auto airport = WorldBuilder::assembleAirport(
        host,
        Airport::Header("ABCD", "Test Airport", GeoPoint(30, 40), 12),
        {},
        {},
        {},
        {});

    auto insertedAirport = world->addAirport(airport);

    ASSERT_TRUE(!!insertedAirport);
    EXPECT_EQ(insertedAirport, airport);
    ASSERT_EQ(world->airports().size(), 1u);
    EXPECT_EQ(world->getAirport("ABCD"), airport);

    auto duplicateAirport = world->addAirport(airport);
    EXPECT_EQ(duplicateAirport, airport);
    EXPECT_EQ(world->airports().size(), 1u);
}

TEST(WorldTest, canClearAllFlights)
{
    auto host = TestHostServices::create();
    auto world = make_shared<World>(host, 0);
    host->useWorld(world);

    world->addFlight(makeFlight(host, 101, "KJFK", "KMIA"));
    world->addFlight(makeFlight(host, 102, "KMIA", "KJFK"));

    world->clearAllFlights();

    EXPECT_EQ(world->flights().size(), 0);
    EXPECT_THROW({ world->getFlightById(101); }, runtime_error);
    EXPECT_THROW({ world->getFlightById(102); }, runtime_error);

    EXPECT_EQ(host->aircraftObjectService()->callCount_clearAll(), 1);
    EXPECT_EQ(host->textToSpeechService()->callCount_clearAll(), 1);
}

TEST(WorldTest, clearAllFlights_clearsAllWorkItems)
{
    auto host = TestHostServices::create();
    auto world = make_shared<World>(host, 0);
    host->useWorld(world);

    vector<string> workItemLog;

    world->addFlight(makeFlight(host, 101, "KJFK", "KMIA"));
    world->addFlight(makeFlight(host, 102, "KMIA", "KJFK"));
    world->deferUntil("workItemA", 100, [&]{
        workItemLog.push_back("workItemA");
    });

    world->clearAllFlights();
    world->progressTo(chrono::seconds(200));

    EXPECT_EQ(workItemLog.size(), 0);
}

TEST(WorldTest, canClearAllWorkItems)
{
    auto host = TestHostServices::create();
    auto world = make_shared<World>(host, 0);
    host->useWorld(world);

    vector<string> workItemLog;

    world->deferUntil("workItemA", 100, [&]{
        workItemLog.push_back("workItemA");
    });

    world->deferUntil("workItemB", 200, [&]{
        workItemLog.push_back("workItemB");
    });

    world->progressTo(chrono::seconds(150));
    world->clearWorkItems();
    world->progressTo(chrono::seconds(250));

    ASSERT_EQ(workItemLog.size(), 1);
    EXPECT_EQ(workItemLog[0], "workItemA");
}
