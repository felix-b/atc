#pragma once

#include <memory>
#include <functional>
#include <unordered_map>

#include "stlhelpers.h"
#include "libworld.h"
#include "world.pb.h"
#include "interfaces.hpp"

using namespace std;
using namespace world;

namespace server
{
    class WorldServiceDispatchMiddleware : public DispatcherMiddlewareInterface
    {
    private:
        shared_ptr<HostServices> m_host;
        shared_ptr<WorldServiceInterface> m_service;
        DispatcherInterface::ReplyCallback m_broadcastToClients;
        unordered_map<
            world_proto::ClientToServer::PayloadCase,
            DispatcherInterface::RequestHandler
        > m_requestHandlerMap;
        DispatcherInterface::RequestHandler m_noopRequestHandler;
    public:
        WorldServiceDispatchMiddleware(
            shared_ptr<HostServices> _host,
            shared_ptr<WorldServiceInterface> _service
        ) : m_host(_host),
            m_service(_service),
            m_noopRequestHandler(
                [](const world_proto::ClientToServer& envelope, DispatcherInterface::ReplyCallback reply) {})
        {
            buildHandlerMap();
            m_service->setBroadcastInterface([this](const world_proto::ServerToClient &envelope) {
                m_broadcastToClients(envelope);
            });
        }

    public:

        void setBroadcastInterface(DispatcherInterface::ReplyCallback broadcastInterface) override
        {
            m_broadcastToClients = broadcastInterface;
        }

        const DispatcherInterface::RequestHandler& tryGetHandler(
            const world_proto::ClientToServer& requestEnvelope,
            bool& found) override
        {
            DispatcherInterface::RequestHandler& handler = m_noopRequestHandler;
            found = tryGetValue(m_requestHandlerMap, requestEnvelope.payload_case(),handler);
            return handler;
        }

    private:

        void buildHandlerMap()
        {
            m_requestHandlerMap.insert({
                world_proto::ClientToServer::kConnect,
                [this](const world_proto::ClientToServer& request, DispatcherInterface::ReplyCallback replyToSender) {
                    world_proto::ServerToClient replyEnvelope;
                    m_service->connect(request.connect(), replyEnvelope);
                    replyToSender(replyEnvelope);
                }
            });

            m_requestHandlerMap.insert({
                world_proto::ClientToServer::kQueryAirport,
                [this](const world_proto::ClientToServer& request, DispatcherInterface::ReplyCallback replyToSender) {
                    world_proto::ServerToClient replyEnvelope;
                    m_service->queryAirport(request.query_airport(), replyEnvelope);
                    replyToSender(replyEnvelope);
                }
            });

            m_requestHandlerMap.insert({
                world_proto::ClientToServer::kQueryTaxiPath,
                [this](const world_proto::ClientToServer& request, DispatcherInterface::ReplyCallback replyToSender) {
                    world_proto::ServerToClient replyEnvelope;
                    m_service->queryTaxiPath(request.query_taxi_path(), replyEnvelope);
                    replyToSender(replyEnvelope);
                }
            });
        }
    };
}
