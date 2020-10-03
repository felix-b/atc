// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"
#include "worldHelper.hpp"

using namespace std;

namespace world
{
    Flight::Flight(
        shared_ptr<HostServices> _host,
        int _id,
        RulesType _rules,
        string _airlineIcao,
        string _flightNo,
        string _callSign,
        shared_ptr<FlightPlan> _plan
    ) : m_host(_host),
        m_id(_id),
        m_rules(_rules),
        m_airlineIcao(_airlineIcao),
        m_flightNo(_flightNo),
        m_callSign(_callSign),
        m_plan(_plan),
        m_onChanges(World::onChangesUnassigned),
        m_landingRunwayElevationFeet(ALTITUDE_UNASSIGNED - 1) //TODO: std::optional - does MinGW already support C++17?
    {
    }

    float Flight::landingRunwayElevationFeet()
    {
        if (m_landingRunwayElevationFeet <= ALTITUDE_UNASSIGNED)
        {
            const Runway::End &landingRunwayEnd = m_host->getWorld()->getRunwayEnd(
                m_plan->arrivalAirportIcao(),
                m_plan->arrivalRunway());
            m_landingRunwayElevationFeet = landingRunwayEnd.elevationFeet();
            m_host->writeLog("FLIGHT|%s: landingRunwayElevationFeet = %f", m_callSign.c_str(), m_landingRunwayElevationFeet);
        }
        return m_landingRunwayElevationFeet;
    }

    void Flight::progressTo(chrono::microseconds timestamp)
    {
        m_aircraft->progressTo(timestamp);
        if (m_pilot)
        {
            m_pilot->progressTo(timestamp);
        }
    }

    void Flight::addClearance(shared_ptr<Clearance> clearance)
    {
        m_host->writeLog("flight[%s] ADDING CLEARANCE type[%d]", m_callSign.c_str(), (int)clearance->type());
        m_clearances.push_back(clearance);
    }

    shared_ptr<Clearance> Flight::tryFindClearanceUncast(Clearance::Type type)
    {
        for (int i = 0 ; i < m_clearances.size() ; i++)
        {
            if (m_clearances[i]->type() == type)
            {
                return m_clearances[i];
            }
        }
        
        return nullptr;

        // auto result = tryFindFirst<shared_ptr<Clearance>>(
        //     m_clearances, 
        //     [type](const shared_ptr<Clearance>& item) { 
        //         return (item->type() == type); 
        //     }
        // );
        // m_host->writeLog(
        //     "CLEARANCE-LOOKUP flight[%s] find[%d] success[%s] #clearances[%d]", 
        //     m_callSign.c_str(), type, (result ? "OK" : "fail"), m_clearances.size()
        // );
        // return result;
    }

    shared_ptr<Clearance> Flight::findClearanceUncastOrThrow(Clearance::Type type)
    {
        auto clearance = tryFindClearanceUncast(type);
        if (clearance)
        {
            return clearance;
        }
        throw runtime_error("Required clearance not found: type=" + to_string((int)type));
    }

    void Flight::setAircraft(shared_ptr<Aircraft> _aircraft)
    {
        if (m_aircraft) 
        {
            throw runtime_error("Flight::setAircraft: already set");
        }

        m_aircraft = _aircraft;
        m_aircraft->assignFlight(shared_from_this());
        m_aircraft->onChanges([this](){
            return m_onChanges();
        });
    }

    void Flight::setPilot(shared_ptr<Pilot> _pilot)
    {
        //m_host->writeLog("Flight::setPilot - enter");

        if (m_pilot) 
        {
            throw runtime_error("Flight::setPilot: already set");
        }
        if (!m_aircraft) 
        {
            throw runtime_error("Flight::setPilot: aircraft was not set");
        }

        m_pilot = _pilot;
        //m_host->writeLog("Flight::setPilot - exit");
    }
}
