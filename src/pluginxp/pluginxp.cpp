// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <iostream>
#include <functional>

// SDK
#include "XPLMPlugin.h"
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
    XPLMEnableFeature("XPLM_USE_NATIVE_PATHS", 1);

    //PluginPath::setPluginAbsoluteDirectory(GetThisDllDirectory());
    PluginPath::setPluginDirectoryName("TrafficAndControl");
    LogWriter::getLogger().setLogFile(PluginPath::prependPluginPath("tncpoc0_log.txt"));

    Log() << Log::Info << "XPluginStart" << Log::endl;

    strcpy(outName, "Traffic & Control");
    strcpy(outSig, "felix-b.traffic-and-control");
    strcpy(outDesc, "Local virtual world of traffic and ATC");

    return 1;
}

PLUGIN_API int XPluginEnable(void)
{
    Log() << Log::Info << "XPluginEnable" << Log::endl;
    pInstance = new PluginInstance();
    return 1;
}

PLUGIN_API void XPluginReceiveMessage(XPLMPluginID fromId, long inMsg, void*)
{
    Log() << Log::Info << "XPluginReceiveMessage(inMsg=" << (int32_t)inMsg << ")" << Log::endl;
}

PLUGIN_API void XPluginDisable(void)
{
    Log() << Log::Info << "XPluginDisable" << Log::endl;
    if (pInstance)
    {
        delete pInstance;
        pInstance = nullptr;
    }
}

PLUGIN_API void	XPluginStop(void)
{
    Log() << Log::Info << "XPluginStop" << Log::endl;
}
