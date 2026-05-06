// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <sstream>
#include <string>
#include <vector>
#include <algorithm>

#include "gtest/gtest.h"
#include "libdataxp.h"
#include "xpAltitudeReader.hpp"
#include "libworld.h"
#include "libworld_test.h"

using namespace std;
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
            if (filePath == "KATL.dat")
            {
                return make_shared<stringstream>(sampleCifp());
            }

            if (filePath == "earth_fix.dat")
            {
                return make_shared<stringstream>(sampleEarthFix());
            }

            if (filePath == "earth_nav.dat")
            {
                return make_shared<stringstream>(sampleEarthNav());
            }

            if (filePath == "earth_msa.dat")
            {
                return make_shared<stringstream>(sampleEarthMsa());
            }

            if (filePath == "earth_mora.dat")
            {
                return make_shared<stringstream>(sampleEarthMora());
            }

            return make_shared<stringstream>(string());
        }

    public:
        static string sampleCifp()
        {
            return
                "SID:010,4,BANNG3,RW08B, , , , ,    , ,   ,VA, , , , , ,      ,    ,    ,0950,    ,+,01430,     ,18000, ,   ,    ,   , , , , , , ,J,S;\n"
                "SID:020,4,BANNG3,RW08B,SKNNR,K7,P,C,E   , ,   ,DF, , , , , ,      ,    ,    ,    ,    , ,     ,     ,     , ,   ,    ,   , , , , , , , ,J,S;\n"
                "SID:030,4,BANNG3,RW08B,GRITZ,K7,P,C,E   , ,   ,TF, , , , , ,      ,    ,    ,    ,    , ,     ,     ,     , ,   ,    ,   , , , , , , , ,J,S;\n"
                "SID:040,4,BANNG3,RW08B,HYZMN,K7,E,A,E   , ,   ,TF, , , , , ,      ,    ,    ,    ,    , ,250,    ,   , , , , , , , ,J,S;\n"
                "SID:050,4,BANNG3,RW08B,BANNG,K7,E,A,EE  , ,   ,TF, , , , , ,      ,    ,    ,    ,    , ,   ,    ,   , , , , , , , ,J,S;\n"
                "SID:010,4,GAIRY2,RW08L, , , , ,    , ,   ,VI, , , , , ,      ,    ,    ,0950,    , ,     ,     ,18000, ,   ,    ,   , , , , , , , ,J,S;\n"
                "STAR:010,4,CHPPR1,RW09L,AAKAY,K7,P,C,E   , ,   ,TF, , , , , ,      ,    ,    ,    ,    , ,     ,     ,     , ,   ,    ,   , , , , , , , ,J,S;\n"
                "STAR:020,4,CHPPR1,RW09L,ARRBE,K7,P,C,E   , ,   ,TF, , , , , ,      ,    ,    ,    ,    , ,     ,     ,     , ,   ,    ,   , , , , , , , ,J,S;\n"
                "STAR:030,4,CHPPR1,RW09L,HRDEY,K7,P,C,E   , ,   ,TF, , , , , ,      , ,    ,    ,    , ,     ,     ,     , ,   ,    ,   , , , , , , , ,J,S;\n"
                "APPCH:010,A,I09L,AAKAY,AAKAY,K7,P,C,E   , ,   ,IF, , , , , ,      ,    ,    ,    ,    ,+,06000,     ,18000, ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:020,A,I09L,AAKAY,ARRBE,K7,P,C,E  B, ,   ,TF, , , , , ,      ,    ,    ,    ,    ,+,05000,     ,     , ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:030,A,I09L,AAKAY,HRDEY,K7,P,C,EE  , ,   ,CF, ,IHZK,K7,P,I,      ,2750,0123,0950,0035,+,04000,     ,     , ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:010,A,I08L,JAAJJ,JAAJJ,K7,P,C,E  B, ,   ,IF, , , , , ,      ,    ,    ,    ,    ,+,05000,     ,18000, ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:020,A,I08L,JAAJJ,BAZAR,K7,P,C,EE  , ,   ,CF, ,IHFW,K7,P,I,      ,2750,0118,0950,0035,+,04000,     ,     , ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:030,A,I08L,JAAJJ,SCHEL,K7,P,C,EE  , ,   ,CF, ,IHFW,K7,P,I,      ,2750,0118,0950,0035,+,04000,     ,     , ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:040,A,I08L,JAAJJ,RW08L,K7,P,G,G  M, ,   ,CF, ,IHFW,K7,P,I,      ,2750,0018,0950,0054, ,01077,     ,     , ,   ,    ,   , , , , , ,0,N,S;\n"
                "APPCH:050,A,I08L,JAAJJ,MISSED1;\n"
                "APPCH:060,A,I08L,JAAJJ,MISSED2;\n";
        }

        static string sampleEarthFix()
        {
            return
                "I\n"
                "1200 Version - data cycle 2604\n"
                " 33.634563889  -84.342552778  SKNNR ENRT K7 4464727 SKNNR\n"
                " 33.602144444  -84.276336111  GRITZ ENRT K7 4464727 GRITZ\n"
                " 33.456986111  -84.276388889  HYZMN ENRT K7 4464727 HYZMN\n"
                " 32.961247222  -84.421383333  BANNG ENRT K7 4474455 BANNG\n"
                " 33.634172222  -84.793300000  AAKAY KATL K7 5916995 AAKAY\n"
                " 33.634366667  -84.719441667  ARRBE KATL K7 4607043 ARRBE\n"
                " 33.634511111  -84.650177778  HRDEY KATL K7 4604739 HRDEY\n"
                " 33.620000000  -84.500000000  MISSED1 ENRT K7 0000000 MISSED1\n"
                " 33.700000000  -84.450000000  MISSED2 ENRT K7 0000000 MISSED2\n";
        }

        static string sampleEarthNav()
        {
            return
                "I\n"
                "1200 Version - data cycle 2604\n"
                " 14  42.002829722  -87.893479861      667    99508 464.0     89.994 R09L KORD K5 09L LPV\n";
        }

        static string sampleEarthMsa()
        {
            return
                "I\n"
                "1150 Version - data cycle 2604\n"
                "3 AAKAY K7 KATL M 090 150 25 180 140 25 000 000 0\n"
                "3 ARRBE K7 KATL M 090 130 25 000 000 0\n"
                "10 RW09L K7 KATL M 090 110 25 000 000 0\n"
                "99\n";
        }

        static string sampleEarthMora()
        {
            ostringstream out;
            out << "I\n";
            out << "1150 Version - data cycle 2604\n";
            out << "+33 -085";
            for (int i = 0; i < 30; ++i)
            {
                out << ' ' << "120";
            }
            out << "\n99\n";
            return out.str();
        }
    };
}

namespace
{
    vector<string> waypointNames(const vector<FlightPlan::RouteWaypoint>& waypoints)
    {
        vector<string> names;
        names.reserve(waypoints.size());
        for (const auto& waypoint : waypoints)
        {
            names.push_back(waypoint.name);
        }
        return names;
    }
}

TEST(XPMinimumAltitudeReaderTest, readsMoraGridAltitudes)
{
    auto host = make_shared<ProcedureTestHostServices>();
    XPMinimumAltitudeReader reader(host);

    float altitudeFeet = 0.0f;
    ASSERT_TRUE(reader.tryGetMoraFloorAt(GeoPoint(33.634172222, -84.793300000), altitudeFeet));
    EXPECT_FLOAT_EQ(altitudeFeet, 12000.0f);
}

TEST(XPMinimumAltitudeReaderTest, readsHighestMsaSectorAltitude)
{
    auto host = make_shared<ProcedureTestHostServices>();
    XPMinimumAltitudeReader reader(host);

    float altitudeFeet = 0.0f;
    ASSERT_TRUE(reader.tryGetMsaFloor("KATL", "AAKAY", altitudeFeet));
    EXPECT_FLOAT_EQ(altitudeFeet, 15000.0f);
}

TEST(XPNavaidReaderTest, resolvesFixAndRunwayAlias)
{
    auto host = make_shared<ProcedureTestHostServices>();
    XPNavaidReader reader(host);

    GeoPoint fixLocation;
    ASSERT_TRUE(reader.tryResolveWaypoint("AAKAY", fixLocation));
    EXPECT_NEAR(fixLocation.latitude, 33.634172222, 1e-9);
    EXPECT_NEAR(fixLocation.longitude, -84.793300000, 1e-9);

    GeoPoint runwayLocation;
    ASSERT_TRUE(reader.tryResolveWaypoint("09L", runwayLocation));
    EXPECT_NEAR(runwayLocation.latitude, 42.002829722, 1e-9);
    EXPECT_NEAR(runwayLocation.longitude, -87.893479861, 1e-9);
}

TEST(XPCifpReaderTest, selectsProcedureTrackByAliasAndRunway)
{
    auto host = make_shared<ProcedureTestHostServices>();
    XPCifpReader reader(host);

    istringstream input(ProcedureTestHostServices::sampleCifp());
    auto track = reader.readProcedureTrack(input, "APPCH", "R-09LY", "09L");

    ASSERT_EQ(track.size(), 3u);
    EXPECT_EQ(track[0], "AAKAY");
    EXPECT_EQ(track[1], "ARRBE");
    EXPECT_EQ(track[2], "HRDEY");
}

TEST(XPCifpReaderTest, starTrackUsesInboundWaypointsNotRunwayQualifier)
{
    auto host = make_shared<ProcedureTestHostServices>();
    XPCifpReader reader(host);

    istringstream input(ProcedureTestHostServices::sampleCifp());
    auto track = reader.readProcedureTrack(input, "STAR", "CHPPR1", "09L");

    ASSERT_EQ(track.size(), 3u);
    EXPECT_EQ(track[0], "AAKAY");
    EXPECT_EQ(track[1], "ARRBE");
    EXPECT_EQ(track[2], "HRDEY");
}

TEST(FlightPlanTest, rebuildsProcedureLegsUsingCifpWaypoints)
{
    auto host = make_shared<ProcedureTestHostServices>();

    auto plan = make_shared<FlightPlan>(0, 3600, "KATL", "KATL");
    plan->setDepartureRunway("08B");
    plan->setSid("BANNG3");
    plan->setStar("CHPPR1");
    plan->setApproach("R-09LY");
    plan->setArrivalRunway("09L");
    plan->rebuildProcedureLegs(host);

    ASSERT_GE(plan->legs().size(), 4u);
    EXPECT_EQ(plan->legs()[0]->type(), FlightPlan::LegType::TakeOff);
    EXPECT_EQ(plan->legs()[1]->type(), FlightPlan::LegType::Sid);
    EXPECT_EQ(plan->legs()[1]->fromNavaid(), "RW08B");
    EXPECT_EQ(plan->legs()[1]->toNavaid(), "SKNNR");

    const auto firstStarLeg = find_if(plan->legs().begin(), plan->legs().end(), [](const shared_ptr<FlightPlan::Leg>& leg) {
        return leg && leg->type() == FlightPlan::LegType::Star;
    });
    ASSERT_NE(firstStarLeg, plan->legs().end());
    EXPECT_EQ((*firstStarLeg)->fromNavaid(), "AAKAY");
    EXPECT_EQ((*firstStarLeg)->toNavaid(), "ARRBE");

    EXPECT_EQ(plan->legs().back()->type(), FlightPlan::LegType::Landing);
    EXPECT_EQ(plan->legs().back()->toNavaid(), "09L");
    ASSERT_FALSE(plan->routeWaypoints().empty());
    EXPECT_EQ(plan->routeWaypoints().front().name, "RW08B");
    ASSERT_GT(plan->routeWaypoints().size(), 1u);
    EXPECT_EQ(plan->routeWaypoints()[1].name, "SKNNR");
    EXPECT_NE(plan->routeWaypoints()[1].location, GeoPoint::empty);
    EXPECT_NEAR(plan->routeWaypoints()[1].location.latitude, 33.634563889, 1e-9);
    EXPECT_NEAR(plan->routeWaypoints()[1].location.longitude, -84.342552778, 1e-9);
}

TEST(FlightPlanTest, rebuildsTerrainAwareProcedureAltitudes)
{
    auto host = make_shared<ProcedureTestHostServices>();

    auto plan = make_shared<FlightPlan>(0, 3600, "KATL", "KATL");
    plan->setDepartureRunway("08B");
    plan->setSid("BANNG3");
    plan->setStar("CHPPR1");
    plan->setApproach("R-09LY");
    plan->setArrivalRunway("09L");
    plan->rebuildProcedureLegs(host);

    vector<float> starTargetAltitudes;
    for (const auto& leg : plan->legs())
    {
        if (leg->type() == FlightPlan::LegType::Star)
        {
            starTargetAltitudes.push_back(leg->targetAltitude());
        }
    }

    ASSERT_GE(starTargetAltitudes.size(), 2u);
    EXPECT_GE(starTargetAltitudes.front(), 15000.0f);
    EXPECT_GE(starTargetAltitudes.front(), starTargetAltitudes.back());
}

TEST(FlightTest, setPlanRebuildsProcedureLegsFromHostData)
{
    auto host = make_shared<ProcedureTestHostServices>();
    auto plan = make_shared<FlightPlan>(0, 3600, "KATL", "KATL");
    plan->setDepartureRunway("08B");
    plan->setSid("BANNG3");
    plan->setApproach("R-09LY");
    plan->setArrivalRunway("09L");

    auto flight = make_shared<Flight>(host, 901, Flight::RulesType::IFR, "DAL", "901", "DAL 901", plan);

    ASSERT_TRUE(!!flight->planCursor());
    ASSERT_GE(flight->plan()->legs().size(), 4u);
    EXPECT_EQ(flight->plan()->legs()[1]->fromNavaid(), "RW08B");
    EXPECT_EQ(flight->plan()->legs().back()->toNavaid(), "09L");
}

TEST(FlightPlanTest, repeatedProcedureRebuildKeepsPrimaryRouteStableAndPreservesMissedApproachWaypointsSeparately)
{
    auto host = make_shared<ProcedureTestHostServices>();

    auto plan = make_shared<FlightPlan>(0, 3600, "KATL", "KATL");
    plan->setApproach("I08L");
    plan->setArrivalRunway("08L");

    plan->rebuildProcedureLegs(host);

    const auto firstRouteWaypointNames = waypointNames(plan->routeWaypoints());
    const auto firstKnownWaypointNames = waypointNames(plan->knownWaypoints());

    EXPECT_EQ(count(firstRouteWaypointNames.begin(), firstRouteWaypointNames.end(), "MISSED1"), 0);
    EXPECT_EQ(count(firstRouteWaypointNames.begin(), firstRouteWaypointNames.end(), "MISSED2"), 0);
    EXPECT_EQ(count(firstKnownWaypointNames.begin(), firstKnownWaypointNames.end(), "MISSED1"), 1);
    EXPECT_EQ(count(firstKnownWaypointNames.begin(), firstKnownWaypointNames.end(), "MISSED2"), 1);
    EXPECT_TRUE(any_of(plan->legs().begin(), plan->legs().end(), [](const shared_ptr<FlightPlan::Leg>& leg) {
        return leg && leg->type() == FlightPlan::LegType::GoAround;
    }));

    plan->rebuildProcedureLegs(host);

    EXPECT_EQ(waypointNames(plan->routeWaypoints()), firstRouteWaypointNames);
    EXPECT_EQ(waypointNames(plan->knownWaypoints()), firstKnownWaypointNames);
}

TEST(FlightTest, setArrivalRunwayRebuildsApproachAndResetsCursor)
{
    auto host = make_shared<ProcedureTestHostServices>();
    auto plan = make_shared<FlightPlan>(0, 3600, "KATL", "KATL");
    plan->setApproach("R-09LY");
    plan->setArrivalRunway("09L");

    auto flight = make_shared<Flight>(host, 902, Flight::RulesType::IFR, "DAL", "902", "DAL 902", plan);
    auto cursor = flight->planCursor();

    ASSERT_TRUE(!!cursor);
    ASSERT_TRUE(cursor->activateNextLegOfType(FlightPlan::LegType::Approach));
    EXPECT_EQ(cursor->activeLeg()->fromNavaid(), "AAKAY");

    flight->setArrivalRunway("08L");

    EXPECT_FALSE(cursor->hasActiveLeg());
    ASSERT_TRUE(cursor->activateNextLegOfType(FlightPlan::LegType::Approach));
    EXPECT_EQ(cursor->activeLeg()->fromNavaid(), "JAAJJ");
    EXPECT_EQ(cursor->activeLeg()->toNavaid(), "BAZAR");
    EXPECT_TRUE(any_of(flight->plan()->legs().begin(), flight->plan()->legs().end(), [](const shared_ptr<FlightPlan::Leg>& leg) {
        return leg && leg->type() == FlightPlan::LegType::GoAround && leg->toNavaid() == "MISSED1";
    }));
}
