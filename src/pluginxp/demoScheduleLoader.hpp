// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <cstring>
#include <string>
#include <chrono>
#include <queue>
#include <vector>
#include <random>

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

class DemoScheduleLoader
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<World> m_world;
    DataRef<double> m_userAircraftLatitude;
    DataRef<double> m_userAircraftLongitude;
    shared_ptr<Airport> m_airport;
public:
    DemoScheduleLoader(shared_ptr<HostServices> _host, shared_ptr<World> _world) :
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
        m_airport->selectActiveRunways();
        m_airport->selectArrivalAndDepartureTaxiways();

        m_host->writeLog("SCHEDL|Loading demo AI schedules at airport[%s]", m_airport->header().icao().c_str());

        initDemoSchedules(0.7, m_world->currentTime() + 15, m_world->currentTime() + 10);

        m_host->writeLog(
            "SCHEDL|Loaded [%d] demo AI flights at airport[%s]",
            m_world->flights().size(),
            m_airport->header().icao().c_str());
    }

public:

    shared_ptr<Airport> airport() const { return m_airport; }

private:

    string getUserAirportIcao()
    {
        char airportIcaoId[10] = { 0 };
        float lat = m_userAircraftLatitude;
        float lon = m_userAircraftLongitude;
        m_host->writeLog("SCHEDL|User airport lookup: user aircraft is at (%f,%f)", lat, lon);

        XPLMNavRef navRef = XPLMFindNavAid( nullptr, nullptr, &lat, &lon, nullptr, xplm_Nav_Airport);
        if (navRef != XPLM_NAV_NOT_FOUND)
        {
            XPLMGetNavAidInfo(navRef, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, airportIcaoId, nullptr, nullptr);
        }

        if (strlen(airportIcaoId) > 0)
        {
            m_host->writeLog("SCHEDL|User airport lookup: FOUND [%s]", airportIcaoId);
            return airportIcaoId;
        }

        m_host->writeLog("SCHEDL|User airport lookup: NOT FOUND! - assuming KJFK");
        return "KJFK";
    }

    void initDemoSchedules(float loadFactor, time_t firstDepartureTime, time_t firstArrivalTime)
    {
        unordered_map<string, string> callSignByAirline = {
            { "DAL", "Delta" },
            { "AAL", "American" },
            { "SWA", "Southwest" },
        };

        string activeDepartureRunway;
        string activeArrivalRunway1;
        string activeArrivalRunway2;
        int arrivalIndex = 0;

        const auto findActiveRunways = [this, &activeDepartureRunway, &activeArrivalRunway1, &activeArrivalRunway2] {
            const auto& departure = m_airport->activeDepartureRunways();
            const auto& arrival = m_airport->activeArrivalRunways();

            activeDepartureRunway = !departure.empty() ? departure.at(0) : "";
            activeArrivalRunway1 = !arrival.empty() ? arrival.at(0) : "";
            activeArrivalRunway2 = !arrival.empty() ? arrival.at(arrival.size() - 1) : "";
        };

        const auto addOutboundFlight = [this, &callSignByAirline, &activeDepartureRunway](
            const string& model, const string& airline, int flightId, const string& destination, time_t departureTime, shared_ptr<ParkingStand> gate
        ) {
            string callSign = getValueOrThrow(callSignByAirline, airline);
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(departureTime, departureTime + 60 * 60 * 3, m_airport->header().icao(), destination));
            flightPlan->setDepartureGate(gate->name());
            flightPlan->setDepartureRunway(activeDepartureRunway);

            auto destinationAirport = m_host->getWorld()->getAirport(destination);
            flightPlan->setArrivalRunway(destinationAirport->findLongestRunway()->end1().name());

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));

            auto aircraft = m_host->createAIAircraft(model, airline, to_string(flightId), world::Aircraft::Category::Jet);
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            flight->setPhase(Flight::Phase::TurnAround);

            m_world->addFlightColdAndDark(flight);
        };

        const auto addInboundFlight = [this, &callSignByAirline, &activeArrivalRunway1, &activeArrivalRunway2, &arrivalIndex](
            const string& model, const string& airline, int flightId, const string& origin, time_t arrivalTime, shared_ptr<ParkingStand> gate
        ) {
            m_host->writeLog("SCHEDL|adding inbound flight id[%d]", flightId);

            string callSign = getValueOrThrow(callSignByAirline, airline);
            string arrivalRunway = ((arrivalIndex++) % 2) == 0 ? activeArrivalRunway1 : activeArrivalRunway2;
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(arrivalTime - 60 * 60 * 3, arrivalTime, origin, m_airport->header().icao()));
            flightPlan->setArrivalGate(gate->name());
            flightPlan->setArrivalRunway(arrivalRunway);

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));

            auto aircraft = m_host->createAIAircraft(model, airline, to_string(flightId), world::Aircraft::Category::Jet);
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            flight->setPhase(Flight::Phase::Arrival);

            auto copyOfWorld = m_world;
            auto copyOfAirport = m_airport;
            m_world->deferUntil(
                "addInboundFlight/" + flight->callSign(),
                arrivalTime,
                [flight, copyOfWorld, copyOfAirport, arrivalRunway](){
                    const auto& landingRunwayEnd = copyOfWorld->getRunwayEnd(copyOfAirport->header().icao(), arrivalRunway);
                    copyOfWorld->addFlight(flight);
                    flight->aircraft()->setOnFinal(landingRunwayEnd);
                }
            );
        };

        vector<shared_ptr<ParkingStand>> gates;
        findGatesForFlights(gates, loadFactor);
        findActiveRunways();

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
                    nextDepartureTime += 100;
                    addOutboundFlight(model, airline, flightId, "KMIA", departureTime, gate);
                }
                else
                {
                    time_t arrivalTime = nextArrivalTime;
                    nextArrivalTime += 180;
                    addInboundFlight(model, airline, flightId, m_airport->header().icao(), arrivalTime, gate);
                }
            }
            catch(const std::exception& e)
            {
                m_host->writeLog("SCHEDL|CRASHED while adding AI flight!!! %s", e.what());
            }
        }
    }

    void findGatesForFlights(vector<shared_ptr<ParkingStand>>& found, float loadFactor)
    {
        GeoPoint userAircraftLocation((float)m_userAircraftLatitude, (float)m_userAircraftLongitude);

        const int nameCheckBufferSize = 32;
        char nameCheckBuffer[nameCheckBufferSize + 1] = { 0 };

        const auto isUserAircraftParkedAtGate = [&](const shared_ptr<ParkingStand>& gate)->bool {
            auto distanceToUserAircraft = GeoMath::getDistanceMeters(userAircraftLocation, gate->location().geo());
            return (distanceToUserAircraft < 50);
        };

        const auto isPassengerGateName = [&](const string& name)->bool {
            strncpy(nameCheckBuffer, name.c_str(), nameCheckBufferSize);
            for (int i = 0 ; i < name.length() && i < nameCheckBufferSize ; i++)
            {
                nameCheckBuffer[i] = toupper(nameCheckBuffer[i]);
            }
            return (
                !strstr(nameCheckBuffer, "HEL") &&
                !strstr(nameCheckBuffer, "MILI") &&
                !strstr(nameCheckBuffer, "RAMP") &&
                (!strstr(nameCheckBuffer, "GA") || strstr(nameCheckBuffer, "GATE")) &&
                !strstr(nameCheckBuffer, "G.A") &&
                !strstr(nameCheckBuffer, "GENERAL") &&
                !strstr(nameCheckBuffer, "GRASS") &&
                !strstr(nameCheckBuffer, "DIRT") &&
                !strstr(nameCheckBuffer, "FUEL") &&
                !strstr(nameCheckBuffer, "CARGO") &&
                !strstr(nameCheckBuffer, "HANG") &&
                !strstr(nameCheckBuffer, "TIE") &&
                !strstr(nameCheckBuffer, "MAINT") &&
                !strstr(nameCheckBuffer, "DOCK"));
        };

        const auto canUseGateForAIFlights = [&](const shared_ptr<ParkingStand>& gate)->bool {
            if (isUserAircraftParkedAtGate(gate))
            {
                m_host->writeLog(
                    "SCHEDL|Skipping gate[%s] looks like the user aircraft is parked here!",
                    gate->name().c_str());
                return false;
            }

            bool canUse = (
                gate->type() == ParkingStand::Type::Gate &&
                gate->hasOperationType(world::Aircraft::OperationType::Airline) &&
                !gate->hasOperationType(world::Aircraft::OperationType::Cargo) &&
                (gate->name().length() < 10 || isPassengerGateName(gate->name())));

            if (!canUse)
            {
                m_host->writeLog("SCHEDL|Won't use gate [%s]", gate->name().c_str());
            }

            return canUse;
        };

        const vector<shared_ptr<ParkingStand>>& allGates = m_airport->parkingStands();
        vector<shared_ptr<ParkingStand>> usableGates;
        copy_if(allGates.begin(), allGates.end(), back_inserter(usableGates), canUseGateForAIFlights);
        m_host->writeLog(
            "SCHEDL|Found [%d/%d] gates for AI flights, skipped [%d]",
            usableGates.size(), allGates.size(), allGates.size() - usableGates.size());

        vector<unsigned int> indices(usableGates.size());
        iota(indices.begin(), indices.end(), 0);
        shuffle(indices.begin(), indices.end(), std::default_random_engine());
        int requestedCount = (int)(usableGates.size() * loadFactor);

        for (int i = 0 ; i < indices.size() && i < requestedCount ; i++)
        {
            const shared_ptr<ParkingStand>& gate = usableGates.at(indices.at(i));
            found.push_back(gate);
        }

        m_host->writeLog(
            "SCHEDL|Picked [%d/%d] gates for AI flights at load factor[%f]",
            found.size(),
            requestedCount,
            loadFactor);
    }
};
