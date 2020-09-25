// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

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

using namespace std;
using namespace PPL;

class MultiplayerInitializer
{
private:
public:
    MultiplayerInitializer(const string& resourceDirectory)
    {
        Log() << Log::Info << "MultiplayerInitializer::MultiplayerInitializer()" << Log::endl;

        const char* error = XPMPMultiplayerInit(
            "TNCPOC0",               // plugin name,
            resourceDirectory.c_str(),    // path to supplemental files
            CBIntPrefsFunc,          // configuration callback function
            "C172");                 // default ICAO type
        if (error[0])
        {
            Log() << Log::Error << "XPMPMultiplayerInit failed: " << error << Log::endl;
            return;
        }

        Log() << Log::Info << "XPMPMultiplayerInit: success." << Log::endl;

        // Load our CSL models
        error = XPMPLoadCSLPackage(resourceDirectory.c_str());     // CSL folder root path
        if (error[0])
        {
            Log() << Log::Error << "XPMPLoadCSLPackage failed: " << error << Log::endl;
            return;
        }

        Log() << Log::Info << "XPMPLoadCSLPackage: success." << Log::endl;

        // Now we also try to get control of AI planes. That's optional, though,
        // other plugins (like LiveTraffic, XSquawkBox, X-IvAp...)
        // could have control already
        error = XPMPMultiplayerEnable(CPRequestAIAgain);
        if (error[0]) {
            Log() << Log::Error << "XPMPMultiplayerEnable failed: " << error << Log::endl;
            return;
        }

        Log() << Log::Info << "XPMPMultiplayerEnable: success." << Log::endl;
    }
    ~MultiplayerInitializer()
    {
        Log() << Log::Info << "MultiplayerInitializer::~MultiplayerInitializer()" << Log::endl;
        
        XPLMDebugString("TNCPOC0> invoking XPMPMultiplayerDisable\n");
        XPMPMultiplayerDisable();

        XPLMDebugString("TNCPOC0> invoking XPMPMultiplayerCleanup\n");
        XPMPMultiplayerCleanup();
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
        XPLMDebugString("TNCPOC0> CPRequestAIAgain: invoking XPMPMultiplayerEnable\n");
        XPMPMultiplayerEnable(CPRequestAIAgain);
    }
};
