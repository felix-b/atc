// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <algorithm>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

static const Airport::Header testHeader("TEST", "Test Airport", GeoPoint(0,0), 0);

shared_ptr<Airport> createArrivalTestAirport()
{
    /*
     *          10      20      30      40      50      60      70      80  85
     *
     *     30                x   x   x       x   x   x   x   x   x   x      x
     *                         G1  G2  G3   G4  G5  G6  G7  G8  G9   GA   GB
     *     25                   \   \   \    |   |   |   |   |   |   |   /
     *
     *     20          A X-------X-------X-------X-------X-------X A
     *                   |       |     /       / |       |
     *     15         AA1|    AA2|   /AA3    /   |AA5    |AA6
     *                   |       | /       /AA4  |       |
     *     10    X-------X-------X-------X-------X-------X 09/27
     *
     *          10      20      30      40      50      60      70      80
     *
     */
    auto host = TestHostServices::create();

    //region Taxi Nodes
    auto n_10_10 = shared_ptr<TaxiNode>(new TaxiNode(1010, UniPoint::fromGeo(host, {30.10, 45.10})));
    auto n_10_20 = shared_ptr<TaxiNode>(new TaxiNode(1020, UniPoint::fromGeo(host, {30.10, 45.20})));
    auto n_10_30 = shared_ptr<TaxiNode>(new TaxiNode(1030, UniPoint::fromGeo(host, {30.10, 45.30})));
    auto n_10_40 = shared_ptr<TaxiNode>(new TaxiNode(1040, UniPoint::fromGeo(host, {30.10, 45.40})));
    auto n_10_50 = shared_ptr<TaxiNode>(new TaxiNode(1050, UniPoint::fromGeo(host, {30.10, 45.50})));
    auto n_10_60 = shared_ptr<TaxiNode>(new TaxiNode(1060, UniPoint::fromGeo(host, {30.10, 45.60})));
    auto n_20_20 = shared_ptr<TaxiNode>(new TaxiNode(2020, UniPoint::fromGeo(host, {30.20, 45.20})));
    auto n_20_30 = shared_ptr<TaxiNode>(new TaxiNode(2030, UniPoint::fromGeo(host, {30.20, 45.30})));
    auto n_20_40 = shared_ptr<TaxiNode>(new TaxiNode(2040, UniPoint::fromGeo(host, {30.20, 45.40})));
    auto n_20_50 = shared_ptr<TaxiNode>(new TaxiNode(2050, UniPoint::fromGeo(host, {30.20, 45.50})));
    auto n_20_60 = shared_ptr<TaxiNode>(new TaxiNode(2060, UniPoint::fromGeo(host, {30.20, 45.60})));
    auto n_20_70 = shared_ptr<TaxiNode>(new TaxiNode(2070, UniPoint::fromGeo(host, {30.20, 45.70})));
    //endregion

    //region Taxi Edges
    auto e_1010_1020 = shared_ptr<TaxiEdge>(new TaxiEdge(10101020, "09/27", 1010, 1020, TaxiEdge::Type::Runway));
    auto e_1020_1030 = shared_ptr<TaxiEdge>(new TaxiEdge(10201030, "09/27", 1020, 1030, TaxiEdge::Type::Runway));
    auto e_1030_1040 = shared_ptr<TaxiEdge>(new TaxiEdge(10301040, "09/27", 1030, 1040, TaxiEdge::Type::Runway));
    auto e_1040_1050 = shared_ptr<TaxiEdge>(new TaxiEdge(10401050, "09/27", 1040, 1050, TaxiEdge::Type::Runway));
    auto e_1050_1060 = shared_ptr<TaxiEdge>(new TaxiEdge(10501060, "09/27", 1050, 1060, TaxiEdge::Type::Runway));

    auto e_AA1 = shared_ptr<TaxiEdge>(new TaxiEdge(10202020, "AA1", 1020, 2020));
    auto e_AA2 = shared_ptr<TaxiEdge>(new TaxiEdge(10302030, "AA2", 1030, 2030));
    auto e_AA3 = shared_ptr<TaxiEdge>(new TaxiEdge(10302040, "AA3", 1030, 2040));
    auto e_AA4 = shared_ptr<TaxiEdge>(new TaxiEdge(10402050, "AA4", 1040, 2050));
    auto e_AA5 = shared_ptr<TaxiEdge>(new TaxiEdge(10502050, "AA5", 1050, 2050));
    auto e_AA6 = shared_ptr<TaxiEdge>(new TaxiEdge(10602060, "AA6", 1060, 2060));
    auto e_A1 = shared_ptr<TaxiEdge>(new TaxiEdge(20202030, "A", 2020, 2030));
    auto e_A2 = shared_ptr<TaxiEdge>(new TaxiEdge(20302040, "A", 2030, 2040));
    auto e_A3 = shared_ptr<TaxiEdge>(new TaxiEdge(20402050, "A", 2040, 2050));
    auto e_A4 = shared_ptr<TaxiEdge>(new TaxiEdge(20502060, "A", 2050, 2060));
    auto e_A5 = shared_ptr<TaxiEdge>(new TaxiEdge(20602070, "A", 2060, 2070));
    //endregion

    //region Gates
    auto g_1 = make_shared<ParkingStand>(1001, "G1", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.25}), 315, "1");
    auto g_2 = make_shared<ParkingStand>(1002, "G2", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.30}), 315, "1");
    auto g_3 = make_shared<ParkingStand>(1003, "G3", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.35}), 315, "1");
    auto g_4 = make_shared<ParkingStand>(1004, "G4", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.45}), 0, "1");
    auto g_5 = make_shared<ParkingStand>(1005, "G5", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.50}), 0, "1");
    auto g_6 = make_shared<ParkingStand>(1006, "G6", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.55}), 0, "1");
    auto g_7 = make_shared<ParkingStand>(1007, "G7", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.60}), 0, "1");
    auto g_8 = make_shared<ParkingStand>(1008, "G8", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.65}), 0, "1");
    auto g_9 = make_shared<ParkingStand>(1009, "G9", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.70}), 0, "1");
    auto g_A = make_shared<ParkingStand>(1010, "GA", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.75}), 0, "1");
    auto g_B = make_shared<ParkingStand>(1011, "G1", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.30, 45.85}), 45, "1");
    //endregion

    //region Runways
    auto rwy_0927 = shared_ptr<Runway>(new Runway(
        Runway::End("09", 0, 0, n_10_10->location()),
        Runway::End("27", 0, 0, n_10_60->location()),
        30
    ));
    //endregion

    auto airport = WorldBuilder::assembleAirport(
        host,
        testHeader,
        { rwy_0927 },
        {
            g_1, g_2, g_3, g_4, g_5, g_6, g_7, g_8, g_9, g_A, g_B
        },
        {
            n_10_10, n_10_20, n_10_30, n_10_40, n_10_50, n_10_60, n_20_20, n_20_30, n_20_40, n_20_50, n_20_60, n_20_70,
        },
        {
            e_1010_1020, e_1020_1030, e_1030_1040, e_1040_1050, e_1050_1060,
            e_AA1, e_AA2, e_AA3, e_AA4, e_AA5, e_AA6, e_A1, e_A2, e_A3, e_A4, e_A5,
        }
    );

    return airport;
}

TEST(TaxiPathTest, findClosestNodeOnRunway)
{
    auto airport = createArrivalTestAirport();
    auto rwy0927 = airport->getRunwayOrThrow("27");
    const auto& rwyEnd09 = rwy0927->getEndOrThrow("09");
    const auto& rwyEnd27 = rwy0927->getEndOrThrow("27");

    auto closest09 = airport->taxiNet()->findClosestNodeOnRunway(GeoPoint(30.10, 45.35), rwy0927, rwyEnd09);
    auto closest27 = airport->taxiNet()->findClosestNodeOnRunway(GeoPoint(30.10, 45.35), rwy0927, rwyEnd27);

    ASSERT_TRUE(!!closest09);
    EXPECT_EQ(closest09->id(), 1040);

    ASSERT_TRUE(!!closest27);
    EXPECT_EQ(closest27->id(), 1030);
}

TEST(TaxiPathTest, arrival_runwayToGate_straight)
{
    auto airport = createArrivalTestAirport();
    auto runway = airport->getRunwayOrThrow("09");
    auto path = airport->taxiNet()->tryFindArrivalPathRunwayToGate(
        runway,
        runway->getEndOrThrow("09"),
        airport->getParkingStandOrThrow("G5"),
        GeoPoint(30.10, 45.45));

    ASSERT_TRUE(!!path);
    EXPECT_EQ(path->edges.size(), 4);

    EXPECT_FLOAT_EQ(path->edges[0]->node1()->location().latitude(), 30.10);
    EXPECT_FLOAT_EQ(path->edges[0]->node1()->location().longitude(), 45.45);

    EXPECT_FLOAT_EQ(path->edges[0]->node2()->location().latitude(), 30.10);
    EXPECT_FLOAT_EQ(path->edges[0]->node2()->location().longitude(), 45.50);

    EXPECT_EQ(path->edges[1]->id(), 10502050);

    EXPECT_FLOAT_EQ(path->edges[2]->node1()->location().latitude(), 30.20);
    EXPECT_FLOAT_EQ(path->edges[2]->node1()->location().longitude(), 45.50);

    EXPECT_FLOAT_EQ(
        path->edges[2]->node2()->location().latitude(),
        path->edges[3]->node1()->location().latitude());
    EXPECT_FLOAT_EQ(
        path->edges[2]->node2()->location().longitude(),
        path->edges[3]->node1()->location().longitude());

    EXPECT_FLOAT_EQ(path->edges[3]->heading(), 0);
}
