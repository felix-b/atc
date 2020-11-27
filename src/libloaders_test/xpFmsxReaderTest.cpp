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

using namespace world;
using namespace std;


TEST(XPFmsxReaderTest, readFmx) {
    auto host = TestHostServices::createWithWorld();
    XPFmsxReader reader(host);
    ifstream fmx;
    openTestInputStream("kjfk_kord.fmx", fmx);

    auto flightPlan = reader.readFrom(fmx);

    EXPECT_EQ(flightPlan->departureAirportIcao(), "KJFK");
    EXPECT_EQ(flightPlan->departureRunway(), "04L");
    EXPECT_EQ(flightPlan->sidName(), "GREKI6");
    EXPECT_EQ(flightPlan->sidTransition(), "JUDDS");
    EXPECT_EQ(flightPlan->starName(), "WYNDE1");
    EXPECT_EQ(flightPlan->starTransition(), "EMMMA");
    EXPECT_EQ(flightPlan->approachName(), "R-09LY");
    EXPECT_EQ(flightPlan->arrivalRunway(), "09L");
    EXPECT_EQ(flightPlan->flightNo(), "UAL738");
}

TEST(XPFmsxReaderTest, readFms) {
    auto host = TestHostServices::createWithWorld();
    XPFmsxReader reader(host);
    ifstream fms;
    openTestInputStream("kjfk_kord.fms", fms);

    auto flightPlan = reader.readFrom(fms);

    EXPECT_EQ(flightPlan->departureAirportIcao(), "KJFK");
    EXPECT_EQ(flightPlan->departureRunway(), "04L");
    EXPECT_EQ(flightPlan->sidName(), "DEEZZ5");
    EXPECT_EQ(flightPlan->sidTransition(), "HEERO");
    EXPECT_EQ(flightPlan->starName(), "WYNDE1");
    EXPECT_EQ(flightPlan->starTransition(), "EMMMA");
    EXPECT_EQ(flightPlan->approachName(), "I09L");
    EXPECT_EQ(flightPlan->arrivalRunway(), "09L");
}

TEST(XPFmsxReaderTest, readFms_compareDepartureAirportIcao) {
    auto host = TestHostServices::createWithWorld();
    XPFmsxReader fmsReader(host);
    ifstream fms;
    openTestInputStream("kmia_kfll.fms", fms);
    auto flightPlan = fmsReader.readFrom(fms);

    XPAirportReader aptDatReader(host);
    ifstream aptDat;
    openTestInputStream("apt_kmia.dat", aptDat);
    aptDatReader.readAirport(aptDat);
    auto airport = aptDatReader.getAirport();

    EXPECT_EQ(flightPlan->departureAirportIcao(), "KMIA");
    EXPECT_EQ(flightPlan->departureRunway(), "08R");
    EXPECT_EQ(flightPlan->sidName(), "WINCO2");
    EXPECT_EQ(flightPlan->sidTransition(), "");
    EXPECT_EQ(flightPlan->starName(), "WAVUN5");
    EXPECT_EQ(flightPlan->starTransition(), "ZFP");
    EXPECT_EQ(flightPlan->approachName(), "I10L");
    EXPECT_EQ(flightPlan->arrivalRunway(), "10L");

    if (flightPlan->departureAirportIcao() != airport->header().icao())
    {
        bool departureAirportIcaoEquals = false;
        EXPECT_TRUE(departureAirportIcaoEquals);
    }
}
