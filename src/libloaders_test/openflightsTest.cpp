#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "libopenflights.hpp"
#include <fstream>

stringstream makeStream(const vector<string> &lines)
{
    stringstream output;
    output.exceptions(ios::failbit | ios::badbit);

    for (const auto &line : lines)
    {
        output << line << endl;
    }

    output.seekg(0);
    return output;
}

class OpenFlightsRoutesTest : public ::testing::Test
{
protected:
    static shared_ptr<WorldRoutes> m_routeFinder;
    static shared_ptr<TestHostServices> m_host;

    void SetUp() override
    {
        m_host = TestHostServices::create();
        m_host->enableLogs(true);
        if (m_routeFinder == nullptr)
        {
            auto ofreader = OpenFlightDataReader(m_host);

            m_routeFinder = ofreader.getRoutes("./testinputs/openflights", {});
        }
    }

    void printRoute(const world::Route &route)
    {
        m_host->writeLog("TEST|Route found from [%s] to [%s]", route.departure().c_str(), route.destination().c_str());
        m_host->writeLog("TEST|  Used airframes on this route :");
        for (auto airframe : route.usedAirframes())
        {
            m_host->writeLog("TEST|   * [%s]", airframe.c_str());
        }
    }
};
shared_ptr<WorldRoutes> OpenFlightsRoutesTest::m_routeFinder;
shared_ptr<TestHostServices> OpenFlightsRoutesTest::m_host;

// Check that the datas delivered with the plugin are parsable (they do not throw)
TEST(OpenFlightsRoutesTestNoFixture, parseTest)
{
    auto host = TestHostServices::create();
    shared_ptr<WorldRoutes> routes = nullptr;
    host->enableLogs(true);

    auto ofreader = OpenFlightDataReader(host);
    ASSERT_NO_THROW(routes = ofreader.getRoutes("../../assets/openflights", {}));
    // Check that some routes are available at JFK
    ASSERT_GT(routes->routesFromCount("KJFK"), 0);
    ASSERT_GT(routes->routesToCount("KJFK"), 0);
}

TEST_F(OpenFlightsRoutesTest, findOutboundRouteFixture)
{
    auto defaultRoute = world::Route("", "", "", {});
    const world::Route &route = defaultRoute;
    vector<string> seq1;
    vector<string> seq2;

    size_t nbRoutes = m_routeFinder->routesFromCount("KJFK");

    for (int i = 0 ; i < nbRoutes ; i++)
    {
        ASSERT_NO_THROW({
            const world::Route & _route =  m_routeFinder->getNextRouteFrom("KJFK");
            seq1.push_back(_route.destination());
        });
    }
    for (int i = 0 ; i < nbRoutes ; i++)
    {
        ASSERT_NO_THROW({
            const world::Route & _route =  m_routeFinder->getNextRouteFrom("KJFK");
            seq2.push_back(_route.destination());
        });
    }

    // The sequence of routes is the same
    ASSERT_TRUE(std::equal(seq1.begin(), seq1.end(), seq2.begin()));

}

TEST_F(OpenFlightsRoutesTest, findInboundRouteFixture)
{
    auto defaultRoute = world::Route("", "", "", {});
    const world::Route &route = defaultRoute;
    vector<string> seq1;
    vector<string> seq2;

    size_t nbRoutes = m_routeFinder->routesToCount("KJFK");

    for (int i = 0 ; i < nbRoutes ; i++)
    {
        ASSERT_NO_THROW({
            const world::Route & _route =  m_routeFinder->getNextRouteTo("KJFK");
            seq1.push_back(_route.departure());
        });
    }
    for (int i = 0 ; i < nbRoutes ; i++)
    {
        ASSERT_NO_THROW({
            const world::Route & _route =  m_routeFinder->getNextRouteTo("KJFK");
            seq2.push_back(_route.departure());
        });
    }

    // The sequence of routes is the same
    ASSERT_TRUE(std::equal(seq1.begin(), seq1.end(), seq2.begin()));
    // Every element in a sequence is unique
    sort(seq1.begin(), seq1.end());
    unique(seq1.begin(), seq1.end());
    ASSERT_EQ(seq1.size(), nbRoutes);
}

TEST_F(OpenFlightsRoutesTest, checkAircraftFiltering)
{
    // m_routeFinder is loaded without filter
    ASSERT_EQ(m_routeFinder->routesToCount("KJFK"), 8);
    ASSERT_EQ(m_routeFinder->routesToCount("KWAL"), 2);

    // Reload routes with only B738 routes
    static shared_ptr<WorldRoutes> b738Finder;
    auto ofreader = OpenFlightDataReader(m_host);

    ASSERT_NO_THROW(b738Finder = ofreader.getRoutes("./testinputs/openflights", {"B738"}));
    ASSERT_EQ(b738Finder->routesToCount("KJFK"), 8);
    ASSERT_EQ(b738Finder->routesToCount("KWAL"), 1);

    static shared_ptr<WorldRoutes> a320Finder;
    ofreader = OpenFlightDataReader(m_host);

    ASSERT_NO_THROW(a320Finder = ofreader.getRoutes("./testinputs/openflights", {"A320"}));
    ASSERT_EQ(a320Finder->routesToCount("KJFK"), 0);
    ASSERT_EQ(a320Finder->routesToCount("KWAL"), 1);
    
}

TEST_F(OpenFlightsRoutesTest, checkRandomness)
{
    auto defaultRoute = world::Route("", "", "", {});
    const world::Route &route = defaultRoute;
    vector<string> seq1F, seq1T;
    vector<string> seq2F, seq2T;
    shared_ptr<WorldRoutes> s1Finder;
    auto ofreader = OpenFlightDataReader(m_host);
    auto rng1 = default_random_engine(1);
    ASSERT_NO_THROW(s1Finder = ofreader.getRoutes("./testinputs/openflights", {}, rng1));
    size_t nbRoutes = s1Finder->routesToCount("KJFK");

    m_host->writeLog("TEST|  Sequence 1");
    for (int i = 0 ; i < nbRoutes ; i++)
    {
        ASSERT_NO_THROW({
            const world::Route & _routeT =  s1Finder->getNextRouteTo("KJFK");
            seq1T.push_back(_routeT.departure());
            m_host->writeLog("TEST| %s -> %s", _routeT.departure().c_str(),  _routeT.destination().c_str());
        });
        ASSERT_NO_THROW({
            const world::Route & _routeF =  s1Finder->getNextRouteFrom("KJFK");
            seq1F.push_back(_routeF.destination());
            m_host->writeLog("TEST| %s <- %s",  _routeF.destination().c_str(), _routeF.departure().c_str());

        });
    }

    // Reload routes with another instance of the reader
    shared_ptr<WorldRoutes> s2Finder;
    ofreader = OpenFlightDataReader(m_host);
    auto rng2 = default_random_engine(2);
    ASSERT_NO_THROW(s2Finder = ofreader.getRoutes("./testinputs/openflights", {}, rng2));
    ASSERT_EQ(s2Finder->routesToCount("KJFK"), nbRoutes);

    m_host->writeLog("TEST|  Sequence 2");
    for (int i = 0 ; i < nbRoutes ; i++)
    {
        ASSERT_NO_THROW({
            const world::Route & _routeT =  s2Finder->getNextRouteTo("KJFK");
            seq2T.push_back(_routeT.departure());
            m_host->writeLog("TEST| %s -> %s", _routeT.departure().c_str(),  _routeT.destination().c_str());
        });
        ASSERT_NO_THROW({
            const world::Route & _routeF =  s2Finder->getNextRouteFrom("KJFK");
            seq2F.push_back(_routeF.destination());
            m_host->writeLog("TEST| %s <- %s",  _routeF.destination().c_str(), _routeF.departure().c_str());

        });
    }

    // The two sequences ared different
    ASSERT_FALSE(std::equal(seq1F.begin(), seq1F.end(), seq2F.begin()));
    ASSERT_FALSE(std::equal(seq1T.begin(), seq1T.end(), seq2T.begin()));
}
