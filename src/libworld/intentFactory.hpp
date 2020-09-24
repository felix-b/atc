// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
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
    public:
        IntentFactory(shared_ptr<HostServices> _host) : 
            m_host(_host),
            m_helper(_host)
        {
        }
    public:
        shared_ptr<Intent> pilotAffirmation(shared_ptr<Flight> flight, shared_ptr<ControllerPosition> subjectControl)
        {
            return shared_ptr<Intent>(new PilotAffirmationIntent(
                flight, 
                subjectControl
            ));
        }

        shared_ptr<Intent> pilotHandoffReadback(shared_ptr<Flight> flight, shared_ptr<ControllerPosition> subjectControl, int newFrequencyKhz)
        {
            return shared_ptr<Intent>(new PilotHandoffReadbackIntent(
                flight, 
                subjectControl,
                newFrequencyKhz
            ));
        }

        shared_ptr<Intent> pilotIfrClearanceRequest(shared_ptr<Flight> flight)
        {
            auto clearanceDelivery = m_helper.getClearanceDelivery(flight);
            return shared_ptr<Intent>(new PilotIfrClearanceRequestIntent(
                "Q",
                flight,
                clearanceDelivery
            ));
        }

        shared_ptr<Intent> deliveryIfrClearanceReply(shared_ptr<Flight> flight, shared_ptr<IfrClearance> clearance)
        {
            auto clearanceDelivery = m_helper.getClearanceDelivery(flight);
            return shared_ptr<Intent>(new DeliveryIfrClearanceReplyIntent(
                clearanceDelivery,
                flight,
                !!clearance,
                clearance
            ));
        }

        shared_ptr<Intent> pilotIfrClearanceReadback(shared_ptr<Flight> flight)
        {
            auto clearance = flight->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance);
            return shared_ptr<Intent>(new PilotIfrClearanceReadbackIntent(
                clearance
            ));
        }

        shared_ptr<Intent> deliveryIfrClearanceReadbackCorrect(shared_ptr<Flight> flight)
        {
            auto clearance = flight->findClearanceOrThrow<IfrClearance>(Clearance::Type::IfrClearance);
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new DeliveryIfrClearanceReadbackCorrectIntent(
                clearance,
                true,
                ground->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotPushAndStartRequest(shared_ptr<Flight> flight)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new PilotPushAndStartRequestIntent(
                flight,
                ground
            ));
        }

        shared_ptr<Intent> groundPushAndStartReply(shared_ptr<Flight> flight, shared_ptr<PushAndStartApproval> approval)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new GroundPushAndStartReplyIntent(
                ground,
                flight,
                !!approval,
                approval
            ));
        }

        shared_ptr<Intent> pilotPushAndStartReadback(shared_ptr<Flight> flight, shared_ptr<ControllerPosition> ground)
        {
            auto approval = flight->findClearanceOrThrow<PushAndStartApproval>(Clearance::Type::PushAndStartApproval);
            return shared_ptr<Intent>(new PilotAffirmationIntent(flight, ground));
        }

        shared_ptr<Intent> pilotDepartureTaxiRequest(shared_ptr<Flight> flight)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new PilotDepartureTaxiRequestIntent(
                flight,
                ground
            ));
        }

        shared_ptr<Intent> groundDepartureTaxiReply(shared_ptr<Flight> flight, shared_ptr<DepartureTaxiClearance> clearance)
        {
            auto ground = m_helper.getDepartureGround(flight);
            return shared_ptr<Intent>(new GroundDepartureTaxiReplyIntent(
                ground,
                flight,
                !!clearance,
                clearance
            ));
        }

        shared_ptr<Intent> pilotDepartureTaxiReadback(shared_ptr<Flight> flight)
        {
            auto clearance = flight->findClearanceOrThrow<DepartureTaxiClearance>(Clearance::Type::DepartureTaxiClearance);
            return shared_ptr<Intent>(new PilotDepartureTaxiReadbackIntent(
                clearance
            ));
        }

        shared_ptr<Intent> pilotReportHoldingShort(shared_ptr<Flight> flight, const string& runway, const string& holdingPoint)
        {
            auto tower = m_helper.getDepartureTower(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new PilotReportHoldingShortIntent(
                flight,
                tower,
                runway,
                holdingPoint
            ));
        }
        
        shared_ptr<Intent> groundSwitchToTower(shared_ptr<Flight> flight)
        {
            auto tower = m_helper.getDepartureTower(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new GroundSwitchToTowerIntent(
                flight,
                tower, 
                tower->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotCheckInWithTower(shared_ptr<Flight> flight, const string& runway, const string& holdingPoint, bool haveNumbers)
        {
            auto tower = m_helper.getDepartureTower(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new PilotCheckInWithTowerIntent(
                flight,
                tower,
                runway,
                holdingPoint,
                haveNumbers
            ));
        }

        shared_ptr<Intent> towerLineUp(shared_ptr<LineupApproval> approval)
        {
            return shared_ptr<Intent>(new TowerLineUpIntent(approval));
        }

        shared_ptr<Intent> pilotLineUpReadback(shared_ptr<LineupApproval> approval)
        {
            return shared_ptr<Intent>(new PilotLineUpReadbackIntent(approval));
        }

        shared_ptr<Intent> towerClearedForTakeoff(shared_ptr<TakeoffClearance> clearance)
        {
            auto flight = clearance->header().issuedTo;
            auto tower = m_helper.getDepartureTower(flight);
            auto departure = m_helper.getDeparture(flight);
            
            return shared_ptr<Intent>(new TowerClearedForTakeoffIntent(
                tower, 
                flight,
                true,
                clearance,
                departure->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotTakeoffClearanceReadback(shared_ptr<Flight> flight, shared_ptr<TakeoffClearance> clearance, int departureKhz)
        {
            auto tower = m_helper.getDepartureTower(flight); //TODO: handle arrivals
            return shared_ptr<Intent>(new PilotTakeoffClearanceReadbackIntent(
                clearance,
                departureKhz
            ));
        }

        shared_ptr<Intent> pilotReportFinal(shared_ptr<Flight> flight)
        {
            auto runway = flight->plan()->arrivalRunway();
            auto landingPoint = m_helper.getLandingPoint(flight);
            auto tower = m_helper.getArrivalTower(flight, landingPoint);
            
            return shared_ptr<Intent>(new PilotReportFinalIntent(flight, tower, runway));
        }

        shared_ptr<Intent> towerClearedForLanding(shared_ptr<LandingClearance> clearance)
        {
            auto flight = clearance->header().issuedTo;
            auto landingPoint = m_helper.getLandingPoint(flight);
            auto ground = m_helper.getArrivalGround(flight, landingPoint);
            
            return shared_ptr<Intent>(new TowerClearedForLandingIntent(
                clearance->header().issuedBy, 
                flight,
                true,
                clearance,
                ground->frequency()->khz()
            ));
        }

        shared_ptr<Intent> pilotLandingClearanceReadback(shared_ptr<Flight> flight, shared_ptr<LandingClearance> clearance)
        {
            auto landingPoint = m_helper.getLandingPoint(flight);
            auto tower = m_helper.getArrivalTower(flight, landingPoint);

            return shared_ptr<Intent>(new PilotLandingClearanceReadbackIntent(
                clearance,
                clearance->groundKhz()
            ));
        }
    };
}
