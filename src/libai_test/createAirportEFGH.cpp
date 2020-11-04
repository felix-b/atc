//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//

#include <chrono>
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "clearanceTypes.hpp"
#include "simpleRunwayMutex.hpp"
#include "libworld_test.h"

using namespace std;
using namespace world;
using namespace ai;

shared_ptr<Airport> createAirportEFGH(shared_ptr<TestHostServices> host)
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
     *           :               :                       :
     *     05    +...............+.......................+  //TODO: add this taxiway
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

    Airport::Header header("EFGH", "Test Airport EFGH", GeoPoint(30, 40), 123);
    auto airport = WorldBuilder::assembleAirport(
        host,
        header,
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
