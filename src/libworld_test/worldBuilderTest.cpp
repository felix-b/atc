// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

static const float GROUND = 1000;
static const Airport::Header testHeader("TEST", "Test Airport", GeoPoint(0,0), 0);

TEST(WorldBuilderTest, buildTaxiNet_singleEdge) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e1 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E1", 111, 222));
    
    auto airport = WorldBuilder::assembleAirport(testHeader, {}, {}, { n1, n2 }, { e1 });
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

    auto airport = WorldBuilder::assembleAirport(testHeader, {}, {}, { n1, n2, n3 }, { e12, e23, e13 });
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
