// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"
#include "worldHelper.hpp"
#include "../libdataxp/xpCifpReader.hpp"
#include "../libdataxp/xpNavDataReader.hpp"
#include "../libdataxp/xpAltitudeReader.hpp"

using namespace std;

namespace world
{
    namespace
    {
        string normalizeWaypointKey(string value)
        {
            value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
                return !isalnum(c);
            }), value.end());

            transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
                return static_cast<char>(toupper(c));
            });

            return value;
        }

        string normalizeRunwayKey(string value)
        {
            string key = normalizeWaypointKey(std::move(value));
            if (key.rfind("RW", 0) == 0)
            {
                key.erase(0, 2);
            }
            return key;
        }

        void appendLegFromPoints(
            vector<shared_ptr<FlightPlan::Leg>>& legs,
            FlightPlan::LegType type,
            const string& fromNavaid,
            const string& toNavaid,
            float targetAltitude,
            float targetSpeed)
        {
            legs.push_back(shared_ptr<FlightPlan::Leg>(new FlightPlan::Leg(
                type,
                GeoPolygon::empty(),
                fromNavaid,
                toNavaid,
                targetAltitude,
                targetSpeed)));
        }


        void appendRouteWaypoint(
            vector<FlightPlan::RouteWaypoint>& waypoints,
            const string& name,
            const string& airway,
            const GeoPoint& location)
        {
            if (name.empty())
            {
                return;
            }

            if (!waypoints.empty() && normalizeWaypointKey(waypoints.back().name) == normalizeWaypointKey(name))
            {
                return;
            }

            waypoints.push_back(FlightPlan::RouteWaypoint(name, airway, location));
        }

        GeoPoint resolveWaypointLocation(
            XPNavaidReader& navaidReader,
            const string& contextIcao,
            const string& name,
            const GeoPoint& parsedLocation = GeoPoint::empty,
            const GeoPoint& airportLocation = GeoPoint::empty)
        {
            if (parsedLocation != GeoPoint::empty)
            {
                return parsedLocation;
            }

            GeoPoint location = GeoPoint::empty;
            // Use distance-aware resolution when airport location is provided
            if (airportLocation != GeoPoint::empty)
            {
                if (navaidReader.tryResolveWaypoint(contextIcao, name, airportLocation, location))
                {
                    return location;
                }
            }
            else if (navaidReader.tryResolveWaypoint(contextIcao, name, location))
            {
                return location;
            }

            return GeoPoint::empty;
        }
        void appendTrackLegs(
            vector<shared_ptr<FlightPlan::Leg>>& legs,
            FlightPlan::LegType type,
            const vector<string>& waypoints,
            float targetAltitude,
            float targetSpeed)
        {
            if (waypoints.size() < 2)
            {
                return;
            }

            for (size_t i = 1; i < waypoints.size(); ++i)
            {
                appendLegFromPoints(
                    legs,
                    type,
                    waypoints[i - 1],
                    waypoints[i],
                    targetAltitude,
                    targetSpeed);
            }
        }

        vector<string> toWaypointNames(const vector<FlightPlan::RouteWaypoint>& routeWaypoints)
        {
            vector<string> result;
            result.reserve(routeWaypoints.size());
            for (const auto& waypoint : routeWaypoints)
            {
                if (!waypoint.name.empty())
                {
                    result.push_back(waypoint.name);
                }
            }
            return result;
        }

        GeoPoint tryFindRouteEndpointLocation(
            const vector<FlightPlan::RouteWaypoint>& routeWaypoints,
            bool findFirst)
        {
            if (findFirst)
            {
                for (const auto& waypoint : routeWaypoints)
                {
                    if (waypoint.location != GeoPoint::empty)
                    {
                        return waypoint.location;
                    }
                }
            }
            else
            {
                for (auto it = routeWaypoints.rbegin(); it != routeWaypoints.rend(); ++it)
                {
                    if (it->location != GeoPoint::empty)
                    {
                        return it->location;
                    }
                }
            }

            return GeoPoint::empty;
        }

        GeoPoint tryFindAirportLocation(shared_ptr<HostServices> host, const string& airportIcao)
        {
            if (!host)
            {
                return GeoPoint::empty;
            }

            try
            {
                auto airport = host->getWorld()->getAirport(airportIcao);
                return airport ? airport->header().datum() : GeoPoint::empty;
            }
            catch (const exception&)
            {
                return GeoPoint::empty;
            }
        }

        float selectCruiseAltitudeFeet(
            shared_ptr<HostServices> host,
            const vector<FlightPlan::RouteWaypoint>& routeWaypoints,
            const string& departureAirportIcao,
            const string& arrivalAirportIcao)
        {
            GeoPoint fromPoint = tryFindRouteEndpointLocation(routeWaypoints, true);
            GeoPoint toPoint = tryFindRouteEndpointLocation(routeWaypoints, false);

            if (fromPoint == GeoPoint::empty)
            {
                fromPoint = tryFindAirportLocation(host, departureAirportIcao);
            }
            if (toPoint == GeoPoint::empty)
            {
                toPoint = tryFindAirportLocation(host, arrivalAirportIcao);
            }

            if (fromPoint == GeoPoint::empty || toPoint == GeoPoint::empty)
            {
                return 34000.0f;
            }

            const float routeDistanceNm = GeoMath::getDistanceMeters(fromPoint, toPoint) / METERS_IN_1_NAUTICAL_MILE;
            const float routeHeading = GeoMath::getHeadingFromPoints(fromPoint, toPoint);
            const bool eastbound = routeHeading >= 0.0f && routeHeading < 180.0f;

            if (routeDistanceNm < 120.0f)
            {
                return eastbound ? 19000.0f : 20000.0f;
            }
            if (routeDistanceNm < 250.0f)
            {
                return eastbound ? 23000.0f : 24000.0f;
            }
            if (routeDistanceNm < 450.0f)
            {
                return eastbound ? 29000.0f : 30000.0f;
            }
            if (routeDistanceNm < 800.0f)
            {
                return eastbound ? 33000.0f : 34000.0f;
            }
            if (routeDistanceNm < 1400.0f)
            {
                return eastbound ? 35000.0f : 36000.0f;
            }

            return eastbound ? 39000.0f : 40000.0f;
        }
    }

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
        if (m_plan)
        {
            setPlan(m_plan);
        }
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

    void Flight::removeClearance(Clearance::Type type)
    {
        m_clearances.erase(remove_if(m_clearances.begin(), m_clearances.end(), [type](const shared_ptr<Clearance>& clearance) {
            return clearance && clearance->type() == type;
        }), m_clearances.end());
    }

    shared_ptr<Clearance> Flight::tryFindClearanceUncast(Clearance::Type type)
    {
        for (int i = static_cast<int>(m_clearances.size()) - 1 ; i >= 0 ; --i)
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

    void Flight::setPlan(shared_ptr<FlightPlan> _plan)
    {
        m_plan = _plan;

        if (m_plan)
        {
            m_plan->rebuildProcedureLegs(m_host);
            m_planCursor = shared_ptr<FlightPlan::Cursor>(new FlightPlan::Cursor(m_plan));
        }
        else
        {
            m_planCursor.reset();
            return;
        }

        if (!m_plan->airlineIcao().empty())
        {
            m_airlineIcao = m_plan->airlineIcao();
        }

        if (!m_plan->callsign().empty())
        {
            m_callSign = m_plan->callsign();
        }

        if (!m_plan->flightNo().empty())
        {
            m_flightNo = m_plan->flightNo();
        }
    }

    void Flight::setArrivalRunway(const string& runwayName)
    {
        if (!m_plan)
        {
            return;
        }

        m_plan->setArrivalRunway(runwayName);
        m_plan->rebuildProcedureLegs(m_host);

        if (m_planCursor)
        {
            m_planCursor->reset();
        }

        m_landingRunwayElevationFeet = ALTITUDE_UNASSIGNED - 1;
    }

    void FlightPlan::rebuildProcedureLegs(shared_ptr<HostServices> host)
    {
        vector<string> sidTrack;
        vector<string> starTrack;
        vector<string> approachTrack;
        vector<string> missedApproachTrack;
        unordered_map<string, GeoPoint> cifpWaypointLocations;
        unordered_map<string, float> cifpAltitudeConstraints;  // Waypoint -> altitude (feet)
        unordered_map<string, float> cifpSpeedConstraints;     // Waypoint -> speed (knots)
        vector<RouteWaypoint> parsedRouteWaypoints = m_filedRouteWaypoints;

        if (host)
        {
            try
            {
                XPCifpReader cifpReader(host);
                
                // Read SID with coordinates
                auto sidWithLocations = cifpReader.readProcedureTrackWithLocations(
                    m_departureAirportIcao,
                    "SID",
                    m_sidName,
                    m_departureRunway,
                    m_sidTransition);
                for (const auto& wp : sidWithLocations.waypoints)
                {
                    sidTrack.push_back(wp.name);
                    if (wp.hasLocation)
                    {
                        cifpWaypointLocations[wp.name] = GeoPoint(wp.latitude, wp.longitude);
                    }
                    // Store altitude/speed constraints from CIFP
                    if (wp.altitudeConstraint > 0)
                    {
                        cifpAltitudeConstraints[wp.name] = wp.altitudeConstraint;
                    }
                    if (wp.speedConstraint > 0)
                    {
                        cifpSpeedConstraints[wp.name] = wp.speedConstraint;
                    }
                }
                
                // Read STAR with coordinates
                auto starWithLocations = cifpReader.readProcedureTrackWithLocations(
                    m_arrivalAirportIcao,
                    "STAR",
                    m_starName,
                    m_arrivalRunway,
                    m_starTransition);
                for (const auto& wp : starWithLocations.waypoints)
                {
                    starTrack.push_back(wp.name);
                    if (wp.hasLocation)
                    {
                        cifpWaypointLocations[wp.name] = GeoPoint(wp.latitude, wp.longitude);
                    }
                    // Store altitude/speed constraints from CIFP
                    if (wp.altitudeConstraint > 0)
                    {
                        cifpAltitudeConstraints[wp.name] = wp.altitudeConstraint;
                    }
                    if (wp.speedConstraint > 0)
                    {
                        cifpSpeedConstraints[wp.name] = wp.speedConstraint;
                    }
                }
                
                // Read Approach with coordinates (uses APPCH type internally)
                auto approachWithLocations = cifpReader.readApproachProcedureTracksWithLocations(
                    m_arrivalAirportIcao,
                    m_approachName,
                    m_arrivalRunway,
                    "");
                for (const auto& wp : approachWithLocations.procedureTrack.waypoints)
                {
                    approachTrack.push_back(wp.name);
                    if (wp.hasLocation)
                    {
                        cifpWaypointLocations[wp.name] = GeoPoint(wp.latitude, wp.longitude);
                    }
                    // Store altitude/speed constraints from CIFP
                    if (wp.altitudeConstraint > 0)
                    {
                        cifpAltitudeConstraints[wp.name] = wp.altitudeConstraint;
                    }
                    if (wp.speedConstraint > 0)
                    {
                        cifpSpeedConstraints[wp.name] = wp.speedConstraint;
                    }
                }
                // Also store missed approach waypoints (with coordinates if available)
                for (const auto& wp : approachWithLocations.missedTrack.waypoints)
                {
                    missedApproachTrack.push_back(wp.name);
                    if (wp.hasLocation)
                    {
                        cifpWaypointLocations[wp.name] = GeoPoint(wp.latitude, wp.longitude);
                    }
                }

                if (approachTrack.empty() && !m_arrivalRunway.empty())
                {
                    auto runwayApproachWithLocations = cifpReader.readApproachProcedureTracksWithLocations(
                        m_arrivalAirportIcao,
                        m_arrivalRunway,
                        m_arrivalRunway,
                        "");
                    approachTrack.clear();
                    missedApproachTrack.clear();
                    for (const auto& wp : runwayApproachWithLocations.procedureTrack.waypoints)
                    {
                        approachTrack.push_back(wp.name);
                        if (wp.hasLocation)
                        {
                            cifpWaypointLocations[wp.name] = GeoPoint(wp.latitude, wp.longitude);
                        }
                        // Store altitude/speed constraints from CIFP
                        if (wp.altitudeConstraint > 0)
                        {
                            cifpAltitudeConstraints[wp.name] = wp.altitudeConstraint;
                        }
                        if (wp.speedConstraint > 0)
                        {
                            cifpSpeedConstraints[wp.name] = wp.speedConstraint;
                        }
                    }
                    for (const auto& wp : runwayApproachWithLocations.missedTrack.waypoints)
                    {
                        missedApproachTrack.push_back(wp.name);
                        if (wp.hasLocation)
                        {
                            cifpWaypointLocations[wp.name] = GeoPoint(wp.latitude, wp.longitude);
                        }
                    }
                }
            }
            catch (const exception& e)
            {
                if (host)
                {
                    host->writeLog(
                        "FLIGHTPLAN|CIFP procedure load failed for %s->%s: %s",
                        m_departureAirportIcao.c_str(),
                        m_arrivalAirportIcao.c_str(),
                        e.what());
                }
            }
        }

        XPNavaidReader navaidReader(host);
        XPMinimumAltitudeReader altitudeReader(host);
        vector<RouteWaypoint> rebuiltRouteWaypoints;
        vector<RouteWaypoint> knownWaypoints;
        unordered_map<string, GeoPoint> waypointLocationByKey;

        const auto rememberWaypointLocation = [&](const string& name, const GeoPoint& location) {
            if (name.empty() || location == GeoPoint::empty)
            {
                return;
            }

            const string key = normalizeWaypointKey(name);
            if (key.empty())
            {
                return;
            }

            if (waypointLocationByKey.find(key) == waypointLocationByKey.end())
            {
                waypointLocationByKey.insert({ key, location });
            }
        };

        const auto lookupWaypointLocation = [&](const string& name) -> GeoPoint {
            const string key = normalizeWaypointKey(name);
            const auto found = waypointLocationByKey.find(key);
            if (found == waypointLocationByKey.end())
            {
                return GeoPoint::empty;
            }

            return found->second;
        };

        const auto appendWaypoints = [&](vector<RouteWaypoint>& target, const vector<string>& waypoints, const string& contextIcao, const string& airway) {
            // Get airport location for distance-based waypoint resolution
            GeoPoint airportLocation = GeoPoint::empty;
            if (!contextIcao.empty())
            {
                airportLocation = tryFindAirportLocation(host, contextIcao);
            }
            
            for (const auto& waypoint : waypoints)
            {
                if (!waypoint.empty())
                {
                    // First check if we have CIFP coordinates for this waypoint
                    GeoPoint cifpLocation = GeoPoint::empty;
                    auto cifpIt = cifpWaypointLocations.find(waypoint);
                    if (cifpIt != cifpWaypointLocations.end())
                    {
                        cifpLocation = cifpIt->second;
                    }
                    
                    const GeoPoint location = resolveWaypointLocation(navaidReader, contextIcao, waypoint, cifpLocation, airportLocation);
                    rememberWaypointLocation(waypoint, location);
                    appendRouteWaypoint(target, waypoint, airway, location);
                }
            }
        };

        const auto appendParsedWaypoints = [&](vector<RouteWaypoint>& target, const vector<RouteWaypoint>& waypoints) {
            for (const auto& waypoint : waypoints)
            {
                const GeoPoint location = waypoint.location != GeoPoint::empty
                    ? waypoint.location
                    : resolveWaypointLocation(navaidReader, "", waypoint.name);
                rememberWaypointLocation(waypoint.name, location);
                appendRouteWaypoint(target, waypoint.name, waypoint.airway, location);
            }
        };

        const auto resolveLegFloor = [&](const string& contextIcao, const string& fromName, const string& toName, float baseAltitudeFeet) {
            const GeoPoint fromLocation = lookupWaypointLocation(fromName);
            const GeoPoint toLocation = lookupWaypointLocation(toName);
            return altitudeReader.minimumAltitudeForLeg(
                contextIcao,
                fromName,
                fromLocation,
                toName,
                toLocation,
                baseAltitudeFeet);
        };

        const auto buildAltitudeProfile = [&](const vector<string>& waypoints, const string& contextIcao, float baseAltitudeFeet, bool propagateForward) {
            vector<float> targetAltitudes;
            if (waypoints.size() < 2)
            {
                return targetAltitudes;
            }

            targetAltitudes.reserve(waypoints.size() - 1);

            // Calculate terrain-aware minimum for each leg
            for (size_t i = 1; i < waypoints.size(); ++i)
            {
                targetAltitudes.push_back(resolveLegFloor(contextIcao, waypoints[i - 1], waypoints[i], baseAltitudeFeet));
            }

            if (propagateForward)
            {
                // For SIDs: ensure continuous climb (each leg at least as high as previous)
                for (size_t i = 1; i < targetAltitudes.size(); ++i)
                {
                    targetAltitudes[i] = max(targetAltitudes[i], targetAltitudes[i - 1]);
                }
            }
            else
            {
                // For STARs/Approaches: gradual descent profile instead of pushing final alt all the way back
                // Start from cruise/base altitude at the beginning, descend gradually toward final altitude
                // Use ~3 degree descent gradient (318 ft per NM) as baseline
                const float descentRatePerNm = 318.0f;

                // Work forward, calculating a gradual descent profile
                for (size_t i = 0; i < targetAltitudes.size(); ++i)
                {
                    // Calculate distance from this leg to the end
                    float distanceToEndNm = 0.0f;
                    for (size_t j = i + 1; j < waypoints.size() && j - 1 < targetAltitudes.size(); ++j)
                    {
                        GeoPoint fromLoc = lookupWaypointLocation(waypoints[j - 1]);
                        GeoPoint toLoc = lookupWaypointLocation(waypoints[j]);
                        if (fromLoc != GeoPoint::empty && toLoc != GeoPoint::empty)
                        {
                            distanceToEndNm += GeoMath::getDistanceMeters(fromLoc, toLoc) / METERS_IN_1_NAUTICAL_MILE;
                        }
                        else
                        {
                            // Estimate 5 NM per leg if locations unknown
                            distanceToEndNm += 5.0f;
                        }
                    }

                    // Calculate what altitude we should be at this point for a gradual descent
                    // Final altitude is targetAltitudes.back(), work backwards adding descent needed
                    const float finalAltitude = targetAltitudes.empty() ? baseAltitudeFeet : targetAltitudes.back();
                    const float gradualAltitude = finalAltitude + (distanceToEndNm * descentRatePerNm);

                    // For STARs: gradualAltitude starts high and decreases toward finalAltitude
                    // We want to use gradualAltitude (capped at baseAltitudeFeet), but never below terrain minimum
                    // However, we must NOT force early waypoints to match later waypoint altitudes
                    // The terrain minimum is only a safety floor, not a target
                    const float descentTargetAltitude = min(gradualAltitude, baseAltitudeFeet);

                    // Use the higher of: gradual descent target, terrain minimum
                    // BUT only if the terrain minimum is higher than the descent target
                    // (don't force early waypoints down to match late waypoints)
                    if (targetAltitudes[i] < descentTargetAltitude)
                    {
                        // Terrain is below our descent path, follow the descent path
                        targetAltitudes[i] = descentTargetAltitude;
                    }
                    // else: Terrain minimum is above descent path, must stay at terrain minimum (safety)
                }

                // Removed: backward propagation that was forcing early waypoints to late waypoint altitudes
                // This was causing aircraft to descend too early and "sit" at approach altitude
                // The terrain minimum from resolveLegFloor is the correct safety floor for each leg
            }

            return targetAltitudes;
        };

        const auto appendTrackLegsWithAltitudes = [&](FlightPlan::LegType type, const vector<string>& waypoints, const vector<float>& targetAltitudes, float defaultTargetSpeed) {
            if (waypoints.size() < 2 || targetAltitudes.size() + 1 != waypoints.size())
            {
                return;
            }

            for (size_t i = 1; i < waypoints.size(); ++i)
            {
                const string& fromWp = waypoints[i - 1];
                const string& toWp = waypoints[i];

                // Use CIFP altitude constraint if available, otherwise use calculated
                float legAltitude = targetAltitudes[i - 1];
                auto altIt = cifpAltitudeConstraints.find(toWp);
                if (altIt != cifpAltitudeConstraints.end() && altIt->second > 0)
                {
                    legAltitude = altIt->second;
                }

                // Use CIFP speed constraint if available, otherwise use default
                float legSpeed = defaultTargetSpeed;
                auto spdIt = cifpSpeedConstraints.find(toWp);
                if (spdIt != cifpSpeedConstraints.end() && spdIt->second > 0)
                {
                    legSpeed = spdIt->second;
                }

                appendLegFromPoints(
                    m_legs,
                    type,
                    fromWp,
                    toWp,
                    legAltitude,
                    legSpeed);
            }
        };

        appendWaypoints(rebuiltRouteWaypoints, sidTrack, m_departureAirportIcao, "");
        appendWaypoints(knownWaypoints, sidTrack, m_departureAirportIcao, "");
        appendParsedWaypoints(rebuiltRouteWaypoints, parsedRouteWaypoints);
        appendParsedWaypoints(knownWaypoints, parsedRouteWaypoints);
        appendWaypoints(rebuiltRouteWaypoints, starTrack, m_arrivalAirportIcao, "");
        appendWaypoints(knownWaypoints, starTrack, m_arrivalAirportIcao, "");
        appendWaypoints(rebuiltRouteWaypoints, approachTrack, m_arrivalAirportIcao, "");
        appendWaypoints(knownWaypoints, approachTrack, m_arrivalAirportIcao, "");

        m_cruiseAltitudeFeet = selectCruiseAltitudeFeet(host, parsedRouteWaypoints, m_departureAirportIcao, m_arrivalAirportIcao);

        m_legs.clear();

        const auto appendFallbackLeg = [this](LegType type, const string& fromNavaid, const string& toNavaid, float targetAltitude, float targetSpeed) {
            appendLegFromPoints(m_legs, type, fromNavaid, toNavaid, targetAltitude, targetSpeed);
        };

        const auto appendProcedure = [&](LegType type, const vector<string>& track, const vector<float>& targetAltitudes, float targetSpeed) {
            appendTrackLegsWithAltitudes(type, track, targetAltitudes, targetSpeed);
        };

        const auto appendBridgeLeg = [&](LegType type, const string& fromNavaid, const string& toNavaid, float targetAltitude, float targetSpeed) {
            if (fromNavaid.empty() || toNavaid.empty() || normalizeWaypointKey(fromNavaid) == normalizeWaypointKey(toNavaid))
            {
                return;
            }

            appendFallbackLeg(type, fromNavaid, toNavaid, targetAltitude, targetSpeed);
        };

        const vector<string> routeTrack = toWaypointNames(parsedRouteWaypoints);

        const string takeoffTarget = !sidTrack.empty()
            ? sidTrack.front()
            : (!routeTrack.empty()
                ? routeTrack.front()
                : (!starTrack.empty()
                    ? starTrack.front()
                    : (!approachTrack.empty()
                        ? approachTrack.front()
                        : "")));

        if (!takeoffTarget.empty() && (!m_departureAirportIcao.empty() || !m_departureRunway.empty()))
        {
            appendFallbackLeg(
                LegType::TakeOff,
                m_departureAirportIcao.empty() ? m_departureRunway : m_departureAirportIcao,
                takeoffTarget,
                1500.0f,
                160.0f);
        }

        if (!sidTrack.empty())
        {
            const vector<float> sidTargetAltitudes = buildAltitudeProfile(sidTrack, m_departureAirportIcao, 10000.0f, true);
            appendProcedure(LegType::Sid, sidTrack, sidTargetAltitudes, 210.0f);
        }
        if (!routeTrack.empty())
        {
            if (!sidTrack.empty())
            {
                appendBridgeLeg(LegType::EnRoute, sidTrack.back(), routeTrack.front(), m_cruiseAltitudeFeet, 450.0f);
            }
            appendTrackLegs(m_legs, LegType::EnRoute, routeTrack, m_cruiseAltitudeFeet, 450.0f);
        }
        else if (!sidTrack.empty() && !starTrack.empty())
        {
            appendBridgeLeg(LegType::EnRoute, sidTrack.back(), starTrack.front(), m_cruiseAltitudeFeet, 450.0f);
        }

        if (!starTrack.empty())
        {
            if (!routeTrack.empty())
            {
                appendBridgeLeg(LegType::EnRoute, routeTrack.back(), starTrack.front(), m_cruiseAltitudeFeet, 450.0f);
            }
            else if (!sidTrack.empty())
            {
                appendBridgeLeg(LegType::EnRoute, sidTrack.back(), starTrack.front(), m_cruiseAltitudeFeet, 450.0f);
            }

            const vector<float> starTargetAltitudes = buildAltitudeProfile(starTrack, m_arrivalAirportIcao, 6000.0f, false);
            appendProcedure(LegType::Star, starTrack, starTargetAltitudes, 250.0f);
        }

        if (!approachTrack.empty())
        {
            if (!starTrack.empty())
            {
                appendBridgeLeg(LegType::EnRoute, starTrack.back(), approachTrack.front(), m_cruiseAltitudeFeet, 450.0f);
            }
            else if (!routeTrack.empty())
            {
                appendBridgeLeg(LegType::EnRoute, routeTrack.back(), approachTrack.front(), m_cruiseAltitudeFeet, 450.0f);
            }
            else if (!sidTrack.empty())
            {
                appendBridgeLeg(LegType::EnRoute, sidTrack.back(), approachTrack.front(), m_cruiseAltitudeFeet, 450.0f);
            }

            const vector<float> approachTargetAltitudes = buildAltitudeProfile(approachTrack, m_arrivalAirportIcao, 3000.0f, false);
            appendProcedure(LegType::Approach, approachTrack, approachTargetAltitudes, 180.0f);

            const bool approachAlreadyEndsOnRunway =
                !m_arrivalRunway.empty() &&
                normalizeRunwayKey(approachTrack.back()) == normalizeRunwayKey(m_arrivalRunway);
            if (!approachAlreadyEndsOnRunway)
            {
                if (!m_arrivalRunway.empty())
                {
                    const GeoPoint runwayLocation = resolveWaypointLocation(navaidReader, m_arrivalAirportIcao, m_arrivalRunway);
                    rememberWaypointLocation(m_arrivalRunway, runwayLocation);
                    appendRouteWaypoint(rebuiltRouteWaypoints, m_arrivalRunway, "", runwayLocation);
                    appendRouteWaypoint(knownWaypoints, m_arrivalRunway, "", runwayLocation);
                }

                appendFallbackLeg(
                    LegType::Landing,
                    approachTrack.back(),
                    m_arrivalRunway.empty() ? approachTrack.back() : m_arrivalRunway,
                    0.0f,
                    140.0f);
            }

            if (!missedApproachTrack.empty())
            {
                vector<string> goAroundTrack;
                const string goAroundStart = !approachTrack.empty()
                    ? approachTrack.back()
                    : m_arrivalRunway;

                if (!goAroundStart.empty())
                {
                    goAroundTrack.push_back(goAroundStart);
                }

                for (const auto& waypoint : missedApproachTrack)
                {
                    if (goAroundTrack.empty() ||
                        normalizeWaypointKey(goAroundTrack.back()) != normalizeWaypointKey(waypoint))
                    {
                        goAroundTrack.push_back(waypoint);
                    }
                }

                if (goAroundTrack.size() >= 2)
                {
                    const vector<float> goAroundTargetAltitudes = buildAltitudeProfile(
                        goAroundTrack,
                        m_arrivalAirportIcao,
                        3000.0f,
                        true);
                    appendProcedure(LegType::GoAround, goAroundTrack, goAroundTargetAltitudes, 180.0f);
                }

                appendWaypoints(knownWaypoints, missedApproachTrack, m_arrivalAirportIcao, "");
            }
        }

        m_routeWaypoints.swap(rebuiltRouteWaypoints);
        m_knownWaypoints.swap(knownWaypoints);

        if (m_legs.empty())
        {
            appendFallbackLeg(LegType::EnRoute, m_departureAirportIcao, m_arrivalAirportIcao, m_cruiseAltitudeFeet, 450.0f);
        }
    }
}
