//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <memory>
#include <iostream>
#include <functional>
#include <atomic>
#include <thread>
#include <mutex>
#include <forward_list>
#include <cstdlib>
#include <iostream>
#include <map>
#include <string>
#include <sstream>

// SDK
#include "XPLMPlugin.h"
#include "XPLMUtilities.h"

// PPL
#include "owneddata.h"

// atc
#include "utils.h"
#include "proto/atc.pb.h"

using namespace std;
using namespace PPL;

#include <websocketpp/config/asio_no_tls_client.hpp>
#include <websocketpp/client.hpp>

class ServiceClient
{
private:
    typedef websocketpp::client<websocketpp::config::asio_client> Endpoint;
public:
    typedef function<void(const atc_proto::ServerToClient &envelope)> MessageHandler;
private:
    Endpoint m_endpoint;
    Endpoint::connection_ptr m_connection;
    //shared_ptr<thread> m_thread;
    string m_url;
    atomic<bool> m_stopping;
    MessageHandler m_handler;
public:
    ServiceClient(const string& url, MessageHandler handler) :
        m_url(url),
        m_stopping(false),
        m_handler(handler)
    {
        m_endpoint.set_access_channels(websocketpp::log::alevel::all);
        m_endpoint.clear_access_channels(websocketpp::log::alevel::frame_payload);
        m_endpoint.clear_error_channels(websocketpp::log::elevel::all);

        m_endpoint.init_asio();
        m_endpoint.start_perpetual();
    }

    ~ServiceClient()
    {
        if (!m_stopping.load())
        {
            beginGracefulShutdown();
        }
    }

    void sendMessage(const atc_proto::ClientToServer &envelope)
    {
        error_code error;

        string dataOnWire;
        if (!envelope.SerializeToString(&dataOnWire))
        {
            printDebugString(
                "SVCLNT|SEND payload case[%d] to connection [%p] ERROR: serialization failed",
                envelope.payload_case(),
                m_connection.get());
            return;
        }

        error = m_connection->send(dataOnWire, websocketpp::frame::opcode::binary);
        if (!error)
        {
            printDebugString(
                "SVCLNT|SEND payload case[%d] to connection[%p] size[%llu] OK",
                envelope.payload_case(),
                m_connection.get(),
                dataOnWire.length());
        }
        else
        {
            printDebugString(
                "SVCLNT|SEND ERROR: payload case[%d] to connection[%p] size[%llu] error[%d|%s]",
                envelope.payload_case(),
                m_connection.get(),
                dataOnWire.length(),
                error.value(),
                error.message().c_str());
        }
    }

    void runOnCurrentThread()
    {
        websocketpp::lib::error_code ec;
        m_connection = m_endpoint.get_connection(m_url, ec);
        if (ec)
        {
            printDebugString("SVCLNT|ERROR: could not create connection because: %s", ec.message().c_str());
            return;
        }

        m_connection->set_open_handler([this](websocketpp::connection_hdl handle) {
            printDebugString("SVCLNT|CONNECT SUCCESS: connected to server");
        });

        m_connection->set_fail_handler([this](websocketpp::connection_hdl handle) {
            printDebugString(
                "SVCLNT|CONNECT FAILURE: error[%d|%s]",
                m_connection->get_ec().value(),
                m_connection->get_ec().message().c_str());
        });

        m_connection->set_close_handler([this](websocketpp::connection_hdl handle) {
            printDebugString("SVCLNT|DISCONNECT: disconnected from server");
        });

        m_connection->set_close_handshake_timeout(500);

        m_connection->set_message_handler([this](websocketpp::connection_hdl hdl, Endpoint::message_ptr msg) {
            handleIncomingMessage(hdl, msg);
        });

        printDebugString("SVCLNT|CONNECT: connecting to server");

        m_endpoint.connect(m_connection);

        printDebugString("SVCLNT|START: starting run loop");

        m_endpoint.run();

        printDebugString("SVCLNT|STOPPED: exited run loop");
    }

    void beginGracefulShutdown()
    {
        printDebugString("SVCLNT|STOPPING: beginGracefulShutdown starting");

        m_stopping = true;
        m_endpoint.stop_perpetual();

        if (m_connection)
        {
            m_connection->close(websocketpp::close::status::going_away, "client is going away");
            m_connection.reset();
        }

        printDebugString("SVCLNT|STOPPING: beginGracefulShutdown completed");
    }

private:

    void handleIncomingMessage(websocketpp::connection_hdl hdl, Endpoint::message_ptr msg)
    {
        if (msg->get_opcode() != websocketpp::frame::opcode::binary)
        {
            printDebugString("SVCLNT|RECV WARNING: not binary format, ignored");
            return;
        }

        const string &dataOnWire = msg->get_payload();
        printDebugString("SVCLNT|RECV size[%llu]", dataOnWire.size());

        atc_proto::ServerToClient envelope;
        if (!envelope.ParseFromString(dataOnWire))
        {
            printDebugString("SVCLNT|RECV ERROR: failed to parse");
            return;
        }

        printDebugString("SVCLNT|RECV SUCCESS payload case[%d]", envelope.payload_case());

        m_handler(envelope);
    }
};

class ServiceClientController
{
private:
    enum class ControllerState {
        Stopped,
        Starting,
        Started,
        Stopping
    };
private:
    shared_ptr<ServiceClient> m_client;
    atomic<ControllerState> m_state;
    future<void> m_serverRunCompletion;
public:
    ServiceClientController() :
        m_state(ControllerState::Stopped)
    {
    }

    ~ServiceClientController()
    {
        beginStop();

        if (!waitUntilStopped(chrono::milliseconds(5000)))
        {
            printDebugString("SVCLNT|CTRL WARNING: controller could not stop in timely fashion");
        }
    }
public:
    bool running()
    {
        return (m_state != ControllerState::Stopped);
    }

    void start(int serverPort, ServiceClient::MessageHandler handler)
    {
        printDebugString("SVCLNT|CTRL: controller starting");

        ControllerState expectedState = ControllerState::Stopped;
        if (!m_state.compare_exchange_strong(expectedState, ControllerState::Starting))
        {
            // not stopped!
            return;
        }

        stringstream url;
        url << "http://localhost:" << serverPort << "/ws";
        m_client.reset(new ServiceClient(url.str(), handler));

        m_serverRunCompletion = std::async(std::launch::async, [this] {
            m_client->runOnCurrentThread();
        });

        m_state.store(ControllerState::Started);
        printDebugString("SVCLNT|CTRL: controller started");
    }

    void sendMessage(const atc_proto::ClientToServer &envelope)
    {
        if (m_state != ControllerState::Started)
        {
            printDebugString("SVCLNT|CTRL WARN: cannot send message - not in started state");
            return;
        }

        m_client->sendMessage(envelope);
    }

    void beginStop()
    {
        printDebugString("SVCLNT|CTRL: controller stopping");

        ControllerState expectedState = ControllerState::Started;
        if (!m_state.compare_exchange_strong(expectedState, ControllerState::Stopping))
        {
            // not started!
            // TODO: handle the corner case of beginStop() during start() ?
            printDebugString("SVCLNT|CTRL WARNING: controller not started!");
            return;
        }

        m_client->beginGracefulShutdown();
    }

    bool waitUntilStopped(chrono::milliseconds timeout)
    {
        if (m_state != ControllerState::Stopping)
        {
            // not stopping right now!
            // TODO: handle the corner case of start() during waitUntilStopped() ?
            return (m_state == ControllerState::Stopped);
        }

        if (m_serverRunCompletion.wait_for(timeout) == future_status::ready)
        {
            m_state = ControllerState::Stopped;
            return true;
        }

        return false;
    }
};
