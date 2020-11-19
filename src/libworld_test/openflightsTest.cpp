#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "openflights.hpp"
#include <fstream>

stringstream makeStream(const vector<string>& lines)
{
    stringstream output;
    output.exceptions(ios::failbit | ios::badbit);

    for (const auto& line : lines)
    {
        output << line << endl;
    }

    output.seekg(0);
    return output;
}

void printRoute(TestHostServices &host, world::WorldRoutes::Route &route)
{
    host.writeLog("TEST|Route found from [%s] to [%s] with [%s]",route.departure().c_str(), route.destination().c_str(), route.callsign().c_str() );
    host.writeLog("TEST|  Used airframes on this route :");
    for (auto airframe : route.usedAirframes())
    {
        host.writeLog("TEST|   * [%s]", airframe.c_str() );
    }
}
class OpenFlightsRoutesTest : public ::testing::Test
{
    protected:
    static shared_ptr<WorldRoutes> m_routeFinder;
    static shared_ptr<TestHostServices> m_host;

    void SetUp() override {
        m_host = TestHostServices::create();
        // m_host->enableLogs(true);

        if (m_routeFinder == nullptr)
        {
            auto ofreader = OpenFlightDataReader(m_host);

            auto testAirports = makeStream({
                "3797,,,,\"JFK\",\"KJFK\",,,,,,,, ",
                "3798,,,,\"RAL\",\"KRAL\",,,,,,,,",
                "3799,,,,\"FLV\",\"KFLV\",,,,,,,,",
                "3800,,,,\"WAL\",\"KWAL\",,,,,,,,",
                "3801,,,,\"HMN\",\"KHMN\",,,,,,,,",
                "3802,,,,\"NXX\",\"KNXX\",,,,,,,,",
                "3803,,,,\"CYS\",\"KCYS\",,,,,,,,",
                "3804,,,,\"SCK\",\"KSCK\",,,,,,,,",
                "3805,,,,\"CHS\",\"KCHS\",,,,,,,,",
                "3806,,,,\"RNO\",\"KRNO\",,,,,,,,"
            });
            ofreader.readAirports(testAirports);
            auto testPlanes = makeStream({
                "\"Boeing 737-800\",\"738\",\"B738\"",
                "\"Airbus A320\",\"320\",\"A320\"",
                "\"Cessna 172\",\"CN1\",\"C172\""
            });
            ofreader.readPlanes(testPlanes);

            auto testAirlines = makeStream({
                "137,\"Air France\",\\N,\"AF\",\"AFR\",\"AIRFRANS\",\"France\",\"Y\"",
                "324,\"All Nippon Airways\",\"ANA All Nippon Airways\",\"NH\",\"ANA\",\"ALL NIPPON\",\"Japan\",\"Y\"",
                "4089,\"Qantas\",\"Qantas Airways\",\"QF\",\"QFA\",\"QANTAS\",\"Australia\",\"Y\"",
                "4547,\"Southwest Airlines\",\\N,\"WN\",\"SWA\",\"SOUTHWEST\",\"United States\",\"Y\"",
                "5209,\"United Airlines\",\\N,\"UA\",\"UAL\",\"UNITED\",\"United States\",\"Y\""
            });
            ofreader.readAirlines(testAirlines);
            auto testRoutes = makeStream({
                "UA,5209,JFK,3797,RAL,3798,,0,738",
                "UA,5209,RAL,3798,JFK,3797,,0,738",
                "WN,4547,JFK,3797,FLV,3799,,0,738",
                "WN,4547,FLV,3799,JFK,3797,,0,738",
                "QF,4089,WAL,3800,HMN,3801,,0,320",
                "QF,4089,HMN,3801,WAL,3800,,0,320",
                "UA,5209,JFK,3797,WAL,3800,,0,738",
                "UA,5209,WAL,3800,JFK,3797,,0,738",
                "UA,5209,JFK,3797,HMN,3801,,0,738",
                "UA,5209,HMN,3801,JFK,3797,,0,738",
                "UA,5209,JFK,3797,NXX,3802,,0,738",
                "UA,5209,NXX,3802,JFK,3797,,0,738",
                "UA,5209,JFK,3797,CYS,3803,,0,738",
                "UA,5209,CYS,3803,JFK,3797,,0,738",
                "UA,5209,JFK,3797,SCK,3804,,0,738",
                "UA,5209,SCK,3804,JFK,3797,,0,738",
                "UA,5209,JFK,3797,CHS,3805,,0,738",
                "UA,5209,CHS,3805,JFK,3797,,0,738"
            });
            ofreader.readRoutes(testRoutes);
            m_routeFinder = ofreader.getWorldRoutes();
        }
  }
};
shared_ptr<WorldRoutes> OpenFlightsRoutesTest::m_routeFinder;
shared_ptr<TestHostServices> OpenFlightsRoutesTest::m_host;

// Check that the datas delivered with the plugin are parsable (they do not throw)
TEST(OpenFlightsRoutesTestNoFixture, parseTest)
{
    auto host = TestHostServices::create();
    // host->enableLogs(true);
 
    auto ofreader = OpenFlightDataReader(host);

    ifstream input;
    input.exceptions(ifstream::failbit | ifstream::badbit);
    input.open("../../libs/openflights/data/airports.dat");
    EXPECT_NO_THROW(ofreader.readAirports(input));
    input.close();
    input.clear();

    input.open("../../libs/openflights/data/planes.dat");
    EXPECT_NO_THROW(ofreader.readPlanes(input));
    input.close();
    input.clear();

    input.open("../../libs/openflights/data/airlines.dat");
    EXPECT_NO_THROW(ofreader.readAirlines(input));
    input.close();
    input.clear();

    input.open("../../libs/openflights/data/routes.dat");
    EXPECT_NO_THROW(ofreader.readRoutes(input));
    input.close();
    input.clear();

    auto datas = ofreader.getWorldRoutes();
}

TEST_F(OpenFlightsRoutesTest, findOutboundRouteFixture)
{
    auto defaultRoute = world::WorldRoutes::Route("", "", "", "", {});
    world::WorldRoutes::Route &route = defaultRoute;

    // Never throws with valid parameters
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "B738", {"UAL"}));
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "b738", {"ual"}));
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "b738", {"qfa", "ual"}));

    // Route constraints unmet
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "B738", {"ANA", "afr"}), std::runtime_error);
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "B738", {}));

    // Aircraft constraints unmet
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "A320", {"UAL"}), std::runtime_error);
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {"UAL"}));

    // Both constraints unmet
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "A320", {"QFA"}), std::runtime_error);
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "A320", {}), std::runtime_error);
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {"QFA"}), std::runtime_error);
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));

    // No routes at known airport
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("KRNO", "", {}), std::runtime_error);

    // Unknown airport
    EXPECT_THROW(route = m_routeFinder->findRandomRouteFrom("LFPG", "", {}), std::runtime_error);
}

TEST_F(OpenFlightsRoutesTest, findInboundRouteFixture)
{
    auto defaultRoute = world::WorldRoutes::Route("", "", "", "", {});
    world::WorldRoutes::Route &route = defaultRoute;

    // Never throws with valid parameters
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "B738", {"UAL"}));
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "b738", {"ual"}));
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "b738", {"qfa", "ual"}));

    // Route constraints unmet
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "B738", {"ANA", "afr"}), std::runtime_error);
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "B738", {}));

    // Aircraft constraints unmet
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "A320", {"UAL"}), std::runtime_error);
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {"UAL"}));

    // Both constraints unmet
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "A320", {"QFA"}), std::runtime_error);
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "A320", {}), std::runtime_error);
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {"QFA"}), std::runtime_error);
    EXPECT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));

    // No routes at known airport
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("KRNO", "", {}), std::runtime_error);

    // Unknown airport
    EXPECT_THROW(route = m_routeFinder->findRandomRouteTo("LFPG", "", {}), std::runtime_error);
}

TEST_F(OpenFlightsRoutesTest, checkRandomness)
{
    // By default, the rng is the local time, we should have different flights every time
    dynamic_cast<OpenFlightsRoutes&>(*m_routeFinder).setRng(std::mt19937(2));
    auto defaultRoute = world::WorldRoutes::Route("", "", "", "", {});
    world::WorldRoutes::Route &route = defaultRoute;
    vector<string> seq1;
    vector<string> seq2;
    vector<string> seq3;
    // char Seq1[6][5]={"KWAL", "KSCK", "KWAL", "KCYS", "KWAL", "KWAL"};
    // char Seq2[6][5]={"KCHS", "KCHS", "KHMN", "KHMN", "KFLV", "KCYS"};

    // Set the first RNG
    dynamic_cast<OpenFlightsRoutes&>(*m_routeFinder).setRng(std::mt19937(2));
    // outbound
    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq1.push_back(route.destination());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq1.push_back(route.destination());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq1.push_back(route.destination());

    // Inbound
    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq1.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq1.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq1.push_back(route.departure());

    // Reset the first RNG, check that the sequence is the same
    dynamic_cast<OpenFlightsRoutes&>(*m_routeFinder).setRng(std::mt19937(2));
    // outbound
    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq2.push_back(route.destination());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq2.push_back(route.destination());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq2.push_back(route.destination());

    // Inbound
    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq2.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq2.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq2.push_back(route.departure());
    
    // Change the seed and check that the sequences are different
    dynamic_cast<OpenFlightsRoutes&>(*m_routeFinder).setRng(std::mt19937(3));
    // outbound
    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq3.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq3.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteFrom("KJFK", "", {}));
    seq3.push_back(route.departure());

    // Inbound
    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq3.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq3.push_back(route.departure());

    ASSERT_NO_THROW(route = m_routeFinder->findRandomRouteTo("KJFK", "", {}));
    seq3.push_back(route.departure());

    ASSERT_TRUE(std::equal(seq1.begin(), seq1.end(), seq2.begin()));
    ASSERT_FALSE(std::equal(seq1.begin(), seq1.end(), seq3.begin()));


}
