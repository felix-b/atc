//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <chrono>
#include <sstream>
#include <iomanip>

#include "libworld.h"
#include "worldHelper.hpp"
#include "basicManeuverTypes.hpp"
#include "maneuverFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"

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
        GeoPoint m_location;
        chrono::microseconds m_locationTimespamp;
        chrono::microseconds m_touchdownTimestamp;
        AircraftAttitude m_attitude;
        Altitude m_altitude;
        double m_track;
        double m_groundSpeedKt;
        double m_verticalSpeedFpm;
        string m_squawk;
        LightBits m_lights;
        float m_gearState;
        float m_flapState;
        float m_spoilerState;
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
            m_groundSpeedKt(0),
            m_verticalSpeedFpm(0),
            m_gearState(1.0f),
            m_flapState(0),
            m_spoilerState(0),
            m_locationTimespamp(chrono::seconds(-1)),
            m_touchdownTimestamp(chrono::seconds(-1)),
            m_altitude(Altitude::ground()),
            m_lights(LightBits::None)
        {
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

            setManeuver(flight().lock()->pilot()->getFlightCycle());
        }

        void setOnFinal(const Runway::End& runwayEnd) override
        {
            float minutesToThreshold = 4.0f;
            float descentSpeedFpm = 1000.0f;
            float groundSpeedKt = 145.0f;

            setAltitude(Altitude::msl(
                runwayEnd.elevationFeet() +                // runway elevation
                minutesToThreshold * descentSpeedFpm +          // altitude to lose during descent
                40));                                           // fine tuning for flare
            setGroundSpeedKt(groundSpeedKt);
            setVerticalSpeedFpm(-descentSpeedFpm);
            setFlapState(0);
            setGearState(0);
            setLights(LightBits::BeaconLandingNavStrobe);
            setAttitude(AircraftAttitude(runwayEnd.heading(), -2.0f, 0));

            m_locationTimespamp = host()->getWorld()->timestamp();

            float finalDistance = minutesToThreshold * groundSpeedKt / 60;
            auto aimingPoint = runwayEnd.centerlinePoint().geo();
            auto finalStartPoint = GeoMath::getPointAtDistance(
                aimingPoint,
                GeoMath::flipHeading(runwayEnd.heading()),
                finalDistance * METERS_IN_NAUTICAL_MILE - runwayEnd.displacedThresholdMeters() - 50);
            setLocation(finalStartPoint);

            // stringstream log;
            // log << setprecision(11)
            //     << "Flight[" << m_flight.lock()->callSign() << " ac-id=" << m_id << "]::setOnFinal : "
            //     << "finalStartPoint=" << finalStartPoint.latitude << "," << finalStartPoint.longitude << " ; "
            //     << "aimingPoint=" << aimingPoint.latitude << "," << aimingPoint.longitude << " ; "
            //     << "heading=" << m_attitude.heading() << " ; "
            //     << "track=" << m_track << " ; "
            //     << "groundSpeed=" << m_groundSpeedKt << " ; "
            //     << "verticalSpeed=" << m_verticalSpeedFpm;
            // m_host->writeLog(log.str().c_str());

            setManeuver(flight().lock()->pilot()->getFinalToGate(runwayEnd));
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
            m_track = _track;
        }

        void setGroundSpeedKt(double kt)
        {
            m_groundSpeedKt = kt;
        }

        void setVerticalSpeedFpm(double fpm)
        {
            m_verticalSpeedFpm = fpm;
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
                    return nextFeet <= flightPtr->landingRunwayElevationFeet() + MaxAltitudeAGL
                       ? Altitude::agl(nextFeet - host()->getWorld()->queryTerrainElevationAt(m_location))
                       : Altitude::msl(nextFeet);
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
