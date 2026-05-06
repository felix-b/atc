//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <chrono>
#include <sstream>
#include <iomanip>
#include <cctype>
#include <cmath>

#include "libworld.h"
#include "worldHelper.hpp"
#include "basicManeuverTypes.hpp"
#include "maneuverFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "aircraftPerformanceTable.hpp"

#define METERS_IN_NAUTICAL_MILE 1852.0
#define MICROSECONDS_IN_HOUR 3600000000.0
#define MICROSECONDS_IN_MINUTE 60000000.0

// the highest elevation airport - Qamdo Bamda, China
#define WORLD_MAX_RUNWAY_ELEVATION 14219.0

using namespace std;
using namespace world;

namespace ai
{
    class AIAircraft : public world::Aircraft
    {
    private:
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

        static bool tryFindWaypointLocation(const shared_ptr<FlightPlan>& plan, const string& waypointName, GeoPoint& location)
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

        void separateArrivalSpawn(GeoPoint& arrivalStartLocation, float arrivalHeading, float& arrivalAltitudeFeet, float arrivalGroundSpeedKt)
        {
            auto world = host()->getWorld();
            auto flightPtr = flight().lock();
            if (!world || !flightPtr || arrivalStartLocation == GeoPoint::empty)
            {
                return;
            }

            const float minimumSpacingNm = max(3.0f, min(8.0f, m_performanceProfile.approachSpeedKt / 35.0f));
            const float spawnSpeedKt = max(90.0f, arrivalGroundSpeedKt);

            for (int attempt = 0; attempt < 8; ++attempt)
            {
                float requiredShiftNm = 0.0f;

                for (const auto& otherFlight : world->flights())
                {
                    if (!otherFlight || otherFlight == flightPtr || !otherFlight->aircraft())
                    {
                        continue;
                    }

                    auto otherAircraft = otherFlight->aircraft();
                    if (otherAircraft->location() == GeoPoint::empty)
                    {
                        continue;
                    }

                    if (otherAircraft->altitude().isGround() && otherAircraft->groundSpeedKt() < 5.0)
                    {
                        continue;
                    }

                    const float lateralDistanceNm = static_cast<float>(
                        GeoMath::getDistanceMeters(arrivalStartLocation, otherAircraft->location()) / METERS_IN_NAUTICAL_MILE);
                    if (lateralDistanceNm >= minimumSpacingNm)
                    {
                        continue;
                    }

                    requiredShiftNm = max(requiredShiftNm, minimumSpacingNm - lateralDistanceNm + 0.5f);
                }

                if (requiredShiftNm <= 0.0f)
                {
                    break;
                }

                arrivalStartLocation = GeoMath::getPointAtDistance(
                    arrivalStartLocation,
                    GeoMath::flipHeading(arrivalHeading),
                    requiredShiftNm * METERS_IN_NAUTICAL_MILE);

                const float addedMinutes = requiredShiftNm * 60.0f / spawnSpeedKt;
                arrivalAltitudeFeet += addedMinutes * m_performanceProfile.descentRateFpm;
            }
        }

        void configureArrivalStart(const Runway::End& runwayEnd, bool preferApproachOnly, bool assignFinalManeuver)
        {
            const float minutesToThreshold = m_performanceProfile.minutesToThreshold;
            const float descentSpeedFpm = m_performanceProfile.descentRateFpm;
            const float groundSpeedKt = m_performanceProfile.approachSpeedKt;
            const float flareBufferFeet = m_performanceProfile.flareBufferFeet;

            auto flightPtr = flight().lock();
            auto plan = flightPtr ? flightPtr->plan() : nullptr;
            if (!flightPtr || !plan)
            {
                throw runtime_error("AIAircraft::configureArrivalStart failed: flight plan was not available");
            }

            GeoPoint arrivalStartLocation;
            float arrivalHeading = runwayEnd.heading();
            float arrivalGroundSpeedKt = groundSpeedKt;
            float arrivalAltitudeFeet = runwayEnd.elevationFeet() + minutesToThreshold * descentSpeedFpm + flareBufferFeet;
            bool useProcedureStart = false;

            FlightPlan::Cursor arrivalCursor(plan);
            shared_ptr<FlightPlan::Leg> arrivalStartLeg;
            if (preferApproachOnly)
            {
                if (arrivalCursor.activateNextLegOfType(FlightPlan::LegType::Approach) && arrivalCursor.activeLeg())
                {
                    arrivalStartLeg = arrivalCursor.activeLeg();
                }
                else if (arrivalCursor.activateNextLegOfType(FlightPlan::LegType::Star) && arrivalCursor.activeLeg())
                {
                    arrivalStartLeg = arrivalCursor.activeLeg();
                }
            }
            else
            {
                if (arrivalCursor.activateNextLegOfType(FlightPlan::LegType::Star) && arrivalCursor.activeLeg())
                {
                    arrivalStartLeg = arrivalCursor.activeLeg();
                }
                else if (arrivalCursor.activateNextLegOfType(FlightPlan::LegType::Approach) && arrivalCursor.activeLeg())
                {
                    arrivalStartLeg = arrivalCursor.activeLeg();
                }
            }

            if (arrivalStartLeg)
            {
                GeoPoint startPoint;
                GeoPoint nextPoint;
                if (tryFindWaypointLocation(plan, arrivalStartLeg->fromNavaid(), startPoint))
                {
                    arrivalStartLocation = startPoint;
                    useProcedureStart = true;
                    if (arrivalStartLeg->targetAltitude() > 0.0f)
                    {
                        arrivalAltitudeFeet = max(arrivalAltitudeFeet, arrivalStartLeg->targetAltitude());
                    }

                    if (tryFindWaypointLocation(plan, arrivalStartLeg->toNavaid(), nextPoint))
                    {
                        arrivalHeading = GeoMath::getHeadingFromPoints(arrivalStartLocation, nextPoint);
                    }

                    if (arrivalStartLeg->targetSpeed() > 0.0f)
                    {
                        arrivalGroundSpeedKt = min(arrivalGroundSpeedKt, arrivalStartLeg->targetSpeed());
                    }
                }
            }

            GeoPoint finalStartLocation = GeoPoint::empty;
            if (!useProcedureStart)
            {
                float finalDistance = minutesToThreshold * groundSpeedKt / 60;
                auto aimingPoint = runwayEnd.centerlinePoint().geo();
                finalStartLocation = GeoMath::getPointAtDistance(
                    aimingPoint,
                    GeoMath::flipHeading(runwayEnd.heading()),
                    finalDistance * METERS_IN_NAUTICAL_MILE - runwayEnd.displacedThresholdMeters() - 50);
                arrivalStartLocation = finalStartLocation;
            }

            separateArrivalSpawn(arrivalStartLocation, arrivalHeading, arrivalAltitudeFeet, arrivalGroundSpeedKt);
            if (!useProcedureStart)
            {
                finalStartLocation = arrivalStartLocation;
            }

            const GeoPoint& altitudeReferenceLocation = useProcedureStart ? arrivalStartLocation : finalStartLocation;
            if (altitudeReferenceLocation != GeoPoint::empty)
            {
                const float terrainElevationFeet = host()->getWorld()->queryTerrainElevationAt(altitudeReferenceLocation);
                arrivalAltitudeFeet = max(arrivalAltitudeFeet, terrainElevationFeet + MaxAltitudeAGL);
            }

            setAltitude(Altitude::msl(arrivalAltitudeFeet));
            setGroundSpeedKt(arrivalGroundSpeedKt);
            setVerticalSpeedFpm(-descentSpeedFpm);
            setFlapState(0);
            setGearState(0);
            setLights(LightBits::BeaconLandingNavStrobe);
            setAttitude(AircraftAttitude(arrivalHeading, -2.0f, 0));

            flightPtr->setPhase(Flight::Phase::Arrival);

            m_locationTimespamp = host()->getWorld()->timestamp();

            if (useProcedureStart)
            {
                setLocation(arrivalStartLocation);
                host()->writeLog(
                    "AIPILO|flight[%s] starting arrival at procedure waypoint[%s] runway[%s] retry[%d]",
                    flightPtr->callSign().c_str(),
                    arrivalStartLeg->fromNavaid().c_str(),
                    runwayEnd.name().c_str(),
                    preferApproachOnly ? 1 : 0);
            }
            else
            {
                setLocation(finalStartLocation);
            }

            if (assignFinalManeuver)
            {
                setManeuver(flightPtr->pilot()->getFinalToGate(runwayEnd));
            }
        }

        GeoPoint m_location;
        chrono::microseconds m_locationTimespamp;
        chrono::microseconds m_touchdownTimestamp;
        AircraftAttitude m_attitude;
        Altitude m_altitude;
        double m_track;
        double m_targetTrack;
        double m_groundSpeedKt;
        double m_targetGroundSpeedKt;
        double m_verticalSpeedFpm;
        double m_targetVerticalSpeedFpm;
        string m_squawk;
        LightBits m_lights;
        float m_gearState;
        float m_flapState;
        float m_spoilerState;
        AircraftPerformanceProfile m_performanceProfile;
        shared_ptr<Maneuver> m_maneuver;
    public:
        AIAircraft(
            shared_ptr<HostServices> _host,
            int _id,
            const string& _modelIcao,
            const string& _airlineIcao,
            const string& _tailNo,
            Category _category
        ) : Aircraft(
                _host,
                _id,
                Actor::Nature::AI,
                _modelIcao,
                _airlineIcao,
                _tailNo,
                _category
            ),
            m_location(0, 0),
            m_attitude({ 0, 0, 0 }),
            m_track(0),
            m_targetTrack(0),
            m_groundSpeedKt(0),
            m_targetGroundSpeedKt(0),
            m_verticalSpeedFpm(0),
            m_targetVerticalSpeedFpm(0),
            m_gearState(1.0f),
            m_flapState(0),
            m_spoilerState(0),
            m_locationTimespamp(chrono::seconds(-1)),
            m_touchdownTimestamp(chrono::seconds(-1)),
            m_altitude(Altitude::ground()),
            m_lights(LightBits::None)
        {
            m_performanceProfile = AircraftPerformanceTable::lookup(_host, _modelIcao, _category);
        }

        const GeoPoint& location() const override { return m_location; }
        chrono::microseconds locationTiemstamp() const { return m_locationTimespamp; }
        const AircraftAttitude& attitude() const override { return m_attitude; }
        double track() const override { return m_track; }
        const Altitude& altitude() const override { return m_altitude; }
        double groundSpeedKt() const override { return m_groundSpeedKt; }
        double verticalSpeedFpm() const override { return m_verticalSpeedFpm; }
        const string& squawk() const override { return m_squawk; }
        LightBits lights() const override { return m_lights; }
        float gearState() const override { return m_gearState; }
        float flapState() const override { return m_flapState; }
        float spoilerState() const override { return m_spoilerState; }
        const AircraftPerformanceProfile& performanceProfile() const { return m_performanceProfile; }

        bool isLightsOn(LightBits bits) const override
        {
            return ((m_lights & bits) == bits);
        }

        void park(shared_ptr<ParkingStand> parkingStand) override
        {
            m_location = GeoMath::getPointAtDistance(parkingStand->location().geo(), GeoMath::flipHeading(parkingStand->heading()), 13);
            m_attitude = AircraftAttitude({ parkingStand->heading(), 0, 0 });
            m_altitude = Altitude::ground();
            m_groundSpeedKt = 0;
            m_verticalSpeedFpm = 0;
            m_gearState = 1.0f;
            m_flapState = 0.0f;
            m_spoilerState = 0.0f;
            m_lights = LightBits::None;

            if (auto flightPtr = flight().lock())
            {
                flightPtr->setPhase(Flight::Phase::TurnAround);
            }

            setManeuver(flight().lock()->pilot()->getFlightCycle());
        }

        void setOnFinal(const Runway::End& runwayEnd) override
        {
            configureArrivalStart(runwayEnd, false, true);
        }

        void prepareForApproachRetry(const Runway::End& runwayEnd)
        {
            configureArrivalStart(runwayEnd, true, false);
        }

        void progressTo(chrono::microseconds timestamp) override
        {
            if (m_maneuver)
            {
                //m_host->writeLog("Aircraft[%d]: maneuver->progressTo(%lld)", m_id, timestamp.count());
                m_maneuver->progressTo(timestamp);
            }

            bool touchedDown = false;
            int64_t elapsedMicroseconds = (timestamp - m_locationTimespamp).count();

            moveFor(elapsedMicroseconds, touchedDown);

            m_locationTimespamp = timestamp;
            if (touchedDown)
            {
                m_touchdownTimestamp = timestamp;
            }
        }

        void setLocation(const GeoPoint& _location)
        {
            //m_host->writeLog("Aircraft[%d]::setLocation(lat=%.10f,lon=%.10f,alt=%f)", m_id, _location.latitude, _location.longitude, _location.altitude);
            m_location = _location;
            notifyChanges();
        }

        void setAttitude(const AircraftAttitude& _attitude, TrackSyncMode trackSync = TrackSyncMode::SyncToHeading)
        {
            //m_host->writeLog("Aircraft[%d]::setAttitude(hdg=%f)", m_id, _attitude.heading());
            m_attitude = _attitude;

            if (trackSync == TrackSyncMode::SyncToHeading)
            {
                setTrack(m_attitude.heading());
            }

            notifyChanges();
        }

        void setAltitude(const Altitude& _altitude)
        {
            // m_host->writeLog(
            //     "Aircraft[%d]::setAltitude(%f %s)",
            //     m_id,
            //     _altitude.feet(),
            //     _altitude.isGround() ? "GND" : _altitude.type() == Altitude::Type::AGL ? "AGL" : "MSL");

            m_altitude = _altitude;
            notifyChanges();
        }

        void setTrack(double _track)
        {
            m_targetTrack = _track;
            m_track = _track;
        }

        void setGroundSpeedKt(double kt)
        {
            m_targetGroundSpeedKt = clampTargetGroundSpeedKt(kt);
            m_groundSpeedKt = m_targetGroundSpeedKt;
        }

        void setVerticalSpeedFpm(double fpm)
        {
            m_targetVerticalSpeedFpm = clampTargetVerticalSpeedFpm(fpm);
            m_verticalSpeedFpm = m_targetVerticalSpeedFpm;
        }

        void setGearState(float ratio)
        {
            m_gearState = ratio;
            notifyChanges();
        }

        void setSpoilerState(float ratio)
        {
            m_spoilerState = ratio;
            notifyChanges();
        }

        void setFlapState(float ratio)
        {
            m_flapState = ratio;
            notifyChanges();
        }

        void setSquawk(const string& _squawk)
        {
            m_squawk = _squawk;
            notifyChanges();
        }

        void setLights(LightBits _lights)
        {
            m_lights = _lights;
            notifyChanges();
        }

        void setManeuver(shared_ptr<Maneuver> _maneuver)
        {
            m_maneuver = _maneuver;
        }

        string getStatusString() override
        {
            return m_maneuver ? m_maneuver->getStatusString() : "N/A";
        }

        void notifyChanges() override
        {
            getWorldChangeSet()->mutableFlights().updated(flight().lock());
        }

        void moveFor(int64_t elapsedMicroseconds, bool& touchedDown)
        {
            updateDynamicFlightModel(elapsedMicroseconds);

            if (abs(m_groundSpeedKt) > 0.00001)
            {
                double elapsedHours = elapsedMicroseconds / MICROSECONDS_IN_HOUR;
                GeoPoint nextLocation = GeoMath::getPointAtDistance(
                    m_location,
                    m_track,
                    m_groundSpeedKt * elapsedHours * METERS_IN_NAUTICAL_MILE);
                setLocation(nextLocation);
            }

            if (abs(m_verticalSpeedFpm) > 0.00001)
            {
                double elapsedMinutes = elapsedMicroseconds / MICROSECONDS_IN_MINUTE;
                float nextFeet = m_altitude.feet() + m_verticalSpeedFpm * elapsedMinutes;
                Altitude nextAltitude  = getNextAltitude(nextFeet);
                touchedDown = (
                    m_altitude.type() != Altitude::Type::Ground &&
                    nextAltitude.type() == Altitude::Type::Ground);
                setAltitude(nextAltitude);
            }
        }

        tuple<double, double> getGroundSpeedEnvelopeKt() const
        {
            auto flightPtr = flight().lock();
            const float approach = max(70.0f, m_performanceProfile.approachSpeedKt);

            if (m_altitude.type() == Altitude::Type::Ground)
            {
                if (flightPtr)
                {
                    switch (flightPtr->phase())
                    {
                    case Flight::Phase::Departure:
                        return make_tuple(0.0, max(170.0, static_cast<double>(approach) * 1.6));
                    case Flight::Phase::Arrival:
                        return make_tuple(0.0, max(150.0, static_cast<double>(approach) * 1.4));
                    case Flight::Phase::TurnAround:
                    default:
                        return make_tuple(0.0, 40.0);
                    }
                }

                return make_tuple(0.0, 40.0);
            }

            const double minSpeed = max(80.0, static_cast<double>(approach) * 0.72);
            double maxSpeed = max(210.0, static_cast<double>(approach) * 2.7);
            if (flightPtr && flightPtr->phase() == Flight::Phase::Arrival)
            {
                maxSpeed = max(170.0, static_cast<double>(approach) * 1.8);
            }

            return make_tuple(minSpeed, maxSpeed);
        }

        tuple<double, double> getVerticalSpeedEnvelopeFpm() const
        {
            const double climbMax = max(900.0, static_cast<double>(m_performanceProfile.descentRateFpm) * 2.7);
            const double descentMin = -max(700.0, static_cast<double>(m_performanceProfile.descentRateFpm) * 2.3);
            return make_tuple(descentMin, climbMax);
        }

        static double normalizeHeading(double heading)
        {
            double normalized = fmod(heading, 360.0);
            if (normalized < 0.0)
            {
                normalized += 360.0;
            }
            return normalized;
        }

        static double approachValue(double current, double target, double delta)
        {
            if (current < target)
            {
                return min(target, current + delta);
            }
            return max(target, current - delta);
        }

        double clampTargetGroundSpeedKt(double kt) const
        {
            auto envelope = getGroundSpeedEnvelopeKt();
            return min(get<1>(envelope), max(get<0>(envelope), kt));
        }

        double clampTargetVerticalSpeedFpm(double fpm) const
        {
            auto envelope = getVerticalSpeedEnvelopeFpm();
            return min(get<1>(envelope), max(get<0>(envelope), fpm));
        }

        void updateDynamicFlightModel(int64_t elapsedMicroseconds)
        {
            const double elapsedSeconds = max(0.0, elapsedMicroseconds / 1000000.0);
            if (elapsedSeconds <= 0.0)
            {
                return;
            }

            // Speed response constrained by envelope.
            m_targetGroundSpeedKt = clampTargetGroundSpeedKt(m_targetGroundSpeedKt);
            const bool onGround = m_altitude.type() == Altitude::Type::Ground;
            const double accelKtPerSecond = onGround ? 3.0 : 6.0;
            const double decelKtPerSecond = onGround ? 4.5 : 8.0;
            const double maxSpeedDelta = (m_targetGroundSpeedKt >= m_groundSpeedKt ? accelKtPerSecond : decelKtPerSecond) * elapsedSeconds;
            m_groundSpeedKt = approachValue(m_groundSpeedKt, m_targetGroundSpeedKt, maxSpeedDelta);

            // Vertical speed response constrained by envelope.
            m_targetVerticalSpeedFpm = clampTargetVerticalSpeedFpm(m_targetVerticalSpeedFpm);
            const double maxVerticalDelta = 650.0 * elapsedSeconds;
            m_verticalSpeedFpm = approachValue(m_verticalSpeedFpm, m_targetVerticalSpeedFpm, maxVerticalDelta);

            m_targetTrack = normalizeHeading(m_targetTrack);
            m_track = normalizeHeading(m_track);
        }

        Altitude getNextAltitude(float nextFeet)
        {
            switch (m_altitude.type())
            {
            case Altitude::Type::Ground:
            case Altitude::Type::AGL:
                return nextFeet > MaxAltitudeAGL
                    ? Altitude::msl(nextFeet + host()->getWorld()->queryTerrainElevationAt(m_location))
                    : nextFeet > 0
                         ? Altitude::agl(nextFeet)
                         : Altitude::ground();
            case Altitude::Type::MSL:
                auto flightPtr = flight().lock();
                if (flightPtr)
                {
                    const bool arrivalPhase = flightPtr->phase() == Flight::Phase::Arrival;
                    const float landingRunwayElevationFeet = flightPtr->landingRunwayElevationFeet();
                    const float terrainElevationFeet = arrivalPhase
                        ? max(landingRunwayElevationFeet, host()->getWorld()->queryTerrainElevationAt(m_location))
                        : landingRunwayElevationFeet;

                    if (nextFeet <= terrainElevationFeet)
                    {
                        return Altitude::ground();
                    }

                    if (nextFeet <= terrainElevationFeet + MaxAltitudeAGL)
                    {
                        return Altitude::agl(nextFeet - terrainElevationFeet);
                    }

                    return Altitude::msl(nextFeet);
                }
                return Altitude::ground();
            }

            throw runtime_error(
                "Aircraft id=" + to_string(id()) + " invalid altitude type=" + to_string((int)m_altitude.type()));
        }

        bool justTouchedDown(chrono::microseconds timestamp) override
        {
            auto microsecondsSinceTouchdown = (timestamp -  m_touchdownTimestamp);
            bool wasTouchDown = microsecondsSinceTouchdown.count() < 500000; // < 0.5s
            m_touchdownTimestamp = timestamp - chrono::seconds(1);
            return wasTouchDown;
        }
    };
}
