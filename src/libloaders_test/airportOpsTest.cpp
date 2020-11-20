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


TEST(AirportOpsTest, taxi_separateArrivalsAndDepartures) {
    auto host = TestHostServices::createWithWorld();
    XPAirportReader reader(host, -1, WorldBuilder::assembleSampleAirportControlZone);
    ifstream aptDat;

    openTestInputStream("apt_kmia.dat", aptDat);

    reader.readAirport(aptDat);
    const auto airport = reader.getAirport();

    ASSERT_TRUE(airport->tower());
    auto localController = dynamic_pointer_cast<TestHostServices::TestAIController>(
        airport->localAt(airport->header().datum())->controller()
    );
    ASSERT_TRUE(localController);
    localController->onSelectActiveRunways([](vector<string>& departure, vector<string>& arrival) {
        departure.push_back("08R");
        arrival.push_back("08L");
    });

    airport->selectActiveRunways();

    ASSERT_TRUE(airport->activeDepartureRunways().size() > 0);

    for (const auto& runway : airport->activeDepartureRunways())
    {
        cout << "RWY-DEPARTURE: " << runway << endl;
    }
    for (const auto& runway : airport->activeArrivalRunways())
    {
        cout << "RWY-ARRIVAL: " << runway << endl;
    }

    airport->selectArrivalAndDepartureTaxiways();

    for (const auto& edge : airport->taxiNet()->edges())
    {
        if (edge->flightPhaseAllocation() != world::Flight::Phase::NotAssigned)
        {
            cout << edge->id() << "/" << edge->name() << " : " << (int)edge->flightPhaseAllocation() << endl;
        }
    }

    auto gateF14 = airport->getParkingStandOrThrow("F14");
    const auto& rwy08R = airport->getRunwayEndOrThrow("08R");

    cout << "------ FIND DEPARTURE PATH ------" << endl;

    auto departurePath = airport->taxiNet()->tryFindDepartureTaxiPathToRunway(gateF14->location().geo(), rwy08R);

    cout << "------ FIND ARRIVAL PATH ------" << endl;

    auto arrivalPath = airport->taxiNet()->tryFindTaxiPathToGate(
        gateF14,
        GeoPoint(25.80327988, -80.28641476));

    ASSERT_TRUE(departurePath);
    ASSERT_TRUE(arrivalPath);

    cout << "------ DEPARTURE PATH ------" << endl;

    for (const auto& edge : departurePath->edges)
    {
        cout << edge->id() << "/" << edge->name() << endl;
    }

    cout << "------ END OF DEPARTURE PATH ------" << endl;
    cout << "------ ARRIVAL PATH ------" << endl;

    for (const auto& edge : arrivalPath->edges)
    {
        cout << edge->id() << "/" << edge->name() << endl;
    }

    cout << "------ END OF ARRIVAL PATH ------" << endl;
}
