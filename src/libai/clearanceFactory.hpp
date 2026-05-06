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
        shared_ptr<IfrClearance> ifrClearance(shared_ptr<Flight> flight, int squawk)
        {   
            auto tower = getTower(flight->plan()->departureAirportIcao());
            auto clearanceDelivery = tower->findPositionOrThrow(
                ControllerPosition::Type::ClearanceDelivery, 
                flight->aircraft()->location());

            Clearance::Header header;
            initClearanceHeader(header, Clearance::Type::IfrClearance, clearanceDelivery, flight);
            auto plan = flight->plan();

            return shared_ptr<IfrClearance>(new IfrClearance(
                header, 
                plan->arrivalAirportIcao(),
                plan->sidName(),
                plan->sidTransition(),
                5000,
                plan->cruiseAltitudeFeet(),
                5,
                0, //why DEP??
                to_string(squawk)
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

            auto taxiPath = airport->taxiNet()->tryFindDepartureTaxiPathToRunway(p0, runwayEnd);
            if (!taxiPath) 
            {
                m_host->writeLog(
                    "CFACIL|WARNING: departure taxi path NOT FOUND from [%f,%f] to runway [%s], using direct fallback path",
                    p0.latitude,
                    p0.longitude,
                    runwayEnd.name().c_str());

                auto fallbackEdge = shared_ptr<TaxiEdge>(new TaxiEdge(
                    UniPoint::fromGeo(m_host, p0),
                    runwayEnd.centerlinePoint()
                ));

                taxiPath = shared_ptr<TaxiPath>(new TaxiPath(
                    fallbackEdge->node1(),
                    fallbackEdge->node2(),
                    { fallbackEdge }
                ));
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

        shared_ptr<LineUpAndWaitApproval> lineUpAndWait(shared_ptr<Flight> flight, DeclineReason waitReason = DeclineReason::None)
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
                Clearance::Type::LineUpAndWait,
                resolveDepartureTowerPosition(flight), 
                flight);

            return shared_ptr<LineUpAndWaitApproval>(new LineUpAndWaitApproval(
                header,
                flight->plan()->departureRunway(),
                waitReason
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
                resolveDepartureTowerPosition(flight), 
                flight);

            auto departure = airport->tower()->tryFindPosition(
                ControllerPosition::Type::Departure,
                flight->aircraft()->location());

            return shared_ptr<TakeoffClearance>(new TakeoffClearance(
                header,
                flight->plan()->departureRunway(),
                immediate,
                initialHeading,
                departure ? departure->frequency()->khz() : 0
            ));
        }

        shared_ptr<GoAroundRequest> goAroundRequest(
            shared_ptr<Flight> flight,
            shared_ptr<ControllerPosition> control,
            const string& runwayName,
            DeclineReason reason)
        {
            Clearance::Header header;
            initClearanceHeader(
                header,
                Clearance::Type::GoAroundRequest,
                control,
                flight);

            return shared_ptr<GoAroundRequest>(new GoAroundRequest(
                header,
                runwayName,
                reason
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

        shared_ptr<ArrivalTaxiClearance> arrivalTaxiClearance(shared_ptr<Flight> flight, const GeoPoint& fromPoint)
        {
            auto airport = getArrivalAirport(flight);
            auto gate = airport->getParkingStandOrThrow(flight->plan()->arrivalGate());
            auto taxiPath = airport->taxiNet()->tryFindTaxiPathToGate(gate, fromPoint);

            Clearance::Header header;
            initClearanceHeader(
                header,
                Clearance::Type::ArrivalTaxiClearance,
                airport->groundAt(flight->aircraft()->location()),
                flight);

            return shared_ptr<ArrivalTaxiClearance>(new ArrivalTaxiClearance(
                header,
                flight->plan()->arrivalGate(),
                taxiPath
            ));
        }

    private:
        shared_ptr<ControllerPosition> resolveDepartureTowerPosition(shared_ptr<Flight> flight)
        {
            auto airport = getDepartureAirport(flight);
            const GeoPoint location = flight->aircraft()->location();

            try
            {
                return airport->localAt(location);
            }
            catch (const exception&)
            {
            }

            auto tower = airport->tower();
            if (!tower)
            {
                throw runtime_error("ClearanceFactory::resolveDepartureTowerPosition: airport has no tower/controller facility");
            }

            const vector<ControllerPosition::Type> fallbackTypes = {
                ControllerPosition::Type::Local,
                ControllerPosition::Type::Departure,
                ControllerPosition::Type::Approach
            };

            for (const auto fallbackType : fallbackTypes)
            {
                if (auto fallback = tower->tryFindPosition(fallbackType, location))
                {
                    return fallback;
                }
            }

            if (!tower->positions().empty())
            {
                return tower->positions().front();
            }

            throw runtime_error("ClearanceFactory::resolveDepartureTowerPosition: no suitable controller position found");
        }

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
