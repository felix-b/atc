#pragma once

#include <memory>
#include <iostream>
#include <functional>
#include <thread>
#include <atomic>
#include <chrono>
#include <unordered_map>

#include "blockingconcurrentqueue.h"
#include "atc.pb.h"

using namespace std;

class Dispatcher
{
public:
    typedef function<void()> WorkItem;
private:
    shared_ptr<thread> m_outboundThread;
    moodycamel::BlockingConcurrentQueue<WorkItem> m_inboundQueue;
    moodycamel::BlockingConcurrentQueue<WorkItem> m_outboundQueue;
    atomic<int> m_nextWorkItemId;
    atomic<bool> m_stopRequested;
public:
    Dispatcher() :
        m_stopRequested(false),
        m_nextWorkItemId(1)
    {
        printDebugString("DISPAT|INIT starting");

        m_outboundThread = shared_ptr<thread>(new thread([=](){
            runConsumerThread("sender", 1, m_outboundQueue);
        }));

        printDebugString("DISPAT|INIT completed");
    }

    ~Dispatcher()
    {
        stopNow();
    }

public:

    void enqueueOutbound(WorkItem workItem)
    {
        if (m_outboundQueue.size_approx() > 1000)
        {
            printDebugString("DISPAT|ERROR cannot encqueue outbound, queue full");
            return;
        }

        int workItemId = m_nextWorkItemId++;

        printDebugString("DISPAT|OUTBOUND enqueue work item %d", workItemId);

        m_outboundQueue.enqueue([=]() {
            printDebugString("DISPAT|OUTBOUND dequeue & execute work item %d", workItemId);
            try
            {
                workItem();
            }
            catch (const exception& e)
            {
                printDebugString("DISPAT|OUTBOUND work item[%d] CRASHED!!! %s", workItemId, e.what());
            }
        });
    }

    void enqueueInbound(WorkItem workItem)
    {
        if (m_inboundQueue.size_approx() > 1000)
        {
            printDebugString("DISPAT|ERROR cannot encqueue outbound, queue full");
            return;
        }

        int workItemId = m_nextWorkItemId++;

        printDebugString("DISPAT|INBOUND enqueue work item %d", workItemId);

        m_inboundQueue.enqueue([=]() {
            printDebugString("DISPAT|INBOUND dequeue & execute work item %d", workItemId);
            try
            {
                workItem();
            }
            catch (const exception& e)
            {
                printDebugString("DISPAT|INBOUND work item[%d] CRASHED!!! %s", workItemId, e.what());
            }
        });
    }

    void executePendingInboundOnCurrentThread()
    {
        WorkItem workItem;
        int count = 0;

        while (m_inboundQueue.try_dequeue(workItem) && count < 100)
        {
            workItem();
            count++;
        }
    }

    void beginStop()
    {
        m_stopRequested = true;
    }

    void stopNow()
    {
        m_stopRequested = true;

        if (m_outboundThread->joinable())
        {
            m_outboundThread->join();
        }
    }

private:

    void runConsumerThread(
        string name,
        int threadIndex,
        moodycamel::BlockingConcurrentQueue<WorkItem>& queue)
    {
        printDebugString("DISPAT|RUN thread[%s] index[%d] loop starting", name.c_str(), threadIndex);

        while (!m_stopRequested)
        {
            WorkItem workItem;
            if (queue.wait_dequeue_timed(workItem, chrono::milliseconds(5000)))
            {
                try
                {
                    workItem();
                }
                catch(const exception& e)
                {
                    printDebugString(
                        "DISPAT|RUN thread[%s] index[%d] work item CRASHED!!! %s",
                        name.c_str(), threadIndex, e.what());
                }
            }
        }

        printDebugString("DISPAT|RUN thread[%s] index[%d] loop stopped", name.c_str(), threadIndex);
    }
};
