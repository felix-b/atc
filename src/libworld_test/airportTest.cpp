// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <string>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "airportTest.h"

using namespace world;

TEST(AirportTest, findLongestRunway)
{
    auto host = TestHostServices::create();
    Airport::Header header("ABCD", "Test", GeoPoint(30, 45), 123);
    auto airport = WorldBuilder::assembleAirport(host, header,{
        makeRunway(host, { 30.01, 45.01 }, { 30.02, 45.02 }, "04", "22"),
        makeRunway(host, { 30.01, 45.01 }, { 30.01, 45.05 }, "09", "27"),
        makeRunway(host, { 30.01, 45.01 }, { 30.02, 45.01 }, "01", "19"),
    }, {}, {}, {});

    shared_ptr<Runway> longestRunway = airport->findLongestRunway();

    EXPECT_TRUE(!!longestRunway);
    EXPECT_EQ(longestRunway->end1().name(), "09");
    EXPECT_EQ(longestRunway->end2().name(), "27");
}

TEST(AirportTest, findLongestParallelRunwayGroup)
{
    auto host = TestHostServices::create();
    Airport::Header header("ABCD", "Test", GeoPoint(30, 40), 0);
    auto airport = WorldBuilder::assembleAirport(host, header,{
        makeRunway(host, { 30.01, 40.00 }, { 30.01, 40.02 }, "09R", "27L"),
        makeRunway(host, { 30.00, 40.00 }, { 30.02, 40.00 }, "01L", "19R"),
        makeRunway(host, { 30.00, 40.02 }, { 30.015, 40.02 }, "01R", "19L"),
        makeRunway(host, { 31.03, 41.03 }, { 31.06, 41.06 }, "04R", "22L"),
        makeRunway(host, { 30.00, 40.00 }, { 30.00, 40.02 }, "09L", "27R"),
        makeRunway(host, { 30.00, 40.00 }, { 30.03, 40.03 }, "04L", "22R"),
        makeRunway(host, { 30.00, 40.01 }, { 30.02, 40.01 }, "01C", "19C"),
    }, {}, {}, {}, {});

    ASSERT_EQ(airport->parallelRunwayGroupCount(), 3);

    const vector<shared_ptr<Runway>>& longestRunwayGroup = airport->findLongestParallelRunwayGroup();

    ASSERT_EQ(longestRunwayGroup.size(), 2);
    EXPECT_EQ(longestRunwayGroup[0]->end1().name(), "04R");
    EXPECT_EQ(longestRunwayGroup[1]->end1().name(), "04L");
}

shared_ptr<Runway> makeRunway(
    shared_ptr<HostServices> host,
    const GeoPoint& p1,
    const GeoPoint& p2,
    const string& name1,
    const string& name2,
    float widthMeters,
    float displacedThresholdMeters)
{
    Runway::End end1(name1, displacedThresholdMeters, 0.0f, UniPoint::fromGeo(host, p1));
    Runway::End end2(name2, displacedThresholdMeters, 10.0f, UniPoint::fromGeo(host, p2));
    return shared_ptr<Runway>(new Runway(end1, end2, widthMeters));
}
