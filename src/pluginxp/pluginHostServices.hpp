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
public:

    PluginHostServices() :
        //m_startTime(std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now())),
        m_hTerrainProbe(XPLMCreateProbe(xplm_ProbeY))
    {
        m_directorySeparator = XPLMGetDirectorySeparator();
        m_pluginDirectory = getPluginDirectory();
        m_randomGenerator = mt19937(m_randomDevice());
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
        if (!m_hTerrainProbe)
        {
            writeLog("HOSTSV|probeTerrainElevationAt ERROR! XPLMCreateProbe failed. Returning 0 MSL");
            return 0;
        }

        XPLMProbeInfo_t infoProbe = {
            sizeof(XPLMProbeInfo_t),                      // structSIze
            0.0f, 0.0f, 0.0f, // location
            0.0f, 0.0f, 0.0f,   // normal vector
            0.0f, 0.0f, 0.0f,  // velocity vector
            0                                      // is_wet
        };
        double x, y, z;
        XPLMWorldToLocal(location.latitude, location.longitude, 0, &x, &y, &z);

        if (XPLMProbeTerrainXYZ(m_hTerrainProbe, x, 0.0f, z, &infoProbe) == xplm_ProbeHitTerrain)
        {
            double lat, lon, altMeters;
            XPLMLocalToWorld(x, infoProbe.locationY, z, &lat, &lon, &altMeters);
            return altMeters * FEET_IN_1_METER;
        }
        else
        {
            writeLog("HOSTSV|probeTerrainElevationAt ERROR! XPLMProbeTerrainXYZ failed. Returning 0 MSL");
            return 0;
        }
    }

    LocalPoint geoToLocal(const GeoPoint& geo) override
    {
        return LocalPoint({
            (float)geo.longitude,
            (float)geo.altitude,
            (float)geo.latitude
        });
    }

    GeoPoint localToGeo(const LocalPoint& local) override
    {
        return {
            local.z,
            local.x,
            local.y
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

    string combineFilePath(const string& basePath, const vector<string>& relativePathParts) override
    {
        string fullPath = basePath;
        for (const string& part : relativePathParts)
        {
            fullPath.append(m_directorySeparator);
            fullPath.append(part);
        }
        return fullPath;
    }
	
    string pathAppend(const string &rootPath, const vector<string>& relativePathParts)
    {
        string fullPath = rootPath;
        for (const string& part : relativePathParts)
        {
            fullPath.append(m_directorySeparator);
            fullPath.append(part);
        }
        return fullPath;
    }

    string getResourceFilePath(const vector<string>& relativePathParts) override
    {
        return pathAppend(m_pluginDirectory, relativePathParts);
    }

    string getHostFilePath(const vector<string>& relativePathParts) override
    {
        return pathAppend(getHostDirectory(), relativePathParts);
    }

    vector<string> findFilesInHostDirectory(const vector<string>& relativePathParts)
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
                writeLog("HOSTSV|DIR found[%s]", directoryPath.c_str());
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
        auto file = shared_ptr<ifstream>(new ifstream());
        file->exceptions(ifstream::failbit | ifstream::badbit);
        file->open(filePath);
        return file;
    }

    bool checkFileExists(const string& filePath) override
    {
        ifstream f(filePath.c_str());
        return f.good();
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
            "AT&C Plugin - " + title,
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
};
