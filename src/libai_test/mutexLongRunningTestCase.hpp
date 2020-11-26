//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <functional>
#include <memory>
#include <string>
#include <unordered_map>
#include <vector>
#include <chrono>
#include <iostream>
#include <sstream>

#include "gtest/gtest.h"
#include "libworld.h"
#include "clearanceTypes.hpp"
#include "simpleRunwayMutex.hpp"
#include "libworld_test.h"
#include "mutexTestCase.hpp"

using namespace std;
using namespace world;
using namespace ai;

class MutexLongRunningTestCase
{
public:
    enum class CellAction
    {
        None = 0,
        CheckInArrival = 10,
        CheckInDeparture = 20,
        CheckInCrossing = 30
    };
    struct Column
    {
        size_t index;
        TestFlight flight;
        vector<MutexEvent> actualEvents;
    };
    struct Cell
    {
        bool defined = false;
        GeoPoint location;
        Altitude altitude = Altitude::ground();
        float groundSpeedKt = 0;
        float verticalSpeedFpm = 0;
        CellAction action = CellAction::None;
        MutexEvent expectedEvent;
    };
    struct CellBuilder
    {
    private:
        MutexLongRunningTestCase& m_test;
    public:
        CellBuilder(MutexLongRunningTestCase& _test) : m_test(_test)
        {
        }
    public:
        Cell NOTHING() const
        {
            Cell cell;
            cell.defined = false;
            return cell;
        }
        Cell FIN(int secondsToThreshold) const
        {
            Cell cell;
            cell.defined = true;
            cell.groundSpeedKt = 145;
            cell.verticalSpeedFpm = -1000;
            m_test.setLocationOnFinal(cell, secondsToThreshold);
            return cell;
        }
        Cell FIN_CHK_CONT(
            int secondsToThreshold,
            int numberInLine = 1,
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = FIN(secondsToThreshold);
            cell.action = CellAction::CheckInArrival;
            cell.expectedEvent.type = MutexEventType::Continue;
            cell.expectedEvent.numberInLine = numberInLine;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell FIN_CHK_CLR(
            int secondsToThreshold,
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = FIN(secondsToThreshold);
            cell.action = CellAction::CheckInArrival;
            cell.expectedEvent.type = MutexEventType::ClearedToLand;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell FIN_CLR(
            int secondsToThreshold,
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = FIN(secondsToThreshold);
            cell.expectedEvent.type = MutexEventType::ClearedToLand;
            cell.expectedEvent.numberInLine = 1;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell TOUCHDN() const
        {
            Cell cell;
            cell.defined = true;
            cell.location = m_test.getRunwayEnd().centerlinePoint().geo();
            cell.altitude = Altitude::ground();
            cell.groundSpeedKt = 135;
            cell.verticalSpeedFpm = 0;
            return cell;
        }
        Cell LND_ROLL(int step) const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = min(45, 135 - step * 20);
            cell.location = GeoMath::getPointAtDistance(
                m_test.getRunwayEnd().
                centerlinePoint().geo(),
                m_test.getRunwayEnd().heading(),
                step * 500);
            return cell;
        }
        Cell LND_VACATED() const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = 0;
            cell.location = GeoPoint(30.15, 45.35);
            return cell;
        }
        Cell GND_GATE() const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = 0;
            cell.location = GeoPoint(30.35, 45.80);
            return cell;
        }
        Cell HS(int numberInLine) const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = 0;
            cell.location = GeoPoint(30.10 + 0.025 * numberInLine, 45.10);
            return cell;
        }
        Cell CROSSING() const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = 0;
            cell.location = GeoPoint(30.10, 45.30);
            return cell;
        }
        Cell CRS_VACATED() const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = 0;
            cell.location = GeoPoint(30.15, 45.35);
            return cell;
        }
        Cell HS_CROSS(int numberInLine) const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = 0;
            cell.location = GeoPoint(30.05 - 0.025 * numberInLine, 45.30);
            return cell;
        }
        Cell HS_CHK_CLR(
            bool immediate,
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = HS(1);
            cell.action = CellAction::CheckInDeparture;
            cell.expectedEvent.type = MutexEventType::ClearedForTakeoff;
            cell.expectedEvent.reason = DeclineReason::None;
            cell.expectedEvent.immediate = immediate;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell HS_CHK_HLD_DRLND(int numberInLine, int expectImmediate) const
        {
            Cell cell = HS(numberInLine);
            cell.action = CellAction::CheckInDeparture;
            cell.expectedEvent.type = MutexEventType::HoldShort;
            cell.expectedEvent.reason = DeclineReason::TrafficLanding;
            cell.expectedEvent.immediate = true;
            return cell;
        }
        Cell HS_CHK_HLD_DRLINE(int numberInLine, int expectImmediate) const
        {
            Cell cell = HS(numberInLine);
            cell.action = CellAction::CheckInDeparture;
            cell.expectedEvent.type = MutexEventType::HoldShort;
            cell.expectedEvent.reason = DeclineReason::WaitInLine;
            cell.expectedEvent.immediate = expectImmediate != 0;
            return cell;
        }
        Cell HS_CHK_LUAW(int numberInLine,
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = HS(numberInLine);
            cell.action = CellAction::CheckInDeparture;
            cell.expectedEvent.type = MutexEventType::AuthorizedLineUpAndWait;
            cell.expectedEvent.reason = DeclineReason::None;
            m_test.addCellTraffic(cell, traffic1, traffic2);

            return cell;
        }
        Cell HSCRS_CLR(
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = HS_CROSS(1);
            cell.action = CellAction::CheckInCrossing;
            cell.expectedEvent.type = MutexEventType::ClearedToCross;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            cell.expectedEvent.immediate = !cell.expectedEvent.traffic.empty();
            return cell;
        }
        Cell HSCRS_HLD_LND() const
        {
            Cell cell = HS_CROSS(1);
            cell.action = CellAction::CheckInCrossing;
            cell.expectedEvent.type = MutexEventType::HoldShort;
            cell.expectedEvent.reason = DeclineReason::TrafficLanding;
            return cell;
        }
        Cell HSCRS_HLD_DEP() const
        {
            Cell cell = HS_CROSS(1);
            cell.action = CellAction::CheckInCrossing;
            cell.expectedEvent.type = MutexEventType::HoldShort;
            cell.expectedEvent.reason = DeclineReason::TrafficDeparting;
            return cell;
        }
        Cell CLR_CRS(
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = HS_CROSS(1);
            cell.action = CellAction::None;
            cell.expectedEvent.type = MutexEventType::ClearedToCross;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            cell.expectedEvent.immediate = !cell.expectedEvent.traffic.empty();
            return cell;
        }
        Cell CLR_CRS(bool withoutDelay) const
        {
            Cell cell = HS_CROSS(1);
            cell.action = CellAction::None;
            cell.expectedEvent.type = MutexEventType::ClearedToCross;
            cell.expectedEvent.immediate = withoutDelay;
            return cell;
        }
        Cell LUAW(
            TrafficAdvisory traffic1 = { TrafficAdvisoryType::NotSpecified },
            TrafficAdvisory traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = HS(1);
            cell.expectedEvent.type = MutexEventType::AuthorizedLineUpAndWait;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell LINEDUP() const
        {
            Cell cell = HS(0);
            cell.location = GeoPoint(30.10, 45.10);
            return cell;
        }
        Cell LUAW_CLRTO(
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = LINEDUP();
            cell.expectedEvent.type = MutexEventType::ClearedForTakeoff;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell LUAW_CLR_ITO(
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = LUAW_CLRTO(traffic1, traffic2);
            cell.expectedEvent.immediate = true;
            return cell;
        }
        Cell HS_CLRTO(
            bool immediate,
            const TrafficAdvisory& traffic1 = { TrafficAdvisoryType::NotSpecified },
            const TrafficAdvisory& traffic2 = { TrafficAdvisoryType::NotSpecified }) const
        {
            Cell cell = HS(1);
            cell.expectedEvent.type = MutexEventType::ClearedForTakeoff;
            cell.expectedEvent.reason = DeclineReason::None;
            cell.expectedEvent.immediate = immediate;
            m_test.addCellTraffic(cell, traffic1, traffic2);
            return cell;
        }
        Cell TO_ROLL(int step) const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::ground();
            cell.verticalSpeedFpm = 0;
            cell.groundSpeedKt = max(150, 45 + step * 20);
            cell.location = GeoMath::getPointAtDistance(
                m_test.getRunwayEnd().centerlinePoint().geo(),
                m_test.getRunwayEnd().heading(),
                step * 500);
            return cell;
        }
        Cell TO_VACATED() const
        {
            Cell cell;
            cell.defined = true;
            cell.altitude = Altitude::agl(400);
            cell.verticalSpeedFpm = 2500;
            cell.groundSpeedKt = 180;
            cell.location = GeoMath::getPointAtDistance(
                m_test.getRunwayEnd().centerlinePoint().geo(),
                m_test.getRunwayEnd().heading(),
                100000);
            return cell;
        }
        Cell GO_AROUND() const
        {
            Cell cell = FIN(15);
            cell.expectedEvent.type = MutexEventType::GoAround;
            cell.expectedEvent.reason = DeclineReason::RunwayNotVacated;
            return cell;
        }
        Cell GOING_AROUND(int step) const
        {
            int stepSeconds = step * 5;

            Cell cell;
            cell.defined = true;
            cell.groundSpeedKt = 200;
            cell.verticalSpeedFpm = 2000;
            double feetAgl = stepSeconds * abs(cell.verticalSpeedFpm) / 60;
            cell.altitude = Altitude::agl(feetAgl);

            const auto& runwayEnd = m_test.getRunwayEnd();
            double distanceMeters = METERS_IN_1_NAUTICAL_MILE * stepSeconds * cell.groundSpeedKt / 3600;
            GeoPoint location = GeoMath::getPointAtDistance(
                runwayEnd.centerlinePoint().geo(),
                runwayEnd.heading(),
                distanceMeters);
            cell.location = location;

            return cell;
        }
        TrafficAdvisory TA_FIN(const string& typeIcao, int miles) const
        {
            return { TrafficAdvisoryType::TrafficOnFinal, typeIcao, miles };
        }
        TrafficAdvisory TA_HLD(const string& typeIcao) const
        {
            return { TrafficAdvisoryType::HoldingInPosition, typeIcao };
        }
        TrafficAdvisory TA_DEP(const string& typeIcao) const
        {
            return { TrafficAdvisoryType::DepartingAhead, typeIcao };
        }
        TrafficAdvisory TA_LND(const string& typeIcao, int miles) const
        {
            return { TrafficAdvisoryType::LandingAhead, typeIcao, miles };
        }
        TrafficAdvisory TA_LNDRWY(const string& typeIcao) const
        {
            return { TrafficAdvisoryType::LandedOnRunway, typeIcao };
        }
        TrafficAdvisory TA_CRSRWY() const
        {
            return { TrafficAdvisoryType::CrossingRunway, "" };
        }
    };
private:
    shared_ptr<TestHostServices> m_host;
    shared_ptr<Airport> m_airport;
    shared_ptr<Runway> m_runway;
    shared_ptr<SimpleRunwayMutex> m_mutexUnderTest;
    vector<Column> m_columns;
    unordered_map<int, vector<Cell>> m_rows;
public:
    const CellBuilder CELL;
public:
    MutexLongRunningTestCase() :
        CELL(*this)
    {
        m_host = TestHostServices::createWithWorldAirports({ createAirportEFGH });
        m_host->enableLogs(true);
        m_airport = m_host->getWorld()->getAirport("EFGH");
        m_runway = m_airport->getRunwayOrThrow("09/27");

        SimpleRunwayMutex::TimingThresholds timing;
        timing.RWY_TIME_LUAW_AUTHORIZATION_BEFORE_LANDING_MIN = 100;
        timing.RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN = 90;
        m_mutexUnderTest = make_shared<SimpleRunwayMutex>(
            m_host,
            m_runway,
            m_runway->getEndOrThrow("09"),
            timing,
            RunwayStripBoard());
    }
public:
    void COL_DEPARTURE(int flightNo, const string& typeIcao)
    {
        auto departure = m_host->addIfrFlight(
            flightNo,
            "EFGH",
            "IJKL",
            GeoPoint(0, 0),
            Altitude::ground(),
            typeIcao);
        addColumn(departure);
    }
    void COL_ARRIVAL(int flightNo, const string& typeIcao)
    {
        auto arrival = m_host->addIfrFlight(
            flightNo,
            "IJKL",
            "EFGH",
            GeoPoint(0, 0),
            Altitude::ground(),
            typeIcao);
        addColumn(arrival);
    }
    void ROW(int atSecond, const vector<Cell>& cells)
    {
        m_host->setTimeForLog(chrono::seconds(atSecond));
        //m_rows.insert({ atSecond, cells });
        if (!runSingleRow(atSecond, cells))
        {
            throw runtime_error("FAILURE at ROW[time=" + to_string(atSecond) + "]");
        }
    }
public:
//    bool run(int fromSecond = 0, int toSecond = 500, int stepSeconds = 5)
//    {
//        for (int time = fromSecond ; time <= toSecond ; time += stepSeconds)
//        {
//        }
//        return true;
//    }
private:

    bool runSingleRow(int time, const vector<Cell>& row)
    {
        //const auto& row = getValueOrThrow(m_rows, time);

        for (int index = 0 ; index < m_columns.size() ; index++)
        {
            updateCellAircraft(row.at(index), index);
        }

        for (int index = 0 ; index < m_columns.size() ; index++)
        {
            performCellAction(row.at(index), index);
        }

        m_mutexUnderTest->progressTo(chrono::seconds(time));

        for (int index = 0 ; index < m_columns.size() ; index++)
        {
            if (!assertCellEvents(row.at(index), index))
            {
                cout << "ASSERTION> TIME [" << time << "]sec "
                     << "FLIGHT[" << m_columns.at(index).flight.ptr->callSign() << "] "
                     << "EVENT ASSERT FAILED"
                     << endl;
                return false;
            }
            m_columns.at(index).actualEvents.clear();
        }

        return true;
    }

    const Runway::End& getRunwayEnd()
    {
        return m_runway->getEndOrThrow("09");
    }

    void addColumn(const TestFlight& flight)
    {
        m_columns.push_back({ m_columns.size(), flight });
    }
    void updateCellAircraft(const Cell& cell, int columnIndex)
    {
        if (!cell.defined)
        {
            return;
        }

        const auto& column = m_columns.at(columnIndex);

        column.flight.aircraft->setLocation(cell.location);
        column.flight.aircraft->setAltitude(cell.altitude);
        column.flight.aircraft->setGroundSpeedKt(cell.groundSpeedKt);
        column.flight.aircraft->setVerticalSpeedFpm(cell.verticalSpeedFpm);
    }
    void performCellAction(const Cell& cell, int columnIndex)
    {
        if (!cell.defined)
        {
            return;
        }

        const auto listener = [this, columnIndex](const MutexEvent& e) {
            m_columns.at(columnIndex).actualEvents.push_back(e);
        };

        const auto& column = m_columns.at(columnIndex);

        switch (cell.action)
        {
        case CellAction::CheckInArrival:
            m_mutexUnderTest->checkInArrival(column.flight.ptr, listener);
            break;
        case CellAction::CheckInDeparture:
            m_mutexUnderTest->checkInDeparture(column.flight.ptr, listener);
            break;
        case CellAction::CheckInCrossing:
            m_mutexUnderTest->checkInCrossing(column.flight.ptr, listener);
            break;
        }
    }
    bool assertCellEvents(const Cell& cell, int columnIndex)
    {
        const auto& column = m_columns.at(columnIndex);

        int expectedCount = cell.expectedEvent.type == MutexEventType::NotSet ? 0 : 1;

        if (column.actualEvents.size() != expectedCount)
        {
            cout << "ASSERTION> actualEvents.size(): expected " << expectedCount << ", actual " << column.actualEvents.size() << endl;
            for (int i = 0 ; i < column.actualEvents.size() ; i++)
            {
                cout << "ASSERTION> actualEvents.at(" << i << ").type = " << (int)column.actualEvents.at(i).type << endl;
            }
            if (cell.expectedEvent.type != MutexEventType::NotSet)
            {
                cout << "ASSERTION> expectedEvents.type = " << (int)cell.expectedEvent.type << endl;
            }

            return false;
        }

        if (column.actualEvents.size() == 0)
        {
            return true;
        }

        bool bodyMatch = MutexTestCase::assertMutexEventBody(cell.expectedEvent, column.actualEvents.at(0));
        bool trafficMatch = MutexTestCase::assertMutexEventTraffic(cell.expectedEvent, column.actualEvents.at(0));
        return bodyMatch && trafficMatch;
    }

    void addCellTraffic(Cell& cell, const TrafficAdvisory& traffic1, const TrafficAdvisory& traffic2)
    {
        if (traffic1.type != TrafficAdvisoryType::NotSpecified)
        {
            cell.expectedEvent.traffic.push_back(traffic1);
        }
        if (traffic2.type != TrafficAdvisoryType::NotSpecified)
        {
            cell.expectedEvent.traffic.push_back(traffic2);
        }
    }

    void setLocationOnFinal(Cell& cell, int secondsToTouchDown)
    {
        double feetAgl = secondsToTouchDown * abs(cell.verticalSpeedFpm) / 60;
        cell.altitude = Altitude::agl(feetAgl);

        const auto& runwayEnd = getRunwayEnd();
        double distanceMeters = METERS_IN_1_NAUTICAL_MILE * secondsToTouchDown * cell.groundSpeedKt / 3600;
        GeoPoint location = GeoMath::getPointAtDistance(
            runwayEnd.centerlinePoint().geo(),
            GeoMath::flipHeading(runwayEnd.heading()),
            distanceMeters);
        cell.location = location;
    }
};
