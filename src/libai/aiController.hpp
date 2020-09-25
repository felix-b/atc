// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
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

            switch (id())
            {
            case 1:
                m_speechStyle.voice = VoiceType::Treble;
                m_speechStyle.rate = SpeechRate::Slow;
                m_speechStyle.radioQuality = RadioQuality::Good;
                m_speechStyle.pttDelayBeforeSpeech = chrono::milliseconds(750);
                m_speechStyle.pttDelayAfterSpeech = chrono::milliseconds(500);
                m_speechStyle.disfluencyProbability = 0.5f;
                m_speechStyle.selfCorrectionProbability = 0.5f;
                break;
            case 2:
                m_speechStyle.voice = VoiceType::Contralto;
                m_speechStyle.rate = SpeechRate::Fast;
                m_speechStyle.radioQuality = RadioQuality::Good;
                m_speechStyle.pttDelayBeforeSpeech = chrono::milliseconds(100);
                m_speechStyle.pttDelayAfterSpeech = chrono::milliseconds(0);
                m_speechStyle.disfluencyProbability = 0;
                m_speechStyle.selfCorrectionProbability = 0;
                break;
            case 3:
                m_speechStyle.voice = VoiceType::Tenor;
                m_speechStyle.rate = SpeechRate::Fast;
                m_speechStyle.radioQuality = RadioQuality::Good;
                m_speechStyle.pttDelayBeforeSpeech = chrono::milliseconds(100);
                m_speechStyle.pttDelayAfterSpeech = chrono::milliseconds(0);
                m_speechStyle.disfluencyProbability = 0;
                m_speechStyle.selfCorrectionProbability = 0;
                break;
            }
        }
    public:
        void receiveIntent(shared_ptr<Intent> intent) override 
        {
            shared_ptr<Intent> reply;

            switch (intent->code())
            {
            case PilotIfrClearanceRequestIntent::IntentCode:
                reply = m_intentFactory->deliveryIfrClearanceReply(
                    intent->subjectFlight(), 
                     m_clearanceFactory->ifrClearance(intent->subjectFlight())
                );
                break;
            case PilotIfrClearanceReadbackIntent::IntentCode:
                {
                    auto clearance = intent->subjectFlight()->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance);
                    reply = m_intentFactory->deliveryIfrClearanceReadbackCorrect(
                        intent->subjectFlight()
                    );
                }
                break;
            case PilotPushAndStartRequestIntent::IntentCode:
                reply = m_intentFactory->groundPushAndStartReply(
                    intent->subjectFlight(), 
                    m_clearanceFactory->pushAndStartApproval(intent->subjectFlight())
                );
                break;
            case PilotDepartureTaxiRequestIntent::IntentCode:
                reply = m_intentFactory->groundDepartureTaxiReply(
                    intent->subjectFlight(), 
                    m_clearanceFactory->departureTaxiClearance(intent->subjectFlight())
                );
                break;
            case PilotReportHoldingShortIntent::IntentCode:
                reply = m_intentFactory->groundSwitchToTower(
                    intent->subjectFlight()
                );
                break;
            case PilotCheckInWithTowerIntent::IntentCode:
                reply = m_intentFactory->towerLineUp(
                    m_clearanceFactory->lineupApproval(intent->subjectFlight(), true)
                );
                break;
            case PilotLineUpReadbackIntent::IntentCode:
                host()->getWorld()->deferBy(chrono::seconds(20), [=]() {
                    auto airport = host()->getWorld()->getAirport(intent->subjectFlight()->plan()->departureAirportIcao());
                    auto runway = airport->getRunwayOrThrow(intent->subjectFlight()->plan()->departureRunway());
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
                });
                break;
            case PilotReportFinalIntent::IntentCode:
                {
                    auto airport = host()->getWorld()->getAirport(intent->subjectFlight()->plan()->arrivalAirportIcao());
                    auto runway = airport->getRunwayOrThrow(intent->subjectFlight()->plan()->arrivalRunway());
                    const auto& runwayEnd = runway->getEndOrThrow(intent->subjectFlight()->plan()->arrivalRunway());
                    auto ground = airport->groundAt(runwayEnd.centerlinePoint().geo());
                    auto clearance = m_clearanceFactory->landingClearance(
                        intent->subjectFlight(),
                        runwayEnd.name(),
                        ground->frequency()->khz());
                    reply = m_intentFactory->towerClearedForLanding(clearance);
                }
                break;
            }

            if (reply)
            {
                position()->frequency()->enqueueTransmission(reply);
            }
        }

        void progressTo(chrono::microseconds timestamp) override
        {
            //TODO
        }
    };
}
