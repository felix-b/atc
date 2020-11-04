//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include "libworld.h"
#include "clearanceFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "aiControllerBase.hpp"
#include "libai.hpp"

using namespace std;
using namespace world;

namespace ai
{
    enum class ResourceUsage
    {
        Unknown = 0,
        Takeoff = 10,
        Landing = 20,
        Taxi = 30,
        EnRoute = 40,
        Disabled = 50, // aircraft or vehicle
        Maintenance = 60,
        Emergency = 70
    };

    class ResourceAllocation
    {
    private:
        ResourceUsage m_usage;
        function<shared_ptr<Clearance>()> m_onClearedToStart;
        shared_ptr<Clearance> m_clearance;
        chrono::microseconds m_plannedStartTimestamp;
        chrono::microseconds m_plannedVacationTimestamp;
        chrono::microseconds m_clearedToStartTimestamp;
        chrono::microseconds m_actualStartTimestamp;
        chrono::microseconds m_actualVacationTimestamp;
    protected:
        ResourceAllocation(
            ResourceUsage _usage,
            chrono::microseconds _plannedStartTimestamp,
            chrono::microseconds _plannedVacationTimestamp,
            function<shared_ptr<Clearance>()> _onClearedToStart
        ) : m_usage(_usage),
            m_onClearedToStart(_onClearedToStart),
            m_plannedStartTimestamp(_plannedStartTimestamp),
            m_plannedVacationTimestamp(_plannedVacationTimestamp)
        {
        }
    public:
        ResourceUsage usage() const { return m_usage; }
        shared_ptr<Clearance> clearance() const { return m_clearance; }
        chrono::microseconds plannedStartTimestamp() const { return m_plannedStartTimestamp; }
        chrono::microseconds plannedVacationTimestamp() const { return m_plannedVacationTimestamp; }
    public:
        virtual void clearToStart() = 0;
        virtual bool vacated() = 0;
        virtual void ping(bool vacateImmediately, function<void(bool willVacate)> pong) = 0;
        virtual chrono::microseconds getEstimatedVacationTimestamp() = 0;
        virtual bool willConflictWithUsage(ResourceUsage usage) = 0;
    };

    class ResourceAllocator
    {
    private:
        vector<shared_ptr<ResourceAllocation>> m_allocations;
    public:
        ResourceAllocator()
        {
        }
    public:

    };
}
