// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <chrono>
#include <cmath>
#include <algorithm>
#include <sstream>
#include <iomanip>
#include "libworld.h"
#include "worldHelper.hpp"
#include "basicManeuverTypes.hpp"
#include "maneuverFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "aiAircraft.hpp"
#include "libai.hpp"

using namespace std;
using namespace world;

namespace ai
{
    class AIPilot : public Pilot
    {
    private:
        WorldHelper m_helper;
        shared_ptr<ManeuverFactory> m_maneuverFactory;
        shared_ptr<IntentFactory> m_intentFactory;
        ManeuverFactory& M;
        IntentFactory& I;
        shared_ptr<Airport> m_departureAirport;
        shared_ptr<FlightPlan> m_flightPlan;
        shared_ptr<AIAircraft> m_aircraft;
        int m_departureTowerKhz = 0;
        int m_departureKhz = 0;
        int m_arrivalGroundKhz = 0;
        uint64_t m_lastReceivedIntentId = 0;
        DeclineReason m_lastDeclineReason = DeclineReason::None;
        bool m_wasTakeoffClearanceReadBack = false;
        bool m_continueApproach = false;
        int m_departureNumberInLine = 0;
        bool m_prepareForImmediateTakeoff = false;
        bool m_holdShortForDeparture = false;
        chrono::microseconds m_linedUpTimestamp = chrono::microseconds(0);
        bool m_stoppedBeforeTakeoff = false;
    public:
        AIPilot(
            shared_ptr<HostServices> _host, 
            int _id, 
            Actor::Gender _gender, 
            shared_ptr<Flight> _flight, 
            shared_ptr<ManeuverFactory> _maneuverFactory,
            shared_ptr<IntentFactory> _intentFactory
        ) : Pilot(_host, _id, _gender, _flight),
            m_helper(_host),
            m_maneuverFactory(_maneuverFactory),
            M(*_maneuverFactory),
            m_intentFactory(_intentFactory),
            I(*_intentFactory),
            m_aircraft(dynamic_pointer_cast<AIAircraft>(_flight->aircraft()))
        {
            //_host->writeLog("AIPilot::AIPilot() - enter");

            m_flightPlan = _flight->plan();
            m_departureAirport = _host->getWorld()->getAirport(_flight->plan()->departureAirportIcao());

            aircraft()->onCommTransmission([this](shared_ptr<Intent> intent) {
                handleCommTransmission(intent);
            });

            //_host->writeLog("AIPilot::AIPilot() - exit");
        }
    public:
        shared_ptr<Maneuver> getFlightCycle() override
        {
            return maneuverFlightCycle();
        }
        shared_ptr<Maneuver> getFinalToGate(const Runway::End& landingRunway) override
        {
            return maneuverFinalToGate(landingRunway);
        }
        void progressTo(chrono::microseconds timestamp) override 
        {
            //TODO
        }
        string getStatusString() const override
        {
            return "<twrkhz=" + to_string(m_departureTowerKhz) + ">";
        }
    private:
        void handleCommTransmission(shared_ptr<Intent> intent)
        {
            if (intent->direction() == Intent::Direction::ControllerToPilot && intent->subjectFlight() == flight())
            {
                host()->writeLog("TRANSMISSION HANDLED BY PILOT [%s]", flight()->callSign().c_str(), intent->code());
                shared_ptr<Clearance> newClearance;
                m_lastReceivedIntentId = intent->id();

                switch (intent->code())
                {
                case DeliveryIfrClearanceReplyIntent::IntentCode:
                    newClearance = dynamic_pointer_cast<DeliveryIfrClearanceReplyIntent>(intent)->clearance();
                    break;
                case DeliveryIfrClearanceReadbackCorrectIntent::IntentCode:
                    {
                        auto readbackCorrect = dynamic_pointer_cast<DeliveryIfrClearanceReadbackCorrectIntent>(intent);
                        auto clearance = readbackCorrect->clearance();
                        // auto delivery = clearance->header().issuedBy;
                        // auto intentFactory = host()->services().get<IntentFactory>();
                        // auto handoffReadback = intentFactory->pilotHandoffReadback(flight(), delivery, readbackCorrect->groundKhz());
                        //delivery->frequency()->enqueueTransmission(handoffReadback);
                        clearance->setReadbackCorrect();
                    }
                    break;
                case GroundPushAndStartReplyIntent::IntentCode:
                    newClearance = dynamic_pointer_cast<GroundPushAndStartReplyIntent>(intent)->approval();
                    break;
                case GroundDepartureTaxiReplyIntent::IntentCode:
                    newClearance = dynamic_pointer_cast<GroundDepartureTaxiReplyIntent>(intent)->clearance();
                    break;
                case GroundHoldShortRunwayIntent::IntentCode:
                    m_lastDeclineReason = dynamic_pointer_cast<GroundHoldShortRunwayIntent>(intent)->reason();
                    break;
                case GroundRunwayCrossClearanceIntent::IntentCode:
                    newClearance = dynamic_pointer_cast<GroundRunwayCrossClearanceIntent>(intent)->clearance();
                    break;
                case GroundSwitchToTowerIntent::IntentCode:
                    m_departureTowerKhz = dynamic_pointer_cast<GroundSwitchToTowerIntent>(intent)->towerKhz();
                    intent->subjectControl()->frequency()->enqueuePushToTalk(
                        chrono::milliseconds(150),
                        I.pilotHandoffReadback(flight(), intent->subjectControl(), m_departureTowerKhz, intent->id()));
                    break;
                case TowerDepartureCheckInReplyIntent::IntentCode:
                    m_departureNumberInLine = dynamic_pointer_cast<TowerDepartureCheckInReplyIntent>(intent)->numberInLine();
                    m_prepareForImmediateTakeoff = dynamic_pointer_cast<TowerDepartureCheckInReplyIntent>(intent)->prepareForImmediateTakeoff();
                    break;
                case TowerDepartureHoldShortIntent::IntentCode:
                    m_holdShortForDeparture = true;
                    m_lastDeclineReason = dynamic_pointer_cast<TowerDepartureHoldShortIntent>(intent)->reason();
                    break;
                case TowerLineUpAndWaitIntent::IntentCode:
                    newClearance = dynamic_pointer_cast<TowerLineUpAndWaitIntent>(intent)->approval();
                    break;
                case TowerClearedForTakeoffIntent::IntentCode:
                    {
                        auto takeOffClearance = dynamic_pointer_cast<TowerClearedForTakeoffIntent>(intent)->clearance();
                        m_departureKhz = takeOffClearance->departureKhz();
                        newClearance = takeOffClearance;
                    }
                    break;
                case TowerContinueApproachIntent::IntentCode:
                    m_continueApproach = true;
                    break;
                case TowerClearedForLandingIntent::IntentCode:
                    {
                        auto landingClearance = dynamic_pointer_cast<TowerClearedForLandingIntent>(intent)->clearance();
                        m_arrivalGroundKhz = landingClearance->groundKhz();
                        flight()->setArrivalRunway(landingClearance->runway());
                        m_continueApproach = false;
                        newClearance = landingClearance;
                    }
                    break;
                case TowerGoAroundIntent::IntentCode:
                    flight()->removeClearance(Clearance::Type::LandingClearance);
                    m_continueApproach = false;
                    newClearance = dynamic_pointer_cast<TowerGoAroundIntent>(intent)->request();
                    break;
                case GroundArrivalTaxiReplyIntent::IntentCode:
                    newClearance = dynamic_pointer_cast<GroundArrivalTaxiReplyIntent>(intent)->clearance();
                    break;
                }

                if (newClearance)
                {
                    flight()->addClearance(newClearance);
                }
            }
        }

        static string normalizeWaypointName(string value)
        {
            value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
                return !isalnum(c);
            }), value.end());

            transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
                return static_cast<char>(toupper(c));
            });

            return value;
        }

        static bool tryFindWaypointLocation(const shared_ptr<FlightPlan>& plan, const string& waypointName, GeoPoint& location)
        {
            if (!plan || waypointName.empty())
            {
                return false;
            }

            const string normalizedWaypointName = normalizeWaypointName(waypointName);
            for (const auto& routeWaypoint : plan->knownWaypoints())
            {
                if (routeWaypoint.location == GeoPoint::empty)
                {
                    continue;
                }

                if (normalizeWaypointName(routeWaypoint.name) == normalizedWaypointName)
                {
                    location = routeWaypoint.location;
                    return true;
                }
            }

            return false;
        }

        int procedureLegDurationSeconds(FlightPlan::LegType legType) const
        {
            switch (legType)
            {
            case FlightPlan::LegType::TakeOff:
                return 30;
            case FlightPlan::LegType::Sid:
                return 75;
            case FlightPlan::LegType::EnRoute:
                return 120;
            case FlightPlan::LegType::Star:
                return 90;
            case FlightPlan::LegType::Approach:
                return 60;
            case FlightPlan::LegType::Landing:
                return 30;
            case FlightPlan::LegType::GoAround:
                return 45;
            default:
                return 20;
            }
        }

        int procedureLegDurationSeconds(
            FlightPlan::LegType legType,
            const shared_ptr<FlightPlan::Leg>& leg,
            const GeoPoint& fromPoint,
            const GeoPoint& toPoint) const
        {
            const float targetGroundSpeed = max(1.0f, procedureLegGroundSpeed(legType, leg));
            const double speedMetersPerSecond = targetGroundSpeed * METERS_IN_1_NAUTICAL_MILE / 3600.0;
            if (speedMetersPerSecond <= 0.0)
            {
                return procedureLegDurationSeconds(legType);
            }

            const double distanceMeters = GeoMath::getDistanceMeters(fromPoint, toPoint);
            const int estimatedSeconds = static_cast<int>(distanceMeters / speedMetersPerSecond + 0.5);
            return max(10, estimatedSeconds);
        }

        float procedureLegVerticalSpeed(FlightPlan::LegType legType, const shared_ptr<FlightPlan::Leg>& leg) const
        {
            const auto& performance = m_aircraft->performanceProfile();

            switch (legType)
            {
            case FlightPlan::LegType::TakeOff:
                return 1500.0f;
            case FlightPlan::LegType::Sid:
                return max(800.0f, leg->targetAltitude() > 0 ? leg->targetAltitude() / 10.0f : 1500.0f);
            case FlightPlan::LegType::EnRoute:
                return 0.0f;
            case FlightPlan::LegType::Star:
                return -max(700.0f, leg->targetAltitude() > 0 ? leg->targetAltitude() / 10.0f : 1200.0f);
            case FlightPlan::LegType::Approach:
                return -max(500.0f, leg->targetAltitude() > 0 ? leg->targetAltitude() / 12.0f : 800.0f);
            case FlightPlan::LegType::Landing:
                return -performance.descentRateFpm;
            case FlightPlan::LegType::GoAround:
                return 1200.0f;
            default:
                return 0.0f;
            }
        }

        float procedureLegGroundSpeed(FlightPlan::LegType legType, const shared_ptr<FlightPlan::Leg>& leg) const
        {
            const auto& performance = m_aircraft->performanceProfile();
            const auto constrainedSpeed = [leg](float defaultSpeed) {
                return leg->targetSpeed() > 0.0f
                    ? min(defaultSpeed, leg->targetSpeed())
                    : defaultSpeed;
            };

            const float approachSpeed = max(90.0f, performance.approachSpeedKt);

            switch (legType)
            {
            case FlightPlan::LegType::TakeOff:
                return max(120.0f, leg->targetSpeed() > 0.0f ? leg->targetSpeed() : approachSpeed + 20.0f);
            case FlightPlan::LegType::Sid:
                return constrainedSpeed(max(150.0f, min(230.0f, approachSpeed + 50.0f)));
            case FlightPlan::LegType::EnRoute:
                return constrainedSpeed(max(200.0f, min(280.0f, approachSpeed + 100.0f)));
            case FlightPlan::LegType::Star:
                return constrainedSpeed(max(130.0f, min(210.0f, approachSpeed + 30.0f)));
            case FlightPlan::LegType::Approach:
                return constrainedSpeed(approachSpeed);
            case FlightPlan::LegType::Landing:
                return constrainedSpeed(max(70.0f, approachSpeed - 20.0f));
            case FlightPlan::LegType::GoAround:
                return max(140.0f, leg->targetSpeed() > 0.0f ? leg->targetSpeed() : approachSpeed + 20.0f);
            default:
                return leg->targetSpeed();
            }
        }

        bool hasProcedureLeg(FlightPlan::LegType legType) const
        {
            const auto& legs = m_flightPlan->legs();
            return any_of(legs.begin(), legs.end(), [legType](const shared_ptr<FlightPlan::Leg>& leg) {
                return leg && leg->type() == legType;
            });
        }

        shared_ptr<Maneuver> maneuverProcedureLeg(FlightPlan::LegType legType, Flight::Phase phase, const string& stepId)
        {
            auto cursor = flight()->planCursor();
            if (!cursor)
            {
                return M.instantAction([]{});
            }

            return DeferredManeuver::create(Maneuver::Type::Flight, stepId, [this, cursor, legType, phase, stepId]() {
                if (!cursor->activateNextLegOfType(legType))
                {
                    host()->writeLog(
                        "AIPILO|procedure leg step[%s] skipped: flight[%s] has no remaining leg of type[%d]",
                        stepId.c_str(),
                        flight()->callSign().c_str(),
                        static_cast<int>(legType));
                    return M.instantAction([]{});
                }

                vector<shared_ptr<Maneuver>> steps;
                do
                {
                    auto leg = cursor->activeLeg();
                    if (!leg || leg->type() != legType)
                    {
                        break;
                    }

                    const int legIndex = cursor->activeLegIndex();
                    const float targetGroundSpeed = procedureLegGroundSpeed(legType, leg);
                    const float targetVerticalSpeed = procedureLegVerticalSpeed(legType, leg);
                    GeoPoint fromPoint;
                    GeoPoint toPoint;
                    const bool hasFromPoint = tryFindWaypointLocation(m_flightPlan, leg->fromNavaid(), fromPoint);
                    const bool hasToPoint = tryFindWaypointLocation(m_flightPlan, leg->toNavaid(), toPoint);
                    const bool hasGeometricLeg = hasFromPoint && hasToPoint;
                    // Use aircraft's current location for initial heading calculation
                    // This ensures smooth transitions between leg types (e.g., STAR->Approach)
                    // where the aircraft may not be exactly at the leg's from-point
                    const float targetHeading = hasGeometricLeg
                        ? GeoMath::getHeadingFromPoints(m_aircraft->location(), toPoint)
                        : m_aircraft->attitude().heading();
                    const int legDurationSeconds = hasGeometricLeg
                        ? procedureLegDurationSeconds(legType, leg, fromPoint, toPoint)
                        : procedureLegDurationSeconds(legType);
                    const string legStepId = stepId + "_" + to_string(legIndex);

                    // Capture leg-specific data by value to avoid loop variable issues
                    const shared_ptr<FlightPlan::Leg> currentLeg = leg;
                    const GeoPoint currentFromPoint = fromPoint;
                    const GeoPoint currentToPoint = toPoint;
                    const bool currentHasGeometricLeg = hasGeometricLeg;
                    const float currentTargetGroundSpeed = targetGroundSpeed;
                    const float currentTargetVerticalSpeed = targetVerticalSpeed;
                    const float currentTargetHeading = targetHeading;
                    const int currentLegDurationSeconds = legDurationSeconds;
                    const string currentLegStepId = legStepId;
                    
                    // Detailed logging for arrival procedure legs
                    if (legType == FlightPlan::LegType::Star || legType == FlightPlan::LegType::Approach)
                    {
                        host()->writeLog(
                            "AIPILO|ARR_PROC flight[%s] leg[%d] type[%s] from[%s|%.4f,%.4f] to[%s|%.4f,%.4f] hasGeo[%d]",
                            flight()->callSign().c_str(),
                            legIndex,
                            (legType == FlightPlan::LegType::Star ? "STAR" : "APPROACH"),
                            leg->fromNavaid().c_str(),
                            hasFromPoint ? fromPoint.latitude : 0.0,
                            hasFromPoint ? fromPoint.longitude : 0.0,
                            leg->toNavaid().c_str(),
                            hasToPoint ? toPoint.latitude : 0.0,
                            hasToPoint ? toPoint.longitude : 0.0,
                            (int)hasGeometricLeg);
                    }

                    // Parallel maneuver: track toward waypoint while waiting to reach it
                    vector<shared_ptr<Maneuver>> legSteps;

                    // 1. Initial setup - set speed and initial heading
                    legSteps.push_back(M.instantAction([this, cursor, currentLeg, legType, phase, currentTargetGroundSpeed, currentTargetVerticalSpeed, currentToPoint, currentHasGeometricLeg, currentLegStepId]() {
                        host()->writeLog(
                            "AIPILO|procedure leg step[%s] flight[%s] legIndex[%d] type[%d] from[%s] to[%s] targetAlt[%f] targetSpd[%f]",
                            currentLegStepId.c_str(),
                            flight()->callSign().c_str(),
                            cursor->activeLegIndex(),
                            static_cast<int>(legType),
                            currentLeg->fromNavaid().c_str(),
                            currentLeg->toNavaid().c_str(),
                            currentLeg->targetAltitude(),
                            currentLeg->targetSpeed());

                        flight()->setPhase(phase);
                        if (currentTargetGroundSpeed > 0)
                        {
                            m_aircraft->setGroundSpeedKt(currentTargetGroundSpeed);
                        }
                        if (currentTargetVerticalSpeed != 0)
                        {
                            m_aircraft->setVerticalSpeedFpm(currentTargetVerticalSpeed);
                        }
                        if (currentHasGeometricLeg)
                        {
                            // Calculate heading at execution time, not setup time
                            // Aircraft position may have changed since maneuver was created
                            float initialHeading = GeoMath::getHeadingFromPoints(
                                m_aircraft->location(), currentToPoint);
                            m_aircraft->setAttitude(m_aircraft->attitude().withHeading(initialHeading));
                        }
                    }));

                    // 2. Continuously track toward the target waypoint with heading updates
                    if (currentHasGeometricLeg)
                    {
                        // Create a maneuver that continuously updates heading toward target
                        // and waits until the waypoint is reached
                        // Use a repeating instant action sequence instead of AnimationManeuver
                        // because AnimationManeuver has a fixed duration and stops updating
                        const GeoPoint legStartPoint = currentFromPoint;
                        const GeoPoint targetPoint = currentToPoint;
                        const string targetName = currentLeg->toNavaid();
                        
                        const float legTargetAltitude = currentLeg->targetAltitude();
                        const float legTargetSpeed = currentLeg->targetSpeed();

                        legSteps.push_back(M.await(Maneuver::Type::Unspecified, "track_waypoint",
                            [this, legStartPoint, targetPoint, targetName, legTargetAltitude, legTargetSpeed]() {
                                bool reached = hasReachedWaypoint(targetPoint, legStartPoint);
                                if (reached)
                                {
                                    host()->writeLog(
                                        "AIPILO|Flight[%s] reached waypoint[%s]",
                                        flight()->callSign().c_str(),
                                        targetName.c_str());
                                    return true;
                                }

                                // Calculate heading to target waypoint while enroute.
                                float headingToTarget = GeoMath::getHeadingFromPoints(
                                    m_aircraft->location(), targetPoint);
                                float currentHeading = m_aircraft->attitude().heading();
                                float turnDegrees = GeoMath::getTurnDegrees(
                                    currentHeading, headingToTarget);
                                float headingDiff = fabs(turnDegrees);

                                // Log significant heading changes for debugging.
                                if (headingDiff > 45.0f)
                                {
                                    host()->writeLog(
                                        "AIPILO|Flight[%s] large heading change to waypoint[%s]: "
                                        "current=%.1f target=%.1f turn=%.1f (possible overshoot)",
                                        flight()->callSign().c_str(),
                                        targetName.c_str(),
                                        currentHeading,
                                        headingToTarget,
                                        turnDegrees);
                                }

                                // Only update if off by more than 3 degrees (avoid jitter).
                                if (headingDiff > 3.0f)
                                {
                                    // Banked turn model: smaller per-tick heading change and visible bank.
                                    float limitedTurn = std::max(-4.0f, std::min(4.0f, turnDegrees));
                                    float newHeading = GeoMath::addTurnToHeading(currentHeading, limitedTurn);
                                    const float targetBank = std::max(6.0f, std::min(25.0f, std::abs(limitedTurn) * 4.5f));
                                    const float bankSign = (limitedTurn >= 0.0f ? 1.0f : -1.0f);
                                    const float roll = targetBank * bankSign;
                                    m_aircraft->setAttitude(
                                        m_aircraft->attitude().withHeading(newHeading).withRoll(roll));
                                }
                                else if (std::abs(m_aircraft->attitude().roll()) > 0.5)
                                {
                                    // Smoothly unwind residual bank near rollout.
                                    const float currentRoll = static_cast<float>(m_aircraft->attitude().roll());
                                    const float unwindStep = (currentRoll > 0.0f ? -3.0f : 3.0f);
                                    float nextRoll = currentRoll + unwindStep;
                                    if ((currentRoll > 0.0f && nextRoll < 0.0f) || (currentRoll < 0.0f && nextRoll > 0.0f))
                                    {
                                        nextRoll = 0.0f;
                                    }
                                    m_aircraft->setAttitude(m_aircraft->attitude().withRoll(nextRoll));
                                }

                                if (legTargetSpeed > 0.0f)
                                {
                                    // Apply speed constraint - aircraft must MATCH target speed, not just stay below it
                                    // This ensures aircraft accelerate to the target speed as well as decelerate
                                    if (fabs(m_aircraft->groundSpeedKt() - legTargetSpeed) > 0.5)
                                    {
                                        m_aircraft->setGroundSpeedKt(legTargetSpeed);
                                    }
                                }

                                if (legTargetAltitude > 0.0f)
                                {
                                    const Altitude altitude = m_aircraft->altitude();
                                    double currentAltitudeMslFeet = altitude.feet();
                                    if (altitude.type() == Altitude::Type::AGL)
                                    {
                                        currentAltitudeMslFeet += host()->queryTerrainElevationAt(m_aircraft->location());
                                    }

                                    const double altitudeDeltaFeet = static_cast<double>(legTargetAltitude) - currentAltitudeMslFeet;
                                    const double distanceMeters = max(1.0, static_cast<double>(GeoMath::getDistanceMeters(m_aircraft->location(), targetPoint)));
                                    const double groundSpeedKt = max(80.0, m_aircraft->groundSpeedKt());
                                    const double groundSpeedMps = groundSpeedKt * 1852.0 / 3600.0;
                                    const double timeToGoSeconds = max(20.0, distanceMeters / groundSpeedMps);
                                    const double requiredVsFpm = (altitudeDeltaFeet / timeToGoSeconds) * 60.0;

                                    const auto performance = m_aircraft->performanceProfile();
                                    const double maxClimbFpm = max(900.0, static_cast<double>(performance.descentRateFpm) * 2.5);
                                    const double maxDescentFpm = -max(700.0, static_cast<double>(performance.descentRateFpm) * 2.3);
                                    const double constrainedVsFpm = min(maxClimbFpm, max(maxDescentFpm, requiredVsFpm));

                                    if (fabs(m_aircraft->verticalSpeedFpm() - constrainedVsFpm) > 50.0)
                                    {
                                        m_aircraft->setVerticalSpeedFpm(constrainedVsFpm);
                                    }
                                }

                                return false;
                            }));
                    }
                    else
                    {
                        // No geometric info, fall back to time-based
                        legSteps.push_back(M.delay(chrono::seconds(currentLegDurationSeconds)));
                    }

                    steps.push_back(M.sequence(Maneuver::Type::Flight, currentLegStepId + "_track", legSteps));
                }
                while (cursor->activateNextLeg() && cursor->activeLeg() && cursor->activeLeg()->type() == legType);

                if (steps.empty())
                {
                    return M.instantAction([]{});
                }

                return M.sequence(Maneuver::Type::Flight, stepId, steps);
            });
        }

        shared_ptr<Maneuver> maneuverFlightCycle()
        {
            time_t startTime = flight()->plan()->departureTime() - 180;
            time_t secondsBeforeStart = startTime - host()->getWorld()->currentTime();

            auto result = M.sequence(Maneuver::Type::Flight, "flight_cycle", {
                M.delay(chrono::seconds(secondsBeforeStart)),
                maneuverDepartureAwaitIfrClearance(),
                maneuverDepartureAwaitPushback(),
                maneuverDeparturePushbackAndStart(),
                maneuverDepartureAwaitTaxi(),
                M.parallel(Maneuver::Type::Unspecified, "", {
                    maneuverDepartureTaxi(),
                    maneuverAwaitTakeOffClearance(),
                }),
                maneuverLineup(),
                maneuverTakeoff(),
                maneuverProcedureLeg(FlightPlan::LegType::Sid, Flight::Phase::Departure, "sid_leg"),
                maneuverProcedureLeg(FlightPlan::LegType::EnRoute, Flight::Phase::EnRoute, "enroute_leg"),
                M.instantAction([this]() {
                    auto landingRunwayEnd = m_helper.getLandingRunwayEnd(flight());
                    host()->writeLog(
                        "AIPILO|flight[%s] entering final approach runway[%s]",
                        flight()->callSign().c_str(),
                        landingRunwayEnd.name().c_str());
                    m_aircraft->setOnFinal(landingRunwayEnd);
                }, "enter_final")
            });

            return result;
        }

        shared_ptr<Maneuver> maneuverFinalToGate(const Runway::End& landingRunway)
        {
            flight()->setArrivalRunway(landingRunway.name());

            vector<shared_ptr<Maneuver>> steps;
            if (hasProcedureLeg(FlightPlan::LegType::Star))
            {
                steps.push_back(maneuverProcedureLeg(FlightPlan::LegType::Star, Flight::Phase::Arrival, "star_leg"));
            }
            if (hasProcedureLeg(FlightPlan::LegType::Approach))
            {
                steps.push_back(maneuverProcedureLeg(FlightPlan::LegType::Approach, Flight::Phase::Arrival, "approach_leg"));
            }

            steps.push_back(maneuverFinal());
            steps.push_back(M.deferred([this, landingRunway]() {
                if (flight()->tryFindClearance<GoAroundRequest>(Clearance::Type::GoAroundRequest))
                {
                    return maneuverGoAround(landingRunway);
                }

                return M.sequence(Maneuver::Type::ArrivalApproach, "land_to_gate", {
                    maneuverLanding(),
                    M.deferred([this]() {
                        return maneuverArrivalTaxiToGate();
                    })
                });
            }));

            return M.sequence(Maneuver::Type::ArrivalApproach, "final_to_gate", steps);
        }

        shared_ptr<Maneuver> maneuverApproachRetryToGate(const Runway::End& landingRunway)
        {
            flight()->setArrivalRunway(landingRunway.name());

            vector<shared_ptr<Maneuver>> steps;
            if (hasProcedureLeg(FlightPlan::LegType::Approach))
            {
                steps.push_back(maneuverProcedureLeg(FlightPlan::LegType::Approach, Flight::Phase::Arrival, "approach_retry_leg"));
            }

            steps.push_back(maneuverFinal());
            steps.push_back(M.deferred([this, landingRunway]() {
                if (flight()->tryFindClearance<GoAroundRequest>(Clearance::Type::GoAroundRequest))
                {
                    return maneuverGoAround(landingRunway);
                }

                return M.sequence(Maneuver::Type::ArrivalApproach, "land_after_retry", {
                    maneuverLanding(),
                    M.deferred([this]() {
                        return maneuverArrivalTaxiToGate();
                    })
                });
            }));

            return M.sequence(Maneuver::Type::ArrivalApproach, "approach_retry_to_gate", steps);
        }

        shared_ptr<Maneuver> maneuverFinal()
        {
            auto flaps15GearDown = M.sequence(Maneuver::Type::Unspecified, "flaps_15_gear_down", {
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "flaps",
                    0,
                    0.15,
                    chrono::seconds(7),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setFlapState(value);
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "gear",
                    0,
                    1.0,
                    chrono::seconds(10),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setGearState(value);
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "pitch",
                    -2.0,
                    0.0,
                    chrono::seconds(3),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress;
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                ))
            });
            auto flaps40 = M.parallel(Maneuver::Type::Unspecified, "flaps_40", {
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "flaps",
                    0.15,
                    0.4,
                    chrono::seconds(10),
                    [](const double &from, const double &to, double progress, double &value) {
                        value = from + (to - from) * progress;
                    },
                    [=](const double &value, double progress) {
                        m_aircraft->setFlapState(value);
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "pitch",
                    0.0,
                    1.5,
                    chrono::seconds(5),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress;
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                ))
            });

            auto landingPoint = m_helper.getLandingPoint(flight());
            auto tower = m_helper.getArrivalTower(flight(), landingPoint);
            string landingRunwayName = m_helper.getLandingRunwayEnd(flight()).name();
            
            return M.parallel(Maneuver::Type::ArrivalApproach, "final", {
                M.sequence(Maneuver::Type::Unspecified, "aviate", {
                    M.delay(chrono::seconds(3)),
                    flaps15GearDown,
                    M.delay(chrono::seconds(12)),
                    flaps40,
                }),
                M.sequence(Maneuver::Type::Unspecified, "communicate", {
                    M.tuneComRadio(flight(), tower->frequency()),
                    M.transmitIntent(flight(), I.pilotReportFinal(flight()), "twr_report_final"),
                    M.await(Maneuver::Type::Unspecified, "await_twr_reply", [this]{
                        return (
                            m_continueApproach ||
                            flight()->tryFindClearance<LandingClearance>(Clearance::Type::LandingClearance) ||
                            flight()->tryFindClearance<GoAroundRequest>(Clearance::Type::GoAroundRequest));
                    }),
                    M.deferred([this,tower,landingRunwayName]{
                        if (m_continueApproach)
                        {
                            m_continueApproach = false;
                            return M.transmitIntent(
                                flight(),
                                I.pilotContinueApproachReadback(flight(), tower, landingRunwayName, m_lastReceivedIntentId));
                        }
                        return M.instantAction([]{});
                    }),
                    M.await(Maneuver::Type::Unspecified, "await_landing_or_go_around", [this]{
                        return (
                            flight()->tryFindClearance<LandingClearance>(Clearance::Type::LandingClearance) ||
                            flight()->tryFindClearance<GoAroundRequest>(Clearance::Type::GoAroundRequest));
                    }),
                    M.deferred([=](){
                        auto goAround = flight()->tryFindClearance<GoAroundRequest>(Clearance::Type::GoAroundRequest);
                        if (goAround)
                        {
                            return M.transmitIntent(
                                flight(),
                                I.pilotGoAroundReadback(goAround, m_lastReceivedIntentId),
                                "readback_go_around");
                        }

                        auto clearance = flight()->findClearanceOrThrow<LandingClearance>(Clearance::Type::LandingClearance);
                        auto readback = I.pilotLandingClearanceReadback(flight(), clearance, m_lastReceivedIntentId);
                        return M.transmitIntent(flight(), readback, "readback_landing_clrnc");
                    })
                })
            });
        }

        shared_ptr<Maneuver> maneuverGoAround(const Runway::End& landingRunway)
        {
            const auto performance = m_aircraft->performanceProfile();

            vector<shared_ptr<Maneuver>> steps = {
                M.instantAction([this]() {
                    flight()->removeClearance(Clearance::Type::LandingClearance);
                    m_continueApproach = false;
                }),
                M.parallel(Maneuver::Type::ArrivalGoAround, "go_around_reconfigure", {
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "go_around_pitch",
                        m_aircraft->attitude().pitch(),
                        10.0,
                        chrono::seconds(4),
                        [](const double& from, const double& to, double progress, double& value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double& value, double) {
                            m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                        }
                    )),
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "go_around_speed",
                        max(120.0, m_aircraft->groundSpeedKt()),
                        max(160.0f, performance.approachSpeedKt + 20.0f),
                        chrono::seconds(8),
                        [](const double& from, const double& to, double progress, double& value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double& value, double) {
                            m_aircraft->setGroundSpeedKt(value);
                        }
                    )),
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "go_around_climb",
                        max(0.0, m_aircraft->verticalSpeedFpm()),
                        max(1200.0f, performance.descentRateFpm * 1.5f),
                        chrono::seconds(6),
                        [](const double& from, const double& to, double progress, double& value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double& value, double) {
                            m_aircraft->setVerticalSpeedFpm(value);
                        }
                    )),
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "go_around_gear",
                        m_aircraft->gearState(),
                        0.0,
                        chrono::seconds(6),
                        [](const double& from, const double& to, double progress, double& value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double& value, double) {
                            m_aircraft->setGearState(value);
                        }
                    )),
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "go_around_flaps",
                        m_aircraft->flapState(),
                        0.15,
                        chrono::seconds(8),
                        [](const double& from, const double& to, double progress, double& value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double& value, double) {
                            m_aircraft->setFlapState(value);
                        }
                    ))
                })
            };

            if (hasProcedureLeg(FlightPlan::LegType::GoAround))
            {
                steps.push_back(maneuverProcedureLeg(FlightPlan::LegType::GoAround, Flight::Phase::Arrival, "go_around_leg"));
            }
            else
            {
                steps.push_back(M.sequence(Maneuver::Type::ArrivalGoAround, "go_around_fallback", {
                    M.instantAction([this, landingRunway]() {
                        m_aircraft->setAttitude(m_aircraft->attitude().withHeading(landingRunway.heading()));
                    }),
                    M.delay(chrono::seconds(20))
                }));
            }

            steps.push_back(M.instantAction([this, landingRunway]() {
                if (auto cursor = flight()->planCursor())
                {
                    cursor->reset();
                }
                flight()->removeClearance(Clearance::Type::GoAroundRequest);
                m_aircraft->prepareForApproachRetry(landingRunway);
            }));
            steps.push_back(M.deferred([this, landingRunway]() {
                return maneuverApproachRetryToGate(landingRunway);
            }));

            return M.sequence(Maneuver::Type::ArrivalGoAround, "go_around", steps);
        }

        shared_ptr<Maneuver> maneuverLanding()
        {
            auto landingProcedure = maneuverProcedureLeg(FlightPlan::LegType::Landing, Flight::Phase::Arrival, "landing_leg");
            const auto performance = m_aircraft->performanceProfile();

            auto preFlare = M.parallel(Maneuver::Type::ArrivalLanding, "pre_flare", {
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "", 
                    1.5,
                    3.0,
                    chrono::milliseconds(3500),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "", 
                    -performance.descentRateFpm,
                    -performance.descentRateFpm * 0.5,
                    chrono::milliseconds(3500),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setVerticalSpeedFpm(value);
                    }
                )),
            });
            auto flare = M.parallel(Maneuver::Type::ArrivalLanding, "flare", {
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "pitch",
                    3.0,
                    5.5,
                    chrono::seconds(3),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "gndspd",
                    performance.approachSpeedKt,
                    std::max(30.0f, performance.approachSpeedKt - 10.0f),
                    chrono::seconds(3),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress;
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setGroundSpeedKt(value);
                    }
                )),
                M.sequence(Maneuver::Type::Unspecified, "", {
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "verspd_1",
                        -performance.descentRateFpm * 0.5,
                        -performance.descentRateFpm * 0.05,
                        chrono::seconds(2),
                        [](const double &from, const double &to, double progress, double &value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double &value, double progress) {
                            m_aircraft->setVerticalSpeedFpm(value);
                        }
                    )),
                    shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "verspd_2",
                        -50,
                        -100,
                        chrono::seconds(1),
                        [](const double &from, const double &to, double progress, double &value) {
                            value = from + (to - from) * progress;
                        },
                        [=](const double &value, double progress) {
                            m_aircraft->setVerticalSpeedFpm(value);
                        }
                    ))
                })
            });
            auto logTouchDown = M.instantAction([this](){
                host()->writeLog(
                    "AIPILO|TOUCHDOWN flight[%s] at [%f,%f] vertical-speed[%f]fpm ground-speed[%f]kt pitch[%f]deg",
                    flight()->callSign().c_str(),
                    m_aircraft->location().latitude,
                    m_aircraft->location().longitude,
                    m_aircraft->verticalSpeedFpm(),
                    m_aircraft->groundSpeedKt(),
                    m_aircraft->attitude().pitch());
            });
            auto touchDownAndDeccelerate = M.parallel(Maneuver::Type::ArrivalLandingRoll, "touch_and_decel", {
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "spdbrk",
                    0,
                    1.0,
                    chrono::seconds(1),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setSpoilerState(value);
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "pitch",
                    5.5,
                    0.0,
                    chrono::seconds(6),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                )),
                shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "gndspd",
                    std::max(30.0f, performance.approachSpeedKt - 10.0f),
                    std::max(30.0f, performance.approachSpeedKt * 0.2f),
                    chrono::seconds(20),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setGroundSpeedKt(value);
                    }
                )),
            });

            return M.sequence(Maneuver::Type::ArrivalLanding, "landing", {
                landingProcedure,
                M.await(Maneuver::Type::Unspecified, "await_55_agl", [=]() {
                    return (m_aircraft->altitude().type() == Altitude::Type::AGL && m_aircraft->altitude().feet() <= 55);
                }),
                preFlare,
                M.await(Maneuver::Type::Unspecified, "await_20_agl", [=]() {
                    return (m_aircraft->altitude().type() == Altitude::Type::AGL && m_aircraft->altitude().feet() <= 20);
                }),
                flare,
                M.await(Maneuver::Type::Unspecified, "await_touch_down", [=]() {
                    return (m_aircraft->altitude().type() == Altitude::Type::Ground);
                }),
                logTouchDown,
                touchDownAndDeccelerate
            });
        }

        shared_ptr<Maneuver> maneuverArrivalTaxiToGate()
        {
            WorldHelper helper(host());
            auto aircraft = flight()->aircraft();
            auto airport = helper.getArrivalAirport(flight());
            auto runway = airport->getRunwayOrThrow(m_flightPlan->arrivalRunway());
            const auto& runwayEnd = runway->getEndOrThrow(m_flightPlan->arrivalRunway());
            auto gate = airport->getParkingStandOrThrow(m_flightPlan->arrivalGate());
            shared_ptr<TaxiEdge> exitFirstEdge;
            shared_ptr<TaxiEdge> exitLastEdge;
            string exitName;
            bool needsGroundTaxiClearance = false;

            const auto safeCreateExitManeuver = [
                this, &exitFirstEdge, &exitLastEdge, &exitName, &needsGroundTaxiClearance, airport, runway, gate, aircraft, runwayEnd
            ]{
                shared_ptr<Maneuver> result;
                host()->writeLog(
                    "AIPILO|Flight[%s] landed rwy[%s] will look for exit path",
                    flight()->callSign().c_str(), runwayEnd.name().c_str());

                auto taxiPath = airport->taxiNet()->tryFindExitPathFromRunway(
                    host(),
                    runway,
                    runwayEnd,
                    gate,
                    aircraft->location());

                if (taxiPath)
                {
                    exitFirstEdge = taxiPath->edges[0];
                    exitLastEdge = taxiPath->edges[taxiPath->edges.size() - 1];
                    exitName = taxiPath->toHumanFriendlyString();

                    host()->writeLog(
                        "AIPILO|Flight[%s] arrival gate[%s] will exit runway[%s] via[%s]",
                        flight()->callSign().c_str(),
                        gate->name().c_str(),
                        runwayEnd.name().c_str(),
                        exitName.c_str());

                    needsGroundTaxiClearance = true;
                    result = M.taxiByPath(flight(), taxiPath, ManeuverFactory::TaxiType::HighSpeed);
                }
                else
                {
                    host()->writeLog(
                        "AIPILO|Flight[%s] arrival exit path from runway NOT FOUND! will teleport to gate[%s]",
                        flight()->callSign().c_str(),
                        gate->name().c_str());
                    result = M.instantAction([=]{
                        aircraft->park(gate);
                    });
                }

                return result;
            };

            auto flapsZero = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                "flaps_0",
                0.4,
                0,
                chrono::seconds(30),
                [](const double& from, const double& to, double progress, double& value) {
                    value = from + (to - from) * progress;
                },
                [=](const double& value, double progress) {
                    m_aircraft->setFlapState(value);
                }
            ));
            auto speedBrakeDown = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                "speedbrk_down",
                1.0,
                0,
                chrono::seconds(1),
                [](const double& from, const double& to, double progress, double& value) {
                    value = from + (to - from) * progress;
                },
                [=](const double& value, double progress) {
                    m_aircraft->setSpoilerState(value);
                }
            ));
            auto exitRunway = safeCreateExitManeuver();
            auto logVacatedActive = M.instantAction([this](){
                host()->writeLog(
                    "AIPILO|VACATEDACTIVE flight[%s] at [%f,%f] ground-speed[%f]kt",
                    flight()->callSign().c_str(),
                    m_aircraft->location().latitude,
                    m_aircraft->location().longitude,
                    m_aircraft->groundSpeedKt());
            });
            auto taxiLights = M.instantAction([=] {
                m_aircraft->setLights(Aircraft::LightBits::BeaconTaxiNav);
            });
            auto lightsOff = M.instantAction([=] {
                m_aircraft->setLights(Aircraft::LightBits::None);
                m_aircraft->setGearState(1.0f);
                m_aircraft->setFlapState(0.0f);
                m_aircraft->setSpoilerState(0.0f);
                m_aircraft->setGroundSpeedKt(0);
                m_aircraft->setVerticalSpeedFpm(0);
                flight()->setPhase(Flight::Phase::TurnAround);
            });

            const auto onHoldingShort = [=](shared_ptr<TaxiEdge> holdShortEdge) {
                if (holdShortEdge->activeZones().arrival.runwaysMask() == runway->maskBit())
                {
                    //TODO: this is a hack. Instead check if the runway is ahead or behind
                    return M.instantAction([]{}); // don't hold short of runway we've just landed on (and which is supposed to be behind us).
                }
                return maneuverAwaitCrossRunway(airport, holdShortEdge);
            };

            return M.sequence(Maneuver::Type::ArrivalTaxi, "arrival_taxi", {
                M.instantAction([this] {
                    m_aircraft->setGroundSpeedKt(0);
                }),
                M.parallel(Maneuver::Type::Unspecified, "", {
                    flapsZero,
                    speedBrakeDown,
                    M.deferred([=]() {
                        if (!needsGroundTaxiClearance)
                        {
                            return M.instantAction([]{});
                        }

                        return M.sequence(Maneuver::Type::Unspecified, "arrival_ground_contact", {
                            M.await(Maneuver::Type::Unspecified, "", [this,exitFirstEdge]{
                                return (!exitFirstEdge) || hasReachedWaypoint(exitFirstEdge->node2()->location().geo(), 300.0f);
                            }),
                            M.delay(chrono::seconds(3)),
                            M.tuneComRadio(flight(), airport->groundAt(aircraft->location())->frequency()),
                            M.transmitIntent(flight(), I.pilotArrivalCheckInWithGround(
                                flight(), runwayEnd.name(), exitName, exitLastEdge
                            )),
                            M.awaitClearance(flight(), Clearance::Type::ArrivalTaxiClearance, "await_taxi_clrnc"),
                            M.deferred([this]{
                                auto clearance = flight()->findClearanceOrThrow<ArrivalTaxiClearance>(Clearance::Type::ArrivalTaxiClearance);
                                return M.transmitIntent(flight(), I.pilotArrivalTaxiReadback(flight(), m_lastReceivedIntentId));
                            }),
                        });
                    }),
                    M.sequence(Maneuver::Type::Unspecified, "", {
                       exitRunway,
                       logVacatedActive,
                       taxiLights,
                       M.deferred([=]() {
                           if (!needsGroundTaxiClearance)
                           {
                               return M.instantAction([]{});
                           }

                           return M.sequence(Maneuver::Type::Unspecified, "arrival_taxi_to_gate", {
                               M.awaitClearance(flight(), Clearance::Type::ArrivalTaxiClearance),
                               M.parallel(Maneuver::Type::Unspecified, "", {
                                   M.deferred([=]{
                                       auto clearance = flight()->findClearanceOrThrow<ArrivalTaxiClearance>(Clearance::Type::ArrivalTaxiClearance);
                                       if (!clearance->taxiPath())
                                       {
                                           host()->writeLog(
                                               "AIPILO|Flight[%s] arrival taxi path to gate[%s] not found, parking directly",
                                               flight()->callSign().c_str(),
                                               gate->name().c_str());
                                           return M.instantAction([=]{
                                               aircraft->park(gate);
                                           });
                                       }

                                       return M.taxiByPath(
                                           flight(),
                                           clearance->taxiPath(),
                                           ManeuverFactory::TaxiType::Normal,
                                           onHoldingShort);
                                   }),
                               }),
                           });
                       }),
                   }),
                }),
                M.delay(chrono::seconds(5)),
                lightsOff
            });
        }

        shared_ptr<Maneuver> maneuverDepartureAwaitIfrClearance()
        {
            auto intentFactory = host()->services().get<IntentFactory>();
            auto ifrClearanceRequest = intentFactory->pilotIfrClearanceRequest(flight());
            auto airport = host()->getWorld()->getAirport(flight()->plan()->departureAirportIcao());
            auto clearanceDelivery = airport->clearanceDeliveryAt(flight()->aircraft()->location());
            auto ground = airport->groundAt(flight()->aircraft()->location());

            return M.sequence(Maneuver::Type::DepartureAwaitIfrClearance, "await_ifr_clr", {
                M.tuneComRadio(flight(), clearanceDelivery->frequency()),
                M.transmitIntent(flight(), ifrClearanceRequest),
                M.awaitClearance(flight(), Clearance::Type::IfrClearance),
                M.deferred([=]() {
                    host()->writeLog("ifrClearanceReadback deferred factory");
                    auto ifrClearanceReadback = intentFactory->pilotIfrClearanceReadback(flight(), m_lastReceivedIntentId);
                    return M.transmitIntent(flight(), ifrClearanceReadback);
                }),
                M.await(Maneuver::Type::Unspecified, "", [=](){
                    return flight()->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance)->readbackCorrect();
                }),
                M.deferred([=]() {
                    host()->writeLog("deliveryToGroundHandoffReadback deferred factory");
                    auto handoffReadback = intentFactory->pilotHandoffReadback(
                        flight(), clearanceDelivery, ground->frequency()->khz(), m_lastReceivedIntentId);
                    return M.transmitIntent(flight(), handoffReadback);
                }),
                M.deferred([=]() {
                    return M.delay(chrono::seconds(5));
                })
            });
        }

        shared_ptr<Maneuver> maneuverDepartureAwaitPushback()
        {
            auto intentFactory = host()->services().get<IntentFactory>();
            auto pushAndStartRequest = intentFactory->pilotPushAndStartRequest(flight());
            auto airport = host()->getWorld()->getAirport(flight()->plan()->departureAirportIcao());
            auto ground = airport->groundAt(flight()->aircraft()->location());

            return M.sequence(Maneuver::Type::DepartureAwaitPushback, "await_pushback", {
                M.tuneComRadio(flight(), ground->frequency()),
                M.transmitIntent(flight(), pushAndStartRequest),
                M.awaitClearance(flight(), Clearance::Type::PushAndStartApproval),
                M.deferred([=]() {
                    host()->writeLog("pushAndStartReadback deferred factory");
                    auto pushAndStartReadback =
                        intentFactory->pilotPushAndStartReadback(flight(), ground, m_lastReceivedIntentId);
                    return M.transmitIntent(flight(), pushAndStartReadback);
                }),
                M.deferred([=]() {
                    return M.delay(chrono::seconds(5));
                })
            });
        }
        
        shared_ptr<Maneuver> maneuverDeparturePushbackAndStart()
        {
            const auto createPushbackTaxiPath = [this](const vector<GeoPoint>& pushbackPath) {
                vector<shared_ptr<TaxiEdge>> pushbackEdges;
                for (int i = 0 ; i < pushbackPath.size() - 1 ; i++)
                {
                    host()->writeLog(
                        "PUSHBACK-PATH (%.9f,%.9f)->(%.9f,%.9f)", 
                        pushbackPath[i].latitude,
                        pushbackPath[i].longitude,
                        pushbackPath[i+1].latitude,
                        pushbackPath[i+1].longitude);
                    pushbackEdges.push_back(shared_ptr<TaxiEdge>(new TaxiEdge(
                        UniPoint::fromGeo(host(), pushbackPath[i]),
                        UniPoint::fromGeo(host(), pushbackPath[i+1])
                    )));
                }
                auto taxiPath = shared_ptr<TaxiPath>(new TaxiPath(
                    pushbackEdges[0]->node1(),
                    pushbackEdges[pushbackEdges.size()-1]->node2(),
                    pushbackEdges
                ));
                return taxiPath;
            };

            return DeferredManeuver::create(Maneuver::Type::DeparturePushbackAndStart, "push_and_start", [=]() {
                auto flightPlan = flight()->plan();
                auto approval = flight()->findClearanceOrThrow<PushAndStartApproval>(Clearance::Type::PushAndStartApproval);
                auto taxiPath = createPushbackTaxiPath(approval->pushbackPath());

                vector<shared_ptr<Maneuver>> maneuverSteps = {
                    M.instantAction([=]{
                        flight()->setPhase(Flight::Phase::Departure);
                    }),
                    M.switchLights(flight(), Aircraft::LightBits::Beacon),
                    M.delay(chrono::seconds(10)),
                    M.switchLights(flight(), Aircraft::LightBits::BeaconNav),
                    M.delay(chrono::seconds(5)),
                    M.taxiByPath(flight(), taxiPath, ManeuverFactory::TaxiType::Pushback)
                };

                return shared_ptr<Maneuver>(new SequentialManeuver(
                    Maneuver::Type::DeparturePushbackAndStart, 
                    "",
                    maneuverSteps
                )); 
            });
        }
        
        shared_ptr<Maneuver> maneuverDepartureAwaitTaxi()
        {
            auto flapsToTakeoffPosition = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                "flaps_t_o",
                0.0,
                0.15,
                chrono::seconds(3),
                [](const double& from, const double& to, double progress, double& value) {
                    value = from + (to - from) * progress; 
                },
                [this](const double& value, double progress) {
                    m_aircraft->setFlapState(value);
                }
            ));

            auto intentFactory = host()->services().get<IntentFactory>();
            auto taxiRequest = intentFactory->pilotDepartureTaxiRequest(flight());

            return M.sequence(Maneuver::Type::DepartureAwaitTaxi, "await_departure_taxi", {
                M.delay(chrono::seconds(5)),
                flapsToTakeoffPosition,
                M.delay(chrono::seconds(5)),
                M.transmitIntent(flight(), taxiRequest),
                M.awaitClearance(flight(), Clearance::Type::DepartureTaxiClearance),
                M.deferred([=]() {
                    host()->writeLog("taxiReadback deferred factory");
                    auto taxiReadback = intentFactory->pilotDepartureTaxiReadback(flight(), m_lastReceivedIntentId);
                    return M.transmitIntent(flight(), taxiReadback);
                }),
                M.deferred([=]() {
                    return M.delay(chrono::seconds(10));
                }),
            });
        }

        shared_ptr<Maneuver> maneuverDepartureTaxi()
        {
            const auto addLineupEdges = [=](shared_ptr<DepartureTaxiClearance> clearance) {
                auto taxiPath = clearance->taxiPath();
                if (!taxiPath)
                {
                    host()->writeLog("AIPILO|Flight[%s] addLineupEdges: null taxiPath, cannot add lineup edges", flight()->callSign().c_str());
                    return;
                }

                auto runway = m_departureAirport->getRunwayOrThrow(clearance->departureRunway());
                const auto& runwayEnd = runway->getEndOrThrow(clearance->departureRunway());

                // Use actual runway centerline point; taxiPath terminal node can still be hold-short.
                auto centerlinePoint = runwayEnd.centerlinePoint().geo();
                auto lineupPoint1 = GeoMath::getPointAtDistance(
                    centerlinePoint,
                    runwayEnd.heading(),
                    30);
                auto lineupPoint2 = GeoMath::getPointAtDistance(
                    centerlinePoint,
                    runwayEnd.heading(),
                    60);

                host()->writeLog(
                    "AIPILO|Flight[%s] addLineupEdges: path had %zu edges, adding lineup points from [%f,%f]",
                    flight()->callSign().c_str(),
                    taxiPath->edges.size(),
                    centerlinePoint.latitude,
                    centerlinePoint.longitude);

                taxiPath->appendEdgeTo(UniPoint::fromGeo(host(), lineupPoint1));
                taxiPath->appendEdgeTo(UniPoint::fromGeo(host(), lineupPoint2));
            };

            const auto findFinalDepartureHoldShortEdge = [this](shared_ptr<TaxiPath> taxiPath, shared_ptr<Runway> departureRunway) {
                shared_ptr<TaxiEdge> result;

                if (!taxiPath || !departureRunway)
                {
                    host()->writeLog("AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: null path or runway", flight()->callSign().c_str());
                    return result;
                }

                host()->writeLog(
                    "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: checking %zu edges for runway[%s]",
                    flight()->callSign().c_str(),
                    taxiPath->edges.size(),
                    departureRunway->end1().name().c_str());

                for (int i = 0; i < (int)taxiPath->edges.size(); i++)
                {
                    auto edge = taxiPath->edges[i];
                    auto prevEdge = i > 0 ? taxiPath->edges[i - 1] : nullptr;
                    auto nextEdge = (i < (int)taxiPath->edges.size() - 1) ? taxiPath->edges[i + 1] : nullptr;

                    bool entersActiveZone = edge->activeZones().hasAny() && (!prevEdge || !prevEdge->activeZones().hasAny());
                    bool hasDepRunway = edge->activeZones().departue.has(departureRunway);
                    
                    // The TRUE final hold short is the edge that:
                    // 1. Enters the departure runway's active zone, AND
                    // 2. The NEXT edge is the actual runway edge (Type::Runway)
                    // This prevents taxiway crossings (which have active zones but lead to more taxiway)
                    // from being incorrectly detected as the final hold short
                    bool leadsToRunwayEdge = nextEdge && nextEdge->type() == TaxiEdge::Type::Runway;

                    if (entersActiveZone && hasDepRunway)
                    {
                        host()->writeLog(
                            "AIPILO|Flight[%s] edge[%d|%s|type=%d] enters departure active zone, leadsToRunway=%d",
                            flight()->callSign().c_str(),
                            edge->id(),
                            edge->name().c_str(),
                            static_cast<int>(edge->type()),
                            leadsToRunwayEdge);

                        // Return the edge that enters the departure runway's active zone 
                        // AND leads directly to the runway edge
                        // (the actual hold short point where pilot should contact tower)
                        if (!result && leadsToRunwayEdge)
                        {
                            result = edge;
                        }
                    }
                }

                // Fallback: if no edge leads directly to runway, find the last edge 
                // that enters the departure runway's active zone
                if (!result)
                {
                    for (int i = (int)taxiPath->edges.size() - 1; i >= 0; i--)
                    {
                        auto edge = taxiPath->edges[i];
                        auto prevEdge = i > 0 ? taxiPath->edges[i - 1] : nullptr;
                        
                        bool entersActiveZone = edge->activeZones().hasAny() && (!prevEdge || !prevEdge->activeZones().hasAny());
                        bool hasDepRunway = edge->activeZones().departue.has(departureRunway);
                        
                        if (entersActiveZone && hasDepRunway)
                        {
                            result = edge;
                            host()->writeLog(
                                "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: fallback selected edge[%d|%s]",
                                flight()->callSign().c_str(),
                                edge->id(),
                                edge->name().c_str());
                            break;
                        }
                    }
                }

                // Fallback for airports without active zones (like LECO):
                // Look for HoldShort type edges that lead to runway edges
                if (!result)
                {
                    for (int i = 0; i < (int)taxiPath->edges.size(); i++)
                    {
                        auto edge = taxiPath->edges[i];
                        auto nextEdge = (i < (int)taxiPath->edges.size() - 1) ? taxiPath->edges[i + 1] : nullptr;
                        
                        // Check if this is a HoldShort edge leading to a runway edge
                        if (edge && edge->type() == TaxiEdge::Type::HoldShort && 
                            nextEdge && nextEdge->type() == TaxiEdge::Type::Runway)
                        {
                            result = edge;
                            host()->writeLog(
                                "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: HoldShort edge fallback selected edge[%d|%s]",
                                flight()->callSign().c_str(),
                                edge->id(),
                                edge->name().c_str());
                            break;
                        }
                    }
                }

                // Final fallback for synthetic lineup extensions (edge id < 0):
                // use the last real edge before synthetic extension as hold-short edge.
                if (!result)
                {
                    for (int i = 1; i < static_cast<int>(taxiPath->edges.size()); ++i)
                    {
                        auto edge = taxiPath->edges[i];
                        auto prevEdge = taxiPath->edges[i - 1];
                        if (edge && prevEdge && edge->id() < 0 && prevEdge->id() >= 0)
                        {
                            result = prevEdge;
                            host()->writeLog(
                                "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: synthetic fallback selected edge[%d|%s] before synthetic edge[%d|%s]",
                                flight()->callSign().c_str(),
                                prevEdge->id(),
                                prevEdge->name().c_str(),
                                edge->id(),
                                edge->name().c_str());
                            break;
                        }
                    }
                }

                // Last resort: use the last edge before the runway edge, regardless of type
                if (!result)
                {
                    for (int i = 0; i < (int)taxiPath->edges.size() - 1; i++)
                    {
                        auto edge = taxiPath->edges[i];
                        auto nextEdge = taxiPath->edges[i + 1];
                        
                        if (edge && nextEdge && nextEdge->type() == TaxiEdge::Type::Runway)
                        {
                            result = edge;
                            host()->writeLog(
                                "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: last resort fallback selected edge[%d|%s]",
                                flight()->callSign().c_str(),
                                edge->id(),
                                edge->name().c_str());
                            break;
                        }
                    }
                }

                if (result)
                {
                    host()->writeLog(
                        "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: selected edge[%d|%s]",
                        flight()->callSign().c_str(),
                        result->id(),
                        result->name().c_str());
                }
                else
                {
                    host()->writeLog(
                        "AIPILO|Flight[%s] findFinalDepartureHoldShortEdge: NO hold short edge found!",
                        flight()->callSign().c_str());
                }

                return result;
            };

            return DeferredManeuver::create(Maneuver::Type::DepartureTaxi, "departure_taxi", [=]() {
                auto clearance = flight()->findClearanceOrThrow<DepartureTaxiClearance>(Clearance::Type::DepartureTaxiClearance);
                // Note: addLineupEdges removed - lineup now happens AFTER clearance is received

                auto departureRunway = m_departureAirport->getRunwayOrThrow(m_flightPlan->departureRunway());
                auto finalDepartureHoldShortEdge = findFinalDepartureHoldShortEdge(clearance->taxiPath(), departureRunway);

                const auto onHoldingShort = [=](shared_ptr<TaxiEdge> holdShortEdge) {
                    bool isFinalFallbackEdge = finalDepartureHoldShortEdge && holdShortEdge == finalDepartureHoldShortEdge;
                    bool isHoldingShortDepartureRunway =
                        holdShortEdge->activeZones().departue.has(departureRunway) ||
                        isFinalFallbackEdge;

                    host()->writeLog(
                        "AIPILO|Flight[%s] onHoldingShort edge[%d|%s] hasDepZone[%d] finalHoldShortEdge[%p]",
                        flight()->callSign().c_str(),
                        holdShortEdge->id(),
                        holdShortEdge->name().c_str(),
                        isHoldingShortDepartureRunway,
                        finalDepartureHoldShortEdge.get());

                    if (isHoldingShortDepartureRunway)
                    {
                        bool isFinalDepartureHoldShort = (!finalDepartureHoldShortEdge || holdShortEdge == finalDepartureHoldShortEdge);
                        if (isFinalDepartureHoldShort)
                        {
                            host()->writeLog(
                                "AIPILO|Flight[%s] calling maneuverDepartureAwaitLineup for edge[%d]",
                                flight()->callSign().c_str(),
                                holdShortEdge->id());
                            return maneuverDepartureAwaitLineup(m_flightPlan->departureRunway(), holdShortEdge);
                        }

                        host()->writeLog(
                            "AIPILO|Flight[%s] skipping early departure hold-short edge[%d], proceeding to final hold-short",
                            flight()->callSign().c_str(),
                            holdShortEdge->id());
                        return M.instantAction([]{});
                    }

                    host()->writeLog(
                        "AIPILO|Flight[%s] treating edge[%d] as runway cross (not departure runway)",
                        flight()->callSign().c_str(),
                        holdShortEdge->id());
                    return maneuverAwaitCrossRunway(m_departureAirport, holdShortEdge);
                };

                vector<shared_ptr<Maneuver>> steps;
                steps.push_back(M.delay(chrono::seconds(10)));
                steps.push_back(M.switchLights(flight(), Aircraft::LightBits::BeaconTaxi));
                steps.push_back(M.delay(chrono::seconds(5)));
                steps.push_back(M.taxiByPath(
                    flight(),
                    clearance->taxiPath(),
                    ManeuverFactory::TaxiType::Normal,
                    onHoldingShort));

                return shared_ptr<Maneuver>(new SequentialManeuver(
                    Maneuver::Type::DepartureTaxi,
                    "",
                    steps
                ));
            });
        }

        shared_ptr<Maneuver> maneuverDepartureAwaitLineup(const string& runwayName, shared_ptr<TaxiEdge> holdShortEdge)
        {
            return M.sequence(Maneuver::Type::Unspecified, "await_lineup", {
                M.deferred([=]() {
                    if (m_departureTowerKhz == 0)
                    {
                        return M.transmitIntent(flight(), I.pilotReportHoldingShort(
                      flight(),
                     m_helper.getDepartureAirport(flight()),
                            runwayName,
                            holdShortEdge->name()
                        ), "", 1000, [this]{
                            if (m_departureTowerKhz > 0)
                            {
                                host()->writeLog("AIPILO|Flight[%s] CANCEL_TRANSMIT_HOLDING_SHORT", flight()->callSign().c_str());
                                return true;
                            }
                            return false;
                        });
                    }
                    return M.instantAction([]{});
                }),
                M.await(Maneuver::Type::Unspecified, "await_tower_khz", [this](){
                    return (m_departureTowerKhz > 0);
                }),
//                M.deferred([=]() {
//                    auto ground = m_departureAirport->groundAt(flight()->aircraft()->location());
//                    return M.transmitIntent(flight(), I.pilotHandoffReadback(flight(), ground, m_departureTowerKhz, m_lastReceivedIntentId));
//                }),
                M.instantAction([this]() {
                    m_aircraft->setLights(Aircraft::LightBits::BeaconLandingNavStrobe);
                }),
                M.instantAction([=]() {
                    aircraft()->setFrequencyKhz(m_departureTowerKhz);
                    m_lastDeclineReason = DeclineReason::None;
                }),
                M.transmitIntent(flight(), I.pilotCheckInWithTower(flight(), runwayName, holdShortEdge->name(), false)),
                M.await(Maneuver::Type::AwaitClearance, "await_any_luaw_clrnc_holdshrt", [this]{
                    auto clearance = flight()->tryFindClearance<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    auto luaw = flight()->tryFindClearance<LineUpAndWaitApproval>(Clearance::Type::LineUpAndWait);
                    return (clearance || luaw || m_lastDeclineReason != DeclineReason::None);
                }),
                M.deferred([this, runwayName]{
                    if (m_lastDeclineReason != DeclineReason::None)
                    {
                        return M.transmitIntent(flight(), I.pilotRunwayHoldShortReadback(
                            flight(),
                            m_helper.getDepartureTower(flight()),
                            runwayName,
                            m_lastDeclineReason,
                            m_lastReceivedIntentId));
                    }
                    return M.instantAction([]{});
                }),
                M.await(Maneuver::Type::AwaitClearance, "await_luaw_or_takeoff_clrnc", [this]{
                    auto clearance = flight()->tryFindClearance<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    auto luaw = flight()->tryFindClearance<LineUpAndWaitApproval>(Clearance::Type::LineUpAndWait);
                    return (clearance || luaw);
                }),
//                M.deferred([=]() {
//                    auto clearance = flight()->tryFindClearance<TakeoffClearance>(Clearance::Type::TakeoffClearance);
//                    auto luaw = flight()->tryFindClearance<LineUpAndWaitApproval>(Clearance::Type::LineUpAndWait);
//                    auto readback = clearance
//                        ? I.pilotTakeoffClearanceReadback(flight(), clearance, m_departureKhz, m_lastReceivedIntentId)
//                        : I.pilotLineUpAndWaitReadback(luaw, m_lastReceivedIntentId);
//                    m_wasTakeoffClearanceReadBack = (readback->code() == PilotTakeoffClearanceReadbackIntent::IntentCode);
//                    return M.transmitIntent(flight(), readback);
//                }),
                //M.delay(chrono::seconds(3))
            });
        }

        shared_ptr<Maneuver> maneuverAwaitCrossRunway(shared_ptr<Airport> airport, shared_ptr<TaxiEdge> holdShortEdge)
        {
            auto runway = getActiveZoneRunway(airport, holdShortEdge);
            string runwayName = runway ? runway->end1().name() : "";

            return M.sequence(Maneuver::Type::TaxiHoldShort, "await_cross_rwy", {
                M.instantAction([this]{
                    m_lastDeclineReason = DeclineReason::None;
                    flight()->removeClearance(Clearance::Type::RunwayCrossClearance);
                }),
                M.transmitIntent(flight(), I.pilotReportHoldingShort(flight(), runwayName, holdShortEdge->name())),
                M.await(Maneuver::Type::Unspecified, "", [this]{
                    auto clearance = flight()->tryFindClearance<RunwayCrossClearance>(Clearance::Type::RunwayCrossClearance);
                    return (clearance || m_lastDeclineReason != DeclineReason::None);
                }),
                M.deferred([this, airport, runwayName]{
                    if (m_lastDeclineReason != DeclineReason::None)
                    {
                        return M.transmitIntent(flight(), I.pilotRunwayHoldShortReadback(
                            flight(),
                            airport->groundAt(flight()->aircraft()->location()), runwayName,
                            m_lastDeclineReason,
                            m_lastReceivedIntentId));
                    }
                    return M.instantAction([]{});
                }),
                M.awaitClearance(flight(), Clearance::Type::RunwayCrossClearance),
                M.deferred([=]() {
                    auto clearance = flight()->findClearanceOrThrow<RunwayCrossClearance>(Clearance::Type::RunwayCrossClearance);
                    return M.transmitIntent(flight(), I.pilotRunwayCrossReadback(clearance, m_lastReceivedIntentId));
                }),
                M.instantAction([this]{
                    flight()->removeClearance(Clearance::Type::RunwayCrossClearance);
                })
            });
        }

        shared_ptr<Maneuver> maneuverLineup()
        {
            return M.sequence(Maneuver::Type::DepartureTaxi, "lineup", {
                M.instantAction([this]() {
                    m_linedUpTimestamp = host()->getWorld()->timestamp();
                    host()->writeLog(
                        "AIPILO|Flight[%s] lining up on runway",
                        flight()->callSign().c_str());
                }),
                M.deferred([this]() {
                    auto clearance = flight()->findClearanceOrThrow<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    auto runway = m_departureAirport->getRunwayOrThrow(clearance->departureRunway());
                    const auto& runwayEnd = runway->getEndOrThrow(clearance->departureRunway());
                    const GeoPoint runwayStart = runwayEnd.centerlinePoint().geo();
                    float runwayHeading = runwayEnd.heading();

                    // Move to runway centerline if not already there
                    const float distanceToRunwayStart = GeoMath::getDistanceMeters(m_aircraft->location(), runwayStart);
                    if (distanceToRunwayStart > 5.0f)
                    {
                        host()->writeLog(
                            "AIPILO|Flight[%s] moving to runway centerline, distance=%.0fm",
                            flight()->callSign().c_str(),
                            distanceToRunwayStart);
                        return M.taxiStraight(flight(), m_aircraft->location(), runwayStart, ManeuverFactory::TaxiType::Normal);
                    }
                    return M.instantAction([]{});
                }),
                M.instantAction([this]() {
                    auto clearance = flight()->findClearanceOrThrow<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    auto runway = m_departureAirport->getRunwayOrThrow(clearance->departureRunway());
                    const auto& runwayEnd = runway->getEndOrThrow(clearance->departureRunway());
                    float runwayHeading = runwayEnd.heading();
                    m_aircraft->setAttitude(m_aircraft->attitude().withHeading(runwayHeading).withRoll(0.0f));
                })
            });
        }

        shared_ptr<Maneuver> maneuverAwaitTakeOffClearance()
        {
            return M.sequence(Maneuver::Type::DepartureAwaitTakeOff, "await_takeoff_clrnc", {
                M.await(Maneuver::Type::AwaitClearance, "await_luaw_or_takeoff_clrnc", [this]{
                    auto clearance = flight()->tryFindClearance<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    auto luaw = flight()->tryFindClearance<LineUpAndWaitApproval>(Clearance::Type::LineUpAndWait);
                    return (clearance || luaw);
                }),
                M.deferred([=]() {
                    auto clearance = flight()->tryFindClearance<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    auto luaw = flight()->tryFindClearance<LineUpAndWaitApproval>(Clearance::Type::LineUpAndWait);
                    auto readback = clearance
                        ? I.pilotTakeoffClearanceReadback(flight(), clearance, m_departureKhz, m_lastReceivedIntentId)
                        : I.pilotLineUpAndWaitReadback(luaw, m_lastReceivedIntentId);
                    m_wasTakeoffClearanceReadBack = (readback->code() == PilotTakeoffClearanceReadbackIntent::IntentCode);
                    return M.transmitIntent(flight(), readback);
                }),
                M.awaitClearance(flight(), Clearance::Type::TakeoffClearance),
                M.deferred([=]() {
                    auto clearance = flight()->findClearanceOrThrow<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                    if (!m_wasTakeoffClearanceReadBack)
                    {
                        auto readback = I.pilotTakeoffClearanceReadback(flight(), clearance, m_departureKhz, m_lastReceivedIntentId);
                        m_wasTakeoffClearanceReadBack = true;
                        return M.transmitIntent(flight(), readback);
                    }
                    return M.instantAction([]{});
                }),
            });
        }

        shared_ptr<Maneuver> maneuverTakeoff()
        {
            return DeferredManeuver::create(Maneuver::Type::DepartureTakeOffRoll, "takeoff", [=]() {
                auto clearance = flight()->findClearanceOrThrow<TakeoffClearance>(Clearance::Type::TakeoffClearance);
                auto luaw = flight()->tryFindClearance<LineUpAndWaitApproval>(Clearance::Type::LineUpAndWait);
                auto runway = m_departureAirport->getRunwayOrThrow(clearance->departureRunway());
                const auto& runwayEnd = runway->getEndOrThrow(clearance->departureRunway());
                float runwayHeading = runwayEnd.heading();
                const auto performance = m_aircraft->performanceProfile();

                auto beforeTakeoffChecklist = M.await(Maneuver::Type::Unspecified, "bfr_tkoff_chklst", [=]() {
                    auto now = host()->getWorld()->timestamp();
                    auto elapsed = now - m_linedUpTimestamp;
                    if (elapsed > chrono::milliseconds(100) && !m_stoppedBeforeTakeoff)
                    {
                        m_stoppedBeforeTakeoff = true;
                        host()->writeLog(
                            "AIPILO|BEFORE-TAKEOFF Flight[%s] stopped for before-takeoff checklist lined-up[%lld] now[%lld] elapsed[%lld]",
                            flight()->callSign().c_str(),
                            m_linedUpTimestamp,
                            now,
                            elapsed);
                    }
                    return (clearance->immediate() || elapsed >= chrono::seconds(3));
                });
                auto logTakeoffRoll = M.instantAction([this](){
                    host()->writeLog(
                        "AIPILO|TAKEOFFROLL flight[%s] at [%f,%f] stopped[%d]",
                        flight()->callSign().c_str(),
                        m_aircraft->location().latitude,
                        m_aircraft->location().longitude,
                        m_stoppedBeforeTakeoff ? 1 : 0);
                });
                auto rollOnRunway = M.deferred([this]() {
                    return shared_ptr<Maneuver>(new AnimationManeuver<double>(
                        "roll",
                        m_stoppedBeforeTakeoff ? 0.0f : 20.0f,
                        140.0,
                        chrono::seconds(20),
                        [](const double &from, const double &to, double progress, double &value) {
                            value = from + (to - from) * progress;
                        },
                        [this](const double &value, double progress) {
                            m_aircraft->setGroundSpeedKt(value);
                        }
                    ));
                });
                auto rotate1 = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "rotate_1",
                    0,
                    8.5,
                    chrono::seconds(3),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [=](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                ));
                auto rotate2 = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "rotate_2",
                    8.5,
                    15.0,
                    chrono::seconds(6),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [this](const double& value, double progress) {
                        m_aircraft->setAttitude(m_aircraft->attitude().withPitch(value));
                    }
                ));
                auto logLiftUp = M.instantAction([this](){
                    host()->writeLog(
                        "AIPILO|LIFTUP flight[%s] at [%f,%f] ground-speed[%f]kt pitch[%f]deg",
                        flight()->callSign().c_str(),
                        m_aircraft->location().latitude,
                        m_aircraft->location().longitude,
                        m_aircraft->groundSpeedKt(),
                        m_aircraft->attitude().pitch());
                });
                auto liftUp = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "lift_up",
                    0,
                    std::max(1500.0f, performance.descentRateFpm * 2.5f),
                    chrono::seconds(10),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [this](const double& value, double progress) {
                        m_aircraft->setVerticalSpeedFpm(value);
                        //aircraft->setAltitude(value);
                    }
                ));
                auto gearUp = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "gear_up",
                    1.0,
                    0.0,
                    chrono::seconds(8),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [this](const double& value, double progress) {
                        m_aircraft->setGearState(value);
                    }
                ));
                auto accelerateAirborne = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "accel_airb",
                    140.0,
                    180.0,
                    chrono::seconds(30),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [this](const double& value, double progress) {
                        m_aircraft->setGroundSpeedKt(value);
                    }
                ));
                auto turnToInitialHeading = shared_ptr<Maneuver>(new AnimationManeuver<double>(
                    "turn_init_hdg",
                    140.0,
                    210.0,
                    chrono::seconds(30),
                    [](const double& from, const double& to, double progress, double& value) {
                        value = from + (to - from) * progress; 
                    },
                    [this](const double& value, double progress) {
                        m_aircraft->setGroundSpeedKt(value);
                    }
                ));

                return M.sequence(Maneuver::Type::Unspecified, "", {
                    M.instantAction([this, runway]() {
                        const auto& runwayEnd = runway->getEndOrThrow(m_flightPlan->departureRunway());
                        m_aircraft->setAttitude(m_aircraft->attitude().withHeading(runwayEnd.heading()));
                    }),
                    beforeTakeoffChecklist,
                    logTakeoffRoll,
                    M.parallel(Maneuver::Type::Unspecified, "", {
                        M.sequence(Maneuver::Type::Unspecified, "", {
                            rollOnRunway,
                            accelerateAirborne,
                        }),
                        M.sequence(Maneuver::Type::Unspecified, "", {
                            M.delay(chrono::seconds(20)),
                            rotate1,
                            rotate2,
                        }),
                        M.sequence(Maneuver::Type::Unspecified, "", {
                            M.delay(chrono::seconds(23)),
                            logLiftUp,
                            liftUp
                        }),
                        M.sequence(Maneuver::Type::Unspecified, "", {
                            M.delay(chrono::seconds(25)),
                            gearUp,
                        }),
                        M.sequence(Maneuver::Type::Unspecified, "", {
                            M.delay(chrono::seconds(32)),
                            M.airborneTurn(flight(), runwayHeading, clearance->initialHeading()),
                        }),
                    })
                });
            });
        }

        shared_ptr<Runway> getActiveZoneRunway(shared_ptr<Airport> airport, shared_ptr<TaxiEdge> activeZoneEdge)
        {
            host()->writeLog(
                "AIPILO|getActiveZoneRunway: edge [%d|%s] departure-mask [%d] arrival-mask [%d] ils-mask [%d]",
                activeZoneEdge->id(),
                activeZoneEdge->name().c_str(),
                activeZoneEdge->activeZones().departue.runwaysMask(),
                activeZoneEdge->activeZones().arrival.runwaysMask(),
                activeZoneEdge->activeZones().ils.runwaysMask());

            for (const auto& runway : airport->runways())
            {
                host()->writeLog(
                    "AIPILO|getActiveZoneRunway: checking runway [%s/%s], maskbit [%d]",
                    runway->end1().name().c_str(), runway->end2().name().c_str(), runway->maskBit());

                if (activeZoneEdge->activeZones().departue.has(runway) ||
                    activeZoneEdge->activeZones().arrival.has(runway) ||
                    activeZoneEdge->activeZones().ils.has(runway))
                {
                    host()->writeLog("AIPILO|getActiveZoneRunway: FOUND");
                    return runway;//runwayName = runway->end1().name();
                }
            }

            host()->writeLog("AIPILO|getActiveZoneRunway: RUNWAY NOT FOUND");
            return nullptr;
        }

        float waypointReachThresholdMeters(float distanceThresholdMeters) const
        {
            const double groundSpeedKt = max(0.0, aircraft()->groundSpeedKt());
            const float groundSpeedMetersPerSecond = static_cast<float>(groundSpeedKt * METERS_IN_1_NAUTICAL_MILE / 3600.0);
            const float lookAheadMeters = groundSpeedMetersPerSecond * 6.0f;
            return max(distanceThresholdMeters, min(900.0f, lookAheadMeters));
        }

        bool hasReachedWaypoint(const GeoPoint& point, const GeoPoint& legStartPoint, float distanceThresholdMeters = 250.0f)
        {
            const float effectiveThresholdMeters = waypointReachThresholdMeters(distanceThresholdMeters);

            // Check both distance and along-track geometry to determine if waypoint has been reached.
            float distanceToPoint = GeoMath::getDistanceMeters(aircraft()->location(), point);
            
            // Close enough to waypoint - count as reached.
            if (distanceToPoint <= effectiveThresholdMeters)
            {
                return true;
            }

            // If we have a valid leg start point, use triangle geometry to detect
            // if we already passed abeam/beyond the waypoint on this segment.
            if (legStartPoint != GeoPoint::empty)
            {
                const float legLengthMeters = GeoMath::getDistanceMeters(legStartPoint, point);
                const float distanceFromLegStartMeters = GeoMath::getDistanceMeters(legStartPoint, aircraft()->location());
                if (legLengthMeters > 1.0f && distanceFromLegStartMeters > 1.0f)
                {
                    const double fromToAircraftSquared = static_cast<double>(distanceFromLegStartMeters) * distanceFromLegStartMeters;
                    const double fromToWaypointSquared = static_cast<double>(legLengthMeters) * legLengthMeters;
                    const double aircraftToWaypointSquared = static_cast<double>(distanceToPoint) * distanceToPoint;

                    // By cosine law, if FA^2 >= FT^2 + TA^2 then angle at target waypoint is >= 90°,
                    // which means aircraft is already past the waypoint relative to the leg direction.
                    if (fromToAircraftSquared >= fromToWaypointSquared + aircraftToWaypointSquared)
                    {
                        host()->writeLog(
                            "AIPILO|Flight[%s] waypoint passed by along-track geometry, distance %.0f m",
                            flight()->callSign().c_str(),
                            distanceToPoint);
                        return true;
                    }
                }
            }
            
            // Fallback: waypoint significantly behind current heading.
            float headingToPoint = GeoMath::getHeadingFromPoints(aircraft()->location(), point);
            float turnToPointDegrees = GeoMath::getTurnDegrees(aircraft()->attitude().heading(), headingToPoint);
            
            if (abs(turnToPointDegrees) >= 120.0f && distanceToPoint > effectiveThresholdMeters * 1.5f)
            {
                host()->writeLog(
                    "AIPILO|Flight[%s] waypoint appears to be missed - behind by %.1f degrees, distance %.0f m",
                    flight()->callSign().c_str(),
                    turnToPointDegrees,
                    distanceToPoint);
                return true;
            }
            
            return false;
        }

        bool hasReachedWaypoint(const GeoPoint& point, float distanceThresholdMeters = 250.0f)
        {
            return hasReachedWaypoint(point, GeoPoint::empty, distanceThresholdMeters);
        }
    };
}

