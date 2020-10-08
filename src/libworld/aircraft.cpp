// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <sstream>
#include <iomanip>
#include "libworld.h"

using namespace std;

#define METERS_IN_NAUTICAL_MILE 1852.0
#define MICROSECONDS_IN_HOUR 3600000000.0
#define MICROSECONDS_IN_MINUTE 60000000.0

// the highest elevation airport - Qamdo Bamda, China
#define WORLD_MAX_RUNWAY_ELEVATION 14219.0

namespace world
{
    constexpr float Aircraft::MaxAltitudeAGL;

    bool Aircraft::isLightsOn(LightBits bits) 
    {
        return ((m_lights & bits) == bits);
    }

    void Aircraft::park(shared_ptr<ParkingStand> parkingStand)
    {
        m_location = GeoMath::getPointAtDistance(parkingStand->location().geo(), GeoMath::flipHeading(parkingStand->heading()), 13);
        m_attitude = AircraftAttitude({ parkingStand->heading(), 0, 0 });
        m_altitude = Altitude::ground();
        m_groundSpeedKt = 0;
    }

    void Aircraft::setOnFinal(const Runway::End& runwayEnd)
    {
        setAltitude(Altitude::msl(runwayEnd.elevationFeet() + 2500 + 40));
        setGroundSpeedKt(145.0f);
        setVerticalSpeedFpm(-1000.0f);
        setFlapState(0);
        setGearState(0);
        setLights(LightBits::BeaconLandingNavStrobe);
        setAttitude(AircraftAttitude(runwayEnd.heading(), -2.0f, 0));

        m_locationTimespamp = m_host->getWorld()->timestamp();

        float finalDistance = 2.5 * 145.0 / 60;
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
    }

    void Aircraft::assignFlight(shared_ptr<Flight> _flight)
    {
        m_flight = _flight;
    }

    void Aircraft::progressTo(chrono::microseconds timestamp)
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
    
    void Aircraft::setLocation(const GeoPoint& _location)
    {
        //m_host->writeLog("Aircraft[%d]::setLocation(lat=%.10f,lon=%.10f,alt=%f)", m_id, _location.latitude, _location.longitude, _location.altitude);
        m_location = _location;
        notifyChanges();
    }

    void Aircraft::setAttitude(const AircraftAttitude& _attitude, TrackSyncMode trackSync)
    {
        //m_host->writeLog("Aircraft[%d]::setAttitude(hdg=%f)", m_id, _attitude.heading());
        m_attitude = _attitude;

        if (trackSync == TrackSyncMode::SyncToHeading)
        {
            setTrack(m_attitude.heading());
        }

        notifyChanges();
    }

    void Aircraft::setAltitude(const Altitude& _altitude)
    {
        // m_host->writeLog(
        //     "Aircraft[%d]::setAltitude(%f %s)", 
        //     m_id,
        //     _altitude.feet(),
        //     _altitude.isGround() ? "GND" : _altitude.type() == Altitude::Type::AGL ? "AGL" : "MSL");

        m_altitude = _altitude;
        notifyChanges();
    }
    
    void Aircraft::setTrack(double _track)
    {
        m_track = _track;
    }

    void Aircraft::setGroundSpeedKt(double kt)
    {
        m_groundSpeedKt = kt;
    }

    void Aircraft::setVerticalSpeedFpm(double fpm)
    {
        m_verticalSpeedFpm = fpm;
    }

    void Aircraft::setGearState(float ratio)
    {
        m_gearState = ratio;
        notifyChanges();
    }

    void Aircraft::setSpoilerState(float ratio)
    {
        m_spoilerState = ratio;
        notifyChanges();
    }

    void Aircraft::setFlapState(float ratio)
    {
        m_flapState = ratio;
        notifyChanges();
    }

    void Aircraft::setSquawk(const string& _squawk)
    {
        m_squawk = _squawk;
        notifyChanges();
    }

    void Aircraft::setFrequencyKhz(int _frequencyKhz)
    {
        m_frequencyKhz = _frequencyKhz;
        
        auto flightPtr = m_flight.lock();
        auto newFrequency = flightPtr
            ? m_host->getWorld()->tryFindCommFrequency(flightPtr, _frequencyKhz)
            : nullptr;

        if (!newFrequency)
        {
            return;
        }

        setFrequency(newFrequency);
    }

    void Aircraft::setFrequency(shared_ptr<Frequency> _frequency)
    {
        if (m_frequency && m_frequencyListenerId >= 0)
        {
            m_frequency->removeListener(m_frequencyListenerId);
            m_frequencyListenerId = -1;
            m_frequencyKhz = -1;
        }

        m_frequency = _frequency;

        if (m_frequency)
        {
            m_frequencyKhz = m_frequency->khz();
            m_frequencyListenerId = m_frequency->addListener([=](shared_ptr<Intent> intent) {
                m_onCommTransmission(intent);
            });
        }

        auto flightPtr = m_flight.lock();
        auto controllerPosition = m_frequency ? m_frequency->controllerPosition() : nullptr;
        m_host->writeLog(
            flightPtr 
                ? "TUNE: aircraft[%d] flight[%s] tuned radio to khz[%d] ATC[%s]"
                : "WARNING: aircraft[%d] flight[%s] tuned radio to khz[%d], but no ATC on that frequency",
            m_id,
            flightPtr ? flightPtr->callSign().c_str() : "N/A", 
            m_frequencyKhz,
            controllerPosition ? controllerPosition->callSign().c_str() : "N/A");
    }

    void Aircraft::setLights(LightBits _lights)
    {
        m_lights = _lights;
        notifyChanges();
    }

    void Aircraft::setManeuver(shared_ptr<Maneuver> _maneuver)
    {
        m_maneuver = _maneuver;
    }

    void Aircraft::notifyChanges()
    {
        m_onChanges()->mutableFlights().updated(m_flight.lock());
    }

    void Aircraft::moveFor(int64_t elapsedMicroseconds, bool& touchedDown)
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

    Altitude Aircraft::getNextAltitude(float nextFeet)
    {
        //TODO: handle landing?

        // if (m_altitude.type() == Altitude::Type::AGL && nextFeet > MaxAltitudeAGL)
        // {
        //     float elevation = m_host->getWorld()->queryTerrainElevationAt(m_location);
        //     stringstream log;
        //     log << setprecision(11)
        //         << "Aircraft[" << m_id << "] AGL->MSL location " 
        //         << m_location.latitude << "," << m_location.longitude
        //         << " AGL=" << nextFeet
        //         << " elevation=" << elevation
        //         << " MSL=" << nextFeet + elevation;
        //     m_host->writeLog(log.str().c_str());
        // }

        switch (m_altitude.type())
        {
        case Altitude::Type::Ground:
        case Altitude::Type::AGL:
            return nextFeet > MaxAltitudeAGL
                ? Altitude::msl(nextFeet + m_host->getWorld()->queryTerrainElevationAt(m_location))
                : nextFeet > 0 
                    ? Altitude::agl(nextFeet) 
                    : Altitude::ground();
        case Altitude::Type::MSL:
            auto flightPtr = m_flight.lock();
            if (flightPtr)
            {
                return nextFeet <= flightPtr->landingRunwayElevationFeet() + MaxAltitudeAGL
                   ? Altitude::agl(nextFeet - m_host->getWorld()->queryTerrainElevationAt(m_location))
                   : Altitude::msl(nextFeet);
            }
            return Altitude::ground();
        }

        throw runtime_error(
            "Aircraft id=" + to_string(m_id) + " invalid altitude type=" + to_string((int)m_altitude.type()));
    }

    bool Aircraft::justTouchedDown(chrono::microseconds timestamp)
    {
        auto microsecondsSinceTouchdown = (timestamp -  m_touchdownTimestamp);
        bool wasTouchDown = microsecondsSinceTouchdown.count() < 500000; // < 0.5s
        m_touchdownTimestamp = timestamp - chrono::seconds(1);
        return wasTouchDown;
    }
}
