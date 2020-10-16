// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <sstream>
#include "libworld.h"
#include "clearanceTypes.hpp"
#include "intentTypes.hpp"
#include "worldHelper.hpp"

using namespace std;

namespace world
{
    class IntentFactory
    {
    private:
        shared_ptr<HostServices> m_host;
        WorldHelper m_helper;
        uint64_t m_nextIntentId;
    public:
        IntentFactory(shared_ptr<HostServices> _host) : 
            m_host(_host),
            m_helper(_host),
            m_nextIntentId(1)
        {
        }
    public:
        shared_ptr<Intent> pilotAffirmation(shared_ptr<Flight> flight, shared_ptr<ControllerPosition> subjectControl, uint64_t replyToId)
        {
            return shared_ptr<Intent>(new PilotAffirmationIntent(
                m_nextIntentId++,
                replyToId,
                flight, 
                subjectControl
            ));
        }

        shared_ptr<Intent> pilotHandoffReadback(shared_ptr<Flight> flight, shared_ptr<ControllerPosition> subjectControl, int newFrequencyKhz, uint64_t replyToId)
        {
            return shared_ptr<Intent>(new PilotHandoffReadbackIntent(
                m_nextIntentId++,
                replyToId,
                flight,
                subjectControl,
                newFrequencyKhz
            ));
        }

        shared_ptr<Intent> pilotIfrClearanceRequest(shared_ptr<Flight> flight)
        {
            auto clearanceDelivery = m_helper.getClearanceDelivery(flight);
            return shared_ptr<Intent>(new PilotIfrClearanceRequestIntent(
                m_nextIntentId++,
                "Q",
                flight,
                clearanceDelivery
            ));
        }

        shared_ptr<Intent> deliveryIfrClearanceReply(shared_ptr<Flight> flight, shared_ptr<IfrClearance> clearance, uint64_t replyToId)
        {
            auto clearanceDelivery = m_helper.getClearanceDelivery(flight);
            return shared_ptr<Intent>(new DeliveryIfrClearanceReplyIntent(
                m_nextIntentId++,
                replyToId,
                clearanceDelivery,
                flight,
                !!clearance,
                clearance
            ));
        }

        shared_ptr<Intent> pilotIfrClearanceReadback(shared_ptr<Flight> flight, uint64_t replyToId)
        {
            auto clearance = flight->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance);
            return shared_ptr<Intent>(new PilotIfrClearanceReadbackIntent(
                m_nextIntentId++,
                replyToId,
                clearance
            ));
        }

        shared_ptr<Intent> deliveryIfrClearanceReadbackCorrect(shared_ptr<Flight> flight, uint64_t replyToId)
        {
            auto clearance = flight->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance);
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new DeliveryIfrClearanceReadbackCorrectIntent(
                m_nextIntentId++,
                replyToId,
                clearance,
                true,
                ground->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotPushAndStartRequest(shared_ptr<Flight> flight)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new PilotPushAndStartRequestIntent(
                m_nextIntentId++,
                flight,
                ground
            ));
        }

        shared_ptr<Intent> groundPushAndStartReply(shared_ptr<Flight> flight, shared_ptr<PushAndStartApproval> approval, uint64_t replyToId)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new GroundPushAndStartReplyIntent(
                m_nextIntentId++,
                replyToId,
                ground,
                flight,
                !!approval,
                approval
            ));
        }

        shared_ptr<Intent> pilotPushAndStartReadback(shared_ptr<Flight> flight, shared_ptr<ControllerPosition> ground, uint64_t replyToId)
        {
            auto approval = flight->findClearanceOrThrow<PushAndStartApproval>(Clearance::Type::PushAndStartApproval);
            return shared_ptr<Intent>(new PilotAffirmationIntent(
                m_nextIntentId++,
                replyToId,
                flight,
                ground
            ));
        }

        shared_ptr<Intent> pilotDepartureTaxiRequest(shared_ptr<Flight> flight)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new PilotDepartureTaxiRequestIntent(
                m_nextIntentId++,
                flight,
                ground
            ));
        }

        shared_ptr<Intent> groundDepartureTaxiReply(shared_ptr<Flight> flight, shared_ptr<DepartureTaxiClearance> clearance, uint64_t replyToId)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new GroundDepartureTaxiReplyIntent(
                m_nextIntentId++,
                replyToId,
                ground,
                flight,
                !!clearance,
                clearance
            ));
        }

        shared_ptr<Intent> pilotDepartureTaxiReadback(shared_ptr<Flight> flight, uint64_t replyToId)
        {
            auto clearance = flight->findClearanceOrThrow<DepartureTaxiClearance>(Clearance::Type::DepartureTaxiClearance);
            return shared_ptr<Intent>(new PilotDepartureTaxiReadbackIntent(
                m_nextIntentId++,
                replyToId,
                clearance
            ));
        }

        shared_ptr<Intent> pilotReportHoldingShort(shared_ptr<Flight> flight, const string& runway, const string& holdingPoint)
        {
            auto tower = m_helper.getDepartureGround(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new PilotReportHoldingShortIntent(
                m_nextIntentId++,
                flight,
                tower,
                runway,
                holdingPoint
            ));
        }

        shared_ptr<Intent> groundCrossRunwayClearance(shared_ptr<RunwayCrossClearance> clearance, uint64_t replyToId)
        {
            m_host->writeLog("INTNTF|groundCrossRunwayClearance");
            return shared_ptr<Intent>(new GroundRunwayCrossClearanceIntent(
                m_nextIntentId++,
                replyToId,
                clearance
            ));
        }

        shared_ptr<Intent> groundSwitchToTower(shared_ptr<Flight> flight, uint64_t replyToId)
        {
            m_host->writeLog("INTNTF|groundSwitchToTower");
            auto ground = m_helper.getDepartureGround(flight);
            auto tower = m_helper.getDepartureTower(flight);
            return shared_ptr<Intent>(new GroundSwitchToTowerIntent(
                m_nextIntentId++,
                replyToId,
                flight,
                ground,
                tower->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotCheckInWithTower(shared_ptr<Flight> flight, const string& runway, const string& holdingPoint, bool haveNumbers)
        {
            auto tower = m_helper.getDepartureTower(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new PilotCheckInWithTowerIntent(
                m_nextIntentId++,
                flight,
                tower,
                runway,
                holdingPoint,
                haveNumbers
            ));
        }

        shared_ptr<Intent> towerLineUp(shared_ptr<LineupApproval> approval, uint64_t replyToId)
        {
            return shared_ptr<Intent>(new TowerLineUpIntent(
                m_nextIntentId++,
                replyToId,
                approval
            ));
        }

        shared_ptr<Intent> pilotLineUpReadback(shared_ptr<LineupApproval> approval, uint64_t replyToId)
        {
            return shared_ptr<Intent>(new PilotLineUpReadbackIntent(
                m_nextIntentId++,
                replyToId,
                approval
            ));
        }

        shared_ptr<Intent> towerClearedForTakeoff(shared_ptr<TakeoffClearance> clearance)
        {
            auto flight = clearance->header().issuedTo;
            auto tower = m_helper.getDepartureTower(flight);
            auto departure = m_helper.tryGetDeparture(flight);
            
            return shared_ptr<Intent>(new TowerClearedForTakeoffIntent(
                m_nextIntentId++,
                tower,
                flight,
                true,
                clearance,
                departure ? departure->frequency()->khz() : 0
            ));
        }

        shared_ptr<Intent> pilotTakeoffClearanceReadback(shared_ptr<Flight> flight, shared_ptr<TakeoffClearance> clearance, int departureKhz, uint64_t replyToId)
        {
            auto tower = m_helper.getDepartureTower(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new PilotTakeoffClearanceReadbackIntent(
                m_nextIntentId++,
                replyToId,
                clearance,
                departureKhz
            ));
        }

        shared_ptr<Intent> pilotReportFinal(shared_ptr<Flight> flight)
        {
            auto runway = flight->plan()->arrivalRunway();
            auto landingPoint = m_helper.getLandingPoint(flight);
            auto tower = m_helper.getArrivalTower(flight, landingPoint);
            
            return shared_ptr<Intent>(new PilotReportFinalIntent(
                m_nextIntentId++,
                flight,
                tower,
                runway
            ));
        }

        shared_ptr<Intent> towerClearedForLanding(shared_ptr<LandingClearance> clearance, uint64_t replyToId)
        {
            auto flight = clearance->header().issuedTo;
            auto landingPoint = m_helper.getLandingPoint(flight);
            auto ground = m_helper.getArrivalGround(flight, landingPoint);
            
            return shared_ptr<Intent>(new TowerClearedForLandingIntent(
                m_nextIntentId++,
                replyToId,
                clearance->header().issuedBy,
                flight,
                true,
                clearance,
                ground->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotLandingClearanceReadback(shared_ptr<Flight> flight, shared_ptr<LandingClearance> clearance, uint64_t replyToId)
        {
            auto landingPoint = m_helper.getLandingPoint(flight);
            auto tower = m_helper.getArrivalTower(flight, landingPoint);

            return shared_ptr<Intent>(new PilotLandingClearanceReadbackIntent(
                m_nextIntentId++,
                replyToId,
                clearance,
                clearance->groundKhz()
            ));
        }

        shared_ptr<Intent> pilotArrivalCheckInWithGround(
            shared_ptr<Flight> flight,
            const string& runway,
            const string& exitName,
            shared_ptr<TaxiEdge> exitEdge = nullptr)
        {
            auto ground = m_helper.getArrivalGround(flight, flight->aircraft()->location());
            return shared_ptr<Intent>(new PilotArrivalCheckInWithGroundIntent(
                m_nextIntentId++,
                flight,
                ground,
                runway,
                exitName,
                exitEdge
            ));
        }

        shared_ptr<Intent> groundArrivalTaxiReply(shared_ptr<ArrivalTaxiClearance> clearance, uint64_t replyToId)
        {
            return shared_ptr<Intent>(new GroundArrivalTaxiReplyIntent(
                m_nextIntentId++,
                replyToId,
                clearance->header().issuedBy,
                clearance->header().issuedTo,
                !!clearance,
                clearance
            ));
        }

        shared_ptr<Intent> pilotArrivalTaxiReadback(shared_ptr<Flight> flight, uint64_t replyToId)
        {
            auto clearance = flight->findClearanceOrThrow<ArrivalTaxiClearance>(Clearance::Type::ArrivalTaxiClearance);
            return shared_ptr<Intent>(new PilotArrivalTaxiReadbackIntent(
                m_nextIntentId++,
                replyToId,
                clearance
            ));
        }
    };
}
