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
#include <limits>
#include <random>
#include <algorithm>
#include <cmath>
#include <cctype>

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
#include "fr24AirportScheduleSource.hpp"

using namespace std;
using namespace PPL;
using namespace world;
using namespace ai;

class DemoScheduleLoader
{
private:
    struct AirportLoadedException
    {
    };

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
    void loadSchedules(float loadFactor)
    {
        string userAirportIcao = getUserAirportIcao();
        m_airport = getAirportOrFallback(userAirportIcao);
        m_airport->selectActiveRunways();
        m_airport->selectArrivalAndDepartureTaxiways();
        logActiveRunwaysBounds();

        m_host->writeLog("SCHEDL|Loading schedules at airport[%s]", m_airport->header().icao().c_str());

        if (!loadLiveSchedules(loadFactor))
        {
            m_host->writeLog("SCHEDL|No live schedule data available; using demo schedules instead");
            initDemoSchedules(loadFactor, m_world->currentTime() + 200, m_world->currentTime() + 30);
        }

        m_host->writeLog(
            "SCHEDL|Loaded [%d] AI flights at airport[%s]",
            m_world->flights().size(),
            m_airport->header().icao().c_str());
    }

public:

    shared_ptr<Airport> airport() const { return m_airport; }

private:

    shared_ptr<Airport> findNearestAirport(const GeoPoint& location, const string& excludedIcao = "")
    {
        shared_ptr<Airport> bestAirport;
        double bestDistanceMeters = numeric_limits<double>::max();

        for (const auto& airport : m_world->airports())
        {
            if (!airport || airport->header().icao() == excludedIcao)
            {
                continue;
            }

            const auto& airportLocation = airport->header().datum();
            if (airportLocation == GeoPoint::empty)
            {
                continue;
            }

            const double distanceMeters = GeoMath::getDistanceMeters(location, airportLocation);
            if (distanceMeters < bestDistanceMeters)
            {
                bestDistanceMeters = distanceMeters;
                bestAirport = airport;
            }
        }

        return bestAirport;
    }

    static string normalizeWaypointName(string value)
    {
        value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
            return !isalnum(c);
        }), value.end());

        transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
            return static_cast<char>(toupper(c));
        });

        return value;
    }

    bool tryFindWaypointLocation(const shared_ptr<FlightPlan>& plan, const string& waypointName, GeoPoint& location) const
    {
        if (!plan || waypointName.empty())
        {
            return false;
        }

        const string normalizedWaypointName = normalizeWaypointName(waypointName);
            for (const auto& routeWaypoint : plan->knownWaypoints())
        {
            if (routeWaypoint.location == GeoPoint::empty)
            {
                continue;
            }

            if (normalizeWaypointName(routeWaypoint.name) == normalizedWaypointName)
            {
                location = routeWaypoint.location;
                return true;
            }
        }

        return false;
    }

    int estimateArrivalLeadSeconds(const shared_ptr<FlightPlan>& flightPlan) const
    {
        if (!flightPlan)
        {
            return 240;
        }

        int estimatedSeconds = 0;
        bool hasProcedureLeg = false;

        for (const auto& leg : flightPlan->legs())
        {
            if (!leg)
            {
                continue;
            }

            if (leg->type() != FlightPlan::LegType::Star &&
                leg->type() != FlightPlan::LegType::Approach &&
                leg->type() != FlightPlan::LegType::Landing)
            {
                continue;
            }

            GeoPoint fromPoint;
            GeoPoint toPoint;
            if (!tryFindWaypointLocation(flightPlan, leg->fromNavaid(), fromPoint) ||
                !tryFindWaypointLocation(flightPlan, leg->toNavaid(), toPoint))
            {
                continue;
            }

            const float targetSpeedKt = max(120.0f, leg->targetSpeed() > 0.0f ? leg->targetSpeed() : 180.0f);
            const double speedMetersPerSecond = targetSpeedKt * METERS_IN_1_NAUTICAL_MILE / 3600.0;
            if (speedMetersPerSecond <= 0.0)
            {
                continue;
            }

            const int legSeconds = max(10, static_cast<int>(GeoMath::getDistanceMeters(fromPoint, toPoint) / speedMetersPerSecond + 0.5));
            estimatedSeconds += legSeconds;
            hasProcedureLeg = true;
        }

        if (!hasProcedureLeg)
        {
            return 240;
        }

        return max(estimatedSeconds + 120, 240);
    }

    struct ProcedureRunwaySelection
    {
        string runwayName;
        string procedureName;
        int score = numeric_limits<int>::min();
    };

    static string normalizeProcedureToken(string value)
    {
        value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
            return !isalnum(c);
        }), value.end());

        transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
            return static_cast<char>(toupper(c));
        });

        return value;
    }

    static string normalizeRunwayToken(const string& value)
    {
        string normalized = normalizeProcedureToken(value);
        if (normalized.rfind("RW", 0) == 0)
        {
            normalized.erase(0, 2);
        }
        return normalized;
    }

    bool tryResolveAirportLocation(const string& airportIcao, GeoPoint& location)
    {
        if (airportIcao.empty())
        {
            return false;
        }

        try
        {
            auto airport = m_world->getAirport(airportIcao);
            if (airport && airport->header().datum() != GeoPoint::empty)
            {
                location = airport->header().datum();
                return true;
            }
        }
        catch (const exception&)
        {
        }

        if (auto airport = tryLoadAirportIntoWorld(airportIcao))
        {
            if (airport->header().datum() != GeoPoint::empty)
            {
                location = airport->header().datum();
                return true;
            }
        }

        return false;
    }

    static int scoreProcedureNameForRunway(const string& procedureName, const string& runwayName)
    {
        if (procedureName.empty() || runwayName.empty())
        {
            return 0;
        }

        const string procedureToken = normalizeProcedureToken(procedureName);
        const string runwayToken = normalizeRunwayToken(runwayName);
        if (procedureToken.empty() || runwayToken.empty())
        {
            return 0;
        }

        int score = 0;
        if (procedureToken.find(runwayToken) != string::npos)
        {
            score += 900;
        }

        if (runwayToken.size() >= 2)
        {
            const string runwayDigits = runwayToken.substr(0, 2);
            if (procedureToken.find(runwayDigits) != string::npos)
            {
                score += 250;
            }
        }

        return score;
    }

    bool tryResolveProcedureAnchorLocation(
        const string& airportIcao,
        const string& recordType,
        const string& procedureName,
        const string& runwayName,
        bool useFirstWaypoint,
        GeoPoint& location) const
    {
        XPCifpReader cifpReader(m_host);
        auto track = cifpReader.readProcedureTrack(airportIcao, recordType, procedureName, runwayName, "");
        if (track.empty())
        {
            return false;
        }

        XPNavaidReader navaidReader(m_host);
        const string runwayToken = normalizeRunwayToken(runwayName);

        const auto tryResolve = [&](const string& waypoint) {
            if (normalizeRunwayToken(waypoint) == runwayToken)
            {
                return false;
            }

            return navaidReader.tryResolveWaypoint(airportIcao, waypoint, location);
        };

        if (useFirstWaypoint)
        {
            for (const auto& waypoint : track)
            {
                if (tryResolve(waypoint))
                {
                    return true;
                }
            }
        }
        else
        {
            for (auto it = track.rbegin(); it != track.rend(); ++it)
            {
                if (tryResolve(*it))
                {
                    return true;
                }
            }
        }

        return false;
    }

    int scoreProcedureForRemoteAirport(
        const string& airportIcao,
        const string& recordType,
        const string& procedureName,
        const string& runwayName,
        const GeoPoint& airportLocation,
        const GeoPoint& remoteLocation,
        bool inboundProcedure) const
    {
        int score = scoreProcedureNameForRunway(procedureName, runwayName);

        if (airportLocation == GeoPoint::empty || remoteLocation == GeoPoint::empty)
        {
            return score;
        }

        GeoPoint anchorLocation;
        const bool useFirstWaypoint = inboundProcedure;
        if (!tryResolveProcedureAnchorLocation(
                airportIcao,
                recordType,
                procedureName,
                runwayName,
                useFirstWaypoint,
                anchorLocation))
        {
            return score;
        }

        const float remoteBearing = inboundProcedure
            ? GeoMath::getHeadingFromPoints(remoteLocation, airportLocation)
            : GeoMath::getHeadingFromPoints(airportLocation, remoteLocation);
        const float anchorBearing = inboundProcedure
            ? GeoMath::getHeadingFromPoints(anchorLocation, airportLocation)  // For STAR: bearing from anchor to airport (inbound)
            : GeoMath::getHeadingFromPoints(airportLocation, anchorLocation); // For SID: bearing from airport to anchor (outbound)
        const float turnDegrees = static_cast<float>(fabs(GeoMath::getTurnDegrees(remoteBearing, anchorBearing)));

        score += max(0, 540 - static_cast<int>(turnDegrees * 4.0f));
        if (turnDegrees <= 30.0f)
        {
            score += 250;
        }
        else if (turnDegrees <= 60.0f)
        {
            score += 125;
        }
        else if (turnDegrees >= 135.0f)
        {
            score -= 150;
        }

        return score;
    }

    ProcedureRunwaySelection selectProcedureAndRunway(
        shared_ptr<Airport> airport,
        const vector<string>& candidateRunways,
        const string& recordType,
        const string& remoteAirportIcao,
        bool inboundProcedure) const
    {
        ProcedureRunwaySelection bestSelection;
        if (!airport)
        {
            return bestSelection;
        }

        vector<string> runways = candidateRunways;
        if (runways.empty())
        {
            try
            {
                const auto runway = (normalizeProcedureToken(recordType) == "STAR")
                    ? airport->findPreferredArrivalRunway()
                    : airport->findLongestRunway();
                if (runway)
                {
                    runways.push_back(runway->end1().name());
                }
            }
            catch (const exception&)
            {
            }
        }

        if (runways.empty())
        {
            return bestSelection;
        }

        GeoPoint airportLocation = airport->header().datum();
        GeoPoint remoteLocation = GeoPoint::empty;
        const bool hasRemoteLocation = const_cast<DemoScheduleLoader*>(this)->tryResolveAirportLocation(remoteAirportIcao, remoteLocation);
        XPCifpReader cifpReader(m_host);

        for (const auto& runwayName : runways)
        {
            int runwayScore = 0;
            try
            {
                const auto& runwayEnd = airport->getRunwayEndOrThrow(runwayName);
                if (hasRemoteLocation && airportLocation != GeoPoint::empty)
                {
                    const float routeBearing = inboundProcedure
                        ? GeoMath::getHeadingFromPoints(remoteLocation, airportLocation)
                        : GeoMath::getHeadingFromPoints(airportLocation, remoteLocation);
                    const float turnDegrees = static_cast<float>(fabs(GeoMath::getTurnDegrees(routeBearing, runwayEnd.heading())));
                    runwayScore += max(0, 360 - static_cast<int>(turnDegrees * 3.0f));
                    if (turnDegrees <= 45.0f)
                    {
                        runwayScore += 150;
                    }
                }
            }
            catch (const exception&)
            {
            }

            string bestProcedureForRunway;
            int bestProcedureScore = numeric_limits<int>::min();
            for (const auto& procedureName : cifpReader.enumProcedures(airport->header().icao(), recordType))
            {
                const int procedureScore = scoreProcedureForRemoteAirport(
                    airport->header().icao(),
                    recordType,
                    procedureName,
                    runwayName,
                    airportLocation,
                    remoteLocation,
                    inboundProcedure);

                if (bestProcedureForRunway.empty() || procedureScore > bestProcedureScore)
                {
                    bestProcedureForRunway = procedureName;
                    bestProcedureScore = procedureScore;
                }
            }

            const int totalScore = runwayScore + (bestProcedureForRunway.empty() ? 0 : bestProcedureScore);
            if (bestSelection.runwayName.empty() || totalScore > bestSelection.score)
            {
                bestSelection.runwayName = runwayName;
                bestSelection.procedureName = bestProcedureForRunway;
                bestSelection.score = totalScore;
            }
        }

        return bestSelection;
    }

    string selectSidForRunway(const string& airportIcao, const string& runwayName)
    {
        try
        {
            XPCifpReader cifpReader(m_host);
            auto sids = cifpReader.enumProcedures(airportIcao, "SID");
            if (sids.empty())
            {
                return "";
            }

            // Try to find a SID matching the runway
            string bestSid;
            for (const auto& sid : sids)
            {
                // If this SID name contains the runway number, it's likely for this runway
                if (sid.find(runwayName) != string::npos)
                {
                    return sid;
                }
                // Also check for common runway suffixes like "24L", "06R" etc
                if (runwayName.length() >= 2)
                {
                    string runwayDigits = runwayName.substr(0, 2);
                    if (sid.find(runwayDigits) != string::npos)
                    {
                        bestSid = sid;
                    }
                }
            }
            // Return best matching SID (may be empty if no good match found)
            // Let caller handle fallback instead of arbitrarily picking first alphabetically
            return bestSid;
        }
        catch (const exception&)
        {
            return "";
        }
    }

    string selectStarForRunway(const string& airportIcao, const string& runwayName)
    {
        try
        {
            XPCifpReader cifpReader(m_host);
            auto stars = cifpReader.enumProcedures(airportIcao, "STAR");
            if (stars.empty())
            {
                return "";
            }

            // Try to find a STAR matching the runway
            string bestStar;
            for (const auto& star : stars)
            {
                // If this STAR name contains the runway number, it's likely for this runway
                if (star.find(runwayName) != string::npos)
                {
                    return star;
                }
                // Also check for common runway suffixes
                if (runwayName.length() >= 2)
                {
                    string runwayDigits = runwayName.substr(0, 2);
                    if (star.find(runwayDigits) != string::npos)
                    {
                        bestStar = star;
                    }
                }
            }
            // Return best matching STAR (may be empty if no good match found)
            // Let caller handle fallback instead of arbitrarily picking first alphabetically
            return bestStar;
        }
        catch (const exception&)
        {
            return "";
        }
    }

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

        if (!m_world->airports().empty())
        {
            const GeoPoint userLocation((float)m_userAircraftLatitude, (float)m_userAircraftLongitude);
            auto fallbackAirport = findNearestAirport(userLocation);
            if (!fallbackAirport && !m_world->airports().empty())
            {
                fallbackAirport = m_world->airports().front();
            }

            if (fallbackAirport)
            {
                m_host->writeLog(
                    "SCHEDL|User airport lookup: NOT FOUND! - falling back to nearest loaded airport [%s]",
                    fallbackAirport->header().icao().c_str());
                return fallbackAirport->header().icao();
            }
        }

        m_host->writeLog("SCHEDL|User airport lookup: NOT FOUND! - no airports available");
        return "";
    }

    shared_ptr<Airport> getAirportOrFallback(const string& icao)
    {
        try
        {
            return m_world->getAirport(icao);
        }
        catch(const exception&)
        {
        }

        if (auto loadedAirport = tryLoadAirportIntoWorld(icao))
        {
            m_host->writeLog(
                "SCHEDL|Airport [%s] not found in world - loaded on demand from apt.dat as [%s]",
                icao.c_str(),
                loadedAirport->header().icao().c_str());
            return loadedAirport;
        }

        if (!m_world->airports().empty())
        {
            const GeoPoint userLocation((float)m_userAircraftLatitude, (float)m_userAircraftLongitude);
            auto fallbackAirport = findNearestAirport(userLocation, icao);
            if (!fallbackAirport && !m_world->airports().empty())
            {
                fallbackAirport = m_world->airports().front();
            }

            if (fallbackAirport)
            {
                m_host->writeLog(
                    "SCHEDL|Airport [%s] not found in world - using nearest fallback airport [%s]",
                    icao.c_str(),
                    fallbackAirport->header().icao().c_str());
                return fallbackAirport;
            }
        }

        throw runtime_error("DemoScheduleLoader::getAirportOrFallback: no airports available in world");
    }

    vector<string> buildAptDatCandidates() const
    {
        vector<string> candidatePaths;

        appendAptDatCandidatesForSceneryRoot("Custom Scenery", candidatePaths);
        appendAptDatCandidatesForSceneryRoot("Global Scenery", candidatePaths);

        candidatePaths.push_back(m_host->getHostFilePath({ "Resources", "default scenery", "default apt dat", "Earth nav data", "apt.dat" }));
        return candidatePaths;
    }

    void appendAptDatCandidatesForSceneryRoot(const string& sceneryRoot, vector<string>& candidatePaths) const
    {
        try
        {
            for (const auto& sceneryPack : m_host->findFilesInHostDirectory({ sceneryRoot }))
            {
                if (sceneryPack.empty() || sceneryPack == "." || sceneryPack == ".." || sceneryPack == "scenery_packs.ini")
                {
                    continue;
                }

                candidatePaths.push_back(m_host->getHostFilePath({ sceneryRoot, sceneryPack, "Earth nav data", "apt.dat" }));
            }
        }
        catch(const exception& e)
        {
            m_host->writeLog("SCHEDL|failed to enumerate %s packages: %s", sceneryRoot.c_str(), e.what());
        }
    }

    string normalizeIcao(const string& value) const
    {
        string result = value;
        transform(result.begin(), result.end(), result.begin(), [](unsigned char c) {
            return static_cast<char>(toupper(c));
        });
        return result;
    }

    shared_ptr<Airport> tryLoadAirportIntoWorld(const string& icao)
    {
        const string normalizedIcao = normalizeIcao(icao);
        if (normalizedIcao.empty())
        {
            return nullptr;
        }

        const auto candidatePaths = buildAptDatCandidates();
        for (const auto& candidatePath : candidatePaths)
        {
            try
            {
                auto input = m_host->openFileForRead(candidatePath);
                if (!input)
                {
                    continue;
                }

                shared_ptr<Airport> foundAirport;
                XPAptDatReader aptDatReader(m_host);

                try
                {
                    aptDatReader.readAptDat(
                        *input,
                        WorldBuilder::assembleSampleAirportControlZone,
                        [&](const Airport::Header& header) {
                            return normalizeIcao(header.icao()) == normalizedIcao;
                        },
                        [&](shared_ptr<Airport> airport) {
                            foundAirport = airport;
                            throw AirportLoadedException();
                        });
                }
                catch (const AirportLoadedException&)
                {
                }

                if (foundAirport)
                {
                    const string loadedIcao = normalizeIcao(foundAirport->header().icao());
                    if (loadedIcao == normalizedIcao)
                    {
                        return m_world->addAirport(foundAirport);
                    }

                    m_host->writeLog(
                        "SCHEDL|Airport [%s] loaded from [%s] but resolved as [%s] - ignoring mismatched airport",
                        icao.c_str(),
                        candidatePath.c_str(),
                        foundAirport->header().icao().c_str());
                }
            }
            catch(const exception&)
            {
            }
        }

        return nullptr;
    }

    string pickOtherAirportIcao(const string& referenceIcao)
    {
        const auto& airports = m_world->airports();
        if (airports.empty())
        {
            return referenceIcao;
        }

        shared_ptr<Airport> referenceAirport;
        try
        {
            referenceAirport = m_world->getAirport(referenceIcao);
        }
        catch(const exception&)
        {
        }

        if (!referenceAirport)
        {
            for (const auto& airport : airports)
            {
                if (airport && airport->header().icao() != referenceIcao)
                {
                    return airport->header().icao();
                }
            }

            // Return first airport as ultimate fallback
            for (const auto& airport : airports)
            {
                if (airport)
                {
                    return airport->header().icao();
                }
            }
            return referenceIcao;
        }

        double bestScore = -1.0;
        string bestIcao = referenceIcao;

        for (const auto& airport : airports)
        {
            if (!airport || airport->header().icao() == referenceIcao)
            {
                continue;
            }

            double runwayLengthMeters = 0.0;
            try
            {
                runwayLengthMeters = airport->findLongestRunway()->lengthMeters();
            }
            catch(const exception&)
            {
            }

            const double distanceMeters = GeoMath::getDistanceMeters(
                referenceAirport->header().datum(),
                airport->header().datum());
            const double score = distanceMeters + runwayLengthMeters * 10.0;

            if (score > bestScore)
            {
                bestScore = score;
                bestIcao = airport->header().icao();
            }
        }

        return bestIcao;
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

            auto departureSelection = selectProcedureAndRunway(
                m_airport,
                m_airport->activeDepartureRunways(),
                "SID",
                destination,
                false);
            flightPlan->setDepartureRunway(
                departureSelection.runwayName.empty() ? activeDepartureRunway : departureSelection.runwayName);
            if (!departureSelection.procedureName.empty())
            {
                flightPlan->setSid(departureSelection.procedureName);
            }

            auto destinationAirport = m_host->getWorld()->getAirport(destination);

            auto arrivalSelection = selectProcedureAndRunway(
                destinationAirport,
                destinationAirport->activeArrivalRunways(),
                "STAR",
                m_airport->header().icao(),
                true);
            flightPlan->setArrivalRunway(
                arrivalSelection.runwayName.empty()
                    ? destinationAirport->findLongestRunway()->end1().name()
                    : arrivalSelection.runwayName);
            if (!arrivalSelection.procedureName.empty())
            {
                flightPlan->setStar(arrivalSelection.procedureName);
            }

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
            auto arrivalSelection = selectProcedureAndRunway(
                m_airport,
                m_airport->activeArrivalRunways(),
                "STAR",
                origin,
                true);
            string arrivalRunway = arrivalSelection.runwayName.empty()
                ? ((((arrivalIndex++) % 2) == 0) ? activeArrivalRunway1 : activeArrivalRunway2)
                : arrivalSelection.runwayName;
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(arrivalTime - 60 * 60 * 3, arrivalTime, origin, m_airport->header().icao()));
            flightPlan->setArrivalGate(gate->name());
            flightPlan->setArrivalRunway(arrivalRunway);

            if (!arrivalSelection.procedureName.empty())
            {
                flightPlan->setStar(arrivalSelection.procedureName);
            }

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));

            auto aircraft = m_host->createAIAircraft(model, airline, to_string(flightId), world::Aircraft::Category::Jet);
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            flight->setPhase(Flight::Phase::Arrival);

            auto copyOfWorld = m_world;
            auto copyOfAirport = m_airport;
            const time_t arrivalStartTime = max(arrivalTime - 240, m_world->currentTime() + 30);
            m_world->deferUntil(
                "addInboundFlight/" + flight->callSign(),
                arrivalStartTime,
                [flight, copyOfWorld, copyOfAirport, arrivalRunway](){
                    const auto& landingRunwayEnd = copyOfWorld->getRunwayEnd(copyOfAirport->header().icao(), arrivalRunway);
                    copyOfWorld->addFlight(flight);
                    flight->aircraft()->setOnFinal(landingRunwayEnd);
                }
            );
        };

        const float normalSecondsBetweenDepartures = 210;
        const float normalSecondsBetweenArrivals = 210;
        const float normalLoadFactor = 0.7f;
        int secondsBetweenDepartures = normalSecondsBetweenDepartures * normalLoadFactor / loadFactor;
        int secondsBetweenArrivals = normalSecondsBetweenArrivals * normalLoadFactor / loadFactor;
        string outboundDestinationIcao = pickOtherAirportIcao(m_airport->header().icao());
        string inboundOriginIcao = pickOtherAirportIcao(m_airport->header().icao());

        m_host->writeLog(
            "SCHEDL|LOADFACTOR [%f] secondsBetweenArrivals=[%d] secondsBetweenDepartures=[%d]",
            loadFactor, secondsBetweenArrivals, secondsBetweenDepartures);

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
                    nextDepartureTime += secondsBetweenDepartures;
                    addOutboundFlight(model, airline, flightId, outboundDestinationIcao, departureTime, gate);
                }
                else
                {
                    time_t arrivalTime = nextArrivalTime;
                    nextArrivalTime += secondsBetweenArrivals;
                    addInboundFlight(model, airline, flightId, inboundOriginIcao, arrivalTime, gate);
                }
            }
            catch(const std::exception& e)
            {
                m_host->writeLog("SCHEDL|CRASHED while adding AI flight!!! %s", e.what());
            }
        }
    }

    bool loadLiveSchedules(float loadFactor)
    {
        Fr24AirportScheduleSource scheduleSource(m_host);
        vector<Fr24ScheduleEntry> departures;
        vector<Fr24ScheduleEntry> arrivals;

        if (!scheduleSource.tryLoadAirportSchedules(m_airport->header().icao(), departures, arrivals))
        {
            return false;
        }

        vector<shared_ptr<ParkingStand>> gates;
        findGatesForFlights(gates, loadFactor);
        if (gates.empty())
        {
            m_host->writeLog("SCHEDL|Live schedules are available but no gates can be used at airport[%s]", m_airport->header().icao().c_str());
            return false;
        }

        const auto collectTraffic = [&](const vector<Fr24ScheduleEntry>& sourceEntries, bool isDeparture, vector<pair<Fr24ScheduleEntry, bool>>& traffic) {
            for (const auto& entry : sourceEntries)
            {
                traffic.push_back({ entry, isDeparture });
            }
        };

        vector<pair<Fr24ScheduleEntry, bool>> traffic;
        collectTraffic(departures, true, traffic);
        collectTraffic(arrivals, false, traffic);

        if (traffic.empty())
        {
            return false;
        }

        sort(traffic.begin(), traffic.end(), [](const pair<Fr24ScheduleEntry, bool>& a, const pair<Fr24ScheduleEntry, bool>& b) {
            if (a.first.scheduledTime != b.first.scheduledTime)
            {
                if (a.first.scheduledTime == 0) return false;
                if (b.first.scheduledTime == 0) return true;
                return a.first.scheduledTime < b.first.scheduledTime;
            }
            if (a.second != b.second)
            {
                return a.second;
            }
            return a.first.flightNumber < b.first.flightNumber;
        });

        const float normalSecondsBetweenDepartures = 210;
        const float normalSecondsBetweenArrivals = 210;
        const float normalLoadFactor = 0.7f;
        int secondsBetweenDepartures = normalSecondsBetweenDepartures * normalLoadFactor / loadFactor;
        int secondsBetweenArrivals = normalSecondsBetweenArrivals * normalLoadFactor / loadFactor;

        string activeDepartureRunway;
        string activeArrivalRunway1;
        string activeArrivalRunway2;
        {
            const auto& departure = m_airport->activeDepartureRunways();
            const auto& arrival = m_airport->activeArrivalRunways();

            activeDepartureRunway = !departure.empty() ? departure.at(0) : "";
            activeArrivalRunway1 = !arrival.empty() ? arrival.at(0) : "";
            activeArrivalRunway2 = !arrival.empty() ? arrival.at(arrival.size() - 1) : "";
        }

        auto chooseRemoteAirport = [this](const string& remoteIcao, const char* roleLabel) {
            const string localIcao = m_airport->header().icao();
            const string fallbackIcao = pickOtherAirportIcao(localIcao);

            const auto tryResolve = [this](const string& icao) -> shared_ptr<Airport> {
                if (icao.empty())
                {
                    return nullptr;
                }

                try
                {
                    return m_world->getAirport(icao);
                }
                catch (const exception&)
                {
                }

                return tryLoadAirportIntoWorld(icao);
            };

            if (!remoteIcao.empty())
            {
                if (auto resolved = tryResolve(remoteIcao))
                {
                    if (resolved->header().icao() != localIcao)
                    {
                        return resolved;
                    }

                    m_host->writeLog(
                        "SCHEDL|Live %s airport [%s] resolves to local airport [%s] - using fallback [%s]",
                        roleLabel,
                        remoteIcao.c_str(),
                        localIcao.c_str(),
                        fallbackIcao.c_str());
                }
                else
                {
                    m_host->writeLog(
                        "SCHEDL|Live %s airport [%s] not found - using fallback [%s]",
                        roleLabel,
                        remoteIcao.c_str(),
                        fallbackIcao.c_str());
                }
            }

            if (auto fallbackAirport = tryResolve(fallbackIcao))
            {
                return fallbackAirport;
            }

            return m_airport;
        };

        const auto resolveModel = [](const string& aircraftIcao) {
            return aircraftIcao.empty() ? string("B738") : aircraftIcao;
        };

        auto addLiveDepartureFlight = [this, &chooseRemoteAirport, &activeDepartureRunway, &resolveModel] (
            const Fr24ScheduleEntry& entry,
            int flightId,
            shared_ptr<ParkingStand> gate,
            time_t departureTime)
        {
            string model = resolveModel(entry.aircraftIcao);
            string airline = entry.airlineIcao;
            string callsign = entry.callsign.empty() ? (airline.empty() ? model : airline + " " + entry.flightNumber) : entry.callsign;
            string destination = !entry.destinationIcao.empty() ? entry.destinationIcao : pickOtherAirportIcao(m_airport->header().icao());
            auto destinationAirport = chooseRemoteAirport(destination, "destination");

            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(departureTime, departureTime + 60 * 60 * 3, m_airport->header().icao(), destinationAirport->header().icao()));
            flightPlan->setDepartureGate(gate->name());
            flightPlan->setArrivalAirportIcao(destinationAirport->header().icao());

            auto departureSelection = selectProcedureAndRunway(
                m_airport,
                m_airport->activeDepartureRunways(),
                "SID",
                destinationAirport->header().icao(),
                false);
            flightPlan->setDepartureRunway(
                departureSelection.runwayName.empty() ? activeDepartureRunway : departureSelection.runwayName);

            auto arrivalSelection = selectProcedureAndRunway(
                destinationAirport,
                destinationAirport->activeArrivalRunways(),
                "STAR",
                m_airport->header().icao(),
                true);
            flightPlan->setArrivalRunway(
                arrivalSelection.runwayName.empty()
                    ? destinationAirport->findLongestRunway()->end1().name()
                    : arrivalSelection.runwayName);

            if (!departureSelection.procedureName.empty())
            {
                flightPlan->setSid(departureSelection.procedureName);
            }

            if (!arrivalSelection.procedureName.empty())
            {
                flightPlan->setStar(arrivalSelection.procedureName);
            }

            flightPlan->setAirlineIcao(airline);
            flightPlan->setFlightNo(entry.flightNumber);
            flightPlan->setCallsign(callsign);

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, entry.flightNumber, callsign, flightPlan));
            auto aircraft = m_host->createAIAircraft(model, airline, entry.flightNumber, world::Aircraft::Category::Jet);
            flight->setAircraft(aircraft);
            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            flight->setPhase(Flight::Phase::TurnAround);
            m_world->addFlightColdAndDark(flight);
        };

        auto addLiveArrivalFlight = [this, &chooseRemoteAirport, &activeArrivalRunway1, &activeArrivalRunway2, &resolveModel] (
            const Fr24ScheduleEntry& entry,
            int flightId,
            shared_ptr<ParkingStand> gate,
            time_t arrivalTime,
            int& arrivalIndex)
        {
            string model = resolveModel(entry.aircraftIcao);
            string airline = entry.airlineIcao;
            string callsign = entry.callsign.empty() ? (airline.empty() ? model : airline + " " + entry.flightNumber) : entry.callsign;
            string origin = !entry.originIcao.empty() ? entry.originIcao : pickOtherAirportIcao(m_airport->header().icao());
            auto originAirport = chooseRemoteAirport(origin, "origin");

            auto arrivalSelection = selectProcedureAndRunway(
                m_airport,
                m_airport->activeArrivalRunways(),
                "STAR",
                originAirport->header().icao(),
                true);
            string arrivalRunway = arrivalSelection.runwayName.empty()
                ? ((((arrivalIndex++) % 2) == 0) ? activeArrivalRunway1 : activeArrivalRunway2)
                : arrivalSelection.runwayName;

            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(arrivalTime - 60 * 60 * 3, arrivalTime, originAirport->header().icao(), m_airport->header().icao()));
            flightPlan->setArrivalGate(gate->name());
            flightPlan->setArrivalRunway(arrivalRunway);
            flightPlan->setDepartureAirportIcao(originAirport->header().icao());

            if (!arrivalSelection.procedureName.empty())
            {
                flightPlan->setStar(arrivalSelection.procedureName);
            }

            auto departureSelection = selectProcedureAndRunway(
                originAirport,
                originAirport->activeDepartureRunways(),
                "SID",
                m_airport->header().icao(),
                false);
            if (!departureSelection.runwayName.empty())
            {
                flightPlan->setDepartureRunway(departureSelection.runwayName);
            }
            if (!departureSelection.procedureName.empty())
            {
                flightPlan->setSid(departureSelection.procedureName);
            }

            flightPlan->setAirlineIcao(airline);
            flightPlan->setFlightNo(entry.flightNumber);
            flightPlan->setCallsign(callsign);

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, entry.flightNumber, callsign, flightPlan));
            auto aircraft = m_host->createAIAircraft(model, airline, entry.flightNumber, world::Aircraft::Category::Jet);
            flight->setAircraft(aircraft);
            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            flight->setPhase(Flight::Phase::Arrival);

            auto copyOfWorld = m_world;
            auto copyOfAirport = m_airport;
            const time_t arrivalStartTime = max(arrivalTime - estimateArrivalLeadSeconds(flight->plan()), m_world->currentTime() + 30);
            m_world->deferUntil(
                "fr24Arrival/" + flight->callSign(),
                arrivalStartTime,
                [flight, copyOfWorld, copyOfAirport, arrivalRunway]() {
                    const auto& landingRunwayEnd = copyOfWorld->getRunwayEnd(copyOfAirport->header().icao(), arrivalRunway);
                    copyOfWorld->addFlight(flight);
                    flight->aircraft()->setOnFinal(landingRunwayEnd);
                });
        };

        const auto resolveTrafficTime = [&](time_t& nextTime, const Fr24ScheduleEntry& entry, int spacingSeconds) -> time_t
        {
            if (entry.scheduledTime > 0)
            {
                const time_t resolvedTime = entry.scheduledTime;
                nextTime = max(nextTime + spacingSeconds, resolvedTime + spacingSeconds);
                return resolvedTime;
            }

            const time_t resolvedTime = nextTime;
            nextTime += spacingSeconds;
            return resolvedTime;
        };

        size_t requestedCount = static_cast<size_t>(traffic.size() * loadFactor);
        if (requestedCount == 0)
        {
            requestedCount = 1;
        }
        requestedCount = min(requestedCount, gates.size());

        time_t nextDepartureTime = m_world->currentTime() + 200;
        time_t nextArrivalTime = m_world->currentTime() + 30;
        int arrivalIndex = 0;

        for (size_t index = 0 ; index < requestedCount ; ++index)
        {
            const auto& trafficItem = traffic.at(index);
            const auto& gate = gates.at(index % gates.size());
            int flightId = 1000 + static_cast<int>(index);

            try
            {
                if (trafficItem.second)
                {
                    const time_t departureTime = resolveTrafficTime(nextDepartureTime, trafficItem.first, secondsBetweenDepartures);
                    addLiveDepartureFlight(trafficItem.first, flightId, gate, departureTime);
                }
                else
                {
                    const time_t arrivalTime = resolveTrafficTime(nextArrivalTime, trafficItem.first, secondsBetweenArrivals);
                    addLiveArrivalFlight(trafficItem.first, flightId, gate, arrivalTime, arrivalIndex);
                }
            }
            catch(const exception& e)
            {
                m_host->writeLog("SCHEDL|FR24 live schedule flight creation failed: %s", e.what());
            }
        }

        m_host->writeLog(
            "SCHEDL|Loaded [%d] live FR24 AI flights at airport[%s]",
            m_world->flights().size(),
            m_airport->header().icao().c_str());

        return true;
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

    void logActiveRunwaysBounds()
    {
        const auto logBounds = [this](shared_ptr<Runway> runway) {
            const auto& bounds = runway->bounds();
            m_host->writeLog(
                "LSCHED|RWY-BOUNDS[%s]: A[%f,%f] B[%f,%f] C[%f,%f] D[%f,%f] minLat[%f] maxLat[%f] minLon[%f] maxLon[%f]",
                runway->name().c_str(),
                bounds.A.latitude, bounds.A.longitude,
                bounds.B.latitude, bounds.B.longitude,
                bounds.C.latitude, bounds.C.longitude,
                bounds.D.latitude, bounds.D.longitude,
                bounds.minLatitude, bounds.maxLatitude,
                bounds.minLongitude, bounds.maxLongitude);
        };

        for (const auto& name : m_airport->activeArrivalRunways())
        {
            logBounds(m_airport->getRunwayOrThrow(name));
        }

        for (const auto& name : m_airport->activeDepartureRunways())
        {
            logBounds(m_airport->getRunwayOrThrow(name));
        }
    }
};
