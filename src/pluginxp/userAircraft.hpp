//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

// STL
#include <string>
#include <utility>
#include <vector>
#include <algorithm>
#include <functional>

// PPL
#include "owneddata.h"

// AT&C
#include "utils.h"
#include "libworld.h"

using namespace std;
using namespace world;

class UserAircraft : public world::Aircraft
{
private:
    GeoPoint m_location;
    AircraftAttitude m_attitude;
    Altitude m_altitude;
    string m_squawk;
    DataRef<double> m_latitudeDataRef;
    DataRef<double> m_longitudeDataRef;
    DataRef<double> m_elevationDataRef;
    DataRef<float> m_aglDataRef;
    DataRef<float> m_headingDataRef;
    DataRef<float> m_pitchDataRef;
    DataRef<float> m_rollDataRef;
    DataRef<float> m_groundspeedDataRef;
    DataRef<int> m_transponderCodeDataRef;
    DataRef<int> m_com1FrequencyKhz;
public:
    UserAircraft(shared_ptr<HostServices> _host, const string& _modelIcao, const string& _airlineIcao) :
        Aircraft(
            _host,
            1,
            Actor::Nature::Human,
            _modelIcao,
            _airlineIcao,
            "",
            Aircraft::Category::Jet
        ),
        m_location(0, 0),
        m_attitude({ 0, 0, 0 }),
        m_altitude(Altitude::ground()),
        m_latitudeDataRef("sim/flightmodel/position/latitude"),
        m_longitudeDataRef("sim/flightmodel/position/longitude"),
        m_elevationDataRef("sim/flightmodel/position/elevation"),
        m_headingDataRef("sim/flightmodel/position/psi"),
        m_pitchDataRef("sim/flightmodel/position/theta"),
        m_rollDataRef("sim/flightmodel/position/phi"),
        m_aglDataRef("sim/flightmodel/position/y_agl"),
        m_groundspeedDataRef("sim/flightmodel/position/groundspeed"),
        m_transponderCodeDataRef("sim/cockpit/radios/transponder_code"),
        m_com1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadWrite)
    {
        updateFromDataRefs(true);
    }
public:
    void progressTo(chrono::microseconds timestamp) override
    {
        bool shouldLog = ((timestamp.count() % 10000000) == 0);
        updateFromDataRefs(shouldLog);
    }
    const GeoPoint& location() const override
    {
        return m_location;
    }
    const AircraftAttitude& attitude() const override
    {
        return m_attitude;
    }
    double track() const override
    {
        return m_attitude.heading();
    }
    const Altitude& altitude() const override
    {
        return m_altitude;
    }
    double groundSpeedKt() const override
    {
        return m_groundspeedDataRef * KNOT_IN_1_METER_PER_SEC;
    }
    double verticalSpeedFpm() const override
    {
        return 0; //TODO
    }
    const string& squawk() const override
    {
        return m_squawk;
    }
    LightBits lights() const override
    {
        return LightBits::None; //TODO
    }
    float gearState() const override
    {
        return 0; //TODO
    }
    float flapState() const override
    {
        return 0; //TODO
    }
    float spoilerState() const override
    {
        return 0; //TODO
    }
    bool isLightsOn(LightBits bits) const override
    {
        return false; //TODO
    }
    bool justTouchedDown(chrono::microseconds timestamp) override
    {
        return false; //TODO
    }
    void park(shared_ptr<ParkingStand> parkingStand) override
    {
        throw runtime_error("UserAircraft::park not implemented");
    }
    void setOnFinal(const Runway::End& runwayEnd) override
    {
        throw runtime_error("UserAircraft::setOnFinal not implemented");
    }
    void notifyChanges() override
    {
        // nothing
    }
private:
    void updateFromDataRefs(bool shouldLog)
    {
        m_location = GeoPoint(m_latitudeDataRef, m_longitudeDataRef);
        m_attitude = AircraftAttitude(m_headingDataRef, m_pitchDataRef, m_rollDataRef);

        float aglMeters = m_aglDataRef;
        m_altitude = aglMeters < 0.1
             ? Altitude::ground()
             : Altitude::agl(aglMeters * FEET_IN_1_METER);

        m_squawk = to_string(m_transponderCodeDataRef);

        int newCom1FrequencyKhz = m_com1FrequencyKhz;
        if (newCom1FrequencyKhz != frequencyKhz())
        {
            host()->writeLog("UPILOT|User aircraft COM1 frequency change detected [%d]->[%d]", frequencyKhz(), newCom1FrequencyKhz);
            setFrequencyKhz(newCom1FrequencyKhz);
        }

        if (shouldLog)
        {
            logCurrentDataRefs();
        }
    }
    void logCurrentDataRefs()
    {
        host()->writeLog(
            "UPILOT|Aircraft data: lat[%f] lon[%f] alt[%s] hdg[%f] pit[%f] rol[%f] gsp[%f] sqw[%s]",
            m_location.latitude,
            m_location.longitude,
            m_altitude.isGround() ? "GND" : m_altitude.toString().c_str(),
            m_attitude.heading(),
            m_attitude.pitch(),
            m_attitude.roll(),
            groundSpeedKt(),
            m_squawk.c_str());
    }
public:
    static shared_ptr<UserAircraft> create(shared_ptr<HostServices> host)
    {
        DataRef<string> icaoDataRef("sim/aircraft/view/acf_ICAO");
        DataRef<string> liveryPathDataRef("sim/aircraft/view/acf_livery_path");

        string icao = icaoDataRef;
        string liveryPath = liveryPathDataRef;

        host->writeLog("UPILOT|UserAircraft::create icao[%s] liveryPath[%s]", icao.c_str(), liveryPath.c_str());

        return shared_ptr<UserAircraft>(new UserAircraft(host, "B738", "UAL"));
    }
};
