//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <functional>
#include <memory>
#include <string>
#include <queue>
#include <vector>
#include <chrono>
#include <iostream>
#include <sstream>

#include "gtest/gtest.h"
#include "libworld.h"
#include "clearanceTypes.hpp"
#include "simpleRunwayMutex.hpp"
#include "libworld_test.h"

using namespace std;
using namespace world;
using namespace ai;

typedef FlightStrip::Event::Type MutexEventType;
typedef FlightStrip::Event MutexEvent;
typedef FlightStrip::Event::Listener MutexEventListener;
typedef TestHostServices::TestFlight TestFlight;

enum class ScenarioType
{
    NotSet,
    Arrival,
    Departure,
    LUAW,
    Crossing
};

enum class RunwaySituation
{
    NotSet,
    Vacated,
    LuawAuthorized,
    ClearedForLanding,
    ClearedForTakeoff,
    ClearedForCrossing,
    LandedNotVacated,
    Incursion
};

enum class WhenWhat
{
    NotSet,
    ArrivalChecksIn,
    DepartureChecksIn,
    SecondsToTouchdown,
    DepartureBeginsRoll,
    ArrivalVacates,
    ReportHoldingShort
};

shared_ptr<Airport> createAirportEFGH(shared_ptr<TestHostServices> host);

class MutexTestCase
{
public:
    struct Given
    {
    private:
        friend class RunwayMutexTestCase;
        MutexTestCase& m_test;
    public:
        Given(MutexTestCase& _test) : m_test(_test) { }
    public:
        Given& checkedIn(bool value) { m_test.m_checkedIn = value; m_test.m_checkedInWasSet = true; return *this; }
        Given& clearedToLand(bool value) { m_test.m_clearedToLand = value; m_test.m_clearedToLandWasSet = true; return *this; }
        Given& numberInLine(int value) { m_test.m_numberInLine = value; return *this; }
        Given& secsToTouchdown(int value) { m_test.m_secondsToTouchdown = value; return *this; }
        MutexTestCase& end() { return m_test; }
    };
private:
    shared_ptr<TestHostServices> m_host;
    shared_ptr<Airport> m_airport;
    shared_ptr<Runway> m_runway;
    shared_ptr<SimpleRunwayMutex> m_mutexUnderTest;
    ScenarioType m_scenarioType = ScenarioType::NotSet;
    Given m_given;
    RunwaySituation m_situation = RunwaySituation::NotSet;
    SimpleRunwayMutex::TimingThresholds m_timing;
    bool m_checkedIn = false;
    bool m_checkedInWasSet = false;
    bool m_clearedToLand = false;
    bool m_clearedToLandWasSet = false;
    int m_numberInLine = -1;
    int m_secondsToTouchdown = -1;
    int m_whenSecondsToTouchdown = -1;
    WhenWhat m_when = WhenWhat::NotSet;
    TestFlight m_arrivalUnderTest;
    TestFlight m_departurelUnderTest;
    TestFlight m_taxiingUnderTest;
    TestFlight m_numberOneInLine;
    TestFlight m_anotherArrival;
    TestFlight m_anotherDeparture;
    TestFlight m_anotherTaxiing;
    RunwayStripBoard m_board;
    chrono::seconds m_time = chrono::seconds(0);
    int m_expectedEventCount = 0;
    int m_actualEventCount = 0;
    MutexEvent m_expectedEvent;
    MutexEvent m_actualEvent;
    MutexEventListener m_listener;
    bool m_listening = false;
public:
    MutexTestCase() :
        m_given(*this)
    {
        m_host = TestHostServices::createWithWorldAirports({ createAirportEFGH });
        m_host->enableLogs(true);
        m_airport = m_host->getWorld()->getAirport("EFGH");
        m_runway = m_airport->getRunwayOrThrow("09/27");
        m_listener = [this](const MutexEvent& e) {
            mutexEventListener(e);
        };

        m_timing.RWY_TIME_LUAW_AUTHORIZATION_BEFORE_LANDING_MIN = 100;
        m_timing.RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN = 90;
    }

public:
    MutexTestCase& type(ScenarioType _scenarioType) { m_scenarioType = _scenarioType; return *this; }

    Given& given(RunwaySituation value)
    {
        m_situation = value;
        return m_given;
    }

    MutexTestCase& whenArrivalChecksIn() { m_when = WhenWhat::ArrivalChecksIn; return *this; }
    MutexTestCase& whenDepartureChecksIn() { m_when = WhenWhat::DepartureChecksIn; return *this; }
    MutexTestCase& whenDepartureBeginsRoll() { m_when = WhenWhat::DepartureBeginsRoll; return *this; }
    MutexTestCase& whenArrivalVacates() { m_when = WhenWhat::ArrivalVacates; return *this; }
    MutexTestCase& whenTaxiingReportsHoldingShort() { m_when = WhenWhat::ReportHoldingShort; return *this; }

    MutexTestCase& whenSecondsToTouchdown(int value)
    {
        m_when = WhenWhat::SecondsToTouchdown;
        m_whenSecondsToTouchdown = value;

        return *this;
    }

    bool thenContinueInLineForDeparture(int numberInLine, bool readyForImmediateTakeoff)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::Continue;
        m_expectedEvent.numberInLine = numberInLine;
        m_expectedEvent.immediate = readyForImmediateTakeoff;

        return run();
    }

    bool thenContinueApproach(int numberInLine)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::Continue;
        m_expectedEvent.numberInLine = numberInLine;

        return run();
    }

    bool thenContinueApproachAndTraffic(
        int numberInLine,
        const TrafficAdvisory& traffic1,
        const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified })
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::Continue;
        m_expectedEvent.numberInLine = numberInLine;
        m_expectedEvent.traffic.push_back(traffic1);
        if (traffic2.type != TrafficAdvisoryType::NotSpecified)
        {
            m_expectedEvent.traffic.push_back(traffic2);
        }

        return run();
    }

    bool thenClearForTakeoff(bool immediate)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::ClearedForTakeoff;
        m_expectedEvent.immediate = immediate;

        return run();
    }

    bool thenClearForTakeoffAndTraffic(bool immediate, const TrafficAdvisory& traffic)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::ClearedForTakeoff;
        m_expectedEvent.immediate = immediate;
        m_expectedEvent.traffic.push_back(traffic);

        return run();
    }

    bool thenClearToLand()
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::ClearedToLand;
        m_expectedEvent.numberInLine = 1;

        return run();
    }

    bool thenClearToLandAndTraffic(const TrafficAdvisory& traffic)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::ClearedToLand;
        m_expectedEvent.numberInLine = 1;
        m_expectedEvent.traffic.push_back(traffic);

        return run();
    }

    bool thenHoldShort(DeclineReason reason, bool prepareForImmediate = false)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::HoldShort;
        m_expectedEvent.reason = reason;
        m_expectedEvent.immediate = prepareForImmediate;

        return run();
    }

    bool thenLUAW()
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::AuthorizedLineUpAndWait;
        return run();
    }

    bool thenLUAWAndTraffic(const TrafficAdvisory& traffic)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::AuthorizedLineUpAndWait;
        m_expectedEvent.traffic.push_back(traffic);
        return run();
    }

    bool thenLUAWAndTraffic(const TrafficAdvisory& traffic1, const TrafficAdvisory& traffic2)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::AuthorizedLineUpAndWait;
        m_expectedEvent.traffic.push_back(traffic1);
        m_expectedEvent.traffic.push_back(traffic2);

        return run();
    }

    bool thenClearToCross(bool withoutDelay)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::ClearedToCross;
        m_expectedEvent.immediate = withoutDelay;

        return run();
    }

    bool thenClearToCrossAndTraffic(bool withoutDelay, const TrafficAdvisory& traffic)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::ClearedToCross;
        m_expectedEvent.immediate = withoutDelay;
        m_expectedEvent.traffic.push_back(traffic);

        return run();
    }

    bool thenGoAround(DeclineReason reason)
    {
        m_expectedEventCount = 1;
        m_expectedEvent.type = MutexEventType::GoAround;
        m_expectedEvent.reason = reason;

        return run();
    }

    bool thenNothing()
    {
        m_expectedEventCount = 0;
        m_expectedEvent.type = MutexEventType::NotSet;
        return run();
    }

private:

    bool run()
    {
        doArrange();
        doAct();
        return doAssert();
    }

    void doArrange()
    {
        if (m_numberInLine > 1 && !(m_scenarioType == ScenarioType::Arrival && m_situation == RunwaySituation::ClearedForLanding))
        {
            addNumberOneInLine();
        }

        addFlightUnderTest();
        addOtherFlights();

        m_mutexUnderTest = make_shared<SimpleRunwayMutex>(m_host, m_runway, m_runway->getEndOrThrow("09"), m_timing, m_board);
        m_listening = true;
    }

    void doAct()
    {
        float newFeetAgl = 0;

        switch (m_when)
        {
        case WhenWhat::DepartureChecksIn:
            m_mutexUnderTest->checkInDeparture(m_departurelUnderTest.ptr, m_listener);
            break;
        case WhenWhat::DepartureBeginsRoll:
            if (m_situation == RunwaySituation::LuawAuthorized)
            {
                m_anotherTaxiing.aircraft->setLocation(GeoPoint(30.20, 45.40));
                mutexRunChecks();
                EXPECT_TRUE(m_mutexUnderTest->board().crossing.empty());
                EXPECT_TRUE(m_mutexUnderTest->board().clearedToCross.empty());
                EXPECT_EQ(m_mutexUnderTest->board().clearedToTakeoff, m_board.authorizedLuaw);
            }
            m_anotherDeparture.aircraft->setGroundSpeedKt(80);
            break;
        case WhenWhat::ArrivalChecksIn:
            m_mutexUnderTest->checkInArrival(m_arrivalUnderTest.ptr, m_listener);
            break;
        case WhenWhat::ArrivalVacates:
            m_anotherArrival.aircraft->setLocation({30.15, 45.35});
            m_anotherArrival.aircraft->setAltitude(Altitude::ground());
            m_anotherArrival.aircraft->setGroundSpeedKt(0);
            break;
        case WhenWhat::ReportHoldingShort:
            m_mutexUnderTest->checkInCrossing(m_taxiingUnderTest.ptr, m_listener);
            break;
        case WhenWhat::SecondsToTouchdown:
            adjustArrivalLocation(m_arrivalUnderTest, m_whenSecondsToTouchdown);
            //newFeetAgl = m_whenSecondsToTouchdown * abs(m_arrivalUnderTest.aircraft->verticalSpeedFpm()) / 60;
            //m_arrivalUnderTest.aircraft->setAltitude(Altitude::agl(newFeetAgl));
            break;
        }

        mutexRunChecks();
    }

    bool doAssert()
    {
        if (m_actualEventCount != m_expectedEventCount)
        {
            cout << "ASSERTION> m_actualEventCount: expected "
                 << m_expectedEventCount << ", actual " << m_actualEventCount
                 << endl;
            return false;
        }

        bool body = assertEventBody();
        bool traffic = assertEventTraffic();

        return body && traffic;
    }

    bool assertEventBody()
    {
        bool result = true;

        if (m_actualEvent.type != m_expectedEvent.type)
        {
            cout << "ASSERTION> type: expected " << (int)m_expectedEvent.type << ", actual " << (int)m_actualEvent.type << endl;
            result = false;
        }

        if (m_actualEvent.immediate != m_expectedEvent.immediate)
        {
            cout << "ASSERTION> immediate: expected " << m_expectedEvent.immediate << ", actual " << m_actualEvent.immediate << endl;
            result = false;
        }

        if (m_actualEvent.reason != m_expectedEvent.reason)
        {
            cout << "ASSERTION> reason: expected " << (int)m_expectedEvent.reason << ", actual " << (int)m_actualEvent.reason << endl;
            result = false;
        }

        if (m_actualEvent.numberInLine != m_expectedEvent.numberInLine)
        {
            cout << "ASSERTION> numberInLine: expected " << m_expectedEvent.numberInLine << ", actual " << m_actualEvent.numberInLine << endl;
            result = false;
        }

        if (m_actualEvent.traffic.size() != m_expectedEvent.traffic.size())
        {
            cout << "ASSERTION> traffic.size: expected " << m_expectedEvent.traffic.size() << ", actual " << m_actualEvent.traffic.size() << endl;
            result = false;
        }


        return result;
    }

    bool assertEventTraffic()
    {
        bool result = true;

        for (int i = 0 ; i < m_expectedEvent.traffic.size() && i < m_actualEvent.traffic.size() ; i++)
        {
            const auto& expected = m_expectedEvent.traffic.at(i);
            const auto& actual = m_actualEvent.traffic.at(i);

            if (actual.type != expected.type)
            {
                cout << "ASSERTION> traffic[" << i << "].type: expected " << (int)expected.type << ", actual " << (int)actual.type << endl;
                result = false;
            }

            if (actual.aircraftTypeIcao != expected.aircraftTypeIcao)
            {
                cout << "ASSERTION> traffic[" << i << "].aircraftTypeIcao: expected [" << expected.aircraftTypeIcao << "], actual [" << actual.aircraftTypeIcao << "]" << endl;
                result = false;
            }

            if (actual.miles != expected.miles)
            {
                cout << "ASSERTION> traffic[" << i << "].miles: expected " << expected.miles << ", actual " << actual.miles << endl;
                result = false;
            }
        }

        return result;
    }

    void mutexEventListener(const MutexEvent& event)
    {
        if (!m_listening)
        {
            return;
        }

        m_actualEventCount++;
        m_actualEvent = event;
    }

    void addFlightUnderTest()
    {
        switch (m_scenarioType)
        {
        case ScenarioType::Arrival:
            m_arrivalUnderTest = addArrivalOnFinal("B738", 123, m_secondsToTouchdown);
            if (m_checkedIn)
            {
                if (m_clearedToLand)
                {
                    m_board.flags = RWY_STATE_CLEARED_LANDING;
                    m_board.clearedToLand = make_shared<FlightStrip>(m_arrivalUnderTest.ptr, m_listener);
                }
                else
                {
                    m_board.arrivalsLine.push_back(make_shared<FlightStrip>(m_arrivalUnderTest.ptr, m_listener));
                }
            }
            break;
        case ScenarioType::Departure:
            m_departurelUnderTest = addDepartureHoldingShort("B738", 123, m_numberInLine * 50);
            if (m_checkedIn)
            {
                m_board.departuresLine.push_back(make_shared<FlightStrip>(m_departurelUnderTest.ptr, m_listener));
            }
            break;
        case ScenarioType::LUAW:
            m_departurelUnderTest = addDepartureLinedUp("B738", 123);
            if (m_checkedIn)
            {
                m_board.departuresLine.push_back(make_shared<FlightStrip>(m_departurelUnderTest.ptr, m_listener));
            }
            else
            {
                throw runtime_error("MutexTestCase: ScenarioType::LUAW cannot be used with checkedIn==false");
            }
            break;
        case ScenarioType::Crossing:
            m_taxiingUnderTest = addTaxiing("B738", 123, GeoPoint(30.15, 45.60));
            if (m_checkedIn)
            {
                m_board.crossingsLine.push_back(make_shared<FlightStrip>(m_taxiingUnderTest.ptr, m_listener));
            }
            break;
        }
    }

    void addNumberOneInLine()
    {
        switch (m_scenarioType)
        {
        case ScenarioType::Arrival:
            m_numberOneInLine = addArrivalOnFinal("A320", 102, m_secondsToTouchdown / 2);
            m_board.arrivalsLine.push_back(make_shared<FlightStrip>(m_numberOneInLine.ptr, noopListener));
            break;
        case ScenarioType::Departure:
            m_numberOneInLine = addDepartureHoldingShort("A320", 102, 50);
            m_board.departuresLine.push_back(make_shared<FlightStrip>(m_numberOneInLine.ptr, noopListener));
            break;
        case ScenarioType::LUAW:
            throw runtime_error("MutexTestCase: ScenarioType::LUAW cannot be used with numberInLine==2");
        case ScenarioType::Crossing:
            m_numberOneInLine = addTaxiing("A320", 102, GeoPoint(30.15, 45.60));
            m_board.crossingsLine.push_back(make_shared<FlightStrip>(m_numberOneInLine.ptr, noopListener));
            break;
        }
    }

    void addOtherFlights()
    {
        int callbackCount = 0;
        shared_ptr<FlightStrip> anotherTaxiingStrip;

        switch (m_situation)
        {
        case RunwaySituation::Vacated:
            if (m_scenarioType != ScenarioType::Arrival && m_secondsToTouchdown < m_timing.RWY_TIME_INFINITY)
            {
                m_anotherArrival = addArrivalOnFinal("A320", 456, m_secondsToTouchdown);
                m_board.arrivalsLine.push_back(make_shared<FlightStrip>(m_anotherArrival.ptr, noopListener));
            }
            break;
        case RunwaySituation::ClearedForLanding:
            m_board.flags = RWY_STATE_CLEARED_LANDING;
            m_anotherArrival = addArrivalOnFinal("A320", 456, 5);
            m_board.clearedToLand = make_shared<FlightStrip>(m_anotherArrival.ptr, noopListener);
            break;
        case RunwaySituation::ClearedForTakeoff:
            m_board.flags = RWY_STATE_CLEARED_TAKEOFF;
            m_anotherDeparture = addDepartureLinedUp("A320", 456);
            m_board.clearedToTakeoff = make_shared<FlightStrip>(m_anotherDeparture.ptr, noopListener);
            break;
        case RunwaySituation::LuawAuthorized:
            m_board.flags = RWY_STATE_CLEARED_CROSSING | RWY_STATE_AUTHORIZED_LUAW;
            m_anotherTaxiing = addTaxiing("A320", 103, GeoPoint(30.10, 45.30));
            anotherTaxiingStrip = make_shared<FlightStrip>(m_anotherTaxiing.ptr, noopListener);
            m_board.clearedToCross.insert(anotherTaxiingStrip);
            m_board.crossing.insert(anotherTaxiingStrip);
            m_anotherDeparture = addDepartureLinedUp("A320", 456);
            m_board.authorizedLuaw = make_shared<FlightStrip>(m_anotherDeparture.ptr, noopListener);
            break;
        case RunwaySituation::ClearedForCrossing:
            m_board.flags = RWY_STATE_CLEARED_CROSSING;
            m_anotherTaxiing = addTaxiing("A320", 456, GeoPoint(30.10, 45.30));
            m_board.clearedToCross.insert(make_shared<FlightStrip>(m_anotherTaxiing.ptr, noopListener));
            break;
        case RunwaySituation::LandedNotVacated:
            m_board.flags = RWY_STATE_CLEARED_LANDING;
            m_anotherArrival = addArrivalRollingOnRunway("A320", 456);
            m_board.clearedToLand = make_shared<FlightStrip>(m_anotherArrival.ptr, noopListener);
            break;
        case RunwaySituation::Incursion:
            m_board.flags = RWY_STATE_VACATED;
            m_anotherTaxiing = addTaxiing("A320", 456, GeoPoint(30.10, 45.30));
            break;
        }

        if (!m_arrivalUnderTest.ptr && !m_anotherArrival.ptr && m_secondsToTouchdown > 0 && m_secondsToTouchdown < 360)
        {
            m_anotherArrival = addArrivalOnFinal("A320", 789, m_secondsToTouchdown);
            m_board.arrivalsLine.push_back(make_shared<FlightStrip>(m_anotherArrival.ptr, noopListener));
        }
    }

    TestHostServices::TestFlight addArrivalOnFinal(const string& typeIcao, int flightNo, int secondsToTouchDown)
    {
        auto arrival = m_host->addIfrFlight(
            flightNo,
            "ABCD",
            "EFGH",
            GeoPoint(30.10, 44.10),
            Altitude::agl(3000),
            typeIcao);

        arrival.aircraft->setLights(Aircraft::LightBits::Landing);
        arrival.aircraft->setVerticalSpeedFpm(-1000);
        arrival.aircraft->setGroundSpeedKt(145);
        adjustArrivalLocation(arrival, secondsToTouchDown);

        return arrival;
    }

    void adjustArrivalLocation(TestHostServices::TestFlight& arrival, int secondsToTouchDown)
    {
        double verticalSpeedFpm = arrival.aircraft->verticalSpeedFpm();
        double feetAgl = secondsToTouchDown * abs(verticalSpeedFpm) / 60;
        arrival.aircraft->setAltitude(Altitude::agl(feetAgl));

        const auto& runwayEnd = getRunwayEnd();
        double distanceMeters = METERS_IN_1_NAUTICAL_MILE * secondsToTouchDown * arrival.aircraft->groundSpeedKt() / 3600;
        GeoPoint location = GeoMath::getPointAtDistance(
            runwayEnd.centerlinePoint().geo(),
            GeoMath::flipHeading(runwayEnd.heading()),
            distanceMeters);
        arrival.aircraft->setLocation(location);
    }

    TestHostServices::TestFlight addDepartureLinedUp(const string& typeIcao, int flightNo)
    {
        auto departure = m_host->addIfrFlight(
            flightNo,
            "EFGH",
            "IJKL",
            m_runway->getEndOrThrow("09").centerlinePoint().geo(),
            Altitude::ground(),
            typeIcao);

        return departure;
    }

    TestHostServices::TestFlight addArrivalRollingOnRunway(const string& typeIcao, int flightNo)
    {
        GeoPoint location = GeoMath::getPointAtDistance(
            getRunwayEnd().centerlinePoint().geo(),
            getRunwayEnd().heading(),
            300);

        auto arrival = m_host->addIfrFlight(
            flightNo,
            "IJKL",
            "EFGH",
            location,
            Altitude::ground(),
            typeIcao);

        arrival.aircraft->setGroundSpeedKt(100);
        return arrival;
    }

    TestHostServices::TestFlight addDepartureHoldingShort(const string& typeIcao, int flightNo, float metersFromCenterlinePoint)
    {
        GeoPoint location = GeoMath::getPointAtDistance(
            getRunwayEnd().centerlinePoint().geo(),
            0,
            metersFromCenterlinePoint);

        auto departure = m_host->addIfrFlight(
            flightNo,
            "EFGH",
            "IJKL",
                location,
            Altitude::ground(),
            typeIcao);

        return departure;
    }

    TestHostServices::TestFlight addTaxiing(const string& typeIcao, int flightNo, const GeoPoint& location)
    {
        auto taxiing = m_host->addIfrFlight(
            flightNo,
            "EFGH",
            "IJKL",
            location,
            Altitude::ground(),
            typeIcao);

        return taxiing;
    }

    void mutexRunChecks()
    {
        m_time = chrono::seconds(m_time.count() + 2);
        m_mutexUnderTest->progressTo(m_time);
    }

    const Runway::End& getRunwayEnd()
    {
        return m_runway->getEndOrThrow("09");
    }

public:

    static void noopListener(const MutexEvent)
    {
    }

    static bool assertMutexEventBody(const MutexEvent& expected, const MutexEvent& actual)
    {
        bool result = true;

        if (actual.type != expected.type)
        {
            cout << "ASSERTION> type: expected " << (int)expected.type << ", actual " << (int)actual.type << endl;
            result = false;
        }

        if (actual.immediate != expected.immediate)
        {
            cout << "ASSERTION> immediate: expected " << expected.immediate << ", actual " << actual.immediate << endl;
            result = false;
        }

        if (actual.reason != expected.reason)
        {
            cout << "ASSERTION> reason: expected " << (int)expected.reason << ", actual " << (int)actual.reason << endl;
            result = false;
        }

        if (actual.numberInLine != expected.numberInLine)
        {
            cout << "ASSERTION> numberInLine: expected " << expected.numberInLine << ", actual " << actual.numberInLine << endl;
            result = false;
        }

        if (actual.traffic.size() != expected.traffic.size())
        {
            cout << "ASSERTION> traffic.size: expected " << expected.traffic.size() << ", actual " << actual.traffic.size() << endl;
            result = false;
        }

        return result;
    }

    static bool assertMutexEventTraffic(const MutexEvent& expectedEvent, const MutexEvent& actualEvent)
    {
        bool result = true;

        for (int i = 0 ; i < expectedEvent.traffic.size() && i < actualEvent.traffic.size() ; i++)
        {
            const auto& expected = expectedEvent.traffic.at(i);
            const auto& actual = actualEvent.traffic.at(i);

            if (actual.type != expected.type)
            {
                cout << "ASSERTION> traffic[" << i << "].type: expected " << (int)expected.type << ", actual " << (int)actual.type << endl;
                result = false;
            }

            if (actual.aircraftTypeIcao != expected.aircraftTypeIcao)
            {
                cout << "ASSERTION> traffic[" << i << "].aircraftTypeIcao: expected [" << expected.aircraftTypeIcao << "], actual [" << actual.aircraftTypeIcao << "]" << endl;
                result = false;
            }

            if (actual.miles != expected.miles)
            {
                cout << "ASSERTION> traffic[" << i << "].miles: expected " << expected.miles << ", actual " << actual.miles << endl;
                result = false;
            }
        }

        return result;
    }

};

