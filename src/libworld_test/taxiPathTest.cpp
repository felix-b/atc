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

shared_ptr<TaxiEdge> findEdgeByName(shared_ptr<TaxiNet> net, const string& name);
shared_ptr<TaxiEdge> findEdgeById(shared_ptr<TaxiNet> net, int id);
void assertTaxiPath(const string& testCase, const vector<shared_ptr<TaxiEdge>>& expected, const shared_ptr<TaxiPath> actual);
void assertTaxiPath(
    const shared_ptr<TaxiNet> net, 
    const string& testCase, 
    const shared_ptr<TaxiPath> actualPath,
    const vector<int>& expectedNodeIds);
void assertTaxiPathEdgeNames(const string& testCase, const vector<string>& expected, const shared_ptr<TaxiPath> actual);

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
        host,
        testHeader,
        {},
        {},
        { n1, n2, n3, n4, n5, n6, n7, n8 },
        { e12, e14, e13, e24, e23, e35, e56, e48, e67, e68 }
    );

    return airport->taxiNet();
}

shared_ptr<Airport> createTaxiAllocationTestAirport(shared_ptr<TestHostServices> host)
{
    /*
     *          10      20  25  30      40      50      60      70      80
     *                                                                  G1
     *     30                            X       X       X       X       X
     *                                   |       |       |       |       |
     *     25                            |K      |L      |M      |N      |O
     *                                   |       |       |       |       |
     *     20    X-----1-----X-----2-----X---3---X---4---X---5---X---6---X B
     *           |BB1      /BB2        /BB3      |BB4    |BB5    |BB6    |BB7
     *     15    X---1---X-----2-----X------3----X---4---X---5---X---6---X A
     *           |AA1              /AA2                  |AA3
     *     10    X===============X=======================X 09/27
     *
     *          10      20      30      40      50      60      70      80
     *
     */

    //region Taxi Nodes
    auto n_10_10 = shared_ptr<TaxiNode>(new TaxiNode(1010, UniPoint::fromGeo(host, {30.10, 45.10})));
    auto n_10_30 = shared_ptr<TaxiNode>(new TaxiNode(1030, UniPoint::fromGeo(host, {30.10, 45.30})));
    auto n_10_60 = shared_ptr<TaxiNode>(new TaxiNode(1060, UniPoint::fromGeo(host, {30.10, 45.60})));

    auto n_15_10 = shared_ptr<TaxiNode>(new TaxiNode(1510, UniPoint::fromGeo(host, {30.15, 45.10})));
    auto n_15_20 = shared_ptr<TaxiNode>(new TaxiNode(1520, UniPoint::fromGeo(host, {30.15, 45.20})));
    auto n_15_35 = shared_ptr<TaxiNode>(new TaxiNode(1535, UniPoint::fromGeo(host, {30.15, 45.35})));
    auto n_15_50 = shared_ptr<TaxiNode>(new TaxiNode(1550, UniPoint::fromGeo(host, {30.15, 45.50})));
    auto n_15_60 = shared_ptr<TaxiNode>(new TaxiNode(1560, UniPoint::fromGeo(host, {30.15, 45.60})));
    auto n_15_70 = shared_ptr<TaxiNode>(new TaxiNode(1570, UniPoint::fromGeo(host, {30.15, 45.70})));
    auto n_15_80 = shared_ptr<TaxiNode>(new TaxiNode(1580, UniPoint::fromGeo(host, {30.15, 45.80})));

    auto n_20_10 = shared_ptr<TaxiNode>(new TaxiNode(2010, UniPoint::fromGeo(host, {30.20, 45.10})));
    auto n_20_25 = shared_ptr<TaxiNode>(new TaxiNode(2025, UniPoint::fromGeo(host, {30.20, 45.25})));
    auto n_20_40 = shared_ptr<TaxiNode>(new TaxiNode(2040, UniPoint::fromGeo(host, {30.20, 45.40})));
    auto n_20_50 = shared_ptr<TaxiNode>(new TaxiNode(2050, UniPoint::fromGeo(host, {30.20, 45.50})));
    auto n_20_60 = shared_ptr<TaxiNode>(new TaxiNode(2060, UniPoint::fromGeo(host, {30.20, 45.60})));
    auto n_20_70 = shared_ptr<TaxiNode>(new TaxiNode(2070, UniPoint::fromGeo(host, {30.20, 45.70})));
    auto n_20_80 = shared_ptr<TaxiNode>(new TaxiNode(2080, UniPoint::fromGeo(host, {30.20, 45.80})));

    auto n_30_40 = shared_ptr<TaxiNode>(new TaxiNode(3040, UniPoint::fromGeo(host, {30.30, 45.40})));
    auto n_30_50 = shared_ptr<TaxiNode>(new TaxiNode(3050, UniPoint::fromGeo(host, {30.30, 45.50})));
    auto n_30_60 = shared_ptr<TaxiNode>(new TaxiNode(3060, UniPoint::fromGeo(host, {30.30, 45.60})));
    auto n_30_70 = shared_ptr<TaxiNode>(new TaxiNode(3070, UniPoint::fromGeo(host, {30.30, 45.70})));
    auto n_30_80 = shared_ptr<TaxiNode>(new TaxiNode(3080, UniPoint::fromGeo(host, {30.30, 45.80})));
    //endregion

    //region Taxi Edges
    auto e_1010_1030 = shared_ptr<TaxiEdge>(new TaxiEdge(10101030, "09/27", 1010, 1030, TaxiEdge::Type::Runway));
    auto e_1030_1060 = shared_ptr<TaxiEdge>(new TaxiEdge(10301060, "09/27", 1030, 1060, TaxiEdge::Type::Runway));

    auto e_AA1 = shared_ptr<TaxiEdge>(new TaxiEdge(10101510, "AA1", 1010, 1510));
    auto e_AA2 = shared_ptr<TaxiEdge>(new TaxiEdge(10301535, "AA2", 1030, 1535));
    auto e_AA3 = shared_ptr<TaxiEdge>(new TaxiEdge(10601560, "AA3", 1060, 1560));

    auto e_BB1 = shared_ptr<TaxiEdge>(new TaxiEdge(15102010, "BB1", 1510, 2010));
    auto e_BB2 = shared_ptr<TaxiEdge>(new TaxiEdge(15202025, "BB2", 1520, 2025));
    auto e_BB3 = shared_ptr<TaxiEdge>(new TaxiEdge(15352040, "BB3", 1535, 2040));
    auto e_BB4 = shared_ptr<TaxiEdge>(new TaxiEdge(15502050, "BB4", 1550, 2050));
    auto e_BB5 = shared_ptr<TaxiEdge>(new TaxiEdge(15602060, "BB5", 1560, 2060));
    auto e_BB6 = shared_ptr<TaxiEdge>(new TaxiEdge(15702070, "BB6", 1570, 2070));
    auto e_BB7 = shared_ptr<TaxiEdge>(new TaxiEdge(15802080, "BB7", 1580, 2080));

    auto e_A1 = shared_ptr<TaxiEdge>(new TaxiEdge(15101520, "A", 1510, 1520));
    auto e_A2 = shared_ptr<TaxiEdge>(new TaxiEdge(15201535, "A", 1520, 1535));
    auto e_A3 = shared_ptr<TaxiEdge>(new TaxiEdge(15351550, "A", 1535, 1550));
    auto e_A4 = shared_ptr<TaxiEdge>(new TaxiEdge(15501560, "A", 1550, 1560));
    auto e_A5 = shared_ptr<TaxiEdge>(new TaxiEdge(15601570, "A", 1560, 1570));
    auto e_A6 = shared_ptr<TaxiEdge>(new TaxiEdge(15701580, "A", 1570, 1580));

    auto e_B1 = shared_ptr<TaxiEdge>(new TaxiEdge(20102025, "B", 2010, 2025));
    auto e_B2 = shared_ptr<TaxiEdge>(new TaxiEdge(20252040, "B", 2025, 2040));
    auto e_B3 = shared_ptr<TaxiEdge>(new TaxiEdge(20402050, "B", 2040, 2050));
    auto e_B4 = shared_ptr<TaxiEdge>(new TaxiEdge(20502060, "B", 2050, 2060));
    auto e_B5 = shared_ptr<TaxiEdge>(new TaxiEdge(20602070, "B", 2060, 2070));
    auto e_B6 = shared_ptr<TaxiEdge>(new TaxiEdge(20702080, "B", 2070, 2080));

    auto e_K = shared_ptr<TaxiEdge>(new TaxiEdge(20403040, "K", 2040, 3040));
    auto e_L = shared_ptr<TaxiEdge>(new TaxiEdge(20503050, "L", 2050, 3050));
    auto e_M = shared_ptr<TaxiEdge>(new TaxiEdge(20603060, "M", 2060, 3060));
    auto e_N = shared_ptr<TaxiEdge>(new TaxiEdge(20703070, "N", 2070, 3070));
    auto e_O = shared_ptr<TaxiEdge>(new TaxiEdge(20803080, "O", 2080, 3080));
    //endregion

    auto g_1 = make_shared<ParkingStand>(
        1001, "G1", ParkingStand::Type::Gate, UniPoint::fromGeo(host, {30.35, 45.80}), 0, "1");

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
        { g_1 },
        {
            n_10_10, n_10_30, n_10_60, n_15_10, n_15_20, n_15_35, n_15_50, n_15_60, n_15_70, n_15_80, n_20_10, n_20_25,
            n_20_40, n_20_50, n_20_60, n_20_70, n_20_80, n_30_40, n_30_50, n_30_60, n_30_70, n_30_80
        },
        {
            e_1010_1030, e_1030_1060, e_AA1, e_AA2, e_AA3, e_BB1, e_BB2, e_BB3, e_BB4, e_BB5, e_BB6, e_BB7,
            e_A1, e_A2, e_A3, e_A4, e_A5, e_A6,
            e_B1, e_B2, e_B3, e_B4, e_B5, e_B6, e_K, e_L, e_M, e_N, e_O,
        }
    );

    return airport;
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
    auto airport = WorldBuilder::assembleAirport(host, testHeader, {}, {}, { n1, n2 }, { e1 });
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
    
    auto airport = WorldBuilder::assembleAirport(host, testHeader, {}, {}, { n1, n2, n3 }, { e12, e23, e13 });
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
    
    auto airport = WorldBuilder::assembleAirport(host, testHeader, {}, {}, { n1, n2, n3 }, { e12, e23, e13 });
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

    auto airport = WorldBuilder::assembleAirport(host, testHeader, { rwy13 }, {}, { n1, n2, n3 }, { e12, e23, e13 });
    auto net = airport->taxiNet();

    auto path13 = TaxiPath::find(net, n1, n3);
    
    assertTaxiPath("n1->n3", {e12, e23}, path13);
}

TEST(TaxiPathTest, findPath_assignFlightPhaseAllocation)
{
    auto host = TestHostServices::create();
    auto airport = createTaxiAllocationTestAirport(host);
    auto net = airport->taxiNet();

    auto n_10_30 = net->getNodeById(1030);
    auto n_30_80 = net->getNodeById(3080);

    auto path1 = TaxiPath::find(net, n_10_30, n_30_80);
    assertTaxiPathEdgeNames("1030->3080", {"AA2", "BB3", "B", "B", "B", "B", "O"}, path1);

    net->assignFlightPhaseAllocation(path1, Flight::Phase::Arrival);

    EXPECT_EQ(findEdgeByName(net, "AA2")->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeByName(net, "BB3")->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeByName(net, "O")->flightPhaseAllocation(), Flight::Phase::Arrival);

    EXPECT_EQ(findEdgeByName(net, "AA1")->flightPhaseAllocation(), Flight::Phase::NotAssigned);
    EXPECT_EQ(findEdgeByName(net, "BB2")->flightPhaseAllocation(), Flight::Phase::NotAssigned);
    EXPECT_EQ(findEdgeByName(net, "L")->flightPhaseAllocation(), Flight::Phase::NotAssigned);
}

TEST(TaxiPathTest, findPath_assignFlightPhaseAllocation_keepExistingAllocations)
{
    auto host = TestHostServices::create();
    auto airport = createTaxiAllocationTestAirport(host);
    auto net = airport->taxiNet();

    auto path1 = TaxiPath::find(net, net->getNodeById(1030), net->getNodeById(3080));
    auto path2 = TaxiPath::find(net, net->getNodeById(3060), net->getNodeById(2010));

    assertTaxiPathEdgeNames("1030->3080", {"AA2", "BB3", "B", "B", "B", "B", "O"}, path1);
    assertTaxiPathEdgeNames("3060->2010", {"M", "B", "B", "B", "B"}, path2);

    net->assignFlightPhaseAllocation(path1, Flight::Phase::Arrival);

    EXPECT_EQ(findEdgeById(net, 20102025)->flightPhaseAllocation(), Flight::Phase::NotAssigned);
    EXPECT_EQ(findEdgeById(net, 20252040)->flightPhaseAllocation(), Flight::Phase::NotAssigned);
    EXPECT_EQ(findEdgeById(net, 20402050)->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeById(net, 20502060)->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeById(net, 20602070)->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeById(net, 20702080)->flightPhaseAllocation(), Flight::Phase::Arrival);

    net->assignFlightPhaseAllocation(path2, Flight::Phase::Departure);

    EXPECT_EQ(findEdgeByName(net, "M")->flightPhaseAllocation(), Flight::Phase::Departure);
    EXPECT_EQ(findEdgeById(net, 20102025)->flightPhaseAllocation(), Flight::Phase::Departure);
    EXPECT_EQ(findEdgeById(net, 20252040)->flightPhaseAllocation(), Flight::Phase::Departure);
    EXPECT_EQ(findEdgeById(net, 20402050)->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeById(net, 20502060)->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeById(net, 20602070)->flightPhaseAllocation(), Flight::Phase::Arrival);
    EXPECT_EQ(findEdgeById(net, 20702080)->flightPhaseAllocation(), Flight::Phase::Arrival);
}

TEST(TaxiPathTest, findPath_useCostFunction)
{
    auto host = TestHostServices::create();
    auto airport = createTaxiAllocationTestAirport(host);
    const auto& runway09 = airport->getRunwayOrThrow("09")->getEndOrThrow("09");
    auto net = airport->taxiNet();
    auto arrivalPath = TaxiPath::find(net, net->getNodeById(1030), net->getNodeById(3080));
    net->assignFlightPhaseAllocation(arrivalPath, Flight::Phase::Arrival);

    const TaxiPath::CostFunction costFunc = [](shared_ptr<TaxiEdge> edge) {
        return (edge->flightPhaseAllocation() == Flight::Phase::Arrival
            ? edge->lengthMeters() * 2.0f
            : edge->lengthMeters());
    };

    auto departurePath1 = TaxiPath::find(net, net->getNodeById(3080), net->getNodeById(1010), costFunc);
    auto departurePath2 = TaxiPath::find(net, net->getNodeById(3050), net->getNodeById(1010), costFunc);

    assertTaxiPathEdgeNames("A:1030->3080", {"AA2", "BB3", "B", "B", "B", "B", "O"}, arrivalPath);
    assertTaxiPathEdgeNames("D1:3080->1010", {"O", "BB7", "A", "A", "A", "A", "A", "A", "AA1"}, departurePath1);
    assertTaxiPathEdgeNames("D2:3050->1010", {"L", "BB4", "A", "A", "A", "AA1"}, departurePath2);
}

//TODO: extract into TaxiNetTest
TEST(TaxiPathTest, taxiNetFindPaths_avoidArrivalDepartureConflict)
{
    auto host = TestHostServices::create();
    auto airport = createTaxiAllocationTestAirport(host);
    const auto& runway09 = airport->getRunwayOrThrow("09")->getEndOrThrow("09");
    auto net = airport->taxiNet();
    auto arrivalPath = net->tryFindTaxiPathToGate(airport->getParkingStandOrThrow("G1"), net->getNodeById(1030)->location().geo());

    auto departurePath1 = net->tryFindDepartureTaxiPathToRunway(net->getNodeById(3080)->location().geo(), runway09);
    auto departurePath2 = net->tryFindDepartureTaxiPathToRunway(net->getNodeById(3050)->location().geo(), runway09);
    TaxiPath::find(net, net->getNodeById(3050), net->getNodeById(1010));

    assertTaxiPathEdgeNames("A:1030->3080", {"AA2", "BB3", "B", "B", "B", "B", "O", "", ""}, arrivalPath);
    assertTaxiPathEdgeNames("D1:3080->1010", {"O", "BB7", "A", "A", "A", "A", "A", "A", "AA1"}, departurePath1);
    assertTaxiPathEdgeNames("D2:3050->1010", {"L", "BB4", "A", "A", "A", "AA1"}, departurePath2);
}

shared_ptr<TaxiEdge> findEdgeByName(shared_ptr<TaxiNet> net, const string& name)
{
    auto it = find_if(net->edges().begin(), net->edges().end(), [&](shared_ptr<TaxiEdge> edge) {
        return edge->name() == name;
    });
    if (it != net->edges().end())
    {
        return *it;
    }
    throw runtime_error("findEdgeByName: [" + name + "] not found");
}

shared_ptr<TaxiEdge> findEdgeById(shared_ptr<TaxiNet> net, int id)
{
    auto it = find_if(net->edges().begin(), net->edges().end(), [&](shared_ptr<TaxiEdge> edge) {
        return edge->id() == id;
    });
    if (it != net->edges().end())
    {
        return *it;
    }
    throw runtime_error("findEdgeById: [" + to_string(id) + "] not found");
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

void assertTaxiPathEdgeNames(const string& testCase, const vector<string>& expected, const shared_ptr<TaxiPath> actual)
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
        string expectedEdge = expected[expectedIndex];
        if (expectedIndex >= actual->edges.size())
        {
            cout << "ASSERT[" << testCase << "]> missing edge at [" << expectedIndex << "], expected: " << expectedEdge << endl;
            hasErrors = true;
            continue;
        }
        const shared_ptr<TaxiEdge> actualEdge = actual->edges[expectedIndex];
        if (!actualEdge)
        {
            cout << "ASSERT[" << testCase << "]> missing edge [" << expectedIndex << "], expected: " << expectedEdge << endl;
            hasErrors = true;
            continue;
        }
        if (expectedEdge.compare(actualEdge->name()) != 0)
        {
            cout << "ASSERT[" << testCase << "]> edge mismatch at [" << expectedIndex
                 << ", expected[" << expectedEdge
                 << "] actual [" << actualEdge-> name() << "]" << endl;
            hasErrors = true;
            continue;
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
