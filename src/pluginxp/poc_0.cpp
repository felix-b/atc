// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
// Standard C headers
//#include <cstdio>
//#include <cstdarg>
//#include <cstring>
//#include <cmath>
#include <iostream>
#include <functional>
#include <cmath>

// X-Plane SDK
//#include "XPLMDataAccess.h"
#include "XPLMUtilities.h"
#include "XPLMPlugin.h"
#include "XPLMGraphics.h"
//#include "XPLMMenus.h"

// Include XPMP2 headers
#include "XPCAircraft.h"
#include "XPMPAircraft.h"
#include "XPMPMultiplayer.h"

// PPL 
#include "log.h"
#include "owneddata.h"

// tnc
#include "utils.h"
#include "poc.h"

#define MSG_ADD_DATAREF 0x01000000

using namespace std;
using namespace PPL;
using namespace XPMP2;

const double PI = 3.141592653589793;
const double FEET_PER_M = 3.280839895013123;
const double poc_longitude = 34.869753;
const double poc_lattitude = 32.008744;
const float poc_heading = 298.12;
const float poc_pitch = -1.5;

//OwnedData<bool> g_dataTouchdown("tnc/p1/tdown", PPL::ReadWrite, true);

//OwnedData<double> g_dataLongitude("tnc/p1/pos_lon", PPL::ReadWrite, true);
//OwnedData<double> g_dataLatitude("tnc/p1/pos_lat", PPL::ReadWrite, true);
//OwnedData<double> g_dataAltitude("tnc/p1/pos_ele", PPL::ReadWrite, true);
//OwnedData<float> g_dataVertOffset("tnc0/p1/vertofs", PPL::ReadOnly, true);
//OwnedData<float> g_dataHeading("tnc0/p1/pos_hdg", PPL::ReadWrite, true);
//OwnedData<float> g_dataRoll("tnc/p1/pos_roll", PPL::ReadWrite, true);
//OwnedData<float> g_dataPitch("tnc0/p1/pos_pitch", PPL::ReadWrite, true);
//OwnedData<float> g_dataGearRatio("tnc/p1/gratio", PPL::ReadWrite, true);
//OwnedData<float> g_dataTaxiSpeed("tnc0/p1/taxispd", PPL::ReadWrite, true);

inline char* strScpy(char* dest, const char* src, size_t size)
{
    strncpy(dest, src, size);
    dest[size - 1] = 0;               // this ensures zero-termination!
    return dest;
}

void moveLocalCoords(float& x, float& z, float headingRadians, float distanceMeters)
{
    x += cosf(headingRadians) * distanceMeters;
    z -= sinf(headingRadians) * distanceMeters;
}

float headingToRadians(float heading)
{
    float degrees = 90.0f - heading;
    float radians = degrees * PI / 180.0f;
    PrintDebugString("TNCPOC0> new heading: hdg=[%f] deg=[%f] rad=[%f]", heading, degrees, radians);
    return radians;
}

class TncPoc0DataRefs
{
public:
    //OwnedData<double>& longitude;
    //OwnedData<double>& latitude;
    OwnedData<float> heading;
    //OwnedData<float>& roll;
    //OwnedData<double>& altitude;
    OwnedData<float> pitch;
    //OwnedData<float>& gearRatio;
    OwnedData<float> vertOffset;
    OwnedData<float> taxiSpeed;
public:
    TncPoc0DataRefs() :
        //longitude(g_dataLongitude),
        //latitude(g_dataLatitude),
        heading("tnc0/p1/pos_hdg", PPL::ReadWrite, true),
        //roll(g_dataRoll),
        //altitude(g_dataAltitude),
        pitch("tnc0/p1/pos_pitch", PPL::ReadWrite, true),
        //gearRatio(g_dataGearRatio),
        vertOffset("tnc0/p1/vertofs", PPL::ReadOnly, true),
        taxiSpeed("tnc0/p1/taxispd", PPL::ReadWrite, true)
    {
    }
};

class Poc0Aircraft : public Aircraft
{
private:
    float m_lastHeading;
    float m_headingRadians;
    double m_zeroAltY;
    TncPoc0DataRefs& m_dataRefs;
public:
    /// Constructor just passes on all parameters to library
    Poc0Aircraft(TncPoc0DataRefs& dataRefs, double lon, double lat, double ele, float hdg, float pitch, bool onGround) :
        Aircraft("B738", "ELY", "", 0xABCDEF, ""),
        m_dataRefs(dataRefs)
    {
        // Label
        label = "POC#0-v4";
        colLabel[0] = 0.0f;             // green
        colLabel[1] = 1.0f;
        colLabel[2] = 0.0f;

        // Radar
        acRadar.code = 7654;
        acRadar.mode = xpmpTransponderMode_ModeC;

        // informational texts
        strScpy(acInfoTexts.icaoAcType, "B738", sizeof(acInfoTexts.icaoAcType));
        strScpy(acInfoTexts.icaoAirline, "ELY", sizeof(acInfoTexts.icaoAirline));
        strScpy(acInfoTexts.tailNum, "123456", sizeof(acInfoTexts.tailNum));

        m_lastHeading = hdg;
        m_headingRadians = headingToRadians(hdg);

        SetLocation(lat, lon, onGround ? ele : 0);
        SetHeading(hdg);
        SetPitch(pitch);
        SetRoll(0.0f);
        SetGearRatio(onGround ? 1.0f : 0.0f);
        SetFlapRatio(0.0f);
        SetSlatRatio(0.0f);
        SetTouchDown(false);

        double unusedX, unusedZ;
        XPLMWorldToLocal(lat, lon, 0, &unusedX, &m_zeroAltY, &unusedZ);

        if (onGround)
        {
            ClampToGround();
        }

        double actualLat = 0, actualLon = 0, actualAltitude = 0;
        GetLocation(actualLat, actualLon, actualAltitude);

        //m_dataRefs.longitude = actualLon;
        //m_dataRefs.latitude = actualLat;
        //m_dataRefs.altitude = actualAltitude;// -GetVertOfs() * FEET_PER_M;
        m_dataRefs.heading = hdg;
        m_dataRefs.pitch = pitch;
        //m_dataRefs.roll = 0.0;
        //m_dataRefs.gearRatio = onGround ? 1.0 : 0.0;
        m_dataRefs.vertOffset = GetVertOfs();
    }

    /// Custom implementation for the virtual function providing updates values
    virtual void UpdatePosition(float, int)
    {
        float taxiSpeed = m_dataRefs.taxiSpeed.value();

        // So, here we tell the plane its position, which takes care of vertical offset, too
        //SetLocation(g_dataLatitude.value(), g_dataLongitude.value(), g_dataAltitude.value());

        //// further attitude information
        float newHeading = m_dataRefs.heading.value();
        if (newHeading != m_lastHeading)
        {
            SetHeading(newHeading);
            m_lastHeading = newHeading;
            m_headingRadians = headingToRadians(newHeading);
        }

        SetPitch(m_dataRefs.pitch.value());
        SetRoll(0.0f);// m_dataRefs.roll.value());
        SetGearRatio(1.0f);// m_dataRefs.gearRatio.value());

        if (taxiSpeed > 0.0f)
        {
            moveLocalCoords(drawInfo.x, drawInfo.z, m_headingRadians, taxiSpeed);
            drawInfo.y = m_zeroAltY;
            ClampToGround();
            //double newLat, newLon, newAlt;
            //GetLocation(newLat, newLon, newAlt);
            //SetLocation(newLat, newLon, newAlt - GetVertOfs() * FEET_PER_M);
            //m_dataRefs.latitude = newLat;
            //m_dataRefs.longitude = newLon;
            //m_dataRefs.altitude = newAlt;
        }
        //else
        //{
        //    SetLocation(m_dataRefs.latitude.value(), m_dataRefs.longitude.value(), m_dataRefs.altitude.value() - GetVertOfs() * FEET_PER_M);
        //}

        //SetTouchDown(g_dataTouchdown.value());

        /*
        // Plane configuration info
        // This fills a large array of float values:
        SetGearRatio(1.0);
        SetFlapRatio(0);
        SetSpoilerRatio(0);
        SetSpeedbrakeRatio(0);
        SetSlatRatio(0);
        SetWingSweepRatio(0.0f);
        SetThrustRatio(0.5f);
        SetYokePitchRatio(0.0f);
        SetYokeHeadingRatio(0.0f);
        SetYokeRollRatio(0.0f);

        // lights
        SetLightsTaxi(false);
        SetLightsLanding(false);
        SetLightsBeacon(true);
        SetLightsStrobe(true);
        SetLightsNav(true);

        // tires don't roll in the air
        SetTireDeflection(0.0f);
        SetTireRotAngle(0.0f);
        SetTireRotRpm(0.0f);                    // also sets the rad/s value!

        //// For simplicity, we keep engine and prop rotation identical...probably unrealistic
        //SetEngineRotRpm(PLANE_PROP_RPM);        // also sets the rad/s value!
        //SetPropRotRpm(PLANE_PROP_RPM);          // also sets the rad/s value!

        //// Current position of engine / prop: keeps turning as per engine/prop speed:
        //const float deg = std::fmod(PLANE_PROP_RPM * PLANE_CIRCLE_TIME_MIN * GetTimeFragment() * 360.0f,
        //    360.0f);
        SetEngineRotAngle(0.0);
        SetPropRotAngle(0.0);

        //// no reversers and no moment of touch-down in flight
        SetThrustReversRatio(0.0f);
        SetReversDeployRatio(0.0f);
        SetTouchDown(false);
        */
    }
};


//static bool g_dataRefsPublished = false;
//
//static void publishDataRefs()
//{
//    XPLMDebugString("TNCPOC0> publishDataRefs()");
//
//    XPLMPluginID pluginId = XPLMFindPluginBySignature("xplanesdk.examples.DataRefEditor");
//    if (pluginId == XPLM_NO_PLUGIN_ID)
//    {
//        XPLMDebugString("TNCPOC0> publishDataRefs() FAILED: could not find data ref editor plugin");
//    }
//    
//    XPLMSendMessageToPlugin(pluginId, MSG_ADD_DATAREF, (void*)"tnc/p1/pos_lon");
//    XPLMSendMessageToPlugin(pluginId, MSG_ADD_DATAREF, (void*)"tnc/p1/pos_lat");
//    XPLMSendMessageToPlugin(pluginId, MSG_ADD_DATAREF, (void*)"tnc/p1/pos_hdg");
//    XPLMSendMessageToPlugin(pluginId, MSG_ADD_DATAREF, (void*)"tnc/p1/pos_roll");
//    XPLMSendMessageToPlugin(pluginId, MSG_ADD_DATAREF, (void*)"tnc/p1/pos_ele");
//    XPLMSendMessageToPlugin(pluginId, MSG_ADD_DATAREF, (void*)"tnc/p1/pos_pitch");
//}

class TncPoc0 : public TncPoc
{
private:
    TncPoc0DataRefs m_dataRefs;
    Poc0Aircraft* m_aircraft = nullptr;
public:
    void start() override
    {
        //WH1A
        //longitude=34.869753
        //lattitude=32.008744
        //altitude=36.209288
        //heading=298.12

        Log() << Log::Info << "Starting POC # 0" << Log::endl;
        m_aircraft = new Poc0Aircraft(m_dataRefs, poc_longitude, poc_lattitude, 0, 90, poc_pitch, true);
    }
    void stop() override
    {
        Log() << Log::Info << "Stopping POC # 0" << Log::endl;
        if (m_aircraft)
        {
            delete m_aircraft;
            m_aircraft = nullptr;
        }
    }
    void setTimeFactor(uint64_t factor) override 
    {
    }
};

shared_ptr<TncPoc> createPoc0()
{
    try
    {
        //if (!g_dataRefsPublished)
        //{
        //    publishDataRefs();
        //    g_dataRefsPublished = true;
        //}

        auto result = make_shared<TncPoc0>();
        Log() << Log::Info << "Succesfully initialized POC # 0" << Log::endl;
        return result;
    }
    catch(...)
    {
        Log() << Log::Error << "FAILED to initialize POC # 0" << Log::endl;
        return nullptr;
    }
}
