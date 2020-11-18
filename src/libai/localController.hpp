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

#include "libworld.h"
#include "clearanceFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "aiControllerBase.hpp"
#include "libai.hpp"
#include "simpleRunwayMutex.hpp"

using namespace std;
using namespace world;

namespace ai
{
    typedef FlightStrip::Event MutexEvent;
    typedef FlightStrip::Event::Type MutexEventType;

    class LocalController : public AIControllerBase
    {
    private:

        unordered_map<string, shared_ptr<SimpleRunwayMutex>> m_activeRunwayMutex;
        float m_departureInitialTurn = 15.0f;

    public:
        LocalController(shared_ptr<HostServices> _host, int _id, Actor::Gender _gender, shared_ptr<ControllerPosition> _position) :
            AIControllerBase(_host, _id, _gender, _position)
        {
            registerIntentHandlers();
        }

        void selectActiveRunways(vector<string>& departure, vector<string>& arrival) override
        {
            host()->writeLog("AICONT|AIController::selectActiveRunways");

            auto airport = facility()->airport();
            if (!airport || position()->type() != ControllerPosition::Type::Local)
            {
                throw runtime_error("Cannot select active runways: not a local controller or no airport");
            }

            if (airport->hasParallelRunways())
            {
                const auto& longestGroup = airport->findLongestParallelRunwayGroup();

                departure.push_back(longestGroup.at(0)->end1().name());
                arrival.push_back(longestGroup.at(1)->end1().name());
                if (longestGroup.size() > 2)
                {
                    arrival.push_back(longestGroup.at(longestGroup.size() - 1)->end1().name());
                }

                host()->writeLog(
                    "AICONT|Selected parallel active runways departure[%s] arrival[%s][%s]",
                    departure.at(0).c_str(),
                    arrival.at(0).c_str(),
                    arrival.at(arrival.size() - 1).c_str());
            }
            else
            {
                auto longestRunway = airport->findLongestRunway()->end1().name();
                arrival.push_back(longestRunway);
                departure.push_back(longestRunway);
                host()->writeLog("AICONT|Selected single active runway [%s]", departure.at(0).c_str());
            }

            createActiveRunwayMutexes();
        }

        void progressTo(chrono::microseconds timestamp) override
        {
            AIControllerBase::progressTo(timestamp);

            for (const auto& mutexEntry : m_activeRunwayMutex)
            {
                mutexEntry.second->progressTo(timestamp);
            }
        }

        void clearFlights() override
        {
            for (const auto& mutexEntry : m_activeRunwayMutex)
            {
                mutexEntry.second->clearFlights();
            }
        }

    private:

        void registerIntentHandlers()
        {
            AI_CONTROLLER_MAP_INTENT(PilotReportFinalIntent, onPilotReportFinal);
            AI_CONTROLLER_MAP_INTENT(PilotCheckInWithTowerIntent, onDeparturePilotCheckInWithTower);
            AI_CONTROLLER_MAP_INTENT(PilotLineUpAndWaitReadbackIntent, onPilotLineUpReadback);
            AI_CONTROLLER_MAP_INTENT(GroundCrossRunwayRequestFromTowerIntent, onCrossRunwayRequestFromGround);
        }

        void createActiveRunwayMutexes()
        {
            const auto addMutex = [this](const string& runwayName, shared_ptr<Runway> runway) {
                if (!hasKey(m_activeRunwayMutex, runwayName))
                {
                    auto mutex = make_shared<SimpleRunwayMutex>(
                        host(),
                        runway,
                        runway->getEndOrThrow(runwayName),
                        SimpleRunwayMutex::TimingThresholds(),
                        RunwayStripBoard());

                    m_activeRunwayMutex.insert({ runway->name(), mutex });
                    m_activeRunwayMutex.insert({ runway->end1().name(), mutex });
                    m_activeRunwayMutex.insert({ runway->end2().name(), mutex });
                }
            };

            for (const auto& runwayName : airport()->activeDepartureRunways())
            {
                addMutex(runwayName, airport()->getRunwayOrThrow(runwayName));
            }

            for (const auto& runwayName : airport()->activeArrivalRunways())
            {
                addMutex(runwayName, airport()->getRunwayOrThrow(runwayName));
            }
        }

        shared_ptr<SimpleRunwayMutex> getRunwayMutex(const string& runwayName)
        {
            return getValueOrThrow(m_activeRunwayMutex, runwayName);
        }

        void onDeparturePilotCheckInWithTower(shared_ptr<PilotCheckInWithTowerIntent> intent)
        {
            host()->writeLog(
                "AICONT|TWR checkin departure[%s] with RWY-MUTEX[%s]",
                intent->subjectFlight()->callSign().c_str(),
                intent->runway().c_str());

            auto mutex = getRunwayMutex(intent->runway());
            mutex->checkInDeparture(intent->subjectFlight(), [this, intent](const MutexEvent& event) {
                logMutexEvent(event, intent->runway(), "departure");
//                host()->writeLog(
//                    "AICONT|TWR got event type[%d] from RWY-MUTEX[%s] to departure[%s]",
//                    event.type, intent->runway().c_str(), event.subject->callSign().c_str());

                switch (event.type)
                {
                case MutexEventType::ClearedForTakeoff:
                    clearForTakeoff(event, intent->runway(), intent->id());
                    break;
                case MutexEventType::AuthorizedLineUpAndWait:
                    authorizeLineUpAndWait(event, intent->runway(), intent->id());
                    break;
                case MutexEventType::HoldShort:
                    transmit(I.towerDepartureHoldShort(
                        intent->runway(), event.subject, position(), event.reason, intent->id()));
                    break;
                case MutexEventType::Continue:
                    transmit(I.towerDepartureCheckInReply(
                        intent->runway(),
                        event.subject,
                        position(),
                        event.numberInLine,
                        event.immediate,
                        intent->id()));
                    break;
                default:
                    host()->writeLog(
                        "AICONT|TWR WARNING: UNEXPECTED event type[%d] from RWY-MUTEX[%s] to departure[%s]",
                        event.type, intent->runway().c_str(), event.subject->callSign().c_str());
                }
            });
            /*
            mutex->checkInDeparture(
                intent->subjectFlight(),
                [this, intent](bool immediate){
                    clearForTakeoff(intent->subjectFlight(), intent->runway(), immediate);
                },
                [this, intent](DeclineReason reason, int numberInLine){
                    if (numberInLine > 1)
                    {
                        transmit(I.towerDepartureCheckInReply(
                            intent->runway(), intent->subjectFlight(), position(), numberInLine, intent->id()));
                    }
                    else if (reason == DeclineReason::WakeTurbulence)
                    {
                        transmit(I.towerLineUpAndWait(
                            C.lineUpAndWait(intent->subjectFlight(), reason), intent->id()));
                    }
                    else
                    {
                        transmit(I.towerDepartureHoldShort(
                            intent->runway(), intent->subjectFlight(), position(), reason, intent->id()));
                    }
                }
            );
             */
        }

        void onPilotReportFinal(shared_ptr<PilotReportFinalIntent> intent)
        {
            host()->writeLog(
                "AICONT|TWR checkin arrival[%s] with TWR-RWY-MUTEX[%s]",
                intent->subjectFlight()->callSign().c_str(),
                intent->runway().c_str());

            auto mutex = getRunwayMutex(intent->runway());
            mutex->checkInArrival(intent->subjectFlight(), [this, intent](const MutexEvent& event) {
                logMutexEvent(event, intent->runway(), "arrival");
//                host()->writeLog(
//                    "AICONT|TWR got event type[%d] from RWY-MUTEX[%s] to arrival[%s]",
//                    event.type, intent->runway().c_str(), event.subject->callSign().c_str());

                switch (event.type)
                {
                case MutexEventType::ClearedToLand:
                    clearToLand(event, intent->runway(), intent->id());
                    break;
                case MutexEventType::Continue:
                    transmit(I.towerContinueApproach(
                        event.subject,
                        position(),
                        intent->runway(),
                        event.numberInLine,
                        event.traffic,
                        intent->id()));
                    break;
                case MutexEventType::GoAround:
                    requestGoAround(event, intent->runway());
                    break;
                default:
                    host()->writeLog(
                        "AICONT|TWR WARNING: UNEXPECTED event type[%d] from RWY-MUTEX[%s] to arrival[%s]",
                        event.type, intent->runway().c_str(), event.subject->callSign().c_str());
                }
            });


            /*
            mutex->checkInArrival(
                intent->subjectFlight(),
                [this, intent](bool doNotSlowDown){
                    auto runway = airport()->getRunwayOrThrow(intent->subjectFlight()->plan()->arrivalRunway());
                    const auto& runwayEnd = runway->getEndOrThrow(intent->subjectFlight()->plan()->arrivalRunway());
                    auto ground = airport()->groundAt(runwayEnd.centerlinePoint().geo());
                    auto clearance = C.landingClearance(
                        intent->subjectFlight(),
                        runwayEnd.name(),
                        ground->frequency()->khz());
                    transmit(I.towerClearedForLanding(clearance, intent->id()));
                },
                [](DeclineReason, int){ }
            );
             */
        }

        void onPilotLineUpReadback(shared_ptr<PilotLineUpAndWaitReadbackIntent> intent)
        {
//            host()->getWorld()->deferBy(
//                "takeOffClearance/" + intent->subjectFlight()->callSign(),
//                chrono::seconds(20),
//                [=]() {
//                    auto runway = airport()->getRunwayOrThrow(intent->subjectFlight()->plan()->departureRunway());
//                    const auto& runwayEnd = runway->getEndOrThrow(intent->subjectFlight()->plan()->departureRunway());
//                    float initialHeading = runwayEnd.heading() + m_departureInitialTurn;
//                    auto clearance = C.takeoffClearance(
//                        intent->subjectFlight(),
//                        initialHeading,
//                        false
//                    );
//                    position()->frequency()->enqueueTransmission(
//                        I.towerClearedForTakeoff(clearance)
//                    );
//                    m_departureInitialTurn = (m_departureInitialTurn < 60 ? m_departureInitialTurn + 15 : 15);
//                }
//            );
        }

        void onCrossRunwayRequestFromGround(shared_ptr<GroundCrossRunwayRequestFromTowerIntent> intent)
        {
            host()->writeLog(
                "AICONT|TWR checkin taxiing[%s] with RWY-MUTEX[%s] for crossing",
                intent->subjectFlight()->callSign().c_str(),
                intent->runwayName().c_str());

            auto mutex = getRunwayMutex(intent->runwayName());
            mutex->checkInCrossing(intent->subjectFlight(), [this, intent](const MutexEvent& event) {
                logMutexEvent(event, intent->runwayName(), "crossing");
//                host()->writeLog(
//                    "AICONT|TWR got event type[%d] from RWY-MUTEX[%s] to taxiing[%s]",
//                    event.type, intent->runwayName().c_str(), event.subject->callSign().c_str());

                shared_ptr<RunwayCrossClearance> clearance;
                DeclineReason reason = DeclineReason::None;

                switch (event.type)
                {
                case MutexEventType::ClearedToCross:
                    clearance = C.runwayCrossClearance(intent->subjectFlight(), intent->runwayName());
                    break;
                case MutexEventType::HoldShort:
                    reason = event.reason;
                    break;
                default:
                    host()->writeLog(
                        "AICONT|TWR WARNING: UNEXPECTED event type[%d] from RWY-MUTEX[%s] to taxiing[%s]",
                        event.type, intent->runwayName().c_str(), event.subject->callSign().c_str());
                    return;
                }

                //TODO: transmit on an internal frequency?
                intent->subjectControl()->controller()->receiveIntent(I.towerCrossRunwayReplyToGround(
                    intent->id(),
                    intent->pilotRequestId(),
                    intent->subjectFlight(),
                    position(),
                    intent->subjectControl(),
                    intent->runwayName(),
                    clearance,
                    reason
                ));
            });

            /*
            mutex->addCrossing(
                intent->subjectFlight(),
                [this, intent](bool withoutDelay){
                    host()->writeLog(
                        "AICONT|TWR->GND flight[%s] cleared to cross runway[%s] without-delay=",
                        intent->subjectFlight()->callSign().c_str(), intent->runwayName().c_str(), withoutDelay ? "Y" : "N");

                    intent->subjectControl()->controller()->receiveIntent(I.towerCrossRunwayReplyToGround(
                        intent->id(),
                        intent->pilotRequestId(),
                        intent->subjectFlight(),
                        position(),
                        intent->subjectControl(),
                        intent->runwayName(),
                        C.runwayCrossClearance(intent->subjectFlight(), intent->runwayName()),
                        DeclineReason::None
                    ));
                },
                [this, intent](DeclineReason reason, int numberInLine){
                    host()->writeLog(
                        "AICONT|TWR->GND flight[%s] HOLD SHORT runway[%s] reason[%d] number-in-line[%d]",
                        intent->subjectFlight()->callSign().c_str(), intent->runwayName().c_str(), reason, numberInLine);

                    intent->subjectControl()->controller()->receiveIntent(I.towerCrossRunwayReplyToGround(
                        intent->id(),
                        intent->pilotRequestId(),
                        intent->subjectFlight(),
                        position(),
                        intent->subjectControl(),
                        intent->runwayName(),
                        nullptr,
                        reason
                    ));
                }
            );
             */
        }

        void clearForTakeoff(const MutexEvent& event, const string& runwayName, uint64_t replyToId)
        {
            auto runway = airport()->getRunwayOrThrow(runwayName);
            const auto& runwayEnd = runway->getEndOrThrow(runwayName);
            float initialHeading = runwayEnd.heading() + m_departureInitialTurn;
            auto clearance = C.takeoffClearance(
                event.subject,
                initialHeading,
                event.immediate
            );
            transmit(I.towerClearedForTakeoff(clearance, event.traffic, replyToId));
            m_departureInitialTurn = (m_departureInitialTurn < 60 ? m_departureInitialTurn + 15 : 15);
        }

        void authorizeLineUpAndWait(const MutexEvent& event, const string& runwayName, uint64_t replyToId)
        {
            auto runway = airport()->getRunwayOrThrow(runwayName);
            const auto& runwayEnd = runway->getEndOrThrow(runwayName);
            auto approval = C.lineUpAndWait(
                event.subject,
                event.reason
            );
            transmit(I.towerLineUpAndWait(approval, event.traffic, replyToId));
        }

        void clearToLand(const MutexEvent& event, const string& runwayName, uint64_t replyToId)
        {
            auto runway = airport()->getRunwayOrThrow(runwayName);
            const auto& runwayEnd = runway->getEndOrThrow(runwayName);
            auto ground = airport()->groundAt(runwayEnd.centerlinePoint().geo());
            auto clearance = C.landingClearance(
                event.subject,
                runwayEnd.name(),
                ground->frequency()->khz());
            transmit(I.towerClearedForLanding(clearance, event.traffic, replyToId));
        }

        void requestGoAround(const MutexEvent& event, const string& runwayName)
        {
            auto runway = airport()->getRunwayOrThrow(runwayName);
            const auto& runwayEnd = runway->getEndOrThrow(runwayName);
            auto request = C.goAroundRequest(
                event.subject,
                position(),
                runwayEnd.name(),
                event.reason);
            transmitCritical(I.towerGoAround(request, event.traffic));
        }

        void logMutexEvent(const MutexEvent& event, const string& runwayName, const string& subjectType)
        {
            string typeString;
            switch (event.type)
            {
            case MutexEventType::Continue: typeString = "Continue"; break;
            case MutexEventType::HoldShort: typeString = "HoldShort"; break;
            case MutexEventType::GoAround: typeString = "GoAround"; break;
            case MutexEventType::ClearedForTakeoff: typeString = "ClearedForTakeoff"; break;
            case MutexEventType::ClearedToLand: typeString = "ClearedToLand"; break;
            case MutexEventType::AuthorizedLineUpAndWait: typeString = "AuthorizedLineUpAndWait"; break;
            case MutexEventType::ClearedToCross: typeString = "ClearedToCross"; break;
            default: typeString = "??"; break;
            }

            host()->writeLog(
                "AICONT|TWR-RWY-MUTEX[%s] to %s[%s]: event[%s] num-in-line[%d] immediate[%d] traffic#[%d]",
                runwayName.c_str(),
                subjectType.c_str(),
                event.subject->callSign().c_str(),
                typeString.c_str(),
                event.numberInLine,
                event.immediate ? 1 : 0,
                event.traffic.size()
            );
        }
    };
}
