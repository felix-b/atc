//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

// SDK
#include "XPLMPlugin.h"
#include "XPLMUtilities.h"
#include "XPLMProcessing.h"

// PPL
#include "owneddata.h"

// atc
#include "utils.h"
#include "serviceClient.hpp"
#include "dispatcher.hpp"

using namespace std;
using namespace PPL;

class PluginInstance
{
private:
    XPLMCommandRef m_pttCommand;
    Dispatcher m_dispatcher;
    ServiceClientController m_serviceClient;
public:
    PluginInstance() :
        m_dispatcher()
    {
        printDebugString("INSTNC|constructor");

        m_serviceClient.start(9002, [this](const atc_proto::ServerToClient& envelope) {
            m_dispatcher.enqueueInbound([this, envelope]() {
                onMessageFromServer(envelope);
            });
        });

        printDebugString("INSTNC|constructor --- 2");

        m_pttCommand = XPLMCreateCommand("ATC/PTT", "Push To Talk");
        XPLMRegisterCommandHandler(
            m_pttCommand,
            &pttHandlerCallback,
            1,
            this);

        printDebugString("INSTNC|constructor --- 3");

        XPLMRegisterFlightLoopCallback(&flightLoopCallback, -1.0, this);

        printDebugString("INSTNC|constructor --- 4");

        printDebugString("INSTNC|XPLM callbacks registered");
    }

    ~PluginInstance()
    {
        printDebugString("INSTNC|destructor");
        XPLMUnregisterCommandHandler(m_pttCommand, &pttHandlerCallback, 1, this);
        XPLMUnregisterFlightLoopCallback(&flightLoopCallback, this);
    }

    void notifyAirportLoaded()
    {
        printDebugString("INSTNC|Airport loaded, resetting flight on server");

        m_dispatcher.enqueueOutbound([this] {
            atc_proto::ClientToServer envelope;
            envelope.mutable_user_acquire_aircraft()->set_aircraft_id(1);
            m_serviceClient.sendMessage(envelope);
        });

        printDebugString("INSTNC|Reset message sent");
    }

private:

    void onMessageFromServer(const atc_proto::ServerToClient& envelope)
    {
        printDebugString("INSTNC|RECV message from server payload case[%d]", envelope.payload_case());

        switch (envelope.payload_case())
        {
        case atc_proto::ServerToClient::PayloadCase::kReplyUserAcquireAircraft:
            const auto& reply = envelope.reply_user_acquire_aircraft();
            printDebugString(
                "INSTNC|WHOA Got weather from server! QNH[%d] WND[%d @ %d]",
                reply.weather().qnh_hpa(),
                reply.weather().wind_true_bearing_degrees(),
                reply.weather().wind_speed_kt());
            break;
        }
    }

    void onFlightLoopIteration()
    {
        m_dispatcher.executePendingInboundOnCurrentThread();
    }

    void onPttCommand(XPLMCommandPhase phase)
    {
        atc_proto::ClientToServer envelope;

        switch (phase)
        {
        case xplm_CommandBegin:
            printDebugString("INSTNC|PTT pushed");
            envelope.mutable_user_ptt_pressed()->set_frequency_khz(138500);
            m_dispatcher.enqueueOutbound([this, envelope] {
                m_serviceClient.sendMessage(envelope);
                printDebugString("INSTNC|PTT pushed - message sent to server");
            });
            break;
        case xplm_CommandEnd:
            printDebugString("INSTNC|PTT released");
            envelope.mutable_user_ptt_released()->set_frequency_khz(138500);
            m_dispatcher.enqueueOutbound([this, envelope] {
                m_serviceClient.sendMessage(envelope);
                printDebugString("INSTNC|PTT released - message sent to server");
            });
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
            static_cast<PluginInstance *>(inRefcon)->onPttCommand(inPhase);
        }
        return 0;
    }

    static float flightLoopCallback(
        float  inElapsedSinceLastCall,
        float  inElapsedTimeSinceLastFlightLoop,
        int    inCounter,
        void * inRefcon)
    {
        if (inRefcon)
        {
            static_cast<PluginInstance*>(inRefcon)->onFlightLoopIteration();
        }

        return -1;
    }

};

