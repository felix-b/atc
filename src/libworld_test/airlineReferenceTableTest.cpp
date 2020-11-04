// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "airlineReferenceTable.hpp"
#include "libworld_test.h"

using namespace world;

TEST(AirlineReferenceTableTest, tryFindByIcao_success)
{
    AirlineReferenceTable::Entry entry;

    bool result = AirlineReferenceTable::tryFindByIcao("UAL", entry);

    EXPECT_TRUE(result);
    EXPECT_EQ(entry.icao, "UAL");
    EXPECT_EQ(entry.callsign, "UNITED");
    EXPECT_EQ(entry.name, "United Airlines");
    EXPECT_EQ(entry.regionIcao, "K");
}

TEST(AirlineReferenceTableTest, tryFindByIcao_failure)
{
    AirlineReferenceTable::Entry entry;

    bool result = AirlineReferenceTable::tryFindByIcao("A-NON-EXISTENT-CODE", entry);

    EXPECT_FALSE(result);
    EXPECT_TRUE(entry.icao.empty());
    EXPECT_TRUE(entry.callsign.empty());
    EXPECT_TRUE(entry.name.empty());
    EXPECT_TRUE(entry.regionIcao.empty());
}

TEST(AirlineReferenceTableTest, tryFindByFLightNumber_success)
{
    AirlineReferenceTable::Entry entry;
    string flightCallsign;
    bool result = AirlineReferenceTable::tryFindByFlightNumber("UAL738", entry, flightCallsign);

    EXPECT_TRUE(result);

    EXPECT_EQ(entry.icao, "UAL");
    EXPECT_EQ(entry.callsign, "UNITED");
    EXPECT_EQ(entry.name, "United Airlines");
    EXPECT_EQ(entry.regionIcao, "K");

    EXPECT_EQ(flightCallsign, "UNITED 738");
}

TEST(AirlineReferenceTableTest, tryFindByFLightNumber_failure)
{
    AirlineReferenceTable::Entry entry;
    string flightCallsign;
    bool result = AirlineReferenceTable::tryFindByFlightNumber("_N_O_N_EXISTENT_123", entry, flightCallsign);

    EXPECT_FALSE(result);
    EXPECT_TRUE(entry.icao.empty());
    EXPECT_TRUE(entry.callsign.empty());
    EXPECT_TRUE(entry.name.empty());
    EXPECT_TRUE(entry.regionIcao.empty());
    EXPECT_TRUE(flightCallsign.empty());
}
