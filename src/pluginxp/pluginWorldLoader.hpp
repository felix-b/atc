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
#include <stdexcept>

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

private:
    static string buildHostPath(shared_ptr<HostServices> host, const vector<string>& relativePathParts)
    {
        return host->getHostFilePath(relativePathParts);
    }

    void appendAptDatCandidatesForSceneryRoot(const string& sceneryRoot, vector<string>& candidatePaths)
    {
        try
        {
            for (const auto& sceneryPack : m_host->findFilesInHostDirectory({ sceneryRoot }))
            {
                if (sceneryPack.empty() || sceneryPack == "." || sceneryPack == ".." || sceneryPack == "scenery_packs.ini")
                {
                    continue;
                }

                candidatePaths.push_back(buildHostPath(m_host, { sceneryRoot, sceneryPack, "Earth nav data", "apt.dat" }));
            }
        }
        catch(const exception& e)
        {
            m_host->writeLog("LWORLD|failed to enumerate %s packages: %s", sceneryRoot.c_str(), e.what());
        }

        // X-Plane 11/12 installs can place scenery packages under either root.
        // Enumerating both keeps the loader resilient to package naming changes.
    }

    void appendAptDatCandidates(vector<string>& candidatePaths)
    {
        appendAptDatCandidatesForSceneryRoot("Custom Scenery", candidatePaths);
        appendAptDatCandidatesForSceneryRoot("Global Scenery", candidatePaths);

        candidatePaths.push_back(buildHostPath(m_host, { "Resources", "default scenery", "default apt dat", "Earth nav data", "apt.dat" }));
    }

    void loadAptDatFile(const string& aptDatFilePath, vector<shared_ptr<Airport>>& airports)
    {
        m_host->writeLog("LWORLD|trying apt.dat file path [%s]", aptDatFilePath.c_str());

        try
        {
            auto aptDatFile = m_host->openFileForRead(aptDatFilePath);
            XPAptDatReader aptDatReader(m_host);

            const size_t airportsBefore = airports.size();
            aptDatReader.readAptDat(
                *aptDatFile,
                WorldBuilder::assembleSampleAirportControlZone,
                [](const Airport::Header&) {
                    return true;
                },
                [&](shared_ptr<Airport> airport) {
                    airports.push_back(airport);
                }
            );

            m_host->writeLog(
                "LWORLD|loaded apt.dat [%s] airports added[%d]",
                aptDatFilePath.c_str(),
                static_cast<int>(airports.size() - airportsBefore));
        }
        catch(const exception& e)
        {
            m_host->writeLog("LWORLD|skipping apt.dat [%s]: %s", aptDatFilePath.c_str(), e.what());
        }
    }

    void loadAirports(vector<shared_ptr<Airport>>& airports)
    {
        m_host->writeLog("LWORLD|--- begin load airports ---");

        vector<string> aptDatPathCandidates;
        appendAptDatCandidates(aptDatPathCandidates);

        unordered_set<string> seenPaths;
        for (const auto& candidatePath : aptDatPathCandidates)
        {
            if (!seenPaths.insert(candidatePath).second)
            {
                continue;
            }

            loadAptDatFile(candidatePath, airports);
        }

        if (airports.empty())
        {
            throw runtime_error("PluginWorldLoader::loadAirports could not locate any readable X-Plane apt.dat file");
        }

        m_host->writeLog("LWORLD|--- end load airports ---");
    }

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
    }

    shared_ptr<World> getWorld() const { return m_world; }

    shared_ptr<World> world() const { return m_world; }
};
