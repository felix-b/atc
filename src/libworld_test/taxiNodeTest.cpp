// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

TEST(TaxiNodeTest, findEdgeTo) {
    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, GeoPoint({1, 1})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, GeoPoint({2, 2})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, GeoPoint({3, 3})));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, GeoPoint({4, 4})));

    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(12, "12", n1->id(), n2->id()));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(13, "13", n1->id(), n3->id()));

    auto taxiNet = WorldBuilder::assembleTaxiNet(TestHostServices::create(), {}, {n1, n2, n3, n4}, {e12, e13});

    EXPECT_EQ(n1->getEdgeTo(n2), e12);
    EXPECT_EQ(n1->getEdgeTo(n3), e13);

    EXPECT_THROW({
        n1->getEdgeTo(n4);
    }, runtime_error);
}

TEST(TaxiNodeTest, tryFindEdge) {
    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, GeoPoint({1, 1})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, GeoPoint({2, 2})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, GeoPoint({3, 3})));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, GeoPoint({4, 4})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(12, "12", n1->id(), n2->id()));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(13, "13", n1->id(), n3->id()));
    auto e14 = shared_ptr<TaxiEdge>(new TaxiEdge(14, "14", n1->id(), n4->id()));
    auto taxiNet = WorldBuilder::assembleTaxiNet(TestHostServices::create(), {}, {n1, n2, n3, n4}, {e12, e13, e14});

    auto found1 = n1->tryFindEdge([](shared_ptr<TaxiEdge> e) {
        return e->name() == "13";
    });
    auto found2 = n1->tryFindEdge([](shared_ptr<TaxiEdge> e) {
        return e->name() == "ZZZ";
    });

    EXPECT_EQ(found1, e13);
    EXPECT_FALSE(!!found2);
}
