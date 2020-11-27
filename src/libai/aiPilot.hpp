// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <chrono>
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
            m_departureAirport = m_helper.getDepartureAirport(_flight);

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
                        newClearance = landingClearance;
                    }
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
                maneuverTakeoff()
            });

            return result;
        }

        shared_ptr<Maneuver> maneuverFinalToGate(const Runway::End& landingRunway)
        {
            return M.sequence(Maneuver::Type::ArrivalApproach, "final_to_gate", {
                maneuverFinal(),
                maneuverLanding(),
                M.deferred([this]() {
                    return maneuverArrivalTaxiToGate();
                })
            });
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
                    M.delay(chrono::seconds(10)),
                    flaps15GearDown,
                    M.delay(chrono::seconds(20)),
                    flaps40,
                }),
                M.sequence(Maneuver::Type::Unspecified, "communicate", {
                    M.tuneComRadio(flight(), tower->frequency()),
                    M.transmitIntent(flight(), I.pilotReportFinal(flight()), "twr_report_final"),
                    M.await(Maneuver::Type::Unspecified, "await_twr_reply", [this]{
                        return (m_continueApproach || flight()->tryFindClearance<LandingClearance>(Clearance::Type::LandingClearance));
                    }),
                    M.deferred([this,tower,landingRunwayName]{
                        if (m_continueApproach)
                        {
                            return M.transmitIntent(
                                flight(),
                                I.pilotContinueApproachReadback(flight(), tower, landingRunwayName, m_lastReceivedIntentId));
                        }
                        return M.instantAction([]{});
                    }),
                    M.awaitClearance(flight(), Clearance::Type::LandingClearance, "await_landing_clrnc"),
                    M.deferred([=](){
                        auto clearance = flight()->findClearanceOrThrow<LandingClearance>(Clearance::Type::LandingClearance);
                        auto readback = I.pilotLandingClearanceReadback(flight(), clearance, m_lastReceivedIntentId);
                        return M.transmitIntent(flight(), readback, "readback_landing_clrnc");
                    })
                })
            });
        }

        shared_ptr<Maneuver> maneuverLanding()
        {
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
                    -1000,
                    -500,
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
                    145,
                    135,
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
                        -500,
                        -50,
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
                    135,
                    30,
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

            const auto safeCreateExitManeuver = [
                this, &exitFirstEdge, &exitLastEdge, &exitName, airport, runway, gate, aircraft, runwayEnd
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
                    M.sequence(Maneuver::Type::Unspecified, "", {
                        M.await(Maneuver::Type::Unspecified, "", [this,exitFirstEdge]{
                            return (!exitFirstEdge) || isPointBehind(exitFirstEdge->node2()->location().geo());
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
                    }),
                    M.sequence(Maneuver::Type::Unspecified, "", {
                       exitRunway,
                       logVacatedActive,
                       taxiLights,
                       M.awaitClearance(flight(), Clearance::Type::ArrivalTaxiClearance),
                       M.parallel(Maneuver::Type::Unspecified, "", {
                           M.deferred([=]{
                               auto clearance = flight()->findClearanceOrThrow<ArrivalTaxiClearance>(Clearance::Type::ArrivalTaxiClearance);
                               return M.taxiByPath(
                                   flight(),
                                   clearance->taxiPath(),
                                   ManeuverFactory::TaxiType::Normal,
                                   onHoldingShort);
                           }),
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
            auto airport = m_helper.getDepartureAirport(flight());
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
            auto airport = m_helper.getDepartureAirport(flight());
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
                auto runway = m_departureAirport->getRunwayOrThrow(clearance->departureRunway());
                const auto& runwayEnd = runway->getEndOrThrow(clearance->departureRunway());

                auto centerlinePoint = taxiPath->toNode->location().geo();
                auto lineupPoint1 = GeoMath::getPointAtDistance(
                    centerlinePoint,
                    runwayEnd.heading(),
                    30);
                auto lineupPoint2 = GeoMath::getPointAtDistance(
                    centerlinePoint,
                    runwayEnd.heading(),
                    60);

                taxiPath->appendEdgeTo(UniPoint::fromGeo(host(), lineupPoint1));
                taxiPath->appendEdgeTo(UniPoint::fromGeo(host(), lineupPoint2));
            };

            const auto onHoldingShort = [=](shared_ptr<TaxiEdge> holdShortEdge) {
                auto departureRunway = m_departureAirport->getRunwayOrThrow(m_flightPlan->departureRunway());
                bool isHoldingShortDepartureRunway = holdShortEdge->activeZones().departue.has(departureRunway);
                shared_ptr<Maneuver> holdShortManeuver = isHoldingShortDepartureRunway
                    ? maneuverDepartureAwaitLineup(m_flightPlan->departureRunway(), holdShortEdge)
                    : maneuverAwaitCrossRunway(m_departureAirport, holdShortEdge);
                return holdShortManeuver;
            };

            return DeferredManeuver::create(Maneuver::Type::DepartureTaxi, "departure_taxi", [=]() {
                auto clearance = flight()->findClearanceOrThrow<DepartureTaxiClearance>(Clearance::Type::DepartureTaxiClearance);
                addLineupEdges(clearance);

                vector<shared_ptr<Maneuver>> steps;
                steps.push_back(M.delay(chrono::seconds(10)));
                steps.push_back(M.switchLights(flight(), Aircraft::LightBits::BeaconTaxi));
                steps.push_back(M.delay(chrono::seconds(5)));
                steps.push_back(M.taxiByPath(
                    flight(), 
                    clearance->taxiPath(), 
                    ManeuverFactory::TaxiType::Normal,
                    onHoldingShort));
                steps.push_back(M.instantAction([this]{
                    m_linedUpTimestamp = host()->getWorld()->timestamp();
                }));

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
                }),
                M.transmitIntent(flight(), I.pilotReportHoldingShort(
                    flight(),
                    airport,
                    runwayName,
                    holdShortEdge->name()
                )),
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
                    2500.0,
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

        bool isPointBehind(const GeoPoint& point)
        {
            float headingToPoint = GeoMath::getHeadingFromPoints(aircraft()->location(), point);
            float turnToNodeDegrees = GeoMath::getTurnDegrees(aircraft()->attitude().heading(), headingToPoint);
            return (abs(turnToNodeDegrees) >= 45);
        }
    };
}

