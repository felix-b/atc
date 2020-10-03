// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <chrono>
#include <queue>
#include <vector>

// SDK
#include "XPLMProcessing.h"
#include "XPLMNavigation.h"

// PPL
#include "owneddata.h"

// tnc
#include "utils.h"
#include "libworld.h"
#include "intentFactory.hpp"
#include "libdataxp.h"
#include "libai.hpp"
#include "simplePhraseologyService.hpp"
#include "nativeTextToSpeechService.hpp"
#include "pluginHostServices.hpp"
#include "xpmp2AircraftObjectService.hpp"

using namespace std;
using namespace PPL;
using namespace world;
using namespace ai;

class PluginScheduleLoader
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<World> m_world;
    DataRef<double> m_userAircraftLatitude;
    DataRef<double> m_userAircraftLongitude;
    shared_ptr<Airport> m_airport;
public:
    PluginScheduleLoader(shared_ptr<HostServices> _host, shared_ptr<World> _world) :
        m_host(_host),
        m_world(_world),
        m_userAircraftLatitude("sim/flightmodel/position/latitude", PPL::ReadOnly),
        m_userAircraftLongitude("sim/flightmodel/position/longitude", PPL::ReadOnly)
    {
    }
public:
    void loadSchedules()
    {
        string userAirportIcao = getUserAirportIcao();
        m_airport = m_world->getAirport(userAirportIcao);

        initDemoFlights(20, m_world->currentTime() + 190, m_world->currentTime() + 10);
        m_host->writeLog("LSCHED|AI schedules initialized");

        m_host->writeLog(
            "The world now has [%d] airports, [%d] control facilities, [%d] flights",
            m_world->airports().size(), 
            m_world->controlFacilities().size(),
            m_world->flights().size());
    }

public:

    shared_ptr<Airport> airport() const { return m_airport; }

private:

    string getUserAirportIcao()
    {
        char airportIcaoId[10] = { 0 };
        float lat = m_userAircraftLatitude;
        float lon = m_userAircraftLongitude;
        m_host->writeLog("User airport lookup: user aircraft is at (%f,%f)", lat, lon);

        XPLMNavRef navRef = XPLMFindNavAid( nullptr, nullptr, &lat, &lon, nullptr, xplm_Nav_Airport);
        if (navRef != XPLM_NAV_NOT_FOUND)
        {
            XPLMGetNavAidInfo(navRef, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, airportIcaoId, nullptr, nullptr);
        }

        if (strlen(airportIcaoId) > 0)
        {
            m_host->writeLog("User airport lookup: FOUND [%s]", airportIcaoId);
            return airportIcaoId;
        }

        m_host->writeLog("User airport lookup: NOT FOUND! - assuming KJFK");
        return "KJFK";
    }

    void initDemoFlights(int count, time_t firstDepartureTime, time_t firstArrivalTime)
    {
        unordered_map<string, string> callSignByAirline = {
            { "DAL", "Delta" },
            { "AAL", "American" },
            { "SWA", "Southwest" },
        };

        string activeRunwayName = m_airport->findLongestRunway()->end1().name(); // airport->runways()[0]->end1().name();

        const auto addOutboundFlight = [this, &callSignByAirline, &activeRunwayName](
            const string& model, const string& airline, int flightId, const string& destination, time_t departureTime, shared_ptr<ParkingStand> gate
        ) {
            string callSign = getValueOrThrow(callSignByAirline, airline);
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(departureTime, departureTime + 60 * 60 * 3, m_airport->header().icao(), destination));
            flightPlan->setDepartureGate(gate->name());
            flightPlan->setDepartureRunway(activeRunwayName);

            auto destinationAirport = m_host->getWorld()->getAirport(destination);
            flightPlan->setArrivalRunway(destinationAirport->findLongestRunway()->end1().name());

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));
            int aircraftId = 1000 + flightId;
            
            auto aircraft = shared_ptr<world::Aircraft>(new world::Aircraft(m_host, aircraftId, model, airline, to_string(flightId), world::Aircraft::Category::Jet));
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            aircraft->setManeuver(pilot->getFlightCycle());

            m_world->addFlightColdAndDark(flight);
        };

        const auto addInboundFlight = [this, &callSignByAirline, &activeRunwayName](
            const string& model, const string& airline, int flightId, const string& origin, time_t arrivalTime, shared_ptr<ParkingStand> gate
        ) {
            m_host->writeLog("adding inbound flight id=%d", flightId);

            string callSign = getValueOrThrow(callSignByAirline, airline);
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(arrivalTime - 60 * 60 * 3, arrivalTime, origin, m_airport->header().icao()));
            flightPlan->setArrivalGate(gate->name());
            flightPlan->setArrivalRunway(activeRunwayName);

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));
            int aircraftId = 1000 + flightId;
            
            auto aircraft = shared_ptr<world::Aircraft>(new world::Aircraft(m_host, aircraftId, model, airline, to_string(flightId), world::Aircraft::Category::Jet));
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);

            auto copyOfWorld = m_world;
            auto copyOfAirport = m_airport;
            m_world->deferUntil(
                "addInboundFlight/" + flight->callSign(),
                arrivalTime,
                [flight, copyOfWorld, copyOfAirport, activeRunwayName](){
                    const auto& landingRunwayEnd = copyOfWorld->getRunwayEnd(copyOfAirport->header().icao(), activeRunwayName);
                    copyOfWorld->addFlight(flight);
                    flight->aircraft()->setOnFinal(landingRunwayEnd);
                    flight->aircraft()->setManeuver(flight->pilot()->getFinalToGate(landingRunwayEnd));
                }
            );
        };

        vector<shared_ptr<ParkingStand>> gates;
        findGatesForFlights(gates, count);

        int index = 0;
        time_t nextDepartureTime = firstDepartureTime;
        time_t nextArrivalTime = firstArrivalTime;
        vector<string> airlineOptions = { "DAL", "AAL", "SWA" };
        vector<string> modelOptions = { "B738" /*, "A320"*/ };

        for (const auto& gate : gates)
        {
            index++;
            int flightId = 100 + index;
            const string& airline = airlineOptions[index % airlineOptions.size()];
            const string& model = modelOptions[index % modelOptions.size()];
            
            try
            {
                if ((index % 2) == 1)
                {
                    time_t departureTime = nextDepartureTime;
                    nextDepartureTime += 180;
                    addOutboundFlight(model, airline, flightId, "KMIA", departureTime, gate);
                }
                else
                {
                    time_t arrivalTime = nextArrivalTime;
                    nextArrivalTime += 45;
                    addInboundFlight(model, airline, flightId, m_airport->header().icao(), arrivalTime, gate);
                }
            }
            catch(const std::exception& e)
            {
                m_host->writeLog("CRASHED while adding AI flight!!! %s", e.what());
            }
        }
    }

    void findGatesForFlights(vector<shared_ptr<ParkingStand>>& found, int count)
    {
        int skipCount = 1;
        GeoPoint userAircraftLocation((float)m_userAircraftLatitude, (float)m_userAircraftLongitude);

        for (const auto& gate : m_airport->parkingStands())
        {
            m_host->writeLog("Checking gate [%s]", gate->name().c_str());
            if (gate->type() == ParkingStand::Type::Gate && gate->hasOperationType(world::Aircraft::OperationType::Airline) && !gate->hasOperationType(world::Aircraft::OperationType::Cargo))
            {
                if (skipCount-- > 0)
                {
                    m_host->writeLog("Skipping gate [%s] by count", gate->name().c_str());
                    continue;
                }

                auto distanceToUserAircraft = GeoMath::getDistanceMeters(userAircraftLocation, gate->location().geo());
                if (distanceToUserAircraft < 50)
                {
                    m_host->writeLog("Skipping gate [%s] - looks like the user aircraft is parked here!", gate->name().c_str());
                    continue;
                }

                m_host->writeLog("Will use gate [%s] for AI flights", gate->name().c_str());
                found.push_back(gate);
                if (found.size() >= count)
                {
                    m_host->writeLog("Found %d gates for AI flights.", found.size());
                    return;
                }
            }
        }
    }
};
