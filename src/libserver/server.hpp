#pragma once
#define ASIO_STANDALONE

#include <memory>
#include <iostream>
#include <functional>
#include <atomic>
#include <thread>
#include <mutex>
#include <forward_list>

#include <websocketpp/config/asio_no_tls.hpp>
#include <websocketpp/server.hpp>

#include "libworld.h"
#include "interfaces.hpp"

using namespace std;
using namespace world;

namespace server
{
    class Server : public ServerInterface
    {
    private:
        typedef websocketpp::server<websocketpp::config::asio> Endpoint;
    private:
        shared_ptr<HostServices> m_host;
        shared_ptr<DispatcherInterface> m_dispatcher;
        Endpoint m_endpoint;
        forward_list<websocketpp::connection_hdl> m_connections;
        mutex m_connectionsLock;
        atomic<bool> m_stopping;
    public:
        Server(shared_ptr<HostServices> _host, shared_ptr<DispatcherInterface> _dispatcher) :
            m_host(_host),
            m_dispatcher(_dispatcher),
            m_stopping(false)
        {
            m_host->writeLog("SRVHST|INIT starting");

            // Set logging settings
            m_endpoint.set_error_channels(websocketpp::log::elevel::all);
            m_endpoint.set_access_channels(websocketpp::log::alevel::all ^ websocketpp::log::alevel::frame_payload);

            m_endpoint.set_validate_handler([=](websocketpp::connection_hdl hdl) {
                return onConnection(hdl);
            });
            m_endpoint.set_message_handler([=](websocketpp::connection_hdl hdl, Endpoint::message_ptr msg) {
                onMessage(hdl, msg);
            });

            // Initialize Asio
            m_endpoint.init_asio();
            m_endpoint.get_alog().set_ostream(&std::cout);
            m_endpoint.get_elog().set_ostream(&std::cout);

            m_dispatcher->setBroadcastInterface([this](const world_proto::ServerToClient &envelope) {
                broadcastToClients(envelope);
            });

            m_host->writeLog("SRVHST|INIT completed");
        }

        ~Server() override
        {
            if (!m_stopping.load())
            {
                beginGracefulShutdown();
            }
        }

        void runOnCurrentThread(int listenPortNumber) override
        {
            m_host->writeLog("SRVHST|ENDP entered runOnCurrentThread");

            try
            {
                m_endpoint.listen(listenPortNumber);
                m_endpoint.start_accept();

                m_host->writeLog("SRVHST|ENDP listening on port[%d]", listenPortNumber);

                m_endpoint.run();

                m_host->writeLog("SRVHST|ENDP endpoint stopped");
            }
            catch (const exception& e)
            {
                m_host->writeLog("SRVHST|ENDP CRASHED!!! %s", e.what());
            }

            m_host->writeLog("SRVHST|ENDP exiting runOnCurrentThread");
        }

        void beginGracefulShutdown() override
        {
            m_host->writeLog("SRVHST|ENDP beginGracefulShutdown starting");

            m_stopping = true;
            m_endpoint.stop_listening();
            m_dispatcher->beginStop();

            closeAllConnections();

            m_host->writeLog("SRVHST|ENDP beginGracefulShutdown completed");
        }

    private:

        bool onConnection(websocketpp::connection_hdl hdl)
        {
            error_code error;

            if (!m_stopping.load())
            {
                lock_guard<mutex> lock(m_connectionsLock);

                if (!m_stopping)
                {
                    const auto connection = m_endpoint.get_con_from_hdl(hdl, error);
                    if (connection && !error)
                    {
                        m_host->writeLog("SRVHST|CONN approved new connection [%p]", connection.get());
                        m_connections.push_front(hdl);
                        return true;
                    }
                }
            }

            m_host->writeLog("SRVHST|CONN WARNING: new connection rejected: %s", error.message().c_str());
            return false;
        }

        void onMessage(websocketpp::connection_hdl hdl, Endpoint::message_ptr msg)
        {
            if (msg->get_opcode() != websocketpp::frame::opcode::binary)
            {
                m_host->writeLog("SRVHST|RECV WARNING: not binary format, ignored");
                return;
            }

            const string &dataOnWire = msg->get_payload();
            m_host->writeLog("SRVHST|RECV size[%llu]", dataOnWire.size());

            world_proto::ClientToServer envelope;
            if (!envelope.ParseFromString(dataOnWire))
            {
                m_host->writeLog("SRVHST|RECV ERROR: failed to parse");
                return;
            }

            m_host->writeLog("SRVHST|RECV payload case[%d], enqueue", envelope.payload_case());
            m_dispatcher->enqueueInbound(envelope, [=](const world_proto::ServerToClient &replyEnvelope) {
                sendToConnection(hdl, replyEnvelope);
            });
        }

        void sendToConnection(websocketpp::connection_hdl hdl, const world_proto::ServerToClient &envelope)
        {
            error_code error;

            const auto connection = m_endpoint.get_con_from_hdl(hdl, error);
            if (error || !connection)
            {
                m_host->writeLog(
                    "SRVHST|SEND payload case[%d] ERROR: connection was closed [%s]",
                    envelope.payload_case(),
                    error.message().c_str());
                return;
            }

            string dataOnWire;
            if (!envelope.SerializeToString(&dataOnWire))
            {
                m_host->writeLog(
                    "SRVHST|SEND payload case[%d] to connection [%p] ERROR: serialization failed",
                    envelope.payload_case(),
                    connection.get());
                return;
            }

            error = connection->send(dataOnWire, websocketpp::frame::opcode::binary);
            if (!error)
            {
                m_host->writeLog(
                    "SRVHST|SEND payload case[%d] to connection[%p] size[%llu] OK",
                    envelope.payload_case(),
                    connection.get(),
                    dataOnWire.length());
            }
            else
            {
                m_host->writeLog(
                    "SRVHST|SEND ERROR: payload case[%d] to connection[%p] size[%llu] error[%d]",
                    envelope.payload_case(),
                    connection.get(),
                    dataOnWire.length(),
                    error.value());
            }
        }

        void broadcastToClients(const world_proto::ServerToClient &envelope)
        {
            m_host->writeLog(
                "SRVHST|SEND starting broadcast of payload case[%d]",
                envelope.payload_case());

            vector<websocketpp::connection_hdl> copyOfConnections;
            copyAllConnections(copyOfConnections);

            for (const auto& hdl : copyOfConnections)
            {
                sendToConnection(hdl, envelope);
            }

            m_host->writeLog(
                "SRVHST|SEND completed broadcast of payload case[%d]",
                envelope.payload_case());
        }

        void closeAllConnections()
        {
            lock_guard<mutex> lock(m_connectionsLock);

            for (const auto& hdl : m_connections)
            {
                closeConnection(hdl);
            }

            m_connections.clear();
        }

        void closeConnection(const websocketpp::connection_hdl& hdl)
        {
            error_code error;
            const auto connection = m_endpoint.get_con_from_hdl(hdl, error);
            if (connection && !error)
            {
                m_host->writeLog("SRVHST|CONN closing connection [%p]", connection.get());

                error_code error;
                connection->close(websocketpp::close::status::going_away, "shutting down", error);

                if (error)
                {
                    m_host->writeLog("SRVHST|CONN closing connection [%p] FAILED: error[%d]", error.value());
                }
            }
        }

        void copyAllConnections(vector<websocketpp::connection_hdl>& destination)
        {
            lock_guard<mutex> lock(m_connectionsLock);

            for (const auto& hdl : m_connections)
            {
                destination.push_back(hdl);
            }
        }
    };
}