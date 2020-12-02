// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <fstream>
#include <sstream>
#include <vector>
#include <unordered_set>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libdataxp.h"
#include "libworld_test.h"
#include "libdataxp_test.h"

//#include <direct.h>
//#define GetCurrentDir _getcwd

using namespace world;
using namespace std;

// std::string GetCurrentWorkingDir( void ) {
//   char buff[FILENAME_MAX];
//   GetCurrentDir( buff, FILENAME_MAX );
//   std::string current_working_dir(buff);
//   return current_working_dir;
// }
 
// void writeAirportJson(shared_ptr<const Airport> airport, ostream& output);
// void writeTaxiPathJson(shared_ptr<const TaxiPath> taxiPath, ostream& output);

TEST(XPAirportReaderTest, readAptDat_empty) {
    XPAirportReader builder(makeHost());
    const auto airport = builder.getAirport();
    const auto taxiNet = airport->taxiNet();

    EXPECT_EQ(taxiNet->nodes().size(), 0);
    EXPECT_EQ(taxiNet->edges().size(), 0);
    EXPECT_EQ(airport->runways().size(), 0);
}

TEST(XPAirportReaderTest, readAptDat_header) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({"1 123 0 0 KBFI Boeing Field King Co Intl"});

    builder.readAirport(aptDat);

    const auto airport = builder.getAirport();

    EXPECT_EQ(airport->header().icao(), "KBFI");
    EXPECT_EQ(airport->header().name(), "Boeing Field King Co Intl");
    EXPECT_FLOAT_EQ(airport->header().elevation(), 123);
}

TEST(XPAirportReaderTest, readAptDat_metadata) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "1 123 0 0 KBFI Boeing Field King Co Intl",
        "1302 datum_lat 47.44",
        "1302 datum_lon -122.31",
        "1302 icao_code KSEA",
    });

    builder.readAirport(aptDat);

    const auto airport = builder.getAirport();

    EXPECT_EQ(airport->header().icao(), "KSEA");
    EXPECT_EQ(airport->header().name(), "Boeing Field King Co Intl");
    EXPECT_FLOAT_EQ(airport->header().elevation(), 123);
    EXPECT_FLOAT_EQ(airport->header().datum().latitude, 47.44);
    EXPECT_FLOAT_EQ(airport->header().datum().longitude, -122.31);
}

TEST(XPAirportReaderTest, readAptDat_singleTaxiNode) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({"1201  32.1234  034.5678 both 231 K3_stop"});

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();

    const auto taxiNet = airport->taxiNet();
    ASSERT_EQ(taxiNet->nodes().size(), 1);
    EXPECT_EQ(taxiNet->nodes()[0]->id(), 231);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[0]->location().x(), 3456.78);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[0]->location().z(), 3212.34);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[0]->location().y(), 0);
    EXPECT_EQ(taxiNet->nodes()[0]->isJunction(), false);
}

TEST(XPAirportReaderTest, readAptDat_singleTaxiEdge) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "1201  32.1234  034.5678 init 231 K3 stop 1",
        "1201  33.2345  035.6789 dest 233 K3_stop_2",
        "1202  231  233  twoway  taxiway  K3 E1"
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();

    const auto taxiNet = airport->taxiNet();
    ASSERT_EQ(taxiNet->nodes().size(), 2);
    
    EXPECT_EQ(taxiNet->nodes()[0]->id(), 231);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[0]->location().x(), 3456.78);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[0]->location().z(), 3212.34);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[0]->location().y(), 0);
    
    EXPECT_EQ(taxiNet->nodes()[1]->id(), 233);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[1]->location().x(), 3567.89);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[1]->location().z(), 3323.45);
    EXPECT_FLOAT_EQ(taxiNet->nodes()[1]->location().y(), 0);

    ASSERT_EQ(taxiNet->edges().size(), 1);
    EXPECT_EQ(taxiNet->edges()[0]->id(), 1001);
    EXPECT_EQ(taxiNet->edges()[0]->type(), TaxiEdge::Type::Taxiway);
    EXPECT_EQ(taxiNet->edges()[0]->isOneWay(), false);    
    EXPECT_EQ(taxiNet->edges()[0]->name(), "K3 E1");
    EXPECT_EQ(taxiNet->edges()[0]->node1(), taxiNet->nodes()[0]);
    EXPECT_EQ(taxiNet->edges()[0]->node2(), taxiNet->nodes()[1]);

    //writeAirportJson(airport, cout);
}

TEST(XPAirportReaderTest, readAptDat_singleTaxiAndGroundEdge) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "1201  32.1234  034.5678 init 231 K3 stop 1",
        "1201  33.2345  035.6789 dest 233 K3_stop_2",
        "1201  33.2345  035.7890 dest 234 K3_svc_1",
        "1202  231  233  twoway  taxiway  K3 E1",
        "1206  233  234  twoway  GR12"
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();

    const auto taxiNet = airport->taxiNet();
    ASSERT_EQ(taxiNet->nodes().size(), 3);
    EXPECT_EQ(taxiNet->nodes()[0]->id(), 231);
    EXPECT_EQ(taxiNet->nodes()[1]->id(), 233);
    EXPECT_EQ(taxiNet->nodes()[2]->id(), 234);

    ASSERT_EQ(taxiNet->edges().size(), 2);
    EXPECT_EQ(taxiNet->edges()[1]->id(), 1002);
    EXPECT_EQ(taxiNet->edges()[1]->type(), TaxiEdge::Type::Groundway);
    EXPECT_EQ(taxiNet->edges()[1]->isOneWay(), false);
    EXPECT_EQ(taxiNet->edges()[1]->name(), "GR12");
    EXPECT_EQ(taxiNet->edges()[1]->node1()->id(), 233);
    EXPECT_EQ(taxiNet->edges()[1]->node2()->id(), 234);

    //writeAirportJson(airport, cout);
}

TEST(XPAirportReaderTest, readAptDat_runways) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "100 46.02 1 0 0.00 1 3 0 13L  40.1234 -073.4567 277  0 3 2 1 0 31R  40.6437 -073.7592  314  0 3 8 1 0",
        "100 60.00 2 0 0.00 1 3 0 04L  40.2345 -073.5678 140  0 3 0 0 1 22R  40.6505 -073.7633 1044  0 3 0 0 0",
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();

    const auto& runways = airport->runways();

    ASSERT_EQ(runways.size(), 2);

    const auto rwy1 = runways[0];
    EXPECT_EQ(rwy1->end1().name(), "13L");
    EXPECT_EQ(rwy1->end2().name(), "31R");
    EXPECT_EQ(
        rwy1->end1().heading(), 
        GeoMath::getHeadingFromPoints({40.1234, -073.4567}, {40.6437, -073.7592}));
    EXPECT_EQ(
        rwy1->end2().heading(), 
        GeoMath::getHeadingFromPoints({40.6437, -073.7592}, {40.1234, -073.4567}));
    EXPECT_FLOAT_EQ(rwy1->widthMeters(), 46.02);

    const auto rwy2 = runways[1];
    EXPECT_EQ(rwy2->end1().name(), "04L");
    EXPECT_EQ(rwy2->end2().name(), "22R");
    EXPECT_FLOAT_EQ(rwy2->widthMeters(), 60.00);
}

TEST(XPAirportReaderTest, readAptDat_assignRunwaysHeaderElevation) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "1 1234 0 0 ABCD Test",
        "100 46.02 1 0 0.00 1 3 0 13L  40.1234 -073.4567 277  0 3 2 1 0 31R  40.6437 -073.7592  314  0 3 8 1 0",
        "100 60.00 2 0 0.00 1 3 0 04L  40.2345 -073.5678 140  0 3 0 0 1 22R  40.6505 -073.7633 1044  0 3 0 0 0",
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();
    const auto& runways = airport->runways();

    ASSERT_EQ(runways.size(), 2);
    EXPECT_FLOAT_EQ(runways[0]->end1().elevationFeet(), 1234);
    EXPECT_FLOAT_EQ(runways[0]->end2().elevationFeet(), 1234);
    EXPECT_FLOAT_EQ(runways[1]->end1().elevationFeet(), 1234);
    EXPECT_FLOAT_EQ(runways[1]->end2().elevationFeet(), 1234);
}

TEST(XPAirportReaderTest, readAptDat_runwayEdges) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "100 45.00 1 0 0.00 1 3 0 06  40.100 050.200 277  0 3 2 1 0 22  40.130 050.280  314  0 3 8 1 0",
        "100 60.00 1 0 0.00 1 3 0 14  40.160 050.210 277  0 3 2 1 0 32  40.100 050.270  314  0 3 8 1 0",
        "",
        "# nodes",
        "",
        "1201  40.100 50.200 both 11 n1",
        "1201  40.120 50.250 both 22 n2",
        "1201  40.130 50.280 both 33 n3",
        "1201  40.140 50.280 both 44 n4",
        "1201  40.130 50.250 both 55 n5",
        "1201  40.130 50.240 both 66 n6",
        "1201  40.160 50.210 both 77 n7",
        "1201  40.150 50.200 both 88 n8",
        "1201  40.120 50.230 both 99 n9",
        "1201  40.100 50.270 both 110 n10",
        "",
        "# edges - runways",
        "",
        "1202 11 22 twoway runway 06/22",
        "1204 departure 06,22",
        "1204 arrival 06,22",
        "1204 ils 06,22",
        "1202 22 33 twoway runway 06/22",
        "1204 departure 06,22",
        "1204 arrival 06,22",
        "1204 ils 06,22",
        "1202 77 66 twoway runway 14/32",
        "1204 departure 14,32",
        "1204 arrival 14,32",
        "1204 ils 14,32",
        "1202 66 22 twoway runway 14/32",
        "1204 departure 14,32",
        "1204 arrival 14,32",
        "1204 ils 14,32",
        "1202 22 110 twoway runway 14/32",
        "1204 departure 14,32",
        "1204 arrival 14,32",
        "1204 ils 14,32",
        "",
        "# edges - taxiways",
        "",
        "1202 77 88 twoway taxiway_F A1",
        "1204 departure 14,32",
        "1204 ils 14,32",
        "1202 88 99 twoway taxiway_F A",
        "1202 99 66 twoway taxiway_F A2",
        "1204 departure 14,32",
        "1204 arrival 14,32",
        "1202 33 44 twoway taxiway_E B1",
        "1204 departure 06,22",
        "1204 ils 06,22",
        "1202 44 55 twoway taxiway_E B",
        "1202 55 22 twoway taxiway_E B2",
        "1204 arrival 06,22,14,32",
        "1202 66 55 twoway taxiway_D C1",
        "1204 arrival 06,22",
        "1202 99 22 twoway taxiway_D C2",
        "1204 arrival 06,22,14,32",
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();
    const auto taxiNet = airport->taxiNet();
    
    const auto rwy0622 = airport->getRunwayOrThrow("06/22");
    const auto rwy1432 = airport->getRunwayOrThrow("14/32");
    auto n1 = taxiNet->getNodeById(11);
    auto n2 = taxiNet->getNodeById(22);
    auto n3 = taxiNet->getNodeById(33);
    auto n4 = taxiNet->getNodeById(44);
    auto n5 = taxiNet->getNodeById(55);
    auto n6 = taxiNet->getNodeById(66);
    auto n7 = taxiNet->getNodeById(77);
    auto n8 = taxiNet->getNodeById(88);
    auto n9 = taxiNet->getNodeById(99);
    auto n10 = taxiNet->getNodeById(110);

    auto e12_0622 = n1->getEdgeTo(n2);
    auto e23_0622 = n2->getEdgeTo(n3);
    auto e34_b1   = n3->getEdgeTo(n4);
    auto e45_b    = n4->getEdgeTo(n5);
    auto e52_b2   = n5->getEdgeTo(n2);
    auto e67_1432 = n6->getEdgeTo(n7);
    auto e62_1432 = n6->getEdgeTo(n2);
    auto e210_1432 = n2->getEdgeTo(n10);
    auto e78_a1   = n7->getEdgeTo(n8);
    auto e89_a    = n8->getEdgeTo(n9);
    auto e96_a2   = n9->getEdgeTo(n6);
    auto e56_c1   = n5->getEdgeTo(n6);
    auto e92_c2   = n9->getEdgeTo(n2);

    EXPECT_EQ(e12_0622->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e12_0622->runway(), rwy0622);
    EXPECT_EQ(e23_0622->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e23_0622->runway(), rwy0622);

    EXPECT_EQ(e67_1432->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e67_1432->runway(), rwy1432);
    EXPECT_EQ(e62_1432->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e62_1432->runway(), rwy1432);
    EXPECT_EQ(e210_1432->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e210_1432->runway(), rwy1432);

    EXPECT_EQ(e78_a1->type(), TaxiEdge::Type::Taxiway);
    EXPECT_TRUE(e78_a1->activeZones().departue.has(rwy1432));
    EXPECT_TRUE(e78_a1->activeZones().ils.has(rwy1432));
    EXPECT_FALSE(e78_a1->activeZones().arrival.has(rwy1432));
    EXPECT_FALSE(e78_a1->activeZones().departue.has(rwy0622));

    EXPECT_EQ(e92_c2->type(), TaxiEdge::Type::Taxiway);
    EXPECT_FALSE(e92_c2->activeZones().departue.hasAny());
    EXPECT_FALSE(e92_c2->activeZones().ils.hasAny());
    EXPECT_TRUE(e92_c2->activeZones().arrival.has(rwy0622));
    EXPECT_TRUE(e92_c2->activeZones().arrival.has(rwy1432));

    EXPECT_EQ(e45_b->type(), TaxiEdge::Type::Taxiway);
    EXPECT_FALSE(e45_b->activeZones().hasAny());

    //writeAirportJson(airport, cout);
}

TEST(XPAirportReaderTest, readAptDat_runwayEdges_runwayLeadingZerosInconsistent) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "100 45.00 1 0 0.00 1 3 0 06  40.100 050.200 277  0 3 2 1 0 22  40.130 050.280  314  0 3 8 1 0",
        "100 60.00 1 0 0.00 1 3 0 9  40.160 050.210 277  0 3 2 1 0 27  40.100 050.270  314  0 3 8 1 0",
        "",
        "# nodes",
        "",
        "1201  40.100 50.200 both 11 n1",
        "1201  40.120 50.250 both 22 n2",
        "1201  40.130 50.280 both 33 n3",
        "1201  40.140 50.280 both 44 n4",
        "1201  40.130 50.250 both 55 n5",
        "1201  40.130 50.240 both 66 n6",
        "1201  40.160 50.210 both 77 n7",
        "1201  40.150 50.200 both 88 n8",
        "1201  40.120 50.230 both 99 n9",
        "1201  40.100 50.270 both 110 n10",
        "",
        "# edges - runways",
        "",
        "1202 11 22 twoway runway 06/22",
        "1204 departure 06,22",
        "1204 arrival 6,22",
        "1204 ils 06,22",
        "1202 22 33 twoway runway 6/22",
        "1204 departure 6,22",
        "1204 arrival 6,22",
        "1204 ils 06,22",
        "1202 77 66 twoway runway 9/27",
        "1204 departure 09,27",
        "1204 arrival 9,27",
        "1204 ils 09,27",
        "1202 66 22 twoway runway 09/27",
        "1204 departure 09,27",
        "1204 arrival 9,27",
        "1204 ils 09,27",
        "1202 22 110 twoway runway 9/27",
        "1204 departure 9,27",
        "1204 arrival 09,27",
        "1204 ils 9,27",
        "",
        "# edges - taxiways",
        "",
        "1202 77 88 twoway taxiway_F A1",
        "1204 departure 9,27",
        "1204 ils 09,27",
        "1202 88 99 twoway taxiway_F A",
        "1202 99 66 twoway taxiway_F A2",
        "1204 departure 09,27",
        "1204 arrival 9,27",
        "1202 33 44 twoway taxiway_E B1",
        "1204 departure 6,22",
        "1204 ils 06,22",
        "1202 44 55 twoway taxiway_E B",
        "1202 55 22 twoway taxiway_E B2",
        "1204 arrival 6,22,9,27",
        "1202 66 55 twoway taxiway_D C1",
        "1204 arrival 06,22",
        "1202 99 22 twoway taxiway_D C2",
        "1204 arrival 06,22,09,27",
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();

    ASSERT_TRUE(!!airport);

    const auto taxiNet = airport->taxiNet();

    const auto rwy0622 = airport->getRunwayOrThrow("06/22");
    const auto rwy0927 = airport->getRunwayOrThrow("09/27");

    auto n1 = taxiNet->getNodeById(11);
    auto n2 = taxiNet->getNodeById(22);
    auto n3 = taxiNet->getNodeById(33);
    auto n4 = taxiNet->getNodeById(44);
    auto n5 = taxiNet->getNodeById(55);
    auto n6 = taxiNet->getNodeById(66);
    auto n7 = taxiNet->getNodeById(77);
    auto n8 = taxiNet->getNodeById(88);
    auto n9 = taxiNet->getNodeById(99);
    auto n10 = taxiNet->getNodeById(110);

    auto e12_0622 = n1->getEdgeTo(n2);
    auto e23_0622 = n2->getEdgeTo(n3);
    auto e34_b1   = n3->getEdgeTo(n4);
    auto e45_b    = n4->getEdgeTo(n5);
    auto e52_b2   = n5->getEdgeTo(n2);
    auto e67_0927 = n6->getEdgeTo(n7);
    auto e62_0927 = n6->getEdgeTo(n2);
    auto e210_0927 = n2->getEdgeTo(n10);
    auto e78_a1   = n7->getEdgeTo(n8);
    auto e89_a    = n8->getEdgeTo(n9);
    auto e96_a2   = n9->getEdgeTo(n6);
    auto e56_c1   = n5->getEdgeTo(n6);
    auto e92_c2   = n9->getEdgeTo(n2);

    EXPECT_EQ(e12_0622->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e12_0622->runway(), rwy0622);
    EXPECT_EQ(e23_0622->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e23_0622->runway(), rwy0622);

    EXPECT_EQ(e67_0927->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e67_0927->runway(), rwy0927);
    EXPECT_EQ(e62_0927->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e62_0927->runway(), rwy0927);
    EXPECT_EQ(e210_0927->type(), TaxiEdge::Type::Runway);
    EXPECT_EQ(e210_0927->runway(), rwy0927);

    EXPECT_EQ(e78_a1->type(), TaxiEdge::Type::Taxiway);
    EXPECT_TRUE(e78_a1->activeZones().departue.has(rwy0927));
    EXPECT_TRUE(e78_a1->activeZones().ils.has(rwy0927));
    EXPECT_FALSE(e78_a1->activeZones().arrival.has(rwy0927));
    EXPECT_FALSE(e78_a1->activeZones().departue.has(rwy0622));

    EXPECT_EQ(e92_c2->type(), TaxiEdge::Type::Taxiway);
    EXPECT_FALSE(e92_c2->activeZones().departue.hasAny());
    EXPECT_FALSE(e92_c2->activeZones().ils.hasAny());
    EXPECT_TRUE(e92_c2->activeZones().arrival.has(rwy0622));
    EXPECT_TRUE(e92_c2->activeZones().arrival.has(rwy0927));

    EXPECT_EQ(e45_b->type(), TaxiEdge::Type::Taxiway);
    EXPECT_FALSE(e45_b->activeZones().hasAny());
}

TEST(XPAirportReaderTest, readAptDat_parkingStands) {
    XPAirportReader reader(makeHost());
    stringstream aptDat = makeAptDat({
        "1300  40.100 -073.200 152.39 gate heavy|jets|turboprops T1 5",
        "1301 F airline AAL, SWA ",
        "1300  40.110 -073.220 105.95 hangar turboprops|props|helos T1 3",
        "1301 E cargo",
    });

    reader.readAirport(aptDat);
    const auto airport = reader.getAirport();
    const vector<shared_ptr<ParkingStand>>& parkingStands = airport->parkingStands();

    shared_ptr<ParkingStand> gate_t15 = airport->getParkingStandOrThrow("T1 5");
    shared_ptr<ParkingStand> gate_t13 = airport->getParkingStandOrThrow("T1 3");

    ASSERT_EQ(parkingStands.size(), 2);
    EXPECT_EQ(parkingStands[0], gate_t15);
    EXPECT_EQ(parkingStands[1], gate_t13);

    EXPECT_EQ(gate_t15->id(), 301);
    EXPECT_EQ(gate_t15->name(), "T1 5");
    EXPECT_EQ(gate_t15->type(), ParkingStand::Type::Gate);
    EXPECT_FLOAT_EQ(gate_t15->location().latitude(), 40.100);
    EXPECT_FLOAT_EQ(gate_t15->location().longitude(), -73.200);
    EXPECT_FLOAT_EQ(gate_t15->heading(), 152.39);
    EXPECT_EQ(gate_t15->widthCode(), "F");
    EXPECT_EQ(gate_t15->aircraftCategories(), Aircraft::Category::Heavy | Aircraft::Category::Jet | Aircraft::Category::Turboprop);
    EXPECT_EQ(gate_t15->operationTypes(), Aircraft::OperationType::Airline);
    ASSERT_EQ(gate_t15->airlines().size(), 2);
    EXPECT_EQ(gate_t15->airlines()[0], "AAL");
    EXPECT_EQ(gate_t15->airlines()[1], "SWA");

    EXPECT_EQ(gate_t13->id(), 302);
    EXPECT_EQ(gate_t13->name(), "T1 3");
    EXPECT_EQ(gate_t13->type(), ParkingStand::Type::Hangar);
    EXPECT_FLOAT_EQ(gate_t13->location().latitude(), 40.110);
    EXPECT_FLOAT_EQ(gate_t13->location().longitude(), -73.220);
    EXPECT_FLOAT_EQ(gate_t13->heading(), 105.95);
    EXPECT_EQ(gate_t13->widthCode(), "E");
    EXPECT_EQ(
        gate_t13->aircraftCategories(), 
        Aircraft::Category::Turboprop | Aircraft::Category::Prop | Aircraft::Category::Helicopter);
    EXPECT_EQ(gate_t13->operationTypes(), Aircraft::OperationType::Cargo);
    EXPECT_EQ(gate_t13->airlines().size(), 0);

    //writeAirportJson(airport, cout);
}

TEST(XPAirportReaderTest, readAptDat_parkingStands_oldCode15) {
    XPAirportReader reader(makeHost());
    stringstream aptDat = makeAptDat({
        "15  40.100 -073.200 152.39 T1 5",
        "15  40.110 -073.220 105.95 T1 3",
    });

    reader.readAirport(aptDat);
    const auto airport = reader.getAirport();
    const vector<shared_ptr<ParkingStand>>& parkingStands = airport->parkingStands();

    shared_ptr<ParkingStand> gate_t15 = airport->getParkingStandOrThrow("T1 5");
    shared_ptr<ParkingStand> gate_t13 = airport->getParkingStandOrThrow("T1 3");

    ASSERT_EQ(parkingStands.size(), 2);
    EXPECT_EQ(parkingStands[0], gate_t15);
    EXPECT_EQ(parkingStands[1], gate_t13);

    EXPECT_EQ(gate_t15->id(), 301);
    EXPECT_EQ(gate_t15->name(), "T1 5");
    EXPECT_EQ(gate_t15->type(), ParkingStand::Type::Unknown);
    EXPECT_FLOAT_EQ(gate_t15->location().latitude(), 40.100);
    EXPECT_FLOAT_EQ(gate_t15->location().longitude(), -73.200);
    EXPECT_FLOAT_EQ(gate_t15->heading(), 152.39);
    EXPECT_EQ(gate_t15->widthCode(), "F");
    EXPECT_EQ(gate_t15->aircraftCategories(), Aircraft::Category::All);
    EXPECT_EQ(gate_t15->operationTypes(), Aircraft::OperationType::All);
    EXPECT_TRUE(gate_t15->airlines().empty());

    EXPECT_EQ(gate_t13->id(), 302);
    EXPECT_EQ(gate_t13->name(), "T1 3");
    EXPECT_EQ(gate_t13->type(), ParkingStand::Type::Unknown);
    EXPECT_FLOAT_EQ(gate_t13->location().latitude(), 40.110);
    EXPECT_FLOAT_EQ(gate_t13->location().longitude(), -73.220);
    EXPECT_FLOAT_EQ(gate_t13->heading(), 105.95);
    EXPECT_EQ(gate_t13->widthCode(), "F");
    EXPECT_EQ(gate_t13->aircraftCategories(), Aircraft::Category::All);
    EXPECT_EQ(gate_t13->operationTypes(), Aircraft::OperationType::All);
    EXPECT_TRUE(gate_t13->airlines().empty());
}

TEST(XPAirportReaderTest, readAptDat_skipUnrecognizedLines) {
    XPAirportReader builder(makeHost());
    stringstream aptDat = makeAptDat({
        "I",
        "1000 Generated by WorldEditor",        
        "  ",
        "1     13 1 0 KJFK John F Kennedy Intl",
        "1302 city New York",
        "1302 country United States",
        "1201  32.1234  034.5678 both 231 K3_stop",
        "987655  1  12  13  ",
        ""
    });

    builder.readAirport(aptDat);
    const auto airport = builder.getAirport();
    const auto taxiNet = airport->taxiNet();

    ASSERT_EQ(taxiNet->nodes().size(), 1);
}

TEST(XPAirportReaderTest, readAptDat_realKJFK) {
    XPAirportReader reader(makeHost());
    ifstream aptDat;
    openTestInputStream("apt_kjfk.dat", aptDat);

    reader.readAirport(aptDat);
    const auto airport = reader.getAirport();

    assertRunwaysExist(airport, { "04L", "04R", "13L", "13R", "31L", "31R", "22L", "22R" });
    assertGatesExist(airport, { "DelEx Cargo 1", "Korea Cargo", "T8 12", "Prologis Cargo 2", "T4 55" });
    assertTaxiEdgesExist(airport, { "P", "F", "PF", "VA", "W", "MB", "K2", "Z", "TA", "TB" });
}

TEST(XPAirportReaderTest, findTaxiPath_KJFK_1) {
    XPAirportReader reader(makeHost());
    ifstream aptDat;
    aptDat.exceptions(ifstream::failbit | ifstream::badbit);
    aptDat.open(getTestInputFilePath("apt_kjfk.dat"));
    reader.readAirport(aptDat);
    ofstream jsonOutput;
    jsonOutput.exceptions(ofstream::failbit | ofstream::badbit);
    jsonOutput.open(getTestOutputFilePath("taxi_kjfk_79_378.json"), std::ios_base::out | std::ios_base::trunc);
    
    const auto airport = reader.getAirport();
    const auto taxiNet = airport->taxiNet();
    const auto taxiPath = TaxiPath::find(
        taxiNet, 
        taxiNet->getNodeById(79), 
        taxiNet->getNodeById(378));

    //writeTaxiPathJson(taxiPath, jsonOutput);
}

TEST(XPAirportReaderTest, findTaxiPath_KJFK_2) {
    XPAirportReader reader(makeHost());
    ifstream aptDat;
    aptDat.exceptions(ifstream::failbit | ifstream::badbit);
    aptDat.open(getTestInputFilePath("apt_kjfk.dat"));
    reader.readAirport(aptDat);
    ofstream jsonOutput;
    jsonOutput.exceptions(ofstream::failbit | ofstream::badbit);
    jsonOutput.open(getTestOutputFilePath("taxi_kjfk_79_581.json"), std::ios_base::out | std::ios_base::trunc);
    
    const auto airport = reader.getAirport();
    const auto taxiNet = airport->taxiNet();
    const auto taxiPath = TaxiPath::find(
        taxiNet, 
        taxiNet->getNodeById(79), 
        taxiNet->getNodeById(581));

    //writeTaxiPathJson(taxiPath, jsonOutput);
}

TEST(XPAirportReaderTest, readAptDat_realKMIA) {
    XPAirportReader reader(makeHost());
    ifstream aptDat;
    openTestInputStream("apt_kmia.dat", aptDat);

    reader.readAirport(aptDat);
    auto airport = reader.getAirport();

    assertRunwaysExist(airport, { "12", "30", "09", "27", "08R", "08L", "26L", "26R" });
    assertGatesExist(airport, { "F19", "D4", "J49", "N7", "Western U Cargo 70" });
    assertTaxiEdgesExist(airport, { "S", "U", "V", "M", "Z", "K", "M10", "JJ", "S2" });
}

TEST(XPAirportReaderTest, readAptDat_realKORD) {
    XPAirportReader reader(makeHost());
    ifstream aptDat;
    openTestInputStream("apt_kord.dat", aptDat);

    reader.readAirport(aptDat);
    auto airport = reader.getAirport();

    assertRunwaysExist(airport, {
        "10L", "10C", "10R", "28L", "28C", "28R", "22L", "22R", "04L", "04R", "09L", "09R", "27L", "27R"
    });
    assertGatesExist(airport, { "Terminal 1 Gate B5", "Suparna Cargo", "Terminal 5 M25" });
    assertTaxiEdgesExist(airport, { "CC", "DD", "W", "PP", "TT", "R", "B", "A7", "RR", "AA" });
}

TEST(XPAirportReaderTest, readAptDat_realHUEN) {
    XPAirportReader reader(makeHost());
    ifstream aptDat;
    openTestInputStream("apt_huen.dat", aptDat);

    reader.readAirport(aptDat);
    auto airport = reader.getAirport();

    assertTaxiEdgesExist(airport, { "A", "A1" });
}

TEST(XPAirportReaderTest, readToEndOfLine) {
    stringstream aptDat1 = makeAptDat({ "no_whitespace\rABCD" });
    stringstream aptDat2 = makeAptDat({ "  leading_and_trailing_spaces  \r\nABCD" });
    stringstream aptDat3 = makeAptDat({ "  all kinds of\x20\x20spaces  \n\r\nABCD" });

    string text1 = XPAirportReader::readToEndOfLine(aptDat1);
    string text2 = XPAirportReader::readToEndOfLine(aptDat2);
    string text3 = XPAirportReader::readToEndOfLine(aptDat3);

    EXPECT_EQ(text1, "no_whitespace");
    EXPECT_EQ(text2, "leading_and_trailing_spaces");
    EXPECT_EQ(text3, "all kinds of\x20spaces");

    EXPECT_EQ(aptDat1.peek(), 'A');
    EXPECT_EQ(aptDat2.peek(), 'A');
    EXPECT_EQ(aptDat3.peek(), 'A');
}

TEST(XPAirportReaderTest, skipToNextLine) {
    stringstream aptDat = makeAptDat({ 
        "AAA no_spacing",
        "BBB regular spacing",
        "CCC   arbitrary   spacing   ",
        "DDD"
    });

    string token1 = XPAirportReader::readFirstToken(aptDat);
    XPAirportReader::skipToNextLine(aptDat);
    string token2 = XPAirportReader::readFirstToken(aptDat);
    XPAirportReader::skipToNextLine(aptDat);
    string token3 = XPAirportReader::readFirstToken(aptDat);
    XPAirportReader::skipToNextLine(aptDat);
    string token4 = XPAirportReader::readFirstToken(aptDat);

    EXPECT_EQ(token1, "AAA");
    EXPECT_EQ(token2, "BBB");
    EXPECT_EQ(token3, "CCC");
    EXPECT_EQ(token4, "DDD");
}

TEST(XPAirportReaderTest, readAptDat_assembleTower) {
    auto airspace = makeAirspace(40.63, -73.77, 10.0, "KJFK");
    stringstream aptDat = makeAptDat({
        "1    13 0 0 KJFK John F Kennedy Intl",
        "1302 datum_lat 40.63",
        "1302 datum_lon -73.77",
        "1050 128725 ATIS",
        "1051 122950 UNICOM",
        "1052 135050 CLNC DEL",
        "1053 121650 GND",
        "1053 121900 GND",
        "1053 125050 GND",
        "1054 119100 KENNEDY TWR",
        "1054 123900 KENNEDY TWR",
        "1055 123700 NEW YORK APP",
        "1055 126800 NEW YORK APP",
        "1055 127400 NEW YORK APP",
        "1055 132400 NEW YORK APP",
        "1056 123700 NEW YORK DEP",
        "1056 124750 NEW YORK DEP",
        "1056 134350 NEW YORK DEP",
        "1056 135900 NEW YORK DEP"
    });
    XPAirportReader builder(makeHost(), -1, [&](const Airport::Header& header) {
        return airspace;
    });
    builder.readAirport(aptDat);

    const auto airport = builder.getAirport();
    const auto tower = airport->tower();

    ASSERT_TRUE(!!tower);
    EXPECT_EQ(airport->tower().get(), tower.get());
    EXPECT_EQ(tower->type(), ControlFacility::Type::Tower);
    EXPECT_EQ(tower->callSign(), "J F K"); //TODO: Kennedy
    EXPECT_EQ(tower->airport().get(), airport.get());
    EXPECT_EQ(tower->airspace().get(), airspace.get());
    ASSERT_EQ(tower->positions().size(), 5);

    EXPECT_EQ(tower->positions()[0]->type(), ControllerPosition::Type::ClearanceDelivery);
    EXPECT_EQ(tower->positions()[0]->frequency()->khz(), 135050);
    EXPECT_EQ(tower->positions()[0]->callSign(), "J F K Clearance"); //TODO: "Kennedy Clearance"

    EXPECT_EQ(tower->positions()[1]->type(), ControllerPosition::Type::Ground);
    EXPECT_EQ(tower->positions()[1]->frequency()->khz(), 121650);
    EXPECT_EQ(tower->positions()[1]->callSign(), "J F K Ground"); //TODO: "Kennedy Ground"

    EXPECT_EQ(tower->positions()[2]->type(), ControllerPosition::Type::Local);
    EXPECT_EQ(tower->positions()[2]->frequency()->khz(), 119100);
    EXPECT_EQ(tower->positions()[2]->callSign(), "J F K Tower"); //TODO: "Kennedy Tower"

    EXPECT_EQ(tower->positions()[3]->type(), ControllerPosition::Type::Approach);
    EXPECT_EQ(tower->positions()[3]->frequency()->khz(), 123700);
    EXPECT_EQ(tower->positions()[3]->callSign(), "J F K Approach"); //TODO: "New York Approach"

    EXPECT_EQ(tower->positions()[4]->type(), ControllerPosition::Type::Departure);
    EXPECT_EQ(tower->positions()[4]->frequency()->khz(), 124750);
    EXPECT_EQ(tower->positions()[4]->callSign(), "J F K Departure"); //TODO: "New York Departure"
}

TEST(XPAptDatReaderTest, readAptDat_allAirports)
{
    ifstream input;
    openTestInputStream("apt_many.dat", input);
    XPAptDatReader reader(makeHost());
    vector<shared_ptr<Airport>> output;

    reader.readAptDat(
        input,
        XPAirportReader::noopQueryAirspace,
        XPAirportReader::noopFilterAirport,
        [&](shared_ptr<Airport> airport) {
            output.push_back(airport);
        }
    );

    ASSERT_EQ(output.size(), 4);

    EXPECT_EQ(output[0]->header().icao(), "ABCD");
    EXPECT_EQ(output[0]->getParkingStandOrThrow("A1")->name(), "A1");

    EXPECT_EQ(output[1]->header().icao(), "EFGH");
    EXPECT_EQ(output[1]->getParkingStandOrThrow("B1")->name(), "B1");

    EXPECT_EQ(output[2]->header().icao(), "IJKL");
    EXPECT_EQ(output[2]->getParkingStandOrThrow("C1")->name(), "C1");

    EXPECT_EQ(output[3]->header().icao(), "MNOP");
    EXPECT_EQ(output[3]->getParkingStandOrThrow("D1")->name(), "D1");
}

TEST(XPAptDatReaderTest, readAptDat_filterAirports)
{
    ifstream input;
    openTestInputStream("apt_many.dat", input);
    XPAptDatReader reader(makeHost());
    vector<shared_ptr<Airport>> output;

    reader.readAptDat(
        input,
        XPAirportReader::noopQueryAirspace,
        [&](const Airport::Header& header) {
            return (header.icao() == "EFGH" || header.icao() == "MNOP");
        },
        [&](shared_ptr<Airport> airport) {
            output.push_back(airport);
        }
    );

    ASSERT_EQ(output.size(), 2);

    EXPECT_EQ(output[0]->header().icao(), "EFGH");
    EXPECT_EQ(output[0]->getParkingStandOrThrow("B1")->name(), "B1");

    EXPECT_EQ(output[1]->header().icao(), "MNOP");
    EXPECT_EQ(output[1]->getParkingStandOrThrow("D1")->name(), "D1");
}

TEST(XPAptDatReaderTest, readAptDat_skipAirportsFailingToLoad)
{
    ifstream input;
    openTestInputStream("apt_errors.dat", input);
    XPAptDatReader reader(makeHost());
    vector<shared_ptr<Airport>> output;

    reader.readAptDat(
        input,
        XPAirportReader::noopQueryAirspace,
        [&](const Airport::Header& header) {
            return true;
        },
        [&](shared_ptr<Airport> airport) {
            output.push_back(airport);
        }
    );

    ASSERT_EQ(output.size(), 3);
    EXPECT_EQ(output[0]->header().icao(), "ABCD");
    EXPECT_EQ(output[1]->header().icao(), "IJKL");
    EXPECT_EQ(output[2]->header().icao(), "MNOP");
}

#if 0
TEST(XPAptDatReaderTest, readAll_realDefaultAptDat)
{
    ifstream input;
    input.exceptions(ifstream::failbit | ifstream::badbit);
    input.open(R"(E:\X-Plane 11\Resources\default scenery\default apt dat\Earth nav data\apt.dat)");

    ofstream output;
    output.exceptions(ofstream::failbit | ofstream::badbit);
    output.open("../../src/libloaders_test/testOutputs/gates-with-long-names.txt", std::ios_base::out | std::ios_base::trunc);

    unordered_set<string> icaos;
    int count = 0;

    auto host = TestHostServices::create();
    host->enableLogs(true);

    XPAptDatReader reader(host);
    reader.readAptDat(
        input,
        [&](const Airport::Header& header) {
            return WorldBuilder::assembleSampleAirportControlZone(header);
        },
        [&](const Airport::Header& header) {
            return true;
        },
        [&](shared_ptr<Airport> airport) {
            count++;
            if ((count % 100) == 0)
            {
                cout << "done: # " << count << " " << airport->header().icao() << endl;
            }

            icaos.insert(airport->header().icao());

            for (const auto& gate : airport->parkingStands())
            {
                if (gate->name().length() > 5)
                {
                    output << gate->name() << endl;
                }
            }
        }
    );

    EXPECT_EQ(count, 29609);
    EXPECT_GE(icaos.size(), 29608);
    EXPECT_TRUE(hasKey(icaos, string("LCLK")));
    EXPECT_FALSE(hasKey(icaos, string("CKT8")));
}
#endif

void createTestOutputStream(const string& fileName, ofstream& str)
{
    string fullPath = getTestInputFilePath(fileName);
    str.exceptions(ofstream::failbit | ofstream::badbit);
    str.open(fullPath.c_str(), ofstream::out | ofstream::trunc);
}

