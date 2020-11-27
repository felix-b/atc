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

using namespace std;


namespace world
{
    class WorldHelper
    {
    private:
        shared_ptr<HostServices> m_host;
    public:
        WorldHelper(shared_ptr<HostServices> _host) : 
            m_host(_host)
        {
        }
    public:
        shared_ptr<Airport> getDepartureAirport(shared_ptr<Flight> flight)
        {
            return m_host->getWorld()->getAirport(flight->plan()->departureAirportIcao());
        }

        shared_ptr<Airport> getArrivalAirport(shared_ptr<Flight> flight)
        {
            return m_host->getWorld()->getAirport(flight->plan()->arrivalAirportIcao());
        }

        shared_ptr<Airport> getCurrentAirport(shared_ptr<Flight> flight)
        {
            return m_host->getWorld()->getAirport(flight->currentAirportIcao());
        }

        shared_ptr<ControllerPosition> getClearanceDelivery(shared_ptr<Flight> flight)
        {
            auto airport = getDepartureAirport(flight);
            return airport->clearanceDeliveryAt(flight->aircraft()->location());
        }
        
        shared_ptr<ControllerPosition> getDepartureGround(shared_ptr<Flight> flight)
        {
            auto airport = getDepartureAirport(flight);
            return airport->groundAt(flight->aircraft()->location());
        }

        shared_ptr<ControllerPosition> getDepartureTower(shared_ptr<Flight> flight)
        { 
            auto airport = getDepartureAirport(flight);
            return airport->localAt(flight->aircraft()->location());
        }

        shared_ptr<ControllerPosition> getArrivalTower(shared_ptr<Flight> flight, const GeoPoint& landingPoint)
        { 
            auto airport = getArrivalAirport(flight);
            auto tower = airport->localAt(landingPoint);
            return tower;
        }

        shared_ptr<ControllerPosition> getArrivalGround(shared_ptr<Flight> flight, const GeoPoint& landingPoint)
        { 
            auto airport = getArrivalAirport(flight);
            auto ground = airport->groundAt(landingPoint);
            return ground;
        }

        shared_ptr<ControllerPosition> tryGetDeparture(shared_ptr<Flight> flight)
        { 
            auto airport = getDepartureAirport(flight);
            return airport->tower()->tryFindPosition(
                ControllerPosition::Type::Departure,
                flight->aircraft()->location());
        }

        shared_ptr<Intent> verbalize(shared_ptr<Intent> intent)
        {
            //no-op
            // Utterance utterance = m_verbalizer.verbalizeIntent(intent);
            // intent->setTransmissionText(utterance.plainText());
            return intent;
        }

        GeoPoint getLandingPoint(shared_ptr<Flight> flight)
        {
            const auto& runwayEnd = getLandingRunwayEnd(flight);
            return runwayEnd.centerlinePoint().geo();
        }

        const Runway::End& getLandingRunwayEnd(shared_ptr<Flight> flight)
        {
            auto airport = getArrivalAirport(flight);
            auto runwayName = flight->plan()->arrivalRunway();
            auto runway = airport->getRunwayOrThrow(runwayName);
            return runway->getEndOrThrow(runwayName);
        }
    };
}
