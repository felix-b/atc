// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <chrono>
#include <queue>
#include <vector>
#include <unordered_set>

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

class PluginWorldLoader
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<World> m_world;
public:
    PluginWorldLoader(shared_ptr<HostServices> _host) :
        m_host(_host)
    {
    }
public:
    void loadWorld()
    {
        vector<shared_ptr<Airport>> airports;
        loadAirports(airports);

        m_world = WorldBuilder::assembleSampleWorld(m_host, airports);
        m_host->writeLog("World initialized");

#if 0
        for (const auto& airport : airports)
        {
            try
            {
                //auto runway = airport->findLongestRunway();
//                if (runway->lengthMeters() >= 2000 && !airport->hasParallelRunways() && (
//                    airport->header().icao().at(0) == 'K' ||
//                    airport->header().icao().at(0) == 'Y' ||
//                    airport->header().icao().substr(0, 2) == "EG"))
                {
                    m_host->writeLog("LWORLD|ENUMAPT %s", airport->header().icao().c_str());

                    //                    m_host->writeLog(
//                        "LWORLD|FOUNDAPT [%s] rwys[%d] gates[%d] longest-rwy[%d]m name[%s]",
//                        airport->header().icao().c_str(),
//                        airport->runways().size(),
//                        airport->parkingStands().size(),
//                        (int) runway->lengthMeters(),
//                        runway->name().c_str()
//                    );
                }
            }
            catch(const exception& e)
            {
                m_host->writeLog("LWORLD|FOUNDAPT FAILED on [%s]: %s", airport->header().icao().c_str(), e.what());
            }
        }
#endif

        m_host->writeLog(
            "LWORLD|Assembled world with [%d] airports, [%d] airspaces, [%d] control facilities",
            m_world->airports().size(),
            0, //TODO
            m_world->controlFacilities().size());
    }

    shared_ptr<World> getWorld() const { return m_world; }
//    void stop() override
//    {
//        m_host->writeLog("Stopping POC # 1");
//
//        XPLMUnregisterFlightLoopCallback(&worldFlightLoopCallback, this);
//        m_host->writeLog("Unregistered flight loop callback");
//
//        m_world.reset();
//        m_host->writeLog("World shut down");
//    }
//    void setTimeFactor(uint64_t factor) override
//    {
//        m_timeFactor = factor;
//    }
private:
    void loadAirports(vector<shared_ptr<Airport>>& airports)
    {
        unordered_set<string> loadedIcaos;
        int overrideCount = 0;

        m_host->writeLog("LWORLD|--- begin load airports ---");

        XPSceneryAptDatReader aptDatReader(m_host);
        aptDatReader.readSceneryAirports(
            WorldBuilder::assembleSampleAirportControlZone,
            [&](const Airport::Header header) {
                bool isNewIcao = loadedIcaos.insert(header.icao()).second;
                overrideCount += (isNewIcao ? 0 : 1);
                return isNewIcao;
            },
            [this, &airports](shared_ptr<Airport> airport) {
                airports.push_back(airport);
            }
        );

        m_host->writeLog(
            "LWORLD|--- end load airports: [%d] loaded, [%d] overrides ---",
            loadedIcaos.size(),
            overrideCount);
    }
};
