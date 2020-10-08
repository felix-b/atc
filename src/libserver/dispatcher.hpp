#pragma once

#include <memory>
#include <iostream>
#include <functional>
#include <thread>
#include <atomic>
#include <chrono>
#include <unordered_map>

#include "blockingconcurrentqueue.h"
#include "stlhelpers.h"
#include "libworld.h"
#include "world.pb.h"
#include "interfaces.hpp"

using namespace std;
using namespace world;

namespace server
{
    class Dispatcher : public DispatcherInterface
    {
    private:

        shared_ptr<HostServices> m_host;
        shared_ptr<DispatcherMiddlewareInterface> m_middleware;
        ReplyCallback m_broadcastToClients;
        int m_senderThreadCount;
        RequestHandler m_noopRequestHandler;
        shared_ptr<thread> m_serviceThread;
        vector<shared_ptr<thread>> m_senderThreads;
        moodycamel::BlockingConcurrentQueue<WorkItem> m_serviceQueue;
        moodycamel::BlockingConcurrentQueue<WorkItem> m_senderQueue;
        atomic<bool> m_stopRequested;

    public:
        Dispatcher(
            shared_ptr<HostServices> _host,
            shared_ptr<DispatcherMiddlewareInterface> _middleware,
            int _senderThreadCount
        ) : m_host(_host),
            m_middleware(_middleware),
            m_senderThreadCount(_senderThreadCount),
            m_noopRequestHandler([](const world_proto::ClientToServer& envelope, ReplyCallback reply) {}),
            m_stopRequested(false)
        {
            m_host->writeLog("SRVDSP|INIT starting");

            m_serviceThread = shared_ptr<thread>(new thread([this](){
                runConsumerThread("service", 0, m_serviceQueue);
            }));

            for (int i = 0 ; i < m_senderThreadCount ; i++)
            {
                m_senderThreads.push_back(shared_ptr<thread>(new thread([=](){
                    runConsumerThread("sender", i, m_senderQueue);
                })));
            }

            m_middleware->setBroadcastInterface([this](const world_proto::ServerToClient &envelope) {
                enqueueBroadcast(envelope);
            });

            m_host->writeLog("SRVDSP|INIT completed");
        }

        ~Dispatcher() override
        {
            stopNow();
        }

    public:

        void setBroadcastInterface(DispatcherInterface::ReplyCallback broadcastInterface) override
        {
            m_broadcastToClients = broadcastInterface;
        }

        void enqueueBroadcast(const world_proto::ServerToClient& envelope) //override
        {
            if (!m_broadcastToClients)
            {
                throw runtime_error("Dispatcher::enqueueBroadcast() : broadcast interface was not set");
            }

            m_host->writeLog(
                "SRVDSP|SEND enqueuing broadcast envelope id[%d] payload[%d]",
                envelope.id(),
                envelope.payload_case());

            m_senderQueue.enqueue([=]() {
                m_host->writeLog(
                    "SRVDSP|SEND dequeued broadcast envelope id[%d] payload[%d]",
                    envelope.id(),
                    envelope.payload_case());
                m_broadcastToClients(envelope);
            });
        }

        void enqueueOutbound(const world_proto::ServerToClient& envelope, ReplyCallback replyToSender) //override
        {
            m_host->writeLog(
                "SRVDSP|SEND enqueueing envelope id[%d] payload[%d]",
                envelope.id(),
                envelope.payload_case());

            m_senderQueue.enqueue([=]() {
                m_host->writeLog(
                    "SRVDSP|SEND dequeued envelope id[%d] payload[%d]",
                    envelope.id(),
                    envelope.payload_case());
                replyToSender(envelope);
            });
        }

        void enqueueInbound(const world_proto::ClientToServer& envelope, ReplyCallback replyToSender) override
        {
            bool handlerFound = false;
            const RequestHandler& handler = m_middleware->tryGetHandler(envelope, handlerFound);

            if (!handlerFound)
            {
                m_host->writeLog(
                    "SRVDSP|RECV envelope id[%d] payload[%d] ERROR: no handler found",
                    envelope.id(),
                    envelope.payload_case());
                return;
            }

            m_host->writeLog(
                "SRVDSP|RECV envelope id[%d] payload[%d] enqueue",
                envelope.id(),
                envelope.payload_case());

            m_serviceQueue.enqueue([this, envelope, replyToSender, &handler](){
                m_host->writeLog(
                    "SRVDSP|RECV envelope id[%d] payload[%d] dequeued",
                    envelope.id(),
                    envelope.payload_case());

                handler(envelope, [this, replyToSender](const world_proto::ServerToClient &envelope){
                    enqueueOutbound(envelope, replyToSender);
                });
            });
        }

        void beginStop() override
        {
            m_stopRequested = true;
        }

        void stopNow() override
        {
            m_stopRequested = true;

            if (m_serviceThread->joinable())
            {
                m_serviceThread->join();
            }

            for (const auto& senderThread : m_senderThreads)
            {
                if (senderThread->joinable())
                {
                    senderThread->join();
                }
            }
        }

    private:

        void runConsumerThread(
            string name,
            int threadIndex,
            moodycamel::BlockingConcurrentQueue<WorkItem>& queue)
        {
            m_host->writeLog("SRVDSP|RUN thread[%s] index[%d] loop starting", name.c_str(), threadIndex);

            while (!m_stopRequested)
            {
                WorkItem workItem;
                if (queue.wait_dequeue_timed(workItem, chrono::milliseconds(100)))
                {
                    try
                    {
                        workItem();
                    }
                    catch(const exception& e)
                    {
                        m_host->writeLog(
                            "SRVDSP|RUN thread[%s] index[%d] work item CRASHED!!! %s",
                            name.c_str(), threadIndex, e.what());
                    }
                }
            }

            m_host->writeLog("SRVDSP|RUN thread[%s] index[%d] loop stopped", name.c_str(), threadIndex);
        }

    public:

        // static void dumpMessage(const world_proto::ClientToServer& envelope)
        // {
        //     cout << "--- " << title << " " << data.length() << " bytes (first 32) ---" << endl;
        //     for (int i = 0 ; i < data.length() && i < 32 ; i++)
        //     {
        //         int value = data[i];
        //         cout << "[" << i << "]=" << value << endl;
        //     }
        //     cout << "--- end of " << title << " ---" << endl;
        // }

    private:

        static void noopRequestHandler(const world_proto::ClientToServer& envelope, ReplyCallback reply)
        {
        }
    };
}
