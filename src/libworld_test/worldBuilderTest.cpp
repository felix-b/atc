// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "airportTest.h"

using namespace world;

static const float GROUND = 1000;
static const Airport::Header testHeader("TEST", "Test Airport", GeoPoint(0,0), 0);

TEST(WorldBuilderTest, buildTaxiNet_singleEdge) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e1 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E1", 111, 222));
    
    auto airport = WorldBuilder::assembleAirport(host, testHeader, {}, {}, { n1, n2 }, { e1 });
    auto net = airport->taxiNet();

    EXPECT_EQ(net->nodes().size(), 2);
    EXPECT_EQ(net->nodes()[0], n1);
    EXPECT_EQ(net->nodes()[1], n2);

    EXPECT_EQ(net->edges().size(), 1);
    EXPECT_EQ(net->edges()[0], e1);

    EXPECT_EQ(n1->edges().size(), 1);
    EXPECT_EQ(n1->edges()[0], e1);

    EXPECT_EQ(n2->edges().size(), 1);
    EXPECT_NE(n2->edges()[0], e1);
    EXPECT_EQ(n2->edges()[0]->name(), "E1");
    EXPECT_EQ(n2->edges()[0]->node1(), n2);
    EXPECT_EQ(n2->edges()[0]->node2(), n1);
}

TEST(WorldBuilderTest, buildTaxiNet_triangle) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {10, GROUND, 20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "E23", 222, 333));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "E13", 111, 333));

    auto airport = WorldBuilder::assembleAirport(host, testHeader, {}, {}, { n1, n2, n3 }, { e12, e23, e13 });
    auto net = airport->taxiNet();

    EXPECT_EQ(net->nodes().size(), 3);
    EXPECT_EQ(net->nodes()[0], n1);
    EXPECT_EQ(net->nodes()[1], n2);
    EXPECT_EQ(net->nodes()[2], n3);

    EXPECT_EQ(net->edges().size(), 3);
    EXPECT_EQ(net->edges()[0], e12);
    EXPECT_EQ(net->edges()[1], e23);
    EXPECT_EQ(net->edges()[2], e13);

    EXPECT_EQ(n1->edges().size(), 2);
    EXPECT_EQ(n1->edges()[0], e12);
    EXPECT_EQ(n1->edges()[1], e13);

    EXPECT_EQ(n2->edges().size(), 2);
    EXPECT_EQ(n2->edges()[0]->name(), "E12");
    EXPECT_EQ(n2->edges()[0]->node1(), n2);
    EXPECT_EQ(n2->edges()[0]->node2(), n1);
    EXPECT_EQ(n2->edges()[1], e23);

    EXPECT_EQ(n3->edges().size(), 2);
    EXPECT_EQ(n3->edges()[0]->name(), "E23");
    EXPECT_EQ(n3->edges()[0]->node1(), n3);
    EXPECT_EQ(n3->edges()[0]->node2(), n2);
    EXPECT_EQ(n3->edges()[1]->name(), "E13");
    EXPECT_EQ(n3->edges()[1]->node1(), n3);
    EXPECT_EQ(n3->edges()[1]->node2(), n1);
}

TEST(WorldBuilderTest, buildTaxiNet_hasTaxiway_hasRunways)
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {10, GROUND, 20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, UniPoint::fromLocal(host, {20, GROUND, 10})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "E23", 222, 333));
    auto e34 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "E34", 333, 444, TaxiEdge::Type::Groundway));
    auto e41 = shared_ptr<TaxiEdge>(new TaxiEdge(1004, "09/27", 444, 111, TaxiEdge::Type::Runway));

    auto rwy0927 = shared_ptr<Runway>(new Runway(
        Runway::End("09", 0, 0, n1->location()),
        Runway::End("27", 0, 0, n4->location()),
        30
    ));

    auto airport = WorldBuilder::assembleAirport(
        host,
        testHeader,
        { rwy0927 },
        {},
        { n1, n2, n3, n4 },
        { e12, e23, e34, e41 });

    EXPECT_TRUE(n1->hasTaxiway());
    EXPECT_TRUE(n2->hasTaxiway());
    EXPECT_TRUE(n3->hasTaxiway());
    EXPECT_FALSE(n4->hasTaxiway());

    EXPECT_TRUE(n1->hasRunway());
    EXPECT_FALSE(n2->hasRunway());
    EXPECT_FALSE(n3->hasRunway());
    EXPECT_TRUE(n4->hasRunway());
}

TEST(WorldBuilderTest, assembleAirport_taxiNetAndRunways)
{
    auto host = TestHostServices::create();

    auto rwy1836 = shared_ptr<Runway>(new Runway(
        Runway::End("18", 0, 0, UniPoint::fromLocal(host, {5, GROUND, 0})),
        Runway::End("36", 0, 0, UniPoint::fromLocal(host, {5, GROUND, 30})),
        45
    ));
    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {10, GROUND, 20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto nrwy1 = shared_ptr<TaxiNode>(new TaxiNode(11, UniPoint::fromLocal(host, {5, GROUND, 2})));
    auto nrwy2 = shared_ptr<TaxiNode>(new TaxiNode(22, UniPoint::fromLocal(host, {5, GROUND, 10})));
    auto nrwy3 = shared_ptr<TaxiNode>(new TaxiNode(33, UniPoint::fromLocal(host, {5, GROUND, 20})));
    auto nrwy4 = shared_ptr<TaxiNode>(new TaxiNode(44, UniPoint::fromLocal(host, {5, GROUND, 28})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "E23", 222, 333));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "E23", 111, 333));
    auto e1r = shared_ptr<TaxiEdge>(new TaxiEdge(1004, "E1R", 111, 22));
    auto e2r = shared_ptr<TaxiEdge>(new TaxiEdge(1005, "E2R", 222, 33));
    auto er12 = shared_ptr<TaxiEdge>(new TaxiEdge(1006, "18/36", 11, 22, TaxiEdge::Type::Runway));
    auto er23 = shared_ptr<TaxiEdge>(new TaxiEdge(1007, "18/36", 22, 33, TaxiEdge::Type::Runway));
    auto er34 = shared_ptr<TaxiEdge>(new TaxiEdge(1008, "18/36", 33, 44, TaxiEdge::Type::Runway));

    WorldBuilder::addActiveZone(e1r, "18/36", true, false, false);
    WorldBuilder::addActiveZone(e2r, "18/36", false, true, true);

    auto airport = WorldBuilder::assembleAirport(
        host,
        testHeader, 
        { rwy1836 }, 
        {},
        { n1, n2, n3, nrwy1, nrwy2, nrwy3, nrwy4 }, 
        { e12, e23, e13, e1r, e2r, er12, er23, er34 }
    );

    ASSERT_EQ(airport->runways().size(), 1);
    EXPECT_EQ(airport->runways()[0], rwy1836);

    auto net = airport->taxiNet();

    EXPECT_FALSE(!!e12->runway());
    EXPECT_FALSE(!!e23->runway());
    EXPECT_FALSE(!!e13->runway());
    EXPECT_FALSE(e12->activeZones().hasAny());
    EXPECT_FALSE(e23->activeZones().hasAny());
    EXPECT_FALSE(e13->activeZones().hasAny());

    EXPECT_TRUE(e1r->activeZones().departue.has(rwy1836));
    EXPECT_FALSE(e1r->activeZones().arrival.has(rwy1836));
    EXPECT_FALSE(e1r->activeZones().ils.has(rwy1836));
    EXPECT_FALSE(!!e1r->runway());

    EXPECT_TRUE(e1r->activeZones().departue.has(rwy1836));
    EXPECT_FALSE(e1r->activeZones().arrival.hasAny());
    EXPECT_FALSE(e1r->activeZones().ils.hasAny());

    EXPECT_FALSE(e2r->activeZones().departue.hasAny());
    EXPECT_TRUE(e2r->activeZones().arrival.has(rwy1836));
    EXPECT_TRUE(e2r->activeZones().ils.has(rwy1836));
    EXPECT_FALSE(!!e2r->runway());

    EXPECT_EQ(er12->runway(), rwy1836);
    EXPECT_EQ(er12->runway(), rwy1836);
    EXPECT_EQ(er12->runway(), rwy1836);
}

TEST(WorldBuilderTest, buildTaxiNet_isHighSpeedExitRunway)
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromGeo(host, {30.20, 45.10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromGeo(host, {30.20, 45.20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromGeo(host, {30.20, 45.30})));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, UniPoint::fromGeo(host, {30.10, 45.10})));
    auto n5 = shared_ptr<TaxiNode>(new TaxiNode(555, UniPoint::fromGeo(host, {30.10, 45.20})));
    auto n6 = shared_ptr<TaxiNode>(new TaxiNode(666, UniPoint::fromGeo(host, {30.10, 45.30})));
    auto n7 = shared_ptr<TaxiNode>(new TaxiNode(777, UniPoint::fromGeo(host, {30.30, 45.30})));
    auto n8 = shared_ptr<TaxiNode>(new TaxiNode(888, UniPoint::fromGeo(host, {30.30, 45.10})));

    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "09/27", 111, 222, TaxiEdge::Type::Runway));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "09/27", 222, 333, TaxiEdge::Type::Runway));
    auto e14 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "A1", 111, 444));
    auto e26 = shared_ptr<TaxiEdge>(new TaxiEdge(1004, "A2", 222, 666));
    auto e27 = shared_ptr<TaxiEdge>(new TaxiEdge(1005, "B1", 222, 777));
    auto e28 = shared_ptr<TaxiEdge>(new TaxiEdge(1006, "B2", 222, 888));
    auto e36 = shared_ptr<TaxiEdge>(new TaxiEdge(1007, "A3", 333, 666));

    auto rwy0927 = shared_ptr<Runway>(new Runway(
        Runway::End("09", 0, 0, n1->location()),
        Runway::End("27", 0, 0, n3->location()),
        30
    ));

    auto airport = WorldBuilder::assembleAirport(
        host,
        testHeader,
        { rwy0927 },
        {},
        { n1, n2, n3, n4, n5, n6, n7, n8 },
        { e12, e23, e14, e26, e27, e28, e36 });

    EXPECT_FALSE(e12->isHighSpeedExitRunway("09"));
    EXPECT_FALSE(e12->isHighSpeedExitRunway("27"));

    EXPECT_FALSE(e23->isHighSpeedExitRunway("09"));
    EXPECT_FALSE(e23->isHighSpeedExitRunway("27"));

    EXPECT_FALSE(e14->isHighSpeedExitRunway("09"));
    EXPECT_FALSE(e14->isHighSpeedExitRunway("27"));

    EXPECT_TRUE(e26->isHighSpeedExitRunway("09"));
    EXPECT_FALSE(e26->isHighSpeedExitRunway("27"));

    EXPECT_TRUE(e27->isHighSpeedExitRunway("09"));
    EXPECT_FALSE(e27->isHighSpeedExitRunway("27"));

    EXPECT_FALSE(e28->isHighSpeedExitRunway("09"));
    EXPECT_TRUE(e28->isHighSpeedExitRunway("27"));

    EXPECT_FALSE(e36->isHighSpeedExitRunway("09"));
    EXPECT_FALSE(e36->isHighSpeedExitRunway("27"));
}

TEST(WorldBuilderTest, buildTaxiNet_runwayEdgeDirection)
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromGeo(host, {30.20, 45.10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromGeo(host, {30.20, 45.20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromGeo(host, {30.20, 45.30})));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, UniPoint::fromGeo(host, {30.10, 45.10})));
    auto n5 = shared_ptr<TaxiNode>(new TaxiNode(555, UniPoint::fromGeo(host, {30.10, 45.20})));
    auto n6 = shared_ptr<TaxiNode>(new TaxiNode(666, UniPoint::fromGeo(host, {30.10, 45.30})));

    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "09/27", 111, 222, TaxiEdge::Type::Runway));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "09/27", 222, 333, TaxiEdge::Type::Runway));
    auto e41 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "A1", 444, 111));
    auto e63= shared_ptr<TaxiEdge>(new TaxiEdge(1003, "A2", 666, 333));

    auto rwy0927 = shared_ptr<Runway>(new Runway(
        Runway::End("09", 0, 0, n1->location()),
        Runway::End("27", 0, 0, n3->location()),
        30
    ));

    auto airport = WorldBuilder::assembleAirport(
        host,
        testHeader,
        { rwy0927 },
        {},
        { n1, n2, n3, n4, n5, n6},
        { e12, e23, e41, e63 });

    auto e21 = TaxiEdge::flipOver(e12);
    auto e32 = TaxiEdge::flipOver(e23);
    auto e14 = TaxiEdge::flipOver(e41);
    auto e36 = TaxiEdge::flipOver(e63);

    EXPECT_TRUE(e12->isRunway("09"));
    EXPECT_FALSE(e12->isRunway("27"));
    EXPECT_FALSE(e21->isRunway("09"));
    EXPECT_TRUE(e21->isRunway("27"));

    EXPECT_TRUE(e23->isRunway("09"));
    EXPECT_FALSE(e23->isRunway("27"));
    EXPECT_FALSE(e32->isRunway("09"));
    EXPECT_TRUE(e32->isRunway("27"));

    EXPECT_FALSE(e41->isRunway("09"));
    EXPECT_FALSE(e41->isRunway("27"));
    EXPECT_FALSE(e14->isRunway("09"));
    EXPECT_FALSE(e14->isRunway("27"));

    EXPECT_FALSE(e63->isRunway("09"));
    EXPECT_FALSE(e63->isRunway("27"));
    EXPECT_FALSE(e36->isRunway("09"));
    EXPECT_FALSE(e36->isRunway("27"));
}

TEST(WorldBuilderTest, tidyAirportElevations_runways) {
    auto host = TestHostServices::create();
    Airport::Header header("ABCD", "Test", GeoPoint(30, 45), 12);
    auto airport = WorldBuilder::assembleAirport(host, header,{
        makeRunway(host, { 30.01, 45.01 }, { 30.02, 45.02 }, "04", "22"),
        makeRunway(host, { 30.01, 45.01 }, { 30.02, 45.01 }, "01", "19"),
    }, {}, {}, {});

    ASSERT_EQ(airport->runways().size(), 2);
    const auto& rwy1 = airport->runways()[0];
    const auto& rwy2 = airport->runways()[1];

    EXPECT_FLOAT_EQ(rwy1->end1().elevationFeet(), 12);
    EXPECT_FLOAT_EQ(rwy1->end2().elevationFeet(), 12);
    EXPECT_FLOAT_EQ(rwy2->end1().elevationFeet(), 12);
    EXPECT_FLOAT_EQ(rwy2->end2().elevationFeet(), 12);

    WorldBuilder::tidyAirportElevations(host, airport);

    EXPECT_FLOAT_EQ(rwy1->end1().elevationFeet(), 123);
    EXPECT_FLOAT_EQ(rwy1->end2().elevationFeet(), 123);
    EXPECT_FLOAT_EQ(rwy2->end1().elevationFeet(), 123);
    EXPECT_FLOAT_EQ(rwy2->end2().elevationFeet(), 123);
}

TEST(WorldBuilderTest, assembleAirport_detectParallelRunways_positive) {
    auto host = TestHostServices::create();
    Airport::Header header("ABCD", "Test", GeoPoint(30, 45), 12);
    auto airport = WorldBuilder::assembleAirport(host, header,{
        makeRunway(host, { 30.01, 40.00 }, { 30.01, 40.01 }, "09R", "27L"),
        makeRunway(host, { 30.00, 40.00 }, { 30.01, 40.00 }, "01L", "19R"),
        makeRunway(host, { 30.00, 40.02 }, { 30.01, 40.02 }, "01R", "19L"),
        makeRunway(host, { 30.00, 40.00 }, { 30.01, 40.01 }, "04", "22"),
        makeRunway(host, { 30.00, 40.00 }, { 30.00, 40.01 }, "09L", "27R"),
        makeRunway(host, { 30.00, 40.01 }, { 30.01, 40.01 }, "01C", "19C"),
    }, {}, {}, {}, {});

    ASSERT_EQ(airport->parallelRunwayGroupCount(), 2);

    const vector<shared_ptr<Runway>>& group1 = airport->getParallelRunwayGroup(0);
    ASSERT_EQ(group1.size(), 2);
    EXPECT_EQ(group1[0]->end1().name(), "09R");
    EXPECT_EQ(group1[1]->end1().name(), "09L");

    const vector<shared_ptr<Runway>>& group2 = airport->getParallelRunwayGroup(1);
    ASSERT_EQ(group2.size(), 3);
    EXPECT_EQ(group2[0]->end1().name(), "01L");
    EXPECT_EQ(group2[1]->end1().name(), "01R");
    EXPECT_EQ(group2[2]->end1().name(), "01C");
}

TEST(WorldBuilderTest, assembleAirport_detectParallelRunways_negative) {
    auto host = TestHostServices::create();
    Airport::Header header("ABCD", "Test", GeoPoint(30, 45), 12);
    auto airport = WorldBuilder::assembleAirport(host, header,{
        makeRunway(host, { 30.01, 40.00 }, { 30.01, 40.01 }, "09", "27"),
        makeRunway(host, { 30.00, 40.00 }, { 30.01, 40.01 }, "04", "22"),
    }, {}, {}, {}, {});

    EXPECT_EQ(airport->parallelRunwayGroupCount(), 0);
    EXPECT_THROW({
        airport->getParallelRunwayGroup(0);
    }, out_of_range);
}
