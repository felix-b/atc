//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <atomic>
#include <future>
#include <functional>

#include "libserver.hpp"
#include "interfaces.hpp"
#include "worldService.hpp"
#include "worldServiceDispatchMiddleware.hpp"
#include "dispatcher.hpp"
#include "server.hpp"

using namespace std;

namespace server
{
    class ServerController : public ServerControllerInterface
    {
    private:
        enum class ServerState {
            Stopped,
            Starting,
            Started,
            Stopping
        };
    private:
        shared_ptr<HostServices> m_host;
        ServerInterface::Factory m_serverFactory;
        shared_ptr<ServerInterface> m_server;
        atomic<ServerState> m_state;
        future<void> m_serverRunCompletion;
    public:
        ServerController(shared_ptr<HostServices> _host, ServerInterface::Factory _serverFactory) :
            m_host(_host),
            m_state(ServerState::Stopped),
            m_serverFactory(_serverFactory)
        {
        }
    public:
        bool running() override
        {
            return (m_state != ServerState::Stopped);
        }

        void start(int listenPort) override
        {
            ServerState expectedState = ServerState::Stopped;
            if (!m_state.compare_exchange_strong(expectedState, ServerState::Starting))
            {
                // not stopped!
                return;
            }

            m_server = m_serverFactory();
            m_serverRunCompletion = std::async(std::launch::async, [this, listenPort] {
                m_server->runOnCurrentThread(listenPort);
            });

            m_state.store(ServerState::Started);
        }

        void beginStop() override
        {
            ServerState expectedState = ServerState::Started;
            if (!m_state.compare_exchange_strong(expectedState, ServerState::Stopping))
            {
                // not started!
                // TODO: handle the corner case of beginStop() during start() ?
                return;
            }

            m_server->beginGracefulShutdown();
        }

        bool waitUntilStopped(chrono::milliseconds timeout) override
        {
            if (m_state != ServerState::Stopping)
            {
                // not stopping right now!
                // TODO: handle the corner case of start() during waitUntilStopped() ?
                return (m_state == ServerState::Stopped);
            }

            if (m_serverRunCompletion.wait_for(timeout) == future_status::ready)
            {
                m_state = ServerState::Stopped;
                return true;
            }

            return false;
        }
    };

    shared_ptr<ServerControllerInterface> server::ServerControllerInterface::create(shared_ptr<HostServices> host)
    {
        ServerInterface::Factory serverFactory = [host] {
            auto service = shared_ptr<WorldService>(new WorldService(host));
            auto middleware = shared_ptr<WorldServiceDispatchMiddleware>(new WorldServiceDispatchMiddleware(host, service));
            auto dispatcher = shared_ptr<Dispatcher>(new Dispatcher(host, middleware, 1));

            shared_ptr<ServerInterface> server = make_shared<Server>(host, dispatcher);
            return server;
        };

        return make_shared<ServerController>(host, serverFactory);
    }
}
