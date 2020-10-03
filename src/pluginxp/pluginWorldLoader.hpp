// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <chrono>
#include <queue>
#include <vector>

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
        // X-Plane 11\Resources\default scenery\default apt dat\Earth nav data\apt.dat
        string globalAptDatFilePath = m_host->getHostFilePath(2, {
            "default scenery", "default apt dat", "Earth nav data", "apt.dat"
        });
        m_host->writeLog("LWORLD|global apt.dat file path [%s]", globalAptDatFilePath.c_str());

        shared_ptr<istream> aptDatFile = m_host->openFileForRead(globalAptDatFilePath);
        XPAptDatReader aptDatReader(m_host);

        m_host->writeLog("LWORLD|--- begin load airports ---");

        aptDatReader.readAptDat(
            *aptDatFile,
            WorldBuilder::assembleSampleAirportControlZone,
            [&](const Airport::Header header) {
                return true;
//                return (
//                    header.icao() == "KJFK" ||
//                    header.icao() == "KMIA" ||
//                    header.icao() == "KORD" ||
//                    header.icao() == "YBBN"
//                );
            },
            [this, &airports](shared_ptr<Airport> airport) {
                airports.push_back(airport);
            }
        );

        m_host->writeLog("LWORLD|--- end load airports ---");
    }
};
