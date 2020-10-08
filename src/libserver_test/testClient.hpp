//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <string>
#include <vector>
#include <iostream>
#include <sstream>
#include <future>
#include <atomic>
#include <mutex>
#include <websocketpp/config/asio_no_tls_client.hpp>
#include <websocketpp/client.hpp>
#include "world.pb.h"
#include "gtest/gtest.h"

using namespace std;
using websocketpp::lib::placeholders::_1;
using websocketpp::lib::placeholders::_2;
using websocketpp::lib::bind;

class TestClient
{
private:

    typedef websocketpp::client<websocketpp::config::asio_client> Endpoint;
    typedef websocketpp::config::asio_client::message_type::ptr MessagePtr;

private:

    Endpoint m_endpoint;
    Endpoint::connection_ptr m_connection;
    future<void> m_runCompletion;
    vector<world_proto::ServerToClient> m_receivedMessages;
    vector<string> m_errors;
    atomic<bool> m_openState;
    atomic<bool> m_closedState;
    atomic<bool> m_failedState;
    mutex m_outputLock;
public:

    TestClient() :
        m_openState(false),
        m_closedState(false),
        m_failedState(false)
    {
        m_endpoint.clear_access_channels(websocketpp::log::alevel::frame_header);
        m_endpoint.clear_access_channels(websocketpp::log::alevel::frame_payload);

        m_endpoint.init_asio();

        m_endpoint.set_open_handler([this](websocketpp::connection_hdl hdl) { onOpen(hdl); });
        m_endpoint.set_fail_handler([this](websocketpp::connection_hdl hdl) { onFail(hdl); });
        m_endpoint.set_message_handler([this](websocketpp::connection_hdl hdl, MessagePtr msg) { onMessage(hdl, msg); });
        m_endpoint.set_close_handler([this](websocketpp::connection_hdl hdl) { onClose(hdl); });
    }

    ~TestClient()
    {
        disconnect();
    }

public:

    void connect(const string& uri = "ws://localhost:9002")
    {
        if (m_connection)
        {
            throw runtime_error("TestClient::connect() failed : already connected!");
        }

        websocketpp::lib::error_code error;
        Endpoint::connection_ptr connection = m_endpoint.get_connection(uri, error);
        if (error)
        {
            throw runtime_error("TestClient::connect() failed! " + error.message());
        }

        m_connection = m_endpoint.connect(connection);
        m_runCompletion = std::async(std::launch::async, [this] {
            m_endpoint.run();
        });
    }

    void send(world_proto::ClientToServer& envelope)
    {
        if (!m_connection)
        {
            throw runtime_error("TestClient::send() : not connected!");
        }

        string dataOnWire;
        if (!envelope.SerializeToString(&dataOnWire))
        {
            throw runtime_error("TestClient::send() : envelope serialization failed!");
        }

        const auto error = m_connection->send(dataOnWire,websocketpp::frame::opcode::binary);
        if (error)
        {
            throw runtime_error("TestClient::send() failed! " + error.message());
        }
    }

    void disconnect()
    {
        if (m_connection)
        {
            error_code error;
            m_connection->close(websocketpp::close::status::going_away, "shutting down", error);
            if (error)
            {
                lock_guard<mutex> lock(m_outputLock);
                m_errors.push_back("disconnect() failed: " + error.message());
            }

            if (m_runCompletion.valid() && m_runCompletion.wait_for(chrono::milliseconds(5000)) != future_status::ready)
            {
                lock_guard<mutex> lock(m_outputLock);
                m_errors.push_back("disconnect() failed: timed out waiting for run completion");
                return;
            }

            m_connection.reset();
        }
    }

    void assertNoErrors()
    {
        if (!m_errors.empty())
        {
            stringstream message;
            message << "assertNoErrors() : found " << m_errors.size() << " errors: ";
            for (const auto& error : m_errors)
            {
                message << "[" << error << "] ";
            }

            EXPECT_EQ(message.str(), "");
        }

        EXPECT_FALSE(m_failedState.load());
    }

public:

    const atomic<bool>& openState() const { return m_openState; }
    const atomic<bool>& closedState() const { return m_closedState; }
    const atomic<bool>& failedState() const { return m_failedState; }

    int receivedMessagesCount() {
        lock_guard<mutex> lock(m_outputLock);
        return m_receivedMessages.size();
    }

    void copyReceivedMessagesTo(vector<world_proto::ServerToClient>& destination)
    {
        lock_guard<mutex> lock(m_outputLock);
        destination = m_receivedMessages;
    }

private:

    void onOpen(websocketpp::connection_hdl hdl)
    {
        m_openState = true;
    }

    void onFail(websocketpp::connection_hdl hdl)
    {
        lock_guard<mutex> lock(m_outputLock);
        m_failedState = true;
        m_errors.push_back("onFail() called");
    }

    void onMessage(websocketpp::connection_hdl hdl, MessagePtr msg)
    {
        lock_guard<mutex> lock(m_outputLock);

        if (msg->get_opcode() != websocketpp::frame::opcode::binary)
        {
            m_errors.push_back("onMessage(): not binary format, ignored");
            return;
        }

        const string &dataOnWire = msg->get_payload();
        world_proto::ServerToClient envelope;
        if (!envelope.ParseFromString(dataOnWire))
        {
            m_errors.push_back("onMessage(): deserialization failed");
            return;
        }

        m_receivedMessages.push_back(envelope);
    }

    void onClose(websocketpp::connection_hdl hdl)
    {
        m_closedState = true;
    }
};
