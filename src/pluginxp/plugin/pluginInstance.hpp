//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

// SDK
#include "XPLMPlugin.h"
#include "XPLMUtilities.h"

// PPL
#include "owneddata.h"

// atc
#include "utils.h"
#include "serviceClient.hpp"

using namespace std;
using namespace PPL;

class PluginInstance
{
private:
    XPLMCommandRef m_pttCommand;
    ServiceClientController m_serviceClient;
public:
    PluginInstance()
    {
        printDebugString("INSTNC|constructor");

        m_pttCommand = XPLMCreateCommand("ATC/PTT", "Push To Talk");

        XPLMRegisterCommandHandler(
            m_pttCommand,
            &pttHandlerCallback,
            1,
            this);

        printDebugString("INSTNC|commands registered");

        m_serviceClient.start(9002);
    }

    ~PluginInstance()
    {
        printDebugString("INSTNC|destructor");
        XPLMUnregisterCommandHandler(m_pttCommand, &pttHandlerCallback, 1, this);
    }

    void notifyAirportLoaded()
    {
        printDebugString("INSTNC|Airport loaded, resetting flight on server");

        atc_proto::ClientToServer envelope;
        envelope.mutable_user_ptt_released()->set_frequency_khz(-1);
        m_serviceClient.sendMessage(envelope);

        printDebugString("INSTNC|Reset message sent");
    }

private:
    void handlePttCommand(XPLMCommandPhase phase)
    {
        atc_proto::ClientToServer envelope;

        switch (phase)
        {
        case xplm_CommandBegin:
            printDebugString("INSTNC|PTT pushed");
            envelope.mutable_user_ptt_pressed()->set_frequency_khz(138500);
            m_serviceClient.sendMessage(envelope);
            printDebugString("INSTNC|PTT pushed - message sent to server");
            break;
        case xplm_CommandEnd:
            printDebugString("INSTNC|PTT released");
            envelope.mutable_user_ptt_released()->set_frequency_khz(138500);
            m_serviceClient.sendMessage(envelope);
            printDebugString("INSTNC|PTT released - message sent to server");
            break;
        }
//
//        //  Use the structure below to have the command executed
//        //  continuously while the button is being held down.
//        if (inPhase == xplm_CommandContinue)
//        {
//            XPLMSetDataf(gHeadPositionXDataRef, XPLMGetDataf(gHeadPositionXDataRef) + .1);
//        }
//
//        //  Use this structure to have the command executed on button up only.
//        if (inPhase == xplm_CommandEnd)
//        {
//            XPLMSetDataf(gHeadPositionXDataRef, XPLMGetDataf(gHeadPositionXDataRef) + .1);
//        }
//
//        // Return 1 to pass the command to plugin windows and X-Plane.
//        // Returning 0 disables further processing by X-Plane.
//        // In this case we might return 0 or 1 because X-Plane does not duplicate our command.
    }

    static int pttHandlerCallback(
        XPLMCommandRef inCommand,
        XPLMCommandPhase inPhase,
        void *inRefcon)
    {
        if (inRefcon)
        {
            static_cast<PluginInstance*>(inRefcon)->handlePttCommand(inPhase);
        }
        return 0;
    }
};

