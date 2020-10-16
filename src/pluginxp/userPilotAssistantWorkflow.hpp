// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <memory>
#include <string>
#include <algorithm>
#include <unordered_map>

// PPL
#include "owneddata.h"

// AT&C
#include "libworld.h"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "stateMachine.hpp"
#include "transcriptInterface.hpp"


using namespace std;
using namespace world;
using namespace PPL;

enum class PilotState
{
    PerformingPreflightProcedures = 10,
    FlightPlanFiled = 20,
    RequestingIfrClearance = 30,
    HaveIfrClearance = 40,
    RequestingPushAndStart = 50,
    PerformingPushbackAndStart = 60,
    RequestingDepartureTaxi = 70,
    PerformingDepartureTaxi = 80,
    HoldingShortRunway = 90,
    GroundHandedOffToTower = 100,
    CheckingInWithTower = 110,
    HoldingShortDepartureRunwayWithTower = 120,
    PerformingLineUpAndWait = 130,
    PerformingTakeoff = 140,
};

enum class PilotTrigger
{
    None = 0,
    IntentReceived = 10,
    FileFlightPlan = 20,
    RequestIfrClearance = 30,
    GotIfrClearance = 40,
    RequestPushAndStart = 50,
    GotPushAndStartApproval = 60,
    RequestDepartureTaxi = 70,
    ClearedForDepartureTaxi = 80,
    ReportHoldingShortRunway = 90,
    ClearedToCrossRunway = 100,
    GroundHandedOffToTower = 110,
    CheckInWithTower = 120,
    ApprovedToLineUpAndWait = 130,
    ClearedForTakeoff = 140
};

class UserPilotAssistantWorkflow : public StateMachine<PilotState, PilotTrigger>
{
private:
    shared_ptr<TranscriptInterface> m_transcript;
    shared_ptr<IntentFactory> m_intentFactory;
    WorldHelper m_helper;
    shared_ptr<Flight> m_flight;
    shared_ptr<Airport> m_departureAirport;
    shared_ptr<ParkingStand> m_departureGate;
    DataRef<int> m_com1FrequencyKhz;
    shared_ptr<Intent> m_lastReceivedIntent;
    string m_holdingShortRunwayName;
    IntentFactory& I;
    world::Aircraft& A;
    WorldHelper& H;
public:
    UserPilotAssistantWorkflow(
        shared_ptr<HostServices> _host,
        shared_ptr<Flight> _flight,
        shared_ptr<Airport> _departureAirport
    ) : StateMachine<PilotState, PilotTrigger>(_host, "UPILOT"),
        m_transcript(_host->services().get<TranscriptInterface>()),
        m_intentFactory(_host->services().get<IntentFactory>()),
        m_flight(_flight),
        m_departureAirport(_departureAirport),
        m_com1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadWrite),
        m_helper(_host),
        I(*_host->services().get<IntentFactory>()),
        A(*_flight->aircraft()),
        H(m_helper)
    {
        m_flight->aircraft()->onCommTransmission([this](shared_ptr<Intent> intent) {
            if (intent->subjectFlight() == m_flight)
            {
                host()->writeLog("UPILOT|Received transmission: intent code[%d]", intent->code());
                m_lastReceivedIntent = intent;
                receiveTrigger(PilotTrigger::IntentReceived);
            }
        });

        host()->writeLog("UserPilotAssistantWorkflow::UserPilotAssistantWorkflow:1");
        build();
        host()->writeLog("UserPilotAssistantWorkflow::UserPilotAssistantWorkflow:2");
        transitionToState(PilotState::PerformingPreflightProcedures);
        host()->writeLog("UserPilotAssistantWorkflow::UserPilotAssistantWorkflow:3");
    }
private:
    void STATE(
        PilotState stateId,
        const vector<Transition> transitions = {},
        const vector<TranscriptInterface::TransmissionOption> options = {})
    {
        host()->writeLog("STATE: adding STATE [%d]", stateId);
        STATE(stateId, []{}, transitions, options);
    }

    void STATE(
        PilotState stateId,
        function<void()> onEnter,
        const vector<Transition> transitions = {},
        const vector<TranscriptInterface::TransmissionOption> options = {})
    {
        host()->writeLog("STATE: adding STATE [%d]", stateId);

        const auto effectiveOnEnter = [this, onEnter, options]{
            m_transcript->setTransmissionOptions(options);
            onEnter();
        };

        const auto effectiveOnExit = [this]{
            m_transcript->setTransmissionOptions({});
        };

        addState(stateId, [this, stateId, transitions, effectiveOnEnter, effectiveOnExit] {
            return shared_ptr<State>(new DeclarativeState(
                stateId,
                getStateName(stateId),
                transitions,
                effectiveOnEnter,
                effectiveOnExit));
        });
    }

    Transition ON_TRIGGER(PilotTrigger triggerId, PilotState targetStateId)
    {
        return Transition(
            [=](PilotTrigger t) {
                return t == triggerId;
            },
            [=](StateMachine& m) {
                m.transitionToState(targetStateId);
            }
        );
    }

    Transition ON_TRIGGER(PilotTrigger triggerId, function<PilotState()> handler)
    {
        return Transition(
            [=](PilotTrigger t) {
                return t == triggerId;
            },
            [=](StateMachine& m) {
                PilotState newStateId = handler();
                if (newStateId != currentState()->id())
                {
                    m.transitionToState(newStateId);
                }
            }
        );
    }

    template<class TIntent>
    Transition ON_INTENT(function<PilotTrigger(shared_ptr<TIntent> intent)> handler)
    {
        return Transition(
            [this](PilotTrigger t) {
                return (
                    t == PilotTrigger::IntentReceived &&
                    m_lastReceivedIntent &&
                    m_lastReceivedIntent->code() == TIntent::IntentCode);
            },
            [=](StateMachine& m) {
                shared_ptr<TIntent> typedIntent = dynamic_pointer_cast<TIntent>(m_lastReceivedIntent);
                if (!typedIntent)
                {
                    throw runtime_error(
                        "Intent code [" + to_string(m_lastReceivedIntent->code()) +
                        "] cannot be cast to type [" + typeid(TIntent).name() + "]");
                }
                PilotTrigger trigger = handler(typedIntent);
                if (trigger != PilotTrigger::None)
                {
                    m.receiveTrigger(trigger);
                }
            }
        );
    }

    template<class TIntent>
    Transition ON_INTENT(PilotTrigger trigger)
    {
        return ON_INTENT<TIntent>([=](shared_ptr<TIntent>){
            return trigger;
        });
    }

    TranscriptInterface::TransmissionOption OPTION(const string& label, PilotTrigger triggerId)
    {
        return {
            label,
            [this, triggerId]{
                receiveTrigger(triggerId);
            }
        };
    }

    TranscriptInterface::TransmissionOption OPTION(const string& label, function<PilotTrigger()> handler)
    {
        return {
            label,
            [this, handler]{
                PilotTrigger triggerId = handler();
                receiveTrigger(triggerId);
            }
        };
    }

    void TRANSMIT(shared_ptr<Frequency> frequency, shared_ptr<Intent> intent)
    {
        if (frequency)
        {
            host()->writeLog("%s|STATEM TRANSMIT: tuning COM1 to [%d]", nameForLog().c_str(), frequency->khz());
            m_com1FrequencyKhz = frequency->khz();
            frequency->enqueueTransmission(intent);
        }
        else
        {
            host()->writeLog(
                "%s|STATEM WARNING: could not transmit, frequency not found", nameForLog().c_str());
        }
    }

    void build()
    {
        STATE(PilotState::PerformingPreflightProcedures, {
            ON_TRIGGER(PilotTrigger::FileFlightPlan, [this]() {
                bool filed = fileUserFlightPlan();
                return filed
                    ? PilotState::FlightPlanFiled
                    : PilotState::PerformingPreflightProcedures;
            })
        }, {
            OPTION("ATC: File flight plan from FMC", PilotTrigger::FileFlightPlan)
        });

        STATE(PilotState::FlightPlanFiled,{
            ON_TRIGGER(PilotTrigger::RequestIfrClearance, PilotState::RequestingIfrClearance)
        }, {
            OPTION("CLR/DEL: Request IFR clearance", PilotTrigger::RequestIfrClearance)
        });

        STATE(PilotState::RequestingIfrClearance, [this]{
            TRANSMIT(H.getClearanceDelivery(m_flight)->frequency(), I.pilotIfrClearanceRequest(m_flight));
        }, {
            ON_INTENT<DeliveryIfrClearanceReplyIntent>([this](shared_ptr<DeliveryIfrClearanceReplyIntent> intent) {
                m_flight->addClearance(intent->clearance());
                TRANSMIT(H.getClearanceDelivery(m_flight)->frequency(), I.pilotIfrClearanceReadback(m_flight, intent->id()));
                return PilotTrigger::None;
            }),
            ON_INTENT<DeliveryIfrClearanceReadbackCorrectIntent>(PilotTrigger::GotIfrClearance),
            ON_TRIGGER(PilotTrigger::GotIfrClearance, PilotState::HaveIfrClearance),
        });

        STATE(PilotState::HaveIfrClearance, {
            ON_TRIGGER(PilotTrigger::RequestPushAndStart, PilotState::RequestingPushAndStart)
        }, {
            OPTION("GND: Request push and start", PilotTrigger::RequestPushAndStart)
        });

        STATE(PilotState::RequestingPushAndStart, [this]{
            TRANSMIT(
                H.getDepartureGround(m_flight)->frequency(),
                I.pilotPushAndStartRequest(m_flight));
        }, {
            ON_INTENT<GroundPushAndStartReplyIntent>([this](shared_ptr<GroundPushAndStartReplyIntent> intent) {
                m_flight->addClearance(intent->approval());
                TRANSMIT(
                    H.getDepartureGround(m_flight)->frequency(),
                    I.pilotPushAndStartReadback(m_flight, intent->subjectControl(), intent->id()));
                return PilotTrigger::GotPushAndStartApproval;
            }),
            ON_TRIGGER(PilotTrigger::GotPushAndStartApproval, PilotState::PerformingPushbackAndStart),
        });

        STATE(PilotState::PerformingPushbackAndStart, {
            ON_TRIGGER(PilotTrigger::RequestDepartureTaxi, PilotState::RequestingDepartureTaxi)
        }, {
            OPTION("GND: Request taxi", PilotTrigger::RequestDepartureTaxi)
        });

        STATE(PilotState::RequestingDepartureTaxi, [this]{
            TRANSMIT(
                H.getDepartureGround(m_flight)->frequency(),
                I.pilotDepartureTaxiRequest(m_flight));
        }, {
            ON_INTENT<GroundDepartureTaxiReplyIntent>([this](shared_ptr<GroundDepartureTaxiReplyIntent> intent) {
                m_flight->addClearance(intent->clearance());
                TRANSMIT(
                    H.getDepartureGround(m_flight)->frequency(),
                    I.pilotDepartureTaxiReadback(m_flight, intent->id()));
                return PilotTrigger::ClearedForDepartureTaxi;
            }),
            ON_TRIGGER(PilotTrigger::ClearedForDepartureTaxi, PilotState::PerformingDepartureTaxi),
        });

        vector<TranscriptInterface::TransmissionOption> departureTaxiOptions;
        appendDepartureHoldingShortOptions(departureTaxiOptions);

        STATE(PilotState::PerformingDepartureTaxi, {
            ON_TRIGGER(PilotTrigger::ReportHoldingShortRunway, PilotState::HoldingShortRunway)
        }, departureTaxiOptions);

        STATE(PilotState::HoldingShortRunway, [this]{
            TRANSMIT(
                H.getDepartureGround(m_flight)->frequency(),
                I.pilotReportHoldingShort(m_flight, m_holdingShortRunwayName, ""));
        }, {
            ON_INTENT<GroundRunwayCrossClearanceIntent>([this](shared_ptr<GroundRunwayCrossClearanceIntent> intent) {
                m_flight->addClearance(intent->clearance());
                TRANSMIT(
                    H.getDepartureGround(m_flight)->frequency(),
                    I.pilotAffirmation(m_flight, intent->subjectControl(), intent->id()));
                return PilotTrigger::ClearedToCrossRunway;
            }),
            ON_INTENT<GroundSwitchToTowerIntent>([this](shared_ptr<GroundSwitchToTowerIntent> intent) {
                TRANSMIT(
                    H.getDepartureGround(m_flight)->frequency(),
                    I.pilotHandoffReadback(m_flight, intent->subjectControl(), intent->towerKhz(), intent->id()));
                return PilotTrigger::GroundHandedOffToTower;
            }),
            ON_TRIGGER(PilotTrigger::ClearedToCrossRunway, PilotState::PerformingDepartureTaxi),
            ON_TRIGGER(PilotTrigger::GroundHandedOffToTower, PilotState::GroundHandedOffToTower),
        });

        STATE(PilotState::GroundHandedOffToTower, {
            ON_TRIGGER(PilotTrigger::CheckInWithTower, PilotState::CheckingInWithTower)
        }, {
            OPTION("TWR: Check in with tower", PilotTrigger::CheckInWithTower)
        });

        STATE(PilotState::CheckingInWithTower, [this]{
            TRANSMIT(
                H.getDepartureTower(m_flight)->frequency(),
                I.pilotCheckInWithTower(m_flight, m_holdingShortRunwayName, "", false));
        }, {
            ON_INTENT<TowerLineUpIntent>([this](shared_ptr<TowerLineUpIntent> intent) {
                m_flight->addClearance(intent->approval());
                TRANSMIT(
                    H.getDepartureTower(m_flight)->frequency(),
                    I.pilotLineUpReadback(intent->approval(), intent->id()));
                return PilotTrigger::ApprovedToLineUpAndWait;
            }),
            ON_INTENT<TowerClearedForTakeoffIntent>([this](shared_ptr<TowerClearedForTakeoffIntent> intent) {
                m_flight->addClearance(intent->clearance());
                TRANSMIT(
                    H.getDepartureTower(m_flight)->frequency(),
                    I.pilotTakeoffClearanceReadback(m_flight, intent->clearance(), intent->departureKhz(), intent->id()));
                return PilotTrigger::ClearedForTakeoff;
            }),
            ON_TRIGGER(PilotTrigger::ApprovedToLineUpAndWait, PilotState::PerformingLineUpAndWait),
            ON_TRIGGER(PilotTrigger::ClearedForTakeoff, PilotState::PerformingTakeoff),
        });

        STATE(PilotState::PerformingLineUpAndWait, []{}, {
            ON_INTENT<TowerClearedForTakeoffIntent>([this](shared_ptr<TowerClearedForTakeoffIntent> intent) {
                m_flight->addClearance(intent->clearance());
                TRANSMIT(
                    H.getDepartureTower(m_flight)->frequency(),
                    I.pilotTakeoffClearanceReadback(m_flight, intent->clearance(), intent->departureKhz(), intent->id()));
                return PilotTrigger::ClearedForTakeoff;
            }),
            ON_TRIGGER(PilotTrigger::ClearedForTakeoff, PilotState::PerformingTakeoff),
        });

        STATE(PilotState::PerformingTakeoff);
    }

    bool fileUserFlightPlan()
    {
        return true;
    }

    void appendDepartureHoldingShortOptions(vector<TranscriptInterface::TransmissionOption>& options)
    {
        host()->writeLog("UserPilotAssistantWorkflow::appendDepartureHoldingShortOptions:1");

        vector<string> names;
        for (const auto& runway : m_departureAirport->runways())
        {
            names.push_back(runway->end1().name());
            names.push_back(runway->end2().name());
        }

        host()->writeLog("UserPilotAssistantWorkflow::appendDepartureHoldingShortOptions:2");

        sort(names.begin(), names.end());

        host()->writeLog("UserPilotAssistantWorkflow::appendDepartureHoldingShortOptions:3");

        for (const string& name : names)
        {
            options.push_back(OPTION(
                "GND: Holding short rwy " + name,
                [this, name] {
                    m_holdingShortRunwayName = name;
                    return PilotTrigger::ReportHoldingShortRunway;
                }
            ));
        }

        host()->writeLog("UserPilotAssistantWorkflow::appendDepartureHoldingShortOptions:4");
    }

    const char* getStateName(PilotState stateId)
    {
        switch (stateId)
        {
        case PilotState::PerformingPreflightProcedures: return "PerformingPreflightProcedures";
        case PilotState::FlightPlanFiled: return "FlightPlanFiled";
        case PilotState::RequestingIfrClearance: return "RequestingIfrClearance";
        case PilotState::HaveIfrClearance: return "HaveIfrClearance";
        case PilotState::RequestingPushAndStart: return "RequestingPushAndStart";
        case PilotState::PerformingPushbackAndStart: return "PerformingPushbackAndStart";
        case PilotState::RequestingDepartureTaxi: return "RequestingDepartureTaxi";
        case PilotState::PerformingDepartureTaxi: return "PerformingDepartureTaxi";
        case PilotState::HoldingShortRunway: return "HoldingShortRunway";
        case PilotState::GroundHandedOffToTower: return "GroundHandedOffToTower";
        case PilotState::CheckingInWithTower: return "CheckingInWithTower";
        case PilotState::HoldingShortDepartureRunwayWithTower: return "HoldingShortDepartureRunwayWithTower";
        case PilotState::PerformingLineUpAndWait: return "PerformingLineUpAndWait";
        case PilotState::PerformingTakeoff: return "PerformingTakeoff";
        default: return "???";
        }
    }
};
