#pragma once

#include <memory>
#include <functional>

#include "libworld.h"
#include "world.pb.h"

using namespace std;

namespace server
{
    class DispatcherInterface
    {
    public:
        typedef function<void()> WorkItem;
        typedef function<void(
            const world_proto::ServerToClient &envelope
        )> ReplyCallback;
        typedef function<void(
            const world_proto::ClientToServer &request,
            ReplyCallback replyToSender
        )> RequestHandler;
    protected:
        DispatcherInterface() = default;
    public:
        virtual ~DispatcherInterface() = default;
        virtual void setBroadcastInterface(DispatcherInterface::ReplyCallback broadcastInterface) = 0;
        // virtual void enqueueBroadcast(const world_proto::ServerToClient& envelope) = 0;
        // virtual void enqueueOutbound(const world_proto::ServerToClient& envelope, ReplyCallback replyToSender) = 0;
        virtual void enqueueInbound(const world_proto::ClientToServer &envelope, ReplyCallback replyToSender) = 0;
        virtual void beginStop() = 0;
        virtual void stopNow() = 0;
    };

    class DispatcherMiddlewareInterface
    {
    protected:
        DispatcherMiddlewareInterface() = default;
    public:
        virtual ~DispatcherMiddlewareInterface() = default;
        virtual void setBroadcastInterface(DispatcherInterface::ReplyCallback broadcastInterface) = 0;
        virtual const DispatcherInterface::RequestHandler& tryGetHandler(
            const world_proto::ClientToServer& requestEnvelope,
            bool& found) = 0;
    };

    class WorldServiceInterface
    {
    protected:
        WorldServiceInterface() = default;
    public:
        virtual ~WorldServiceInterface() = default;

        virtual void setBroadcastInterface(DispatcherInterface::ReplyCallback broadcastInterface) = 0;

        virtual void connect(
            const world_proto::ClientToServer_Connect &request,
            world_proto::ServerToClient &replyEnvelope) = 0;

        virtual void queryAirport(
            const world_proto::ClientToServer_QueryAirport &request,
            world_proto::ServerToClient &replyEnvelope) = 0;

        virtual void queryTaxiPath(
            const world_proto::ClientToServer_QueryTaxiPath &request,
            world_proto::ServerToClient &replyEnvelope) = 0;
    };

    class ServerInterface
    {
    public:
        typedef function<shared_ptr<ServerInterface>(void)> Factory;
    protected:
        ServerInterface() = default;
    public:
        virtual ~ServerInterface() = default;
        virtual void runOnCurrentThread(int listenPortNumber) = 0;
        virtual void beginGracefulShutdown() = 0;
    };
}
