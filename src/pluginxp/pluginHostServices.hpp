//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#define _USE_MATH_DEFINES

#include <cstdarg>
#include <cstring>
#include <fstream>
#include <string>
#include <chrono>
#include <random>
#include <iomanip>
#include <array>
#include <cmath>

// SDK
#include "XPLMUtilities.h"
#include "XPLMPlugin.h"
#include "XPLMGraphics.h"
#include "XPLMScenery.h"

// PPL
#include "messagewindow.h"

// tnc
#include "utils.h"
#include "libworld.h"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"

using namespace std;
using namespace PPL;
using namespace world;

class PluginHostServices : public HostServices
{
private:
    //chrono::time_point<chrono::high_resolution_clock, chrono::milliseconds> m_startTime;
    string m_directorySeparator;
    string m_pluginDirectory;
    random_device m_randomDevice;
    mt19937 m_randomGenerator;
    XPLMProbeRef m_hTerrainProbe;
    shared_ptr<World> m_world;
    shared_ptr<MessageWindow> m_messageBox;
    chrono::milliseconds m_lastTerrainProbeFailureLog = chrono::milliseconds(0);
    int m_terrainProbeFailureCount = 0;
    float m_lastTerrainElevationFeet = 0.0f;
    bool m_hasLastTerrainElevation = false;
public:

    PluginHostServices() :
        //m_startTime(std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now())),
        m_hTerrainProbe(XPLMCreateProbe(xplm_ProbeY))
    {
        m_directorySeparator = XPLMGetDirectorySeparator();
        m_pluginDirectory = getPluginDirectory();
        m_randomGenerator = mt19937(m_randomDevice());
    }

    ~PluginHostServices()
    {
        if (m_hTerrainProbe)
        {
            XPLMDestroyProbe(m_hTerrainProbe);
            m_hTerrainProbe = nullptr;
        }
    }

public:

    shared_ptr<World> getWorld() override
    {
        if (m_world)
        {
            return m_world;
        }
        throw runtime_error("PluginHostServices::getWorld() failed: world was not injected");
    }

    int getNextRandom(int maxValue) override
    {
        uniform_int_distribution<> distribution(0, maxValue - 1);
        return distribution(m_randomGenerator);
    }

    float queryTerrainElevationAt(const GeoPoint& location) override
    {
        if (!ensureTerrainProbe())
        {
            logTerrainProbeFailureThrottled(
                "XPLMCreateProbe failed. Returning fallback terrain elevation [%.1f] MSL",
                getTerrainFallbackElevationFeet(location));
            return getTerrainFallbackElevationFeet(location);
        }

        constexpr float metersIn5000Feet = 1524.0f;
        constexpr float metersIn1000Feet = 304.8f;

        double x = 0.0;
        double yAtMsl0 = 0.0;
        double z = 0.0;
        XPLMWorldToLocal(location.latitude, location.longitude, 0.0, &x, &yAtMsl0, &z);

        const std::array<float, 4> ySamples = {
            static_cast<float>(yAtMsl0),
            static_cast<float>(yAtMsl0 + metersIn5000Feet),
            static_cast<float>(yAtMsl0 + metersIn1000Feet),
            static_cast<float>(yAtMsl0 - metersIn1000Feet)
        };

        for (const float ySample : ySamples)
        {
            XPLMProbeInfo_t infoProbe = {
                sizeof(XPLMProbeInfo_t),
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0
            };

            const auto probeResult = XPLMProbeTerrainXYZ(m_hTerrainProbe, static_cast<float>(x), ySample, static_cast<float>(z), &infoProbe);
            if (probeResult == xplm_ProbeHitTerrain)
            {
                double lat = 0.0;
                double lon = 0.0;
                double altMeters = 0.0;
                XPLMLocalToWorld(x, infoProbe.locationY, z, &lat, &lon, &altMeters);
                const float terrainFeet = static_cast<float>(altMeters * FEET_IN_1_METER);
                m_lastTerrainElevationFeet = terrainFeet;
                m_hasLastTerrainElevation = true;
                return terrainFeet;
            }

            if (probeResult == xplm_ProbeError)
            {
                // Probe could become stale across scenery changes; recreate and continue trying.
                if (m_hTerrainProbe)
                {
                    XPLMDestroyProbe(m_hTerrainProbe);
                    m_hTerrainProbe = nullptr;
                }
                ensureTerrainProbe();
            }
        }

        const float fallbackFeet = getTerrainFallbackElevationFeet(location);
        logTerrainProbeFailureThrottled(
            "XPLMProbeTerrainXYZ failed near [%.6f, %.6f]. Returning fallback terrain elevation [%.1f] MSL",
            location.latitude,
            location.longitude,
            fallbackFeet);
        return fallbackFeet;
    }

    LocalPoint geoToLocal(const GeoPoint& geo) override
    {
        double x = 0.0;
        double y = 0.0;
        double z = 0.0;
        const double altitudeMeters = geo.altitude / FEET_IN_1_METER;
        XPLMWorldToLocal(geo.latitude, geo.longitude, altitudeMeters, &x, &y, &z);
        return LocalPoint({
            static_cast<float>(x),
            static_cast<float>(y),
            static_cast<float>(z)
        });
    }

    GeoPoint localToGeo(const LocalPoint& local) override
    {
        double latitude = 0.0;
        double longitude = 0.0;
        double altitudeMeters = 0.0;
        XPLMLocalToWorld(local.x, local.y, local.z, &latitude, &longitude, &altitudeMeters);
        return {
            latitude,
            longitude,
            altitudeMeters * FEET_IN_1_METER
        };
    }

    shared_ptr<Controller> createAIController(shared_ptr<ControllerPosition> position) override
    {
        return services().get<AIControllerFactory>()->createController(position);
    }

    shared_ptr<Pilot> createAIPilot(shared_ptr<Flight> flight) override
    {
        return services().get<AIPilotFactory>()->createPilot(flight);
    }

    shared_ptr<Aircraft> createAIAircraft(
        const string& modelIcao,
        const string& operatorIcao,
        const string& tailNo,
        Aircraft::Category category) override
    {
        return services().get<AIAircraftFactory>()->createAircraft(modelIcao, operatorIcao, tailNo, category);
    }

    string getResourceFilePath(const vector<string>& relativePathParts) override
    {
        string fullPath = m_pluginDirectory;
        for (const string& part : relativePathParts)
        {
            fullPath.append(m_directorySeparator);
            fullPath.append(part);
        }
        return fullPath;
    }

    string getHostFilePath(const vector<string>& relativePathParts) override
    {
        string resultPath = getHostDirectory();

        for (const string& part : relativePathParts)
        {
            resultPath.append(m_directorySeparator);
            resultPath.append(part);
        }

        return resultPath;
    }

    vector<string> findFilesInHostDirectory(const vector<string>& relativePathParts) override
    {
        const int bufferSize = 2048;
        char buffer[bufferSize] = { 0 };
        vector<string> results;
        string directoryPath = getHostFilePath(relativePathParts);
        int returnedFileCount;
        string nextFileName;

        writeLog("HOSTSV|DIR listing files in folder[%s]", directoryPath.c_str());

        XPLMGetDirectoryContents(directoryPath.c_str(), 0, buffer, bufferSize - 1, nullptr, 0, nullptr, &returnedFileCount);
        for (int i = 0, fileIndex = 0 ; i < bufferSize && fileIndex < returnedFileCount ; i++)
        {
            char c = buffer[i];
            if (c != 0)
            {
                nextFileName += c;
            }
            else if (!nextFileName.empty())
            {
                results.push_back(nextFileName);
                writeLog("HOSTSV|DIR found[%s]", nextFileName.c_str());
                nextFileName.clear();
                fileIndex++;
            }
            else
            {
                break;
            }
        }

        return results;
    }

    shared_ptr<istream> openFileForRead(const string& filePath) override
    {
        auto file = shared_ptr<ifstream>(new ifstream(filePath));
        if (!file->is_open())
        {
            throw runtime_error("PluginHostServices::openFileForRead failed: " + filePath);
        }
        return file;
    }

    void showMessageBox(const string& title, const char *format, ...) override
    {
        const size_t bufferSize = 1024;
        char buffer[bufferSize];
        va_list argptr;
        va_start(argptr, format);
        int messageLength = vsnprintf(buffer, bufferSize, format, argptr);
        va_end(argptr);

        if (messageLength < 0 || messageLength >= bufferSize)
        {
            strncpy(buffer, "WARNING: log message skipped, buffer overrun!", bufferSize);
        }

        m_messageBox = shared_ptr<MessageWindow>(new MessageWindow(
            400,
            200,
            "AT&C - " + title,
            buffer,
            false
        ));
    }

    void writeLog(const char* format, ...) override
    {
        //TODO: remove
//        if (!strstr(format, "AIPILO|") && !strstr(format, "AICONT|") && !strstr(format, "120500|"))
//        {
//            return;
//        }

        const size_t bufferSize = 1024;
        char buffer[bufferSize];
        va_list argptr;
        va_start(argptr, format);
        int messageLength = vsnprintf(buffer, bufferSize, format, argptr);
        va_end(argptr);

        if (messageLength < 0 || messageLength >= bufferSize)
        {
            strncpy(buffer, "WARNING: log message skipped, buffer overrun!", bufferSize);
        }

        auto now = std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
        auto elapsedMilliseconds = now - getLogStartTime();
        stringstream s;
        s << "AT&C [+" << setw(10) << elapsedMilliseconds.count() << "] " << buffer << endl;

        XPLMDebugString(s.str().c_str());
    }

public:

    void useWorld(shared_ptr<World> _world)
    {
        m_world = _world;
        _world->onQueryTerrainElevation([this](const GeoPoint& location){
            return queryTerrainElevationAt(location);
        });
    }

private:

    bool ensureTerrainProbe()
    {
        if (!m_hTerrainProbe)
        {
            m_hTerrainProbe = XPLMCreateProbe(xplm_ProbeY);
        }
        return m_hTerrainProbe != nullptr;
    }

    float getTerrainFallbackElevationFeet(const GeoPoint& location)
    {
        if (m_hasLastTerrainElevation)
        {
            return m_lastTerrainElevationFeet;
        }

        if (std::isfinite(location.altitude))
        {
            return static_cast<float>(location.altitude);
        }

        return 0.0f;
    }

    template <typename... Args>
    void logTerrainProbeFailureThrottled(const char* format, Args... args)
    {
        ++m_terrainProbeFailureCount;
        const auto now = HostServices::getLogTimestamp();
        if (now - m_lastTerrainProbeFailureLog >= chrono::seconds(10))
        {
            writeLog(
                "HOSTSV|probeTerrainElevationAt WARNING! failures[%d] %s",
                m_terrainProbeFailureCount,
                "(details follow)");
            writeLog(format, args...);
            m_lastTerrainProbeFailureLog = now;
            m_terrainProbeFailureCount = 0;
        }
    }
};
