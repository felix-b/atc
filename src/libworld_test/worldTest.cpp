// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <string>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

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
