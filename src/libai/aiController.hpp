// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"
#include "clearanceFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "libai.hpp"

using namespace std;
using namespace world;

namespace ai
{
    class DummyAIController : public Controller
    {
    private:
        shared_ptr<IntentFactory> m_intentFactory;
        shared_ptr<ClearanceFactory> m_clearanceFactory;
        float m_departureInitialTurn = 15.0f;
    public:
        DummyAIController(shared_ptr<HostServices> _host, int _id, Actor::Gender _gender, shared_ptr<ControllerPosition> _position) : 
            Controller(_host, _id, _gender, _position)
        {
            m_intentFactory = _host->services().get<IntentFactory>();
            m_clearanceFactory = _host->services().get<ClearanceFactory>();

            if (m_speechStyle.rate == world::Actor::SpeechRate::Slow)
            {
                m_speechStyle.rate = world::Actor::SpeechRate::Fast;
            }
        }
    public:
        void receiveIntent(shared_ptr<Intent> intent) override 
        {
            host()->writeLog(
                "AICONT|receiveIntent flight[%s] intent code[%d]",
                intent->subjectFlight()->callSign().c_str(),
                intent->code());

            shared_ptr<Intent> reply;

            switch (intent->code())
            {
            case PilotIfrClearanceRequestIntent::IntentCode:
                reply = m_intentFactory->deliveryIfrClearanceReply(
                    intent->subjectFlight(), 
                     m_clearanceFactory->ifrClearance(intent->subjectFlight()),
                     intent->id()
                );
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
                break;
            case PilotReportHoldingShortIntent::IntentCode:
                {
                    auto typedIntent = dynamic_pointer_cast<PilotReportHoldingShortIntent>(intent);
                    if (typedIntent)
                    {
                        auto flightPlan = intent->subjectFlight()->plan();
                        bool isDeparting =
                            flightPlan->departureAirportIcao() == airport()->header().icao() &&
                            flightPlan->departureRunway().length() > 0;
                        host()->writeLog(
                            "AICONT|PilotReportHoldingShortIntent, isDeparting: [%s] == [%s] ? %d",
                            flightPlan->departureAirportIcao().c_str(),
                            airport()->header().icao().c_str(),
                            isDeparting);
                        auto departureRunway = isDeparting
                            ? airport()->getRunwayOrThrow(flightPlan->departureRunway())
                            : nullptr;
                        bool isDepartureRunway =
                            isDeparting && departureRunway && (
                                typedIntent->runway() == departureRunway->end1().name() ||
                                typedIntent->runway() == departureRunway->end2().name());
                        host()->writeLog(
                            "AICONT|PilotReportHoldingShortIntent, isDepartureRunway: [%s] == [%s]||[%s] ? %d",
                             typedIntent->runway().c_str(),
                             departureRunway ? departureRunway->end1().name().c_str() : "N/A",
                             departureRunway ? departureRunway->end2().name().c_str() : "N/A",
                             isDepartureRunway);
                        reply = isDepartureRunway
                            ? m_intentFactory->groundSwitchToTower(intent->subjectFlight(), intent->id())
                            : m_intentFactory->groundCrossRunwayClearance(m_clearanceFactory->runwayCrossCleaeance(
                                  typedIntent->subjectFlight(),
                                  typedIntent->runway()
                              ), intent->id());
                    }
                }
                break;
            case PilotCheckInWithTowerIntent::IntentCode:
                reply = m_intentFactory->towerLineUp(
                    m_clearanceFactory->lineupApproval(intent->subjectFlight(), true),
                    intent->id()
                );
                break;
            case PilotLineUpReadbackIntent::IntentCode:
                host()->getWorld()->deferBy(
                    "takeOffClearance/" + intent->subjectFlight()->callSign(),
                    chrono::seconds(20),
                    [=]() {
                        auto runway = airport()->getRunwayOrThrow(intent->subjectFlight()->plan()->departureRunway());
                        const auto& runwayEnd = runway->getEndOrThrow(intent->subjectFlight()->plan()->departureRunway());
                        float initialHeading = runwayEnd.heading() + m_departureInitialTurn;
                        auto clearance = m_clearanceFactory->takeoffClearance(
                            intent->subjectFlight(),
                            initialHeading,
                            false
                        );
                        position()->frequency()->enqueueTransmission(
                            m_intentFactory->towerClearedForTakeoff(clearance)
                        );
                        m_departureInitialTurn = (m_departureInitialTurn < 60 ? m_departureInitialTurn + 15 : 15);
                    }
                );
                break;
            case PilotReportFinalIntent::IntentCode:
                {
                    auto runway = airport()->getRunwayOrThrow(intent->subjectFlight()->plan()->arrivalRunway());
                    const auto& runwayEnd = runway->getEndOrThrow(intent->subjectFlight()->plan()->arrivalRunway());
                    auto ground = airport()->groundAt(runwayEnd.centerlinePoint().geo());
                    auto clearance = m_clearanceFactory->landingClearance(
                        intent->subjectFlight(),
                        runwayEnd.name(),
                        ground->frequency()->khz());
                    reply = m_intentFactory->towerClearedForLanding(clearance, intent->id());
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
            }

            if (reply)
            {
                position()->frequency()->enqueueTransmission(reply);
            }

            host()->writeLog("AICONT|receiveIntent - done");
        }

        void progressTo(chrono::microseconds timestamp) override
        {
            //TODO
        }
    };
}
