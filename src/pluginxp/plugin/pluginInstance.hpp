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
    DataRef<int> m_dataRefCom1FrequencyKhz;
    DataRef<int> m_dataRefCom2FrequencyKhz;
    DataRef<float> m_dataRefQnhInHg;
//    DataRef<int> m_dataRefWindAltitudeMetersMsl;
//    DataRef<float> m_dataRefWindDirectionTrueDegrees;
//    DataRef<int> m_dataRefWindSpeedKnots;
public:
    PluginInstance() :
        m_dispatcher(),
        m_dataRefCom1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadOnly),
        m_dataRefCom2FrequencyKhz("sim/cockpit2/radios/actuators/com2_frequency_hz_833", PPL::ReadOnly),
        m_dataRefQnhInHg("sim/weather/barometer_sealevel_inhg", PPL::ReadWrite)
//        m_dataRefWindAltitudeMetersMsl("sim/weather/wind_altitude_msl_m[0]", PPL::ReadWrite),
//        m_dataRefWindDirectionTrueDegrees("sim/weather/wind_direction_degt[0]", PPL::ReadWrite),
//        m_dataRefWindSpeedKnots("sim/weather/wind_speed_kt[0]", PPL::ReadWrite)
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
            auto qnhInHgX100 =  reply.weather().qnh_hpa(); //TODO: rename hpa->ingh as it actually is
            auto windSpeedKt = reply.weather().wind_speed_kt();
            auto windDirectionDegt = reply.weather().wind_true_bearing_degrees();
            printDebugString(
                "INSTNC|WHOA Got weather from server! QNH[%d] WND[%d @ %d]",
                qnhInHgX100,
                windDirectionDegt,
                windSpeedKt);
            m_dataRefQnhInHg = ((float)reply.weather().qnh_hpa()) / 100.0f;
//            m_dataRefWindAltitudeMetersMsl = 1;
//            m_dataRefWindDirectionTrueDegrees = windDirectionDegt;
//            m_dataRefWindSpeedKnots = windSpeedKt;
            printDebugString("INSTNC|WHOA done applying weather");
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

