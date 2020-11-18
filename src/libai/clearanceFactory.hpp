// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include "libworld.h"
#include "worldHelper.hpp"
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
        WorldHelper m_helper;
    public:
        ClearanceFactory(shared_ptr<HostServices> _host) :
            m_host(_host),
            m_nextClearanceId(1),
            m_helper(_host)
        {
        }
    public:
        shared_ptr<IfrClearance> ifrClearance(shared_ptr<Flight> flight, int squawk)
        {   
            auto clearanceDelivery = m_helper.getClearanceDelivery(flight);

            Clearance::Header header;
            initClearanceHeader(header, Clearance::Type::IfrClearance, clearanceDelivery, flight);
            auto plan = flight->plan();

            return shared_ptr<IfrClearance>(new IfrClearance(
                header, 
                plan->arrivalAirportIcao(),
                plan->sidName(),
                plan->sidTransition(),
                5000,
                34000,
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

            auto airport = m_helper.getDepartureAirport(flight);
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
                throw runtime_error(
                    "departure taxi path from [" +
                    to_string(p0.latitude) + "," + to_string(p0.longitude) +
                    "] to runway [" +
                    runwayEnd.name() +
                    "] NOT FOUND!");
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
            auto airport = m_helper.getDepartureAirport(flight);
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

        shared_ptr<RunwayCrossClearance> runwayCrossClearance(shared_ptr<Flight> flight, const string& runwayName)
        {
            auto airport = m_helper.getCurrentAirport(flight);

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
            auto airport = m_helper.getDepartureAirport(flight);
            auto ifr = flight->tryFindClearance<IfrClearance>(Clearance::Type::IfrClearance);
            if (!ifr)
            {
                return nullptr;
            }

            Clearance::Header header;
            initClearanceHeader(
                header, 
                Clearance::Type::LineUpAndWait,
                airport->localAt(flight->aircraft()->location()), 
                flight);

            return shared_ptr<LineUpAndWaitApproval>(new LineUpAndWaitApproval(
                header,
                flight->plan()->departureRunway(),
                waitReason
            ));
        }

        shared_ptr<TakeoffClearance> takeoffClearance(shared_ptr<Flight> flight, float initialHeading, bool immediate)
        {
            auto airport = m_helper.getDepartureAirport(flight);
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
            auto airport = m_helper.getArrivalAirport(flight);
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
            auto airport = m_helper.getArrivalAirport(flight);
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
    };
}
