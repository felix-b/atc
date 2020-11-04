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
#include "mutexTestCase.hpp"
#include "mutexLongRunningTestCase.hpp"

using namespace std;
using namespace world;
using namespace ai;

//int milesOnFinal(int secondsToTouchDown)
//{
//    float result = 145.0f * secondsToTouchDown / 3600;
//    return (result >= 10 ? 10 : (int)result);
//}
//
//====== ARRIVALS ======

TEST(RunwayMutexTest, A__vacated_arrival_more90sec__checkIn__continue)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(111).end()
        .whenArrivalChecksIn()
        .thenContinueApproach(1));
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(111).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(2, TrafficAdvisory::landingAhead("A320", 2)));
}

TEST(RunwayMutexTest, A__vacated_arrival_1stInLine_less90sec__checkIn__clear)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(89).end()
        .whenArrivalChecksIn()
        .thenClearToLand());
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(89).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(2, TrafficAdvisory::landingAhead("A320", 1)));
}

TEST(RunwayMutexTest, A__vacated_arrival_checkedIn_notCleared_1stInLine__60sec__clear)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Arrival)
        .given(RunwaySituation::Vacated).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(59).end()
        .whenSecondsToTouchdown(59)
        .thenClearToLand());
}

//------

TEST(RunwayMutexTest, A__luaw_arrival_1stInLine_more90sec__checkIn__continue_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(1, TrafficAdvisory::holdingInPosition("A320")));
}

TEST(RunwayMutexTest, A__luaw_arrival_2ndInLine_more90sec__checkIn__continue)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Arrival)
        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(2, TrafficAdvisory::landingAhead("A320", 1), TrafficAdvisory::holdingInPosition("A320")));
}

//TEST(RunwayMutexTest, A__luaw_arrival_checkedIn_notCleared_1stInLine_more20sec__luawBeginsRoll__clear_traffic)
//{
//    MutexTestCase test;
//    EXPECT_TRUE(test.type(ScenarioType::Arrival)
//        .given(RunwaySituation::LuawAuthorized).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(21).end()
//        .whenDepartureBeginsRoll()
//        .thenClearToLandAndTraffic(TrafficAdvisory::departingAhead("A320")));
//}

//------

TEST(RunwayMutexTest, A__landing_more90sec__checkIn__continue_traffic)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForLanding).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(2, TrafficAdvisory::landingAhead("A320", 3)));
}

TEST(RunwayMutexTest, A__landing_15to60sec__arrivalVacates__clear)
{
    MutexTestCase test1, test2, test3;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForLanding).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(16).end()
        .whenArrivalVacates()
        .thenClearToLand());
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForLanding).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(89).end()
        .whenArrivalVacates()
        .thenClearToLand());
    EXPECT_TRUE(test3.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForLanding).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(91).end()
        .whenArrivalVacates()
        .thenNothing());
}

//------

TEST(RunwayMutexTest, A__takeoff_more90sec__checkIn__continue_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(2, TrafficAdvisory::landingAhead("A320", 1)));
    cout << "------" << endl;
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(1, TrafficAdvisory::departingAhead("A320")));
}

TEST(RunwayMutexTest, A__takeoff_checkedIn_notCleared_more15sec__depBeginsRoll__clear_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(16).end()
        .whenDepartureBeginsRoll()
        .thenClearToLandAndTraffic(TrafficAdvisory::departingAhead("A320")));
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(false).numberInLine(2).secsToTouchdown(16).end()
        .whenDepartureBeginsRoll()
        .thenNothing());
}

//------

TEST(RunwayMutexTest, A__crossing_more90sec__continue_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(1, TrafficAdvisory::crossingRunway()));
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(91).end()
        .whenArrivalChecksIn()
        .thenContinueApproachAndTraffic(2, TrafficAdvisory::landingAhead("A320", 1)));
}

//------

TEST(RunwayMutexTest, A__occupied_checkedIn__15sec__goAround)
{
    MutexTestCase test1, test2, test3, test4, test5, test6, test7, test8;
    EXPECT_TRUE(test1.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test2.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(true).clearedToLand(true).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test3.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test4.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(true).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test5.type(ScenarioType::Arrival)
        .given(RunwaySituation::ClearedForLanding).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test7.type(ScenarioType::Arrival)
        .given(RunwaySituation::LuawAuthorized).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test8.type(ScenarioType::Arrival)
        .given(RunwaySituation::LuawAuthorized).checkedIn(true).clearedToLand(true).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
    cout << "-------" << endl;
    EXPECT_TRUE(test6.type(ScenarioType::Arrival)
        .given(RunwaySituation::Incursion).checkedIn(true).clearedToLand(true).numberInLine(1).secsToTouchdown(16).end()
        .whenSecondsToTouchdown(14)
        .thenGoAround(DeclineReason::RunwayNotVacated));
}

//======DEPARTURES======

TEST(RunwayMutexTest, D__2ndInLine__checkIn__continue_in_line)
{
    MutexTestCase test1, test2, test3, test4, test5;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).secsToTouchdown(1000).numberInLine(2).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForLanding).checkedIn(false).clearedToLand(true).secsToTouchdown(15).numberInLine(2).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
    EXPECT_TRUE(test3.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(false).clearedToLand(false).secsToTouchdown(1000).numberInLine(2).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
    EXPECT_TRUE(test4.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).secsToTouchdown(1000).numberInLine(2).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
    EXPECT_TRUE(test5.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(false).secsToTouchdown(1000).numberInLine(2).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
}

TEST(RunwayMutexTest, D__vacated_1stInLine_more360sec__checkIn__clear)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(361).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoff(false));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(361).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoff(false));
}

TEST(RunwayMutexTest, D__vacated_1stInLine_180to360sec__checkIn__clear_traffic)
{
    MutexTestCase test1, test2, test3, test4;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(181).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(false, TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(181).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(false, TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test3.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(359).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(false, TrafficAdvisory::onFinal("A320", 10)));
    EXPECT_TRUE(test4.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(359).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(false, TrafficAdvisory::onFinal("A320", 10)));
}

TEST(RunwayMutexTest, D__vacated_1stInLine_90to180sec__checkIn__clear_immediate_traffic)
{
    MutexTestCase test1, test2, test3, test4;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(91).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(true, TrafficAdvisory::onFinal("A320", 3)));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(91).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(true, TrafficAdvisory::onFinal("A320", 3)));
    EXPECT_TRUE(test3.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(179).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(true, TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test4.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(179).end()
        .whenDepartureChecksIn()
        .thenClearForTakeoffAndTraffic(true, TrafficAdvisory::onFinal("A320", 7)));
}

TEST(RunwayMutexTest, D__vacated_1stInLine_less90sec__checkIn__hold_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(89).end()
        .whenDepartureChecksIn()
        .thenHoldShort(DeclineReason::TrafficLanding, true));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(89).end()
        .whenDepartureChecksIn()
        .thenHoldShort(DeclineReason::TrafficLanding, true));
}

//------

TEST(RunwayMutexTest, D__landing__checkIn__hold_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForLanding).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(360).end()
        .whenDepartureChecksIn()
        .thenHoldShort(DeclineReason::TrafficLanding, true));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForLanding).checkedIn(false).clearedToLand(true).numberInLine(2).secsToTouchdown(360).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
}

TEST(RunwayMutexTest, D__luaw__checkIn__hold_waitInLine)
{
    MutexTestCase test1, test2, test3;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(360).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(360).end()
        .whenDepartureChecksIn()
        .thenHoldShort(DeclineReason::WaitInLine, true));
    EXPECT_TRUE(test3.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(90).end()
        .whenDepartureChecksIn()
        .thenHoldShort(DeclineReason::WaitInLine, true));
}

TEST(RunwayMutexTest, D__takeoff__checkIn__hold_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(false).clearedToLand(false).numberInLine(2).secsToTouchdown(360).end()
        .whenDepartureChecksIn()
        .thenContinueInLineForDeparture(2, true));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(89).end()
        .whenDepartureChecksIn()
        .thenHoldShort(DeclineReason::WaitInLine, true));
}

TEST(RunwayMutexTest, D__crossing_more360sec__checkIn__luaw_trafficTaxiing)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(361).end()
        .whenDepartureChecksIn()
        .thenLUAWAndTraffic(TrafficAdvisory::crossingRunway()));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(361).end()
        .whenDepartureChecksIn()
        .thenLUAWAndTraffic(TrafficAdvisory::crossingRunway()));
}

TEST(RunwayMutexTest, D__crossing_180to360sec__checkIn__luaw_trafficTaxiing_trafficFinal)
{
    MutexTestCase test1, test2, test3, test4;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(181).end()
        .whenDepartureChecksIn()
        .thenLUAWAndTraffic(TrafficAdvisory::crossingRunway(), TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(181).end()
        .whenDepartureChecksIn()
        .thenLUAWAndTraffic(TrafficAdvisory::crossingRunway(), TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test3.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(359).end()
        .whenDepartureChecksIn()
        .thenLUAWAndTraffic(TrafficAdvisory::crossingRunway(), TrafficAdvisory::onFinal("A320", 10)));
    EXPECT_TRUE(test4.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForCrossing).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(359).end()
        .whenDepartureChecksIn()
        .thenLUAWAndTraffic(TrafficAdvisory::crossingRunway(), TrafficAdvisory::onFinal("A320", 10)));
}

TEST(RunwayMutexTest, D__luaw_checkedIn_1stInLine_more360sec__luawBeginsRoll__luaw_trafficDeparting)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(true).clearedToLand(false).numberInLine(1).secsToTouchdown(361).end()
        .whenDepartureBeginsRoll()
        .thenLUAW());
}

TEST(RunwayMutexTest, D__takeoff_checkedIn_1stInLine_more360sec__departureBeginsRoll__luaw_trafficDeparting)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(false).secsToTouchdown(361).end()
        .whenDepartureBeginsRoll()
        .thenLUAW());
}

TEST(RunwayMutexTest, D__luaw_checkedIn_1stInLine_180to360sec__luawBeginsRoll__luaw_trafficDeparting_trafficFinal)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(true).clearedToLand(false).secsToTouchdown(191).end()
        .whenDepartureBeginsRoll()
        .thenLUAWAndTraffic(TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::LuawAuthorized).checkedIn(true).clearedToLand(false).secsToTouchdown(359).end()
        .whenDepartureBeginsRoll()
        .thenLUAWAndTraffic(TrafficAdvisory::onFinal("A320", 10)));
}

TEST(RunwayMutexTest, D__takeoff_checkedIn_1stInLine_180to360sec__departureBeginsRoll__luaw_trafficDeparting_trafficFinal)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(false).secsToTouchdown(191).end()
        .whenDepartureBeginsRoll()
        .thenLUAWAndTraffic(TrafficAdvisory::onFinal("A320", 7)));
    EXPECT_TRUE(test2.type(ScenarioType::Departure)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(true).clearedToLand(false).secsToTouchdown(359).end()
        .whenDepartureBeginsRoll()
        .thenLUAWAndTraffic(TrafficAdvisory::onFinal("A320", 10)));
}

//======CROSSING======

TEST(RunwayMutexTest, C__vacated_1stInLine_more360sec__holdingShort__clear)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Crossing)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(361).end()
        .whenTaxiingReportsHoldingShort()
        .thenClearToCross(false));
}

TEST(RunwayMutexTest, C__vacated_1stInLine_more360sec__holdingShort__clear_withoutDelay_traffic)
{
    MutexTestCase test1, test2;
    EXPECT_TRUE(test1.type(ScenarioType::Crossing)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(91).end()
        .whenTaxiingReportsHoldingShort()
        .thenClearToCrossAndTraffic(true, TrafficAdvisory::onFinal("A320", 3)));
    EXPECT_TRUE(test2.type(ScenarioType::Crossing)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(359).end()
        .whenTaxiingReportsHoldingShort()
        .thenClearToCrossAndTraffic(true, TrafficAdvisory::onFinal("A320", 10)));
}

TEST(RunwayMutexTest, C__vacated_1stInLine_less90sec__holdingShort__hold_traffic)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Crossing)
        .given(RunwaySituation::Vacated).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(89).end()
        .whenTaxiingReportsHoldingShort()
        .thenHoldShort(DeclineReason::TrafficLanding));
}

TEST(RunwayMutexTest, C__landing_1stInLine__holdingShort__hold_traffic)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Crossing)
        .given(RunwaySituation::ClearedForLanding).checkedIn(false).clearedToLand(true).numberInLine(1).secsToTouchdown(89).end()
        .whenTaxiingReportsHoldingShort()
        .thenHoldShort(DeclineReason::TrafficLanding));
}

TEST(RunwayMutexTest, C__takeoff_1stInLine__holdingShort__hold_traffic)
{
    MutexTestCase test;
    EXPECT_TRUE(test.type(ScenarioType::Crossing)
        .given(RunwaySituation::ClearedForTakeoff).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(360).end()
        .whenTaxiingReportsHoldingShort()
        .thenHoldShort(DeclineReason::TrafficDeparting));
}

//TEST(RunwayMutexTest, C__luaw_1stInLine__holdingShort__hold_traffic)
//{
//    MutexTestCase test;
//    EXPECT_TRUE(test.type(ScenarioType::Crossing)
//        .given(RunwaySituation::LuawAuthorized).checkedIn(false).clearedToLand(false).numberInLine(1).secsToTouchdown(360).end()
//        .whenTaxiingReportsHoldingShort()
//        .thenHoldShort(DeclineReason::TrafficDeparting));
//}
