// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <algorithm>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

static const int GROUND = 1000;
static int nextNodeId = 1;
static const Airport::Header testHeader("TEST", "Test Airport", GeoPoint(0,0), 0);

void assertTaxiPath(const string& testCase, const vector<shared_ptr<TaxiEdge>>& expected, const shared_ptr<TaxiPath> actual);
void assertTaxiPath(
    const shared_ptr<TaxiNet> net, 
    const string& testCase, 
    const shared_ptr<TaxiPath> actualPath,
    const vector<int>& expectedNodeIds);

shared_ptr<TaxiNet> createMediumTestNet()
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 40})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {30, GROUND, 40})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {30, GROUND, 60})));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, UniPoint::fromLocal(host, {30, GROUND, 10})));
    auto n5 = shared_ptr<TaxiNode>(new TaxiNode(555, UniPoint::fromLocal(host, {90, GROUND, 60})));
    auto n6 = shared_ptr<TaxiNode>(new TaxiNode(666, UniPoint::fromLocal(host, {50, GROUND, 40})));
    auto n7 = shared_ptr<TaxiNode>(new TaxiNode(777, UniPoint::fromLocal(host, {80, GROUND, 20})));
    auto n8 = shared_ptr<TaxiNode>(new TaxiNode(888, UniPoint::fromLocal(host, {40, GROUND, 25})));

    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222));
    auto e14 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "E14", 111, 444));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "E13", 111, 333));
    auto e24 = shared_ptr<TaxiEdge>(new TaxiEdge(1004, "E24", 222, 444));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1005, "E23", 222, 333));
    auto e35 = shared_ptr<TaxiEdge>(new TaxiEdge(1006, "E35", 333, 555));
    auto e56 = shared_ptr<TaxiEdge>(new TaxiEdge(1007, "E56", 555, 666));
    auto e48 = shared_ptr<TaxiEdge>(new TaxiEdge(1008, "E48", 444, 888));
    auto e68 = shared_ptr<TaxiEdge>(new TaxiEdge(1009, "E68", 666, 888));
    auto e67 = shared_ptr<TaxiEdge>(new TaxiEdge(1009, "E67", 666, 777));

    auto airport = WorldBuilder::assembleAirport(
        testHeader,
        {},
        {},
        { n1, n2, n3, n4, n5, n6, n7, n8 }, 
        { e12, e14, e13, e24, e23, e35, e56, e48, e67, e68 }
    );

    return airport->taxiNet();
}

/*
static int nextEdgeId = 1;

#define NODE(var,id,x,z) auto var = shared_ptr<TaxiNode>(new TaxiNode((id), {(x), GROUND, (z)}))
#define JUNC(var,id,x,z) auto var = shared_ptr<TaxiNode>(new TaxiNode((id), {(x), GROUND, (z)}, true))

#define TWY_EDGE(var,name,n1,n2) auto var = shared_ptr<TaxiEdge>(new TaxiEdge( \
    nextEdgeId++, (name), (n1), (n2)))
#define RWY_EDGE(var,name,n1,n2) auto var = shared_ptr<TaxiEdge>(new TaxiEdge( \
    nextEdgeId++, (name), (n1), (n2)))
#define TWY_AZONE(var,name,n1,n2,rwy) auto var = shared_ptr<TaxiEdge>(new TaxiEdge( \
    nextEdgeId++, (name), (n1), (n2), \
    TaxiEdge::Type::RunwayActiveZone, \
    {(rwy)}))
#define TWY_AZONE2(var,name,n1,n2,rwy1,rwy2) auto var = shared_ptr<TaxiEdge>(new TaxiEdge( \
    nextEdgeId++, (name), (n1), (n2), \
    TaxiEdge::Type::RunwayActiveZone, \
    {(rwy1),(rwy2)}))

shared_ptr<TaxiNet> createAirportTestNet()
{
    NODE(n_08   , 1001, 3, 30);
    NODE(n_s1   , 1002, 4, 30);
    NODE(n_12   , 1003, 5, 25);
    JUNC(n_08_k , 1004, 6, 26);
    NODE(n_s2   , 1005, 6, 29);
    JUNC(n_08_12, 1006, 7, 25);
    NODE(n_k1   , 1007, 7, 27);
    JUNC(n_k_s  , 1008, 8, 27);
    JUNC(n_08_w4, 1009, 10, 21);
    JUNC(n_12_w4, 1010, 10, 25);
    JUNC(n_k_w4,  1011, 10, 27);
    JUNC(n_z_k_m, 1012, 11, 27);
    NODE(n_m1,    1013, 11, 28);
    JUNC(n_12_z,  1014, 13, 25);
    JUNC(n_k2_k,  1015, 13, 27);
    JUNC(n_k2_m,  1016, 13, 28);
    JUNC(n_08_w3, 1017, 14, 16);
    NODE(n_r5,    1018, 15, 19);
    JUNC(n_r_n,   1019, 15, 23);
    JUNC(n_12_r,  1020, 15, 25);
    JUNC(n_r_k_m2,1021, 15, 27);
    JUNC(n_r_m2_m,1022, 15, 28);
    JUNC(n_m1_k,  1023, 17, 27);
    JUNC(n_m1_m,  1024, 17, 28);
    JUNC(n_26_w2, 1025, 18, 11);
    JUNC(n_r_w3,  1026, 18, 15);
    JUNC(n_12_y,  1027, 19, 25);
    JUNC(n_k1_k,  1028, 19, 27);
    JUNC(n_k1_m,  1029, 19, 28);
    JUNC(n_r_w2,  1030, 20, 12);
    JUNC(n_26   , 1031, 21, 7);
    JUNC(n_r_e  , 1032, 21, 11);
    JUNC(n_e_t2 , 1033, 21, 17);
    JUNC(n_e_n  , 1034, 21, 23);
    JUNC(n_12_e , 1025, 21, 25);
    JUNC(n_e_k_y, 1026, 21, 27);
    JUNC(n_e_m,   1027, 21, 28);
    NODE(n_e2_1,  1028, 21, 29);
    JUNC(n_21,    1029, 23, 7);
    JUNC(n_21_t2, 1030, 23, 17);
    JUNC(n_21_n,  1031, 23, 23);
    JUNC(n_21_t3, 1032, 23, 24);
    JUNC(n_21_30, 1033, 23, 25);
    JUNC(n_03_k,  1034, 23, 27);
    JUNC(n_03_m,  1035, 23, 28);
    JUNC(n_03_e2, 1036, 23, 29);
    NODE(n_03,    1037, 23, 30);
    NODE(n_t1_1,  1038, 25, 7);
    NODE(n_t1_2,  1039, 25, 13);
    NODE(n_t2_2,  1040, 25, 17);
    JUNC(n_30_f,  1041, 25, 25);
    JUNC(n_f_k,   1042, 25, 27);
    NODE(n_e2_2,  1043, 25, 29);
    NODE(n_m2,    1044, 26, 28);
    JUNC(n_m_k_l, 1045, 27, 27);
    NODE(n_n_l1,  1046, 28, 23);
    NODE(n_k_l3,  1047, 28, 27);
    NODE(n_l_2,   1048, 29, 24);
    JUNC(n_30_l,  1048, 29, 25);
    NODE(n_l_3,   1048, 29, 26);
    NODE(n_30,    1048, 30, 25);

    auto rwy0826 = make_shared<Runway>(n_08, 75.0, 8, n_26, 255.0, 26);
    auto rwy1230 = make_shared<Runway>(n_12, 117.0, 12, n_30, 296.0, 30);
    auto rwy0321 = make_shared<Runway>(n_03, 30.0, 03, n_21, 210.0, 210);

    RWY_EDGE(e0826_1, "08-26", n_08, n_08_k);
    RWY_EDGE(e0826_2, "08-26", n_08_k, n_08_12);
    RWY_EDGE(e0826_3, "08-26", n_08_12, n_08_w4);
    RWY_EDGE(e0826_4, "08-26", n_08_w4, n_08_w3);
    RWY_EDGE(e0826_5, "08-26", n_08_w3, n_26_w2);
    RWY_EDGE(e0826_6, "08-26", n_26_w2, n_26);

    RWY_EDGE(e1230_1, "12-30", n_12, n_08_12);
    RWY_EDGE(e1230_2, "12-30", n_08_12, n_12_w4);
    RWY_EDGE(e1230_3, "12-30", n_12_w4, n_12_z);
    RWY_EDGE(e1230_4, "12-30", n_12_z, n_12_r);
    RWY_EDGE(e1230_5, "12-30", n_12_r, n_12_y);
    RWY_EDGE(e1230_6, "12-30", n_12_y, n_12_e);
    RWY_EDGE(e1230_7, "12-30", n_12_e, n_21_30);
    RWY_EDGE(e1230_8, "12-30", n_21_30, n_30_f);
    RWY_EDGE(e1230_9, "12-30", n_30_f, n_30_l);
    RWY_EDGE(e1230_10,"12-30", n_30_l, n_30);

    RWY_EDGE(e0321_1, "03-21", n_03, n_03_e2);
    RWY_EDGE(e0321_2, "03-21", n_03_e2, n_03_m);
    RWY_EDGE(e0321_3, "03-21", n_03_m, n_03_k);
    RWY_EDGE(e0321_4, "03-21", n_03_k, n_21_30);
    RWY_EDGE(e0321_5, "03-21", n_21_30, n_21_t3);
    RWY_EDGE(e0321_6, "03-21", n_21_t3, n_21_n);
    RWY_EDGE(e0321_7, "03-21", n_21_n, n_21_t2);
    RWY_EDGE(e0321_8, "03-21", n_21_n, n_21_t2);
    RWY_EDGE(e0321_9, "03-21", n_21_t2, n_21);

    TWY_AZONE(eS1, "S", n_08, n_s1, rwy0826);
    TWY_EDGE(eS2, "S", n_s1, n_s2);
    TWY_EDGE(eS3, "S", n_s2, n_k_s);
    
    TWY_AZONE(eK_1, "K", n_08_k, n_k1, rwy0826);
    TWY_EDGE(eK_2, "K", n_k1, n_k_s);
    TWY_EDGE(eK_3, "K", n_k_s, n_k_w4);
    TWY_EDGE(eK_4, "K", n_k_w4, n_z_k_m);
    TWY_EDGE(eK_5, "K", n_z_k_m, n_k2_k);
    TWY_EDGE(eK_6, "K", n_k2_k, n_r_k_m2);
    TWY_EDGE(eK_7, "K", n_r_k_m2, n_m1_k);
    TWY_EDGE(eK_8, "K", n_m1_k, n_k1_k);
    TWY_EDGE(eK_9, "K", n_k1_k, n_e_k_y);
    TWY_AZONE(eK_10, "K", n_e_k_y, n_03_k, rwy0321);
    TWY_AZONE(eK_11, "K", n_03_k, n_f_k, rwy0321);
    TWY_EDGE(eK_12, "K", n_f_k, n_m_k_l);
    TWY_EDGE(eK_13, "K", n_m_k_l, n_k_l3);

    TWY_AZONE2(eW4_1, "W4", n_08_w4, n_12_w4, rwy0826, rwy1230);
    TWY_AZONE(eW4_1, "W4", n_12_w4, n_k_w4, rwy1230);

    TWY_EDGE(eM_1, "M", n_z_k_m, n_m1);
    TWY_EDGE(eM_2, "M", n_m1, n_k2_m);
    TWY_EDGE(eM_3, "M", n_k2_m, n_r_m2_m);
    TWY_EDGE(eM_4, "M", n_r_m2_m, n_m1_m);
    TWY_EDGE(eM_5, "M", n_m1_m, n_k1_m);
    TWY_EDGE(eM_6, "M", n_k1_m, n_e_m);
    TWY_AZONE(eM_7,"M", n_e_m, n_03_m, rwy0321);
    TWY_AZONE(eM_7,"M", n_03_m, n_m2, rwy0321);

    auto net = shared_ptr<TaxiNet>(new TaxiNet(
        { n1, n2, n3, n4, n5, n6, n7, n8 }, 
        { e12, e14, e13, e24, e23, e35, e56, e48, e67, e68 }
    ));

    return net;
}
*/
//shared_ptr<TaxiNet> createLargeExampleNet();

TEST(TaxiPathTest, findPath_trivialSingleEdgeNet) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e1 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E1", 111, 222));
    auto airport = WorldBuilder::assembleAirport(testHeader, {}, {}, { n1, n2 }, { e1 });
    auto net = airport->taxiNet();

    auto path = TaxiPath::find(net, n1, n2);

    EXPECT_EQ(path->fromNode, n1);
    EXPECT_EQ(path->toNode, n2);
    EXPECT_EQ(path->edges.size(), 1);
    EXPECT_EQ(path->edges[0]->name(), "E1");
    EXPECT_EQ(path->edges[0]->node1()->id(), 111);
    EXPECT_EQ(path->edges[0]->node2()->id(), 222);
}

TEST(TaxiPathTest, findPath_trivialTriangleNet) 
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

    auto path12 = TaxiPath::find(net, n1, n2);
    auto path13 = TaxiPath::find(net, n1, n3);
    auto path23 = TaxiPath::find(net, n2, n3);
    
    assertTaxiPath("n1->n2", {e12}, path12);
    assertTaxiPath("n1->n3", {e13}, path13);
    assertTaxiPath("n2->n3", {e23}, path23);
}

TEST(TaxiPathTest, findPath_mediumSizeNet) 
{
    auto net = createMediumTestNet();

    cout << "========================== n1 -> n7 ========================" << endl;
    auto path17 = TaxiPath::find(net, net->getNodeById(111), net->getNodeById(777));

    cout << "========================== n7 -> n1 ========================" << endl;
    auto path71 = TaxiPath::find(net, net->getNodeById(777), net->getNodeById(111));

    cout << "========================== n6 -> n1 ========================" << endl;
    auto path61 = TaxiPath::find(net, net->getNodeById(666), net->getNodeById(111));
    
    assertTaxiPath(net, "n1->n7", path17, {111,444,888,666,777});
    assertTaxiPath(net, "n7->n1", path71, {777,666,888,444,111});
    assertTaxiPath(net, "n6->n1", path61, {666,888,444,111});
}

TEST(TaxiPathTest, findPath_neverUseGroundways) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {10, GROUND, 20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222, TaxiEdge::Type::Taxiway));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "E23", 222, 333, TaxiEdge::Type::Taxiway));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "E13", 111, 333, TaxiEdge::Type::Groundway));
    
    auto airport = WorldBuilder::assembleAirport(testHeader, {}, {}, { n1, n2, n3 }, { e12, e23, e13 });
    auto net = airport->taxiNet();

    auto path13 = TaxiPath::find(net, n1, n3);
    
    assertTaxiPath("n1->n3", {e12, e23}, path13);
}

TEST(TaxiPathTest, findPath_neverUseRunways) 
{
    auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromLocal(host, {10, GROUND, 10})));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromLocal(host, {10, GROUND, 20})));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromLocal(host, {20, GROUND, 20})));
    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1001, "E12", 111, 222, TaxiEdge::Type::Taxiway));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(1002, "E23", 222, 333, TaxiEdge::Type::Taxiway));
    auto e13 = shared_ptr<TaxiEdge>(new TaxiEdge(1003, "13/31", 111, 333, TaxiEdge::Type::Runway));
    auto rwy13 = shared_ptr<Runway>(new Runway(
        Runway::End("13", 0, 0, n1->location()), 
        Runway::End("31", 0, 0, n3->location()), 30));

    auto airport = WorldBuilder::assembleAirport(testHeader, { rwy13 }, {}, { n1, n2, n3 }, { e12, e23, e13 });
    auto net = airport->taxiNet();

    auto path13 = TaxiPath::find(net, n1, n3);
    
    assertTaxiPath("n1->n3", {e12, e23}, path13);
}

void assertTaxiPath(
    const shared_ptr<TaxiNet> net, 
    const string& testCase, 
    const shared_ptr<TaxiPath> actualPath,
    const vector<int>& expectedNodeIds)
{
    vector<shared_ptr<TaxiEdge>> expectedEdges;

    for (int i = 0 ; i < expectedNodeIds.size() - 1 ; i++)
    {
        int thisNodeId = expectedNodeIds[i];
        int nextNodeId = expectedNodeIds[i+1];
        auto thisNode = net->getNodeById(thisNodeId);
        auto& edges = thisNode->edges();
        auto foundEdge = find_if(edges.begin(), edges.end(), [=](const shared_ptr<TaxiEdge> e) {
            return (e->node2()->id() == nextNodeId);
        });
        if (foundEdge == edges.end())
        {
            cout << "ASSERT> could not find edge from node " << thisNodeId << " to " << nextNodeId << endl;
            EXPECT_FALSE(foundEdge == edges.end());
        }
        expectedEdges.push_back(*foundEdge);
    }

    assertTaxiPath(testCase, expectedEdges, actualPath);
}

void assertTaxiPath(const string& testCase, const vector<shared_ptr<TaxiEdge>>& expected, const shared_ptr<TaxiPath> actual)
{
    bool hasErrors = false;
    int expectedIndex;

    if (expected.size() != actual->edges.size())
    {
        cout << "ASSERT[" << testCase << "]> path size differs, expected: " << expected.size() 
             << ", actual: " << actual->edges.size() << endl;
        hasErrors = true;
    }

    for (expectedIndex = 0 ; expectedIndex < expected.size() ; expectedIndex++)
    {
        const shared_ptr<TaxiEdge> expectedEdge = expected[expectedIndex];
        if (expectedIndex >= actual->edges.size())
        {
            cout << "ASSERT[" << testCase << "]> missing edge at [" << expectedIndex << "], expected: " << expectedEdge->name() << endl;
            hasErrors = true;
            continue;
        }
        const shared_ptr<TaxiEdge> actualEdge = actual->edges[expectedIndex];
        if (!actualEdge)
        {
            cout << "ASSERT[" << testCase << "]> missing edge [" << expectedIndex << "], expected: " << expectedEdge->name() << endl;
            hasErrors = true;
            continue;
        }
        if (expectedEdge->name().compare(actualEdge->name()) != 0)
        {
            cout << "ASSERT[" << testCase << "]> edge mismatch at [" << expectedIndex 
                 << ", expected[" << expectedEdge->name() 
                 << "] actual [" << actualEdge-> name() << "]" << endl;
            hasErrors = true;
            continue;
        }
        if (expectedEdge->node1() != actualEdge->node1())
        {
            hasErrors = true;
            cout << "ASSERT[" << testCase << "]> edge node1 mismatch at [" << expectedIndex 
                 << ", expected[" << expectedEdge->node1()->id() 
                 << "] actual [" << actualEdge->node1()->id() << "]" << endl;
        }
        if (expectedEdge->node2() != actualEdge->node2())
        {
            hasErrors = true;
            cout << "ASSERT[" << testCase << "]> edge node2 mismatch at [" << expectedIndex 
                 << ", expected[" << expectedEdge->node2()->id() 
                 << "] actual [" << actualEdge->node2()->id() << "]" << endl;
        }
    }   

    for (int excessIndex = expectedIndex ; excessIndex < actual->edges.size() ; excessIndex++)
    {
        hasErrors = true;
        cout << "ASSERT[" << testCase << "]> unexpected edge at [" << excessIndex << "]: " 
             << actual->edges[excessIndex]->name() << endl;
    }

    EXPECT_EQ(hasErrors, false);
}
