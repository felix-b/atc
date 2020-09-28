// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <iostream>
#include <functional>
#include <string>
#include <cstring>

// SDK
#include "XPLMPlugin.h"
#include "XPLMNavigation.h"

#if !XPLM300
#error This plugin requires version 300 of the SDK
#endif

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
#include "owneddata.h"

// tnc
#include "utils.h"
#include "poc.h"
#include "libworld.h"
#include "pluginInstance.hpp"

using namespace std;
using namespace PPL;

static PluginInstance* pInstance = nullptr;

PLUGIN_API int XPluginStart(char* outName, char* outSig, char* outDesc)
{
    strcpy(outName, "Air Traffic & Control");
    strcpy(outSig, "felix-b.atc");
    strcpy(outDesc, "Offline virtual world of air traffic and ATC");
    XPLMEnableFeature("XPLM_USE_NATIVE_PATHS", 1);

    PluginPath::setPluginDirectoryName("airTrafficAndControl");
    LogWriter::getLogger().setLogFile(PluginPath::prependPluginPath("atnc_log.txt"));

    Log() << Log::Info << "XPluginStart" << Log::endl;

    PrintDebugString(
        "TNC> XPluginStart, platform-build=[%s], plugin-directory=[%s]\n",
        #if APL
            "APL"
        #endif
        #if IBM
            "IBM"
        #endif
        #if LIN
            "LIN"
        #endif
        , 
        getPluginDirectory().c_str()
    );

    return 1;
}

PLUGIN_API int XPluginEnable(void)
{
    char name[256];
    char filePath[256];
    char signature[256];
    char description[256];

    int myPluginId =  XPLMGetMyID();
    XPLMGetPluginInfo(myPluginId, name, filePath, signature, description);

    XPLMDebugString("TNC> XPluginEnable\n");
    PrintDebugString(
        "TNC> XPLMGetPluginInfo(pluginId=%d) -> name=[%s] filePath=[%s] signature=[%s] description=[%s]\n",
        myPluginId, name, filePath, signature, description);

    Log() << Log::Info << "XPluginEnable" << Log::endl;
    pInstance = new PluginInstance();
    return 1;
}

PLUGIN_API void XPluginReceiveMessage(XPLMPluginID fromId, long inMsg, void*)
{
    char name[256];
    char filePath[256];
    char signature[256];
    char description[256];
    XPLMGetPluginInfo(fromId, name, filePath, signature, description);

    PrintDebugString("TNC> XPluginReceiveMessage(fromId=[%d|%s], inMsg=[%ld])\n", fromId, name, inMsg);

    DataRef<double> userAircraftLatitude("sim/flightmodel/position/latitude", PPL::ReadOnly);
    DataRef<double> userAircraftLongitude("sim/flightmodel/position/longitude", PPL::ReadOnly);
    float userLat = userAircraftLatitude;
    float userLon = userAircraftLongitude;
    char icaoCode[10] = { 0 };
    XPLMNavRef navRef = XPLMFindNavAid( nullptr, nullptr, &userLat, &userLon, nullptr, xplm_Nav_Airport);
    if (navRef != XPLM_NAV_NOT_FOUND)
    {
        XPLMGetNavAidInfo(navRef, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, icaoCode, nullptr, nullptr);
    }

    PrintDebugString("     > user@(%f,%f) -> ICAO[%s]\n", userLat, userLon, strlen(icaoCode) > 0 ? icaoCode : "N/A");
}

PLUGIN_API void XPluginDisable(void)
{
    XPLMDebugString("TNC> XPluginDisable\n");
    Log() << Log::Info << "XPluginDisable" << Log::endl;
    if (pInstance)
    {
        delete pInstance;
        pInstance = nullptr;
    }
}

PLUGIN_API void	XPluginStop(void)
{
    XPLMDebugString("TNC> XPluginStop\n");
    Log() << Log::Info << "XPluginStop" << Log::endl;
}
