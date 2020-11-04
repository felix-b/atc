// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <unordered_map>
#include <unordered_set>

#include "libworld.h"
#include "clearanceFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "libai.hpp"
#include "stlhelpers.h"

using namespace std;
using namespace world;

#define AI_CONTROLLER_MAP_INTENT(intent_type, handler_func) \
    onIntent<intent_type>([this](shared_ptr<intent_type> intent) { handler_func(intent); })

namespace ai
{
    class AIControllerBase : public Controller
    {
    public:
        typedef function<void(shared_ptr<Intent> intent)> IntentHandlerCallback;
    private:
        shared_ptr<IntentFactory> m_intentFactory;
        shared_ptr<ClearanceFactory> m_clearanceFactory;
        unordered_map<int, IntentHandlerCallback> m_intentHandlerByCode;
        unordered_set<shared_ptr<Flight>> m_clearedForDepartureTaxi;
        unordered_set<shared_ptr<Flight>> m_departureTaxiHandedOffToTower;
        int m_nextSquawk = 3101;
    protected:
        IntentFactory& I;
        ClearanceFactory& C;
    public:
        AIControllerBase(shared_ptr<HostServices> _host, int _id, Actor::Gender _gender, shared_ptr<ControllerPosition> _position) :
            Controller(_host, _id, _gender, _position),
            m_intentFactory(_host->services().get<IntentFactory>()),
            m_clearanceFactory(_host->services().get<ClearanceFactory>()),
            I(*m_intentFactory),
            C(*m_clearanceFactory)
        {
        }
    public:
        void receiveIntent(shared_ptr<Intent> intent) override
        {
            host()->writeLog(
                "AICONT|callsign[%s] receiveIntent flight[%s] intent code[%d]",
                position()->callSign().c_str(),
                intent->subjectFlight()->callSign().c_str(),
                intent->code());

            try
            {
                IntentHandlerCallback handler;
                if (tryGetValue(m_intentHandlerByCode, intent->code(), handler))
                {
                    handler(intent);
                }
                else if (!fallbackReceiveIntent(intent)) //TODO: get rid of fallbackReceiveIntent
                {
                    host()->writeLog(
                "AICONT|WARNING: callsign[%s] received intent code[%d] from flight[%s], but no handler is registered for this intent code.",
                        position()->callSign().c_str(),
                        intent->code(),
                        intent->subjectFlight()->callSign().c_str());
                }
            }
            catch (const exception &e)
            {
                host()->writeLog(
                    "AICONT|callsign[%s] CRASHED!!! while handling intent code[%d]: %s",
                    position()->callSign().c_str(),
                    intent->code(),
                    e.what());
            }
        }

        void progressTo(chrono::microseconds timestamp) override
        {
            handoffDeparturesToTower();
        }

        void selectActiveRunways(vector<string>& departure, vector<string>& arrival) override
        {
            throw runtime_error(
                "AI controller callsign[" + position()->callSign() + "] cannot selectActiveRunways(): I'm not a local controller");
        }

        void clearFlights() override
        {
        }

    protected:

        //TODO: extract concrete controller classes
        bool fallbackReceiveIntent(shared_ptr<Intent> intent)
        {
            shared_ptr<Intent> reply;

            switch (intent->code())
            {
            case PilotIfrClearanceRequestIntent::IntentCode:
                {
                    int squawk = (m_nextSquawk++) % 4000 + 999;
                    reply = m_intentFactory->deliveryIfrClearanceReply(
                        intent->subjectFlight(),
                         m_clearanceFactory->ifrClearance(intent->subjectFlight(), squawk),
                         intent->id()
                    );
                }
                break;
            case PilotIfrClearanceReadbackIntent::IntentCode:
                {
                    auto clearance = intent->subjectFlight()->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance);
                    reply = m_intentFactory->deliveryIfrClearanceReadbackCorrect(
                        intent->subjectFlight(),
                        intent->id()
                    );
                }
                break;
            case PilotPushAndStartRequestIntent::IntentCode:
                reply = m_intentFactory->groundPushAndStartReply(
                    intent->subjectFlight(), 
                    m_clearanceFactory->pushAndStartApproval(intent->subjectFlight()),
                    intent->id()
                );
                break;
            case PilotDepartureTaxiRequestIntent::IntentCode:
                reply = m_intentFactory->groundDepartureTaxiReply(
                    intent->subjectFlight(), 
                    m_clearanceFactory->departureTaxiClearance(intent->subjectFlight()),
                    intent->id()
                );
                m_clearedForDepartureTaxi.insert(intent->subjectFlight());
                break;
            case PilotReportHoldingShortIntent::IntentCode:
                {
                    auto typedIntent = dynamic_pointer_cast<PilotReportHoldingShortIntent>(intent);
                    if (typedIntent)
                    {
                        auto flightPlan = intent->subjectFlight()->plan();
                        bool isDepartureRunway = isFlightDepartureRunway(intent->subjectFlight(), typedIntent->runway());
                        if (isDepartureRunway || hasKey(m_departureTaxiHandedOffToTower, intent->subjectFlight()))
                        {
                            reply = I.groundSwitchToTower(intent->subjectFlight(), intent->id());
                            moveDepartureToTower(intent->subjectFlight());
                        }
                        else if (airport()->isRunwayActive(typedIntent->runway()))
                        {
                            host()->writeLog(
                                "AICONT|GND->TWR flight[%s] requested to cross active runway[%s]",
                                intent->subjectFlight()->callSign().c_str(),
                                typedIntent->runway().c_str());

                            auto localController = findLocalControllerOrThrow(intent->subjectFlight());
                            localController->receiveIntent(I.groundCrossRunwayRequestToTower(
                                typedIntent->runway(),
                                intent->subjectFlight(),
                                position(),
                                localController->position(),
                                intent->id()
                            ));
                        }
                        else
                        {
                            reply = I.groundCrossRunwayClearance(m_clearanceFactory->runwayCrossCleaeance(
                                typedIntent->subjectFlight(),
                                typedIntent->runway()
                            ), intent->id());
                        }
                    }
                }
                break;
            case TowerCrossRunwayReplyToGroundIntent::IntentCode:
                {
                    reply = intent;
                    auto typedIntent = dynamic_pointer_cast<TowerCrossRunwayReplyToGroundIntent>(intent);
                    if (typedIntent)
                    {
                        host()->writeLog(
                            "AICONT|GND got reply from TWR for flight[%s] on crossing runway[%s]: %s ; reason[%d]",
                            intent->subjectFlight()->callSign().c_str(),
                            typedIntent->runwayName().c_str(),
                            typedIntent->cleared() ? "APPROVED" : "DECLINED",
                            typedIntent->declineReason());

                        if (typedIntent->cleared())
                        {
                            reply = I.groundCrossRunwayClearance(
                                typedIntent->clearance(),
                                typedIntent->pilotRequestId());
                        }
                        else
                        {
                            reply = I.groundHoldShortRunway(
                                typedIntent->runwayName(),
                                typedIntent->subjectFlight(),
                                position(),
                                typedIntent->declineReason(),
                                typedIntent->pilotRequestId());
                        }
                    }
                }
                break;
            case PilotArrivalCheckInWithGroundIntent::IntentCode:
                {
                    auto typedIntent = dynamic_pointer_cast<PilotArrivalCheckInWithGroundIntent>(intent);
                    auto taxiStartPoint = typedIntent->exitEdge()
                        ? typedIntent->exitEdge()->node2()->location().geo()
                        : intent->subjectFlight()->aircraft()->location();
                    auto clearance = m_clearanceFactory->arrivalTaxiClearance(intent->subjectFlight(), taxiStartPoint);
                    reply = m_intentFactory->groundArrivalTaxiReply(clearance, intent->id());
                }
                break;
            default:
                return false;
            }

            if (reply)
            {
                position()->frequency()->enqueueTransmission(reply);
            }

            host()->writeLog("AICONT|fallbackReceiveIntent - done");
            return true;
        }

        void transmitCritical(shared_ptr<Intent> intent)
        {
            position()->frequency()->enqueueTransmission(intent);
        }

        void transmit(
            shared_ptr<Intent> intent,
            Frequency::TransmissionCallback  onTransmit = Frequency::noopTRansmissionCallback,
            Frequency::CancellationQueryCallback onQueryCancel = Frequency::noopQueryCancelCallback)
        {
            position()->frequency()->enqueuePushToTalk(
                chrono::milliseconds(intent->replyToId() > 0 ? 0 : 300),
                intent,
                onTransmit,
                onQueryCancel);

//            if (intent->replyToId() > 0)
//            {
//                position()->frequency()->enqueueTransmission(intent);
//            }
//            else
//            {
//                position()->frequency()->enqueuePushToTalk(chrono::milliseconds(300), intent);
//            }
        }

        template <class TConcreteIntent>
        void onIntent(function<void(shared_ptr<TConcreteIntent> intent)> handler)
        {
            const auto handlerAdaptor = [this, handler](shared_ptr<Intent> intent){
                shared_ptr<TConcreteIntent> concreteIntent = dynamic_pointer_cast<TConcreteIntent>(intent);
                if (!concreteIntent)
                {
                    throw runtime_error(
                        "Intent code[" + to_string(intent->code()) + "] cannot be cast to class[" + typeid(TConcreteIntent).name() + "]");
                }
                handler(concreteIntent);
            };

            int key = TConcreteIntent::IntentCode;
            m_intentHandlerByCode.insert({ key, handlerAdaptor });
        }


    private:

        shared_ptr<Controller> findLocalControllerOrThrow(shared_ptr<Flight> flight)
        {
            auto position = airport()->localAt(flight->aircraft()->location());
            if (position)
            {
                auto controller = position->controller();
                if (controller)
                {
                    return controller;
                }
            }
            throw runtime_error("Could not find local controller at airport [" + airport()->header().icao() + "]");
        }

        bool isFlightDepartureRunway(shared_ptr<Flight> flight, const string& runwayName)
        {
            if (!hasKey(m_clearedForDepartureTaxi, flight) && !hasKey(m_departureTaxiHandedOffToTower, flight))
            {
                return false;
            }

            auto flightPlan = flight->plan();
            bool isDeparting = flight->phase() == Flight::Phase::Departure;

            auto departureRunway = isDeparting
               ? airport()->getRunwayOrThrow(flightPlan->departureRunway())
               : nullptr;
            bool isDepartureRunway =
                isDeparting && departureRunway && (
                    runwayName == departureRunway->end1().name() ||
                    runwayName == departureRunway->end2().name());

            host()->writeLog(
                "AICONT|PilotReportHoldingShortIntent, isDepartureRunway: [%s] == [%s]||[%s] ? %d",
                runwayName.c_str(),
                departureRunway ? departureRunway->end1().name().c_str() : "N/A",
                departureRunway ? departureRunway->end2().name().c_str() : "N/A",
                isDepartureRunway);

            return isDepartureRunway;
        }

        void handoffDeparturesToTower()
        {
            if (airport()->activeDepartureRunways().size() == 0)
            {
                return;
            }

            const Runway::End &departureEnd = airport()->getRunwayEndOrThrow(airport()->activeDepartureRunways().at(0));

            for (const auto &flight : m_clearedForDepartureTaxi)
            {
                //host()->writeLog("AICONT|handoffDeparturesToTower:1");

                GeoPoint location = flight->aircraft()->location();
                float distanceMeters = GeoMath::getDistanceMeters(location, departureEnd.centerlinePoint().geo());

                if (distanceMeters <= 200)
                {
                    //host()->writeLog("AICONT|handoffDeparturesToTower:2");
                    host()->writeLog(
                        "AICONT|GND handing departure [%s] off to TWR",
                        flight->callSign().c_str());

                    shared_ptr<Flight> copyOfFlightPtr = flight;
                    transmit(
                        I.groundSwitchToTower(flight, 0),
                        [this, copyOfFlightPtr](shared_ptr<Transmission> transmission) {
                            moveDepartureToTower(copyOfFlightPtr);
                        },
                        [this, copyOfFlightPtr]{
                            return hasKey(m_departureTaxiHandedOffToTower, copyOfFlightPtr);
                        }
                    );
                    //host()->writeLog("AICONT|handoffDeparturesToTower:3");
                    //host()->writeLog("AICONT|handoffDeparturesToTower:4");
                }
            }

//            for (const auto& flight : handedOff)
//            {
//                //host()->writeLog("AICONT|handoffDeparturesToTower:5");
//                m_clearedForDepartureTaxi.erase(flight);
//                m_departureTaxiHandedOffToTower.insert(flight);
//            }
            //host()->writeLog("AICONT|handoffDeparturesToTower:6");
        }

        void moveDepartureToTower(shared_ptr<Flight> departure)
        {
            m_clearedForDepartureTaxi.erase(departure);
            m_departureTaxiHandedOffToTower.insert(departure);
        }
    };
}
