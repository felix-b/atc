// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

static const float GROUND = 1000;
static const Airport::Header testHeader("TEST", "Test Airport", GeoPoint(0,0), 0);

TEST(TaxiEdgeTest, calculateTaxiDistance) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {40, GROUND, 50})));

    EXPECT_FLOAT_EQ(TaxiEdge::calculateTaxiDistance(n1, n2), 14.142136); //sqrt(200)
    EXPECT_FLOAT_EQ(TaxiEdge::calculateTaxiDistance(n2, n3), 36.0555); //sqrt(1300)
}

TEST(TaxiEdgeTest, flipOver) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222));

    auto net = WorldBuilder::assembleAirport(host, testHeader, {}, {}, { n1, n2 }, { e12 });

    EXPECT_EQ(e12->nodeId1(), 111);
    EXPECT_EQ(e12->node1(), n1);
    EXPECT_EQ(e12->nodeId2(), 222);
    EXPECT_EQ(e12->node2(), n2);
    EXPECT_FLOAT_EQ(e12->lengthMeters(), 14.142136); //sqrt(200)

    auto e21 = TaxiEdge::flipOver(e12);

    EXPECT_EQ(e21->nodeId1(), 222);
    EXPECT_EQ(e21->node1(), n2);
    EXPECT_EQ(e21->nodeId2(), 111);
    EXPECT_EQ(e21->node2(), n1);
    EXPECT_FLOAT_EQ(e21->lengthMeters(), 14.142136); //sqrt(200)
}
