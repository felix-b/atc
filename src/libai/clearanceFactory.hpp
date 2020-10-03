// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include "libworld.h"
#include "clearanceTypes.hpp"

using namespace std;
using namespace world;

namespace ai
{
    class ClearanceFactory
    {
    private:
        shared_ptr<HostServices> m_host;
        long long m_nextClearanceId;
    public:
        ClearanceFactory(shared_ptr<HostServices> _host) :
            m_host(_host),
            m_nextClearanceId(1)
        {
        }
    public:
        shared_ptr<IfrClearance> ifrClearance(shared_ptr<Flight> flight)
        {   
            auto tower = getTower(flight->plan()->departureAirportIcao());
            auto clearanceDelivery = tower->findPositionOrThrow(
                ControllerPosition::Type::ClearanceDelivery, 
                flight->aircraft()->location());
            auto departure = tower->findPositionOrThrow(
                ControllerPosition::Type::Departure, 
                flight->aircraft()->location());

            Clearance::Header header;
            initClearanceHeader(header, Clearance::Type::IfrClearance, clearanceDelivery, flight);

            return shared_ptr<IfrClearance>(new IfrClearance(
                header, 
                flight->plan()->arrivalAirportIcao(),
                "GREKI 6",
                "YNKEE",
                5000,
                34000,
                5,
                departure->frequency()->khz(),
                "3" + to_string(flight->id())
            ));
        }

        shared_ptr<PushAndStartApproval> pushAndStartApproval(shared_ptr<Flight> flight)
        {
            auto ifrClearance = flight->tryFindClearance<IfrClearance>(Clearance::Type::IfrClearance);
            if (!ifrClearance)
            {
                return nullptr;
            }

            auto airport = getDepartureAirport(flight);
            auto gate = airport->getParkingStandOrThrow(flight->plan()->departureGate());
            auto runway = airport->getRunwayOrThrow(flight->plan()->departureRunway());
            auto runwayEnd = runway->getEndOrThrow(flight->plan()->departureRunway());

            GeoPoint p0 = GeoMath::getPointAtDistance(
                gate->location().geo(), 
                GeoMath::flipHeading(gate->heading()),
                40);

            auto taxiPath = TaxiPath::tryFind(airport->taxiNet(), p0, runwayEnd.centerlinePoint().geo());
            if (!taxiPath) 
            {
                throw runtime_error("taxi path NOT FOUND!");
            }

            GeoPoint p1 = taxiPath->edges[0]->node1()->location().geo();
            
            GeoPoint p2 = GeoMath::getPointAtDistance(
                p1, 
                GeoMath::flipHeading(taxiPath->edges[0]->heading()),
                30);

            Clearance::Header header;
            initClearanceHeader(
                header, 
                Clearance::Type::PushAndStartApproval, 
                airport->groundAt(p0), 
                flight);
            return shared_ptr<PushAndStartApproval>(new PushAndStartApproval(
                header, 
                flight->plan()->departureRunway(),
                { flight->aircraft()->location(), p0, p1, p2 }, 
                taxiPath
            ));
        }   

        shared_ptr<DepartureTaxiClearance> departureTaxiClearance(shared_ptr<Flight> flight)
        {
            auto airport = getDepartureAirport(flight);
            auto pushAndStart = flight->tryFindClearance<PushAndStartApproval>(Clearance::Type::PushAndStartApproval);
            if (!pushAndStart)
            {
                return nullptr;
            }

            Clearance::Header header;
            initClearanceHeader(
                header, 
                Clearance::Type::DepartureTaxiClearance, 
                airport->groundAt(flight->aircraft()->location()), 
                flight);

            return shared_ptr<DepartureTaxiClearance>(new DepartureTaxiClearance(
                header,
                pushAndStart->departureRunway(),
                pushAndStart->taxiPath()
            ));
        }

        shared_ptr<RunwayCrossClearance> runwayCrossCleaeance(shared_ptr<Flight> flight, const string& runwayName)
        {
            auto airport = getDepartureAirport(flight);

            Clearance::Header header;
            initClearanceHeader(
                header,
                Clearance::Type::RunwayCrossClearance,
                airport->groundAt(flight->aircraft()->location()),
                flight);

            return shared_ptr<RunwayCrossClearance>(new RunwayCrossClearance(
                header,
                runwayName
            ));
        }

        shared_ptr<LineupApproval> lineupApproval(shared_ptr<Flight> flight, bool wait)
        {
            auto airport = getDepartureAirport(flight);
            auto ifr = flight->tryFindClearance<IfrClearance>(Clearance::Type::IfrClearance);
            if (!ifr)
            {
                return nullptr;
            }

            Clearance::Header header;
            initClearanceHeader(
                header, 
                Clearance::Type::LineupApproval, 
                airport->localAt(flight->aircraft()->location()), 
                flight);

            return shared_ptr<LineupApproval>(new LineupApproval(
                header,
                flight->plan()->departureRunway(),
                wait
            ));
        }

        shared_ptr<TakeoffClearance> takeoffClearance(shared_ptr<Flight> flight, float initialHeading, bool immediate)
        {
            auto airport = getDepartureAirport(flight);
            auto ifr = flight->tryFindClearance<IfrClearance>(Clearance::Type::IfrClearance);
            if (!ifr)
            {
                return nullptr;
            }

            Clearance::Header header;
            initClearanceHeader(
                header, 
                Clearance::Type::TakeoffClearance, 
                airport->localAt(flight->aircraft()->location()), 
                flight);

            auto departure = airport->departureAt(flight->aircraft()->location());

            return shared_ptr<TakeoffClearance>(new TakeoffClearance(
                header,
                flight->plan()->departureRunway(),
                immediate,
                initialHeading,
                departure->frequency()->khz()
            ));
        }   

        shared_ptr<LandingClearance> landingClearance(shared_ptr<Flight> flight, const string& runwayName, int groundKhz)
        {
            auto airport = getArrivalAirport(flight);
            auto runway = airport->getRunwayOrThrow(runwayName);
            const auto& runwayEnd = runway->getEndOrThrow(runwayName);

            Clearance::Header header;
            initClearanceHeader(
                header, 
                Clearance::Type::LandingClearance, 
                airport->localAt(flight->aircraft()->location()), 
                flight);

            auto ground = airport->groundAt(flight->aircraft()->location());

            return shared_ptr<LandingClearance>(new LandingClearance(
                header,
                runwayName,
                ground->frequency()->khz()
            ));
        }   

    private:
        void initClearanceHeader(
            Clearance::Header& header, 
            Clearance::Type type,
            shared_ptr<ControllerPosition> position, 
            shared_ptr<Flight> flight)
        {   
            header.id = m_nextClearanceId++;
            header.type = type;
            header.issuedBy = position;
            header.issuedTo = flight;
            header.issuedTimestamp = m_host->getWorld()->timestamp();
        }
        shared_ptr<Airport> getDepartureAirport(shared_ptr<Flight> flight)
        {
            return m_host->getWorld()->getAirport(flight->plan()->departureAirportIcao());
        }
        shared_ptr<Airport> getArrivalAirport(shared_ptr<Flight> flight)
        {
            return m_host->getWorld()->getAirport(flight->plan()->arrivalAirportIcao());
        }
        shared_ptr<ControlFacility> getTower(const string& airportIcao)
        {
            auto airport = m_host->getWorld()->getAirport(airportIcao);
            return airport->tower();
        }
    };
}
