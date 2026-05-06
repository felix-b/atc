// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <iostream>
#include <sstream>
#include <functional>
#include <vector>
#include <utility>

// SDK
#include "XPLMPlugin.h"
#include "XPLMUtilities.h"

// XPMP2
#include "XPCAircraft.h"
#include "XPMPAircraft.h"
#include "XPMPMultiplayer.h"

// PPL 
#include "log.h"
#include "logwriter.h"
#include "menuitem.h"
#include "action.h"
#include "pluginpath.h"

// tnc
#include "utils.h"
#include "libworld.h"
#include "configuration.hpp"

using namespace std;
using namespace PPL;
using namespace XPMP2;
using namespace world;

class Xpmp2AircraftObjectService;

class Xpmp2AircraftObject : public XPMP2::Aircraft
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<Flight> m_flight;
    shared_ptr<PluginConfiguration> m_config;
    World::OnChangesCallback m_onQueryChanges;
    int m_frameCount;
public:
    Xpmp2AircraftObject(
        shared_ptr<HostServices> _host,
        const shared_ptr<Flight>& _flight
    ) : Aircraft(
            _flight->aircraft()->modelIcao(),
            _flight->aircraft()->airlineIcao(),
            "",
            4333 + _flight->id(), // mode-S id
            ""
        ),
        m_host(std::move(_host)),
        m_flight(_flight),
        m_onQueryChanges(World::onChangesUnassigned),
        m_frameCount(0)
    {
        m_config = m_host->services().get<PluginConfiguration>();

        auto source = m_flight->aircraft();
        auto location = source->location();

        // Label
        label = getLabelText(source);
        colLabel[0] = 0.0f;             // green
        colLabel[1] = 1.0f;
        colLabel[2] = 0.0f;

        // Radar
        acRadar.code = 4333 + _flight->id();
        acRadar.mode = xpmpTransponderMode_ModeC;

        // informational texts
        strcpy(acInfoTexts.icaoAcType, source->modelIcao().c_str());
        strcpy(acInfoTexts.icaoAirline, source->airlineIcao().c_str());
        strcpy(acInfoTexts.tailNum, m_flight->callSign().c_str());

        SetLocation(location.latitude, location.longitude, 0.0f);
        SetHeading(source->attitude().heading());
        SetPitch(0.0f);
        SetRoll(0.0f);
        SetGearRatio(1.0f);
        SetFlapRatio(0.0f);
        SetSlatRatio(0.0f);
        SetSpoilerRatio(0);
        SetSpeedbrakeRatio(0);
        SetWingSweepRatio(0.0f);
        ensureEngineRunning(source, false);
        SetYokePitchRatio(0.0f);
        SetYokeHeadingRatio(0.0f);
        SetYokeRollRatio(0.0f);
        SetLightsBeacon(false);
        SetLightsTaxi(false);
        SetLightsStrobe(false);
        SetLightsLanding(false);
        SetLightsNav(false);
        SetTouchDown(false);

        float groundPitch = 0.0f;
        safeClampToGround(groundPitch);
        SetPitch(groundPitch);
    }

    void UpdatePosition(float, int) override
    {
        //m_host->writeLog("Xpmp2AircraftObject::UpdatePosition - enter");
        try
        {
            safeUpdatePosition();
        }
        catch (const exception& e)
        {
            m_host->writeLog("Xpmp2AircraftObject::UpdatePosition CRASHED!!! %s", e.what());
        }
        //m_host->writeLog("Xpmp2AircraftObject::UpdatePosition - exit");
    }

    void onQueryChanges(World::OnChangesCallback callback)
    {
        m_onQueryChanges = callback;
    }

private:

    void safeUpdatePosition()
    {
        m_frameCount++;

        if (label.length() > 0 && !m_config->showAIAircraftLabels)
        {
            label.clear();
        }

        auto changeSet = m_onQueryChanges();
        bool anyUpdates = changeSet && hasKey(changeSet->flights().updated(), m_flight->id());
        bool configChanged = changeSet && changeSet->configurationChanged();
        bool isDebugMode = m_config->showAIAircraftDebugLabels;
        if (!anyUpdates && !configChanged && !isDebugMode)
        {
            return;
        }

        //m_host->writeLog("Flight %s: updating sim aircraft location", m_flight->callSign().c_str());

        auto source = m_flight->aircraft();
        const auto& location = source->location();
        const auto& attitude = source->attitude();
        const auto& altitude = source->altitude();
        const bool touchedDown = source->justTouchedDown(m_host->getWorld()->timestamp());

        float pitchAdjustment = 0.0f;

        SetLocation(location.latitude, location.longitude, altitude.isGroundBased() ? 0.0f : altitude.feet());

        if (altitude.isGroundBased())
        {
            safeClampToGround(pitchAdjustment);

            if (altitude.type() == Altitude::Type::AGL)
            {
                drawInfo.y += altitude.feet() / FEET_IN_1_METER;
            }
        }

        label = getLabelText(source);

        SetHeading(attitude.heading());
        SetPitch(attitude.pitch() + pitchAdjustment);
        SetRoll(attitude.roll());

        ensureEngineRunning(source, touchedDown);

        SetLightsBeacon(source->isLightsOn(world::Aircraft::LightBits::Beacon));
        SetLightsTaxi(source->isLightsOn(world::Aircraft::LightBits::Taxi)); //TODO: taxi lights not working?
        SetLightsStrobe(source->isLightsOn(world::Aircraft::LightBits::Strobe));
        SetLightsLanding(
            source->isLightsOn(world::Aircraft::LightBits::Taxi) ||
            source->isLightsOn(world::Aircraft::LightBits::Landing));
        SetLightsNav(source->isLightsOn(world::Aircraft::LightBits::Nav));

        SetGearRatio(source->gearState());
        SetFlapRatio(source->flapState());
        SetSlatRatio(source->flapState());
        SetSpeedbrakeRatio(source->spoilerState());
        SetSpoilerRatio(source->spoilerState());

        SetTouchDown(touchedDown);
    }

    void safeClampToGround(float& groundPitch)
    {
        ClampToGround();

        //TODO: use actual model matched by XPMP2
        groundPitch = -1.5;
        // const string& model = m_flight->aircraft()->modelIcao();

        // if (model.compare("B738") == 0)
        // {
        //     SetPitch(-1.5);
        // }
        // else if (model.compare("A320") == 0)
        // {
        //     drawInfo.y += 0.4;
        //     SetPitch(-0.5);
        // }
    }

    static float clampRatio(float value)
    {
        return max(0.0f, min(1.0f, value));
    }

    bool isParked(const shared_ptr<world::Aircraft>& source) const
    {
        if (!source)
        {
            return false;
        }

        try
        {
            return source->getFlightOrThrow()->phase() == Flight::Phase::TurnAround;
        }
        catch (const exception&)
        {
            return false;
        }
    }

    float calculateReverseRatio(const shared_ptr<world::Aircraft>& source, bool touchedDown) const
    {
        if (!source || isParked(source) || !source->altitude().isGroundBased())
        {
            return 0.0f;
        }

        if (touchedDown)
        {
            return 1.0f;
        }

        const float groundSpeedKt = static_cast<float>(abs(source->groundSpeedKt()));
        const float spoilerState = source->spoilerState();

        if (spoilerState > 0.2f && groundSpeedKt > 20.0f)
        {
            return clampRatio((groundSpeedKt - 20.0f) / 80.0f);
        }

        return 0.0f;
    }

    float calculateThrustRatio(const shared_ptr<world::Aircraft>& source, float reverseRatio) const
    {
        if (!source || isParked(source))
        {
            return 0.0f;
        }

        const bool isGroundBased = source->altitude().isGroundBased();
        const float groundSpeedKt = static_cast<float>(abs(source->groundSpeedKt()));
        const float verticalSpeedFpm = static_cast<float>(source->verticalSpeedFpm());

        float thrustRatio = 0.18f;
        if (isGroundBased)
        {
            if (groundSpeedKt > 140.0f)
            {
                thrustRatio = 0.92f;
            }
            else if (groundSpeedKt > 60.0f)
            {
                thrustRatio = 0.68f;
            }
            else if (groundSpeedKt > 15.0f)
            {
                thrustRatio = 0.34f;
            }
        }
        else
        {
            if (verticalSpeedFpm > 1500.0f || groundSpeedKt > 220.0f)
            {
                thrustRatio = 0.95f;
            }
            else if (verticalSpeedFpm > 300.0f)
            {
                thrustRatio = 0.76f;
            }
            else if (verticalSpeedFpm < -1000.0f)
            {
                thrustRatio = 0.44f;
            }
            else if (groundSpeedKt > 180.0f)
            {
                thrustRatio = 0.62f;
            }
            else
            {
                thrustRatio = 0.52f;
            }
        }

        if (reverseRatio > 0.0f)
        {
            thrustRatio *= (1.0f - (0.5f * reverseRatio));
        }

        return clampRatio(thrustRatio);
    }

    float calculateEngineRpm(const shared_ptr<world::Aircraft>& source, float thrustRatio, float reverseRatio) const
    {
        if (!source || isParked(source))
        {
            return 0.0f;
        }

        const bool isGroundBased = source->altitude().isGroundBased();
        float engineRpm = isGroundBased
            ? 800.0f + thrustRatio * 1700.0f
            : 1000.0f + thrustRatio * 2200.0f;

        if (reverseRatio > 0.0f)
        {
            engineRpm = max(engineRpm, 1200.0f + reverseRatio * 700.0f);
        }

        return engineRpm;
    }

    void ensureEngineRunning(const shared_ptr<world::Aircraft>& source, bool touchedDown)
    {
        const float reverseRatio = calculateReverseRatio(source, touchedDown);
        const float thrustRatio = calculateThrustRatio(source, reverseRatio);
        const float engineRpm = calculateEngineRpm(source, thrustRatio, reverseRatio);

        SetThrustRatio(thrustRatio);
        SetThrustReversRatio(reverseRatio);
        SetReversDeployRatio(reverseRatio);
        SetEngineRotRpm(engineRpm);
    }

private:

    string getLabelText(const shared_ptr<world::Aircraft>& aircraft)
    {
        if (!m_config->showAIAircraftLabels)
        {
            return "";
        }

        stringstream text;
        text << m_flight->callSign() << " ";

        switch (m_flight->phase())
        {
        case Flight::Phase::Arrival:
            text << "(A)";
            break;
        case Flight::Phase::Departure:
            text << "(D)";
            break;
        case Flight::Phase::TurnAround:
            text << "(t/a)";
            break;
        }

        string altitudeString = aircraft->altitude().toString();
        if (!altitudeString.empty())
        {
            text << " | " << altitudeString;
        }

        if (m_config->showAIAircraftDebugLabels)
        {
            text << " | " << aircraft->frequencyKhz()
                 << " | " << acRadar.code
                 << (IsCurrentlyShownAsAI() ? 'A' : 'a')
                 << (IsCurrentlyShownAsTcasTarget() ? 'T' : 't')
                 << '/' << tcasTargetIdx;

            string debugString = aircraft->getStatusString();
            if (!debugString.empty())
            {
                text << " | " << debugString;
            }
        }

        return text.str();
    }
};

class Xpmp2AircraftObjectService : public AircraftObjectService
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<World::ChangeSet> m_lastChangeSet;
    vector<shared_ptr<Xpmp2AircraftObject>> m_simAircraft; //TODO: replace vector with linked list
public:
    explicit Xpmp2AircraftObjectService(shared_ptr<HostServices> _host) :
        m_host(std::move(_host))
    {
        m_host->writeLog("MP2SVC|Xpmp2AircraftObjectService::Xpmp2AircraftObjectService()");

        string resourceDirectory = m_host->getResourceFilePath({ "Resources" });
        const char* error = XPMPMultiplayerInit(
            "AT&C",               // plugin name,
            resourceDirectory.c_str(),         // path to supplemental files
            CBIntPrefsFunc,                    // configuration callback function
            "B738");              // default ICAO type

        if (error[0])
        {
            m_host->writeLog("MP2SVC|XPMPMultiplayerInit: FAILED!");
            return;
        }

        m_host->writeLog("MP2SVC|XPMPMultiplayerInit: success.");

        // Load our CSL models
        error = XPMPLoadCSLPackage(resourceDirectory.c_str());     // CSL folder root path
        if (error[0])
        {
            m_host->writeLog("MP2SVC|XPMPLoadCSLPackage: FAILED!");
            return;
        }

        m_host->writeLog("MP2SVC|XPMPLoadCSLPackage: success.");

        // Now we also try to get control of AI planes. That's optional, though,
        // other plugins (like LiveTraffic, XSquawkBox, X-IvAp...)
        // could have control already
        error = XPMPMultiplayerEnable(CPRequestAIAgain);
        if (error[0]) {
            m_host->writeLog("MP2SVC|XPMPMultiplayerEnable FAILED! %s", error);
            return;
        }

        m_host->writeLog("MP2SVC|XPMPMultiplayerEnable: SUCCESS");
    }

    ~Xpmp2AircraftObjectService()
    {
        m_host->writeLog("MP2SVC|Xpmp2AircraftObjectService::Xpmp2AircraftObjectService()");

        m_host->writeLog("MP2SVC|invoking XPMPMultiplayerDisable");
        XPMPMultiplayerDisable();

        m_host->writeLog("MP2SVC|invoking XPMPMultiplayerCleanup");
        XPMPMultiplayerCleanup();
    }

public:

    void processEvents(shared_ptr<World::ChangeSet> changeSet) override
    {
        m_lastChangeSet = changeSet;

        for (const auto& addedFlight : m_lastChangeSet->flights().added())
        {
            if (addedFlight->aircraft()->nature() != world::Actor::Nature::AI)
            {
                continue;
            }

            auto newSimAircraft = shared_ptr<Xpmp2AircraftObject>(new Xpmp2AircraftObject(m_host, addedFlight));
            newSimAircraft->onQueryChanges([this, addedFlight](){
                //m_host->writeLog("onQueryChanges from %s", addedFlight->callSign().c_str());
                return m_lastChangeSet;
            });

            m_simAircraft.push_back(newSimAircraft);
        }
    }

    void clearAll() override
    {
        m_simAircraft.clear();
    }

private:

    /// This is a callback the XPMP2 calls regularly to learn about configuration settings.
    /// Only 3 are left, all of them integers.
    static int CBIntPrefsFunc(const char*, [[maybe_unused]] const char* item, int defaultVal)
    {
        //if (!strcmp(item, "model_matching")) return 1;
        //if (!strcmp(item, "log_level")) return 0;       // DEBUG logging level
        return defaultVal;
    }

    static void CPRequestAIAgain(void*)
    {
        PrintDebugString("MP2SVC|CPRequestAIAgain: invoking XPMPMultiplayerEnable");
        XPMPMultiplayerEnable(CPRequestAIAgain);
    }
};
