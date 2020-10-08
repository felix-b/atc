// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <iostream>
#include <functional>
#include <utility>
#include <memory>
#include <thread>
#include <future>

// SDK
#include "XPLMPlugin.h"
#if !XPLM300
#error This plugin requires version 300 of the SDK
#endif

// PPL 
#include "owneddata.h"

// tnc
#include "utils.h"
#include "libworld.h"
#include "libai.hpp"
#if IBM
#include "libserver.hpp"
#endif
#include "pluginHostServices.hpp"
#include "pluginMenu.hpp"
#include "nativeTextToSpeechService.hpp"
#include "simplePhraseologyService.hpp"
#include "xpmp2AircraftObjectService.hpp"
#include "xplmSpeakStringTtsService.hpp"
#include "pluginWorldLoader.hpp"
#include "pluginScheduleLoader.hpp"

using namespace std;
using namespace PPL;

//const char* poc_resource_directory = "/Users/felixb/Desktop/xp/Resources/plugins/tnc/Resources";
//static string resourceDirectory = "/Users/felixb/Desktop/xp/Resources/plugins/tnc/Resources";//PluginPath::prependPluginResourcesPath("");

class PluginInstance
{
private:

    enum class PluginStateId
    {
        Stopped = 0,
        WorldAssembling = 1,
        WorldAssembled = 2,
        SchedulesStarting = 3,
        SchedulesStarted = 4,
        Failed = 5
    };

    class PluginState
    {
    private:
        PluginStateId m_id;
        string m_name;
    protected:
        PluginState(PluginStateId _id, const string& _name) :
            m_id(_id),
            m_name(_name)
        {
        }
    public:
        PluginStateId id() const { return m_id; }
        const string& name() const { return m_name; }
    public:
        virtual void enter()
        {
        }
        virtual void exit()
        {
        }
        virtual void ping()
        {
        }
    };

    class StoppedState : public PluginState
    {
    private:
        shared_ptr<HostServices> m_host;
    public:
        StoppedState(shared_ptr<HostServices> _host) :
            PluginState(PluginStateId::Stopped, "STOPPED"),
            m_host(std::move(_host))
        {
        }
    public:
        void enter() override
        {
            stopServer();
        }
    private:
        void stopServer()
        {
#if IBM
            auto server = m_host->services().get<server::ServerControllerInterface>();

            if (server->running())
            {
                m_host->writeLog("PLUGIN|stopping the server");
                server->beginStop();
                if (server->waitUntilStopped(chrono::seconds(5)))
                {
                    m_host->writeLog("PLUGIN|server successfully stopped");
                }
                else
                {
                    m_host->writeLog("PLUGIN|WARNING: timeout waiting for server to stop");
                }
            }
#endif
        }
    };

    class FailedState : public PluginState
    {
    public:
        FailedState() :
            PluginState(PluginStateId::Failed, "FAILED")
        {
        }
    };

    class WorldAssemblingState : public PluginState
    {
    private:
        shared_ptr<PluginHostServices> m_host;
        future<shared_ptr<World>> m_worldFuture;
        atomic<bool> m_done;
        PluginMenu::Item m_assemblingItem;
        function<void(shared_ptr<World> world)> m_onAssembled;
        function<void()> m_onFailed;
    public:
        WorldAssemblingState(
            shared_ptr<PluginHostServices> _host,
            PluginMenu& _menu,
            function<void(shared_ptr<World> world)> _onAssembled,
            function<void()> _onFailed
        ) : PluginState(PluginStateId::WorldAssembling, "WORLD-ASSEMBLING"),
            m_host(std::move(_host)),
            m_assemblingItem(_menu, "World is being assembled, please wait...", [](){}),
            m_onAssembled(std::move(_onAssembled)),
            m_onFailed(std::move(_onFailed)),
            m_done(false)
        {
        }

    public:

        void enter() override
        {
            m_worldFuture = std::async(std::launch::async, [this] {
                try
                {
                    auto world = assembleWorld();
                    if (world)
                    {
                        m_host->useWorld(world);
                        startServer();
                    }
                    return world;
                }
                catch (const exception& e)
                {
                    m_host->writeLog("PLUGIN|WorldAssemblingState background task CRASHED!!! %s", e.what());
                    return shared_ptr<World>();
                }
            });
        }

        void exit() override
        {
            if (!m_done && m_worldFuture.wait_for(chrono::milliseconds(0)) != future_status::ready)
            {
                m_host->writeLog("PLUGIN|WARNING: leaving WORLD-ASSEMBLING state while the assembly is still in progress!");
            }
        }

        void ping() override
        {
            if (m_worldFuture.wait_for(chrono::milliseconds(0)) != future_status::ready)
            {
                m_host->writeLog("PLUGIN|ping WORLD-ASSEMBLING: in progress");
            }
            else if (m_worldFuture.valid())
            {
                m_host->writeLog("PLUGIN|ping WORLD-ASSEMBLING: done");
                auto world = m_worldFuture.get();
                m_done = true;

                if (world)
                {
                    m_onAssembled(world);
                }
                else
                {
                    m_host->writeLog("PLUGIN|ERROR: world was not assembled - plugin will not function (see previous errors).");
                    m_onFailed();
                }
            }
        }

    private:

        shared_ptr<World> assembleWorld()
        {
            try
            {
                PluginWorldLoader loader(m_host);
                loader.loadWorld();
                return loader.getWorld();
            }
            catch (const exception& e)
            {
                m_host->writeLog("PLUGIN|assembleWorld CRASHED!!! %s", e.what());
                return shared_ptr<World>();
            }
        }

        void startServer()
        {
#if IBM
            try
            {
                auto server = m_host->services().get<server::ServerControllerInterface>();

                if (!server->running())
                {
                    m_host->writeLog("PLUGIN|starting the server");
                    server->start(9002);
                }
            }
            catch (const exception& e)
            {
                m_host->writeLog("PLUGIN|startServer CRASHED!!! %s", e.what());
            }
#endif
        }
    };

    class WorldAssembledState : public PluginState
    {
    private:
        shared_ptr<HostServices> m_host;
        PluginMenu::Item m_startSchedulesItem;
    public:
        WorldAssembledState(
            shared_ptr<HostServices> _host,
            PluginMenu& _menu,
            function<void()> _onStartSchedules
        ) : PluginState(PluginStateId::WorldAssembled, "WORLD-ASSEMBLED"),
            m_host(std::move(_host)),
            m_startSchedulesItem(_menu, "Start Schedules", _onStartSchedules)
        {
        }
    };

    class SchedulesStartingState : public PluginState
    {
    private:
        shared_ptr<HostServices> m_host;
        shared_ptr<World> m_world;
        PluginMenu::Item m_worldIsStarting;
        function<void(shared_ptr<Airport> userAirport)> m_onStarted;
        shared_ptr<Airport> m_userAirport;
    public:
        SchedulesStartingState(
            shared_ptr<HostServices> _host,
            shared_ptr<World> _world,
            PluginMenu& _menu,
            function<void(shared_ptr<Airport> userAirport)> _onStarted
        ) : PluginState(PluginStateId::SchedulesStarting, "SCHEDULES-STARTING"),
            m_host(std::move(_host)),
            m_world(_world),
            m_worldIsStarting(_menu, "Starting schedules, please wait...", [](){}),
            m_onStarted(std::move(_onStarted))
        {
        }

        void enter() override
        {
            PluginScheduleLoader scheduleLoader(m_host, m_world);
            scheduleLoader.loadSchedules();

            m_userAirport = scheduleLoader.airport();

            WorldBuilder::tidyAirportElevations(m_host, m_userAirport);
            logUserAirportElevations();
        }

        void ping() override
        {
            m_onStarted(m_userAirport);
        }

    private:

        void logUserAirportElevations()
        {
            const auto logRunwayEndElevation = [this](const Runway::End& end) {
                const auto& centerlinePoint = end.centerlinePoint().geo();
                m_host->writeLog(
                    "PLUGIN|User airport elevations [%s/%s] at (%f,%f) elevation [%f]",
                    m_userAirport->header().icao().c_str(),
                    end.name().c_str(),
                    centerlinePoint.latitude,
                    centerlinePoint.longitude,
                    end.elevationFeet());
            };

            for (const auto& runway : m_userAirport->runways())
            {
                logRunwayEndElevation(runway->end1());
                logRunwayEndElevation(runway->end2());
            }
        }
    };

    class SchedulesStartedState : public PluginState
    {
    private:
        shared_ptr<HostServices> m_host;
        shared_ptr<World> m_world;
        shared_ptr<AircraftObjectService> m_aircraftObjectService;
        chrono::time_point<chrono::high_resolution_clock, chrono::microseconds> m_lastTickTime;
        uint64_t m_timeFactor;
        shared_ptr<Airport> m_userAirport;

        PluginMenu::Item m_stopSchedulesItem;
        PluginMenu::Item m_restartSchedulesItem;
        PluginMenu::Item m_tuneClearanceItem;
        PluginMenu::Item m_tuneGroundItem;
        PluginMenu::Item m_tuneTowerItem;
        PluginMenu::Item m_time1XItem;
        PluginMenu::Item m_time10XItem;
        PluginMenu::Item m_time20XItem;
        DataRef<int> m_com1FrequencyKhz;
    public:
        SchedulesStartedState(
            shared_ptr<HostServices> _host,
            shared_ptr<World> _world,
            PluginMenu& _menu,
            shared_ptr<Airport> _userAirport,
            function<void()> _onStopSchedules,
            function<void()> _onRestartSchedules
        ) : PluginState(PluginStateId::SchedulesStarted, "SCHEDULES-STARTED"),
            m_host(std::move(_host)),
            m_world(std::move(_world)),
            m_userAirport(_userAirport),
            m_stopSchedulesItem(_menu, "Stop schedules", std::move(_onStopSchedules)),
            m_restartSchedulesItem(_menu, "Restart schedules", std::move(_onRestartSchedules)),
            m_tuneClearanceItem(_menu, "COM1 " + _userAirport->header().icao() + " CLR/DEL", [this] { tuneToClearance(); }),
            m_tuneGroundItem(_menu, "COM1 " + _userAirport->header().icao() + " GND", [this] { tuneToGround(); }),
            m_tuneTowerItem(_menu, "COM1 " + _userAirport->header().icao() + " TWR", [this] { tuneToTower(); }),
            m_time1XItem(_menu, "Time X 1", [=](){ m_timeFactor = 1; }),
            m_time10XItem(_menu, "Time X 10", [=](){ m_timeFactor = 10; }),
            m_time20XItem(_menu, "Time X 20", [=](){ m_timeFactor = 20; }),
            m_com1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadWrite)
        {
            m_aircraftObjectService = m_host->services().get<AircraftObjectService>();
            m_lastTickTime = getNow();
            m_timeFactor = 1;
        }

        void enter() override
        {
        }

        void ping() override
        {
            auto now = getNow();
            auto microsecondsSinceLastTick = (now - m_lastTickTime) * m_timeFactor;
            auto newWorldTimestamp = m_world->timestamp() + microsecondsSinceLastTick;
            m_lastTickTime = now;

            m_world->progressTo(newWorldTimestamp);
            auto changeSet = m_world->hasChanges()
                             ? m_world->takeChanges()
                             : nullptr;

            if (changeSet)
            {
                processWorldChanges(changeSet);
            }
        }

        void exit() override
        {
            m_world->clearAllFlights();
        }

    private:

        void processWorldChanges(shared_ptr<World::ChangeSet> changeSet)
        {
            m_aircraftObjectService->processEvents(changeSet);
        }

        void tuneToClearance()
        {
            auto clearance = m_userAirport->clearanceDeliveryAt(m_userAirport->header().datum());
            m_com1FrequencyKhz = clearance->frequency()->khz();
        }

        void tuneToGround()
        {
            auto ground = m_userAirport->groundAt(m_userAirport->header().datum());
            m_com1FrequencyKhz = ground->frequency()->khz();
        }

        void tuneToTower()
        {
            auto tower = m_userAirport->localAt(m_userAirport->header().datum());
            m_com1FrequencyKhz = tower->frequency()->khz();
        }
    };

private:
    PluginMenu m_menu;
    shared_ptr<PluginState> m_currentState;
    shared_ptr<PluginHostServices> m_hostServices;
    shared_ptr<World> m_world;
    DataRef<double> m_userAircraftLatitude;
    DataRef<double> m_userAircraftLongitude;
public:
    PluginInstance() :
        m_menu("Air Traffic & Control"),
        m_userAircraftLatitude("sim/flightmodel/position/latitude", PPL::ReadOnly),
        m_userAircraftLongitude("sim/flightmodel/position/longitude", PPL::ReadOnly)
    {
        PrintDebugString("PLUGIN|initializing PluginInstance");

        m_hostServices = createHostServices();
        transitionToState([this]() { return createWorldAssemblingState(); });
        XPLMRegisterFlightLoopCallback(&pluginFlightLoopCallback, -1.0, this);
    }

    ~PluginInstance()
    {
        PrintDebugString("PLUGIN|destroying PluginInstance");

        XPLMUnregisterFlightLoopCallback(&pluginFlightLoopCallback, this);
        if (m_currentState->id() != PluginStateId::Stopped)
        {
            transitionToState([this]() { return createStoppedState(); });
        }
    }

public:

    void notifyAirportLoaded()
    {
        try
        {
            switch (m_currentState->id())
            {
            case PluginStateId::WorldAssembling:
                PrintDebugString("PLUGIN|notifyAirportLoaded pinging state [%s]", m_currentState->name().c_str());
                m_currentState->ping();
                break;
            case PluginStateId::SchedulesStarted:
                transitionToState([this] {
                    return createSchedulesStartingState();
                });
                break;
            }
        }
        catch (const exception& e)
        {
            m_hostServices->writeLog("PLUGIN|notifyAirportLoaded CRASHED!!! %s", e.what());
        }
    }

private:

    shared_ptr<PluginHostServices> createHostServices()
    {
#if APL
        XPLMDebugString("------ BEGIN ALTEST ------\n");
        NativeTextToSpeechService::init_sound();
        XPLMDebugString("------ END ALTEST ------\n");
#endif

        auto hostServices = shared_ptr<PluginHostServices>(new PluginHostServices());

#if LIN
        auto pluginTts = shared_ptr<XPLMSpeakStringTtsService>(new XPLMSpeakStringTtsService(hostServices));
#else
        auto pluginTts = shared_ptr<NativeTextToSpeechService>(new NativeTextToSpeechService(hostServices));
#endif

        auto phraseologyService = shared_ptr<PhraseologyService>(new SimplePhraseologyService(hostServices));
        auto aircraftObjectService = shared_ptr<Xpmp2AircraftObjectService>(new Xpmp2AircraftObjectService(hostServices));
        auto intentFactory = shared_ptr<IntentFactory>(new IntentFactory(hostServices));

        hostServices->services().use<AircraftObjectService>(aircraftObjectService);
        hostServices->services().use<TextToSpeechService>(pluginTts);
        hostServices->services().use<IntentFactory>(intentFactory);
        hostServices->services().use<PhraseologyService>(phraseologyService);

#if IBM
        auto serverController = server::ServerControllerInterface::create(hostServices);
        hostServices->services().use<server::ServerControllerInterface>(serverController);
#endif

        ai::contributeComponents(hostServices);

        hostServices->writeLog("PLUGIN|host services initialized");
        return hostServices;
    }

    void transitionToState(function<shared_ptr<PluginState>()> factory)
    {
        if (m_currentState)
        {
            try
            {
                m_hostServices->writeLog("PLUGIN|exiting state[%s]", m_currentState->name().c_str());
                m_currentState->exit();
                m_currentState.reset();
            }
            catch (const exception &e)
            {
                PrintDebugString("PLUGIN|CRASHED while transitioning state (exit)!!! %s", e.what());
            }
        }

        try
        {
            m_currentState = factory();

            if (m_currentState)
            {
                m_hostServices->writeLog("PLUGIN|entering state[%s]", m_currentState->name().c_str());
                m_currentState->enter();
                m_hostServices->writeLog("PLUGIN|transitioned to state[%s]", m_currentState->name().c_str());
            }
        }
        catch (const exception &e)
        {
            PrintDebugString("PLUGIN|CRASHED while transitioning state (enter)!!! %s", e.what());
        }
    }

    shared_ptr<PluginState> createStoppedState()
    {
        return make_shared<StoppedState>(m_hostServices);
    }

    shared_ptr<PluginState> createWorldAssemblingState()
    {
        const auto onAssembled = [this](shared_ptr<World> world) {
            m_hostServices->writeLog("PLUGIN|createWorldAssemblingState");
            m_world = world;
            transitionToState([this]() {
                return createSchedulesStartingState();
            });
        };

        const auto onFailed = [this] {
            transitionToState([this]() {
                return createFailedState();
            });
        };

        return make_shared<WorldAssemblingState>(m_hostServices, m_menu, onAssembled, onFailed);
    }

    shared_ptr<PluginState> createWorldAssembledState()
    {
        return make_shared<WorldAssembledState>(m_hostServices, m_menu, [this]() {
            transitionToState([this]() {
                return createSchedulesStartingState();
            });
        });
    }

    shared_ptr<PluginState> createSchedulesStartingState()
    {
        return make_shared<SchedulesStartingState>(
            m_hostServices,
            m_world,
            m_menu,
            [this](shared_ptr<Airport> userAirport) {
                transitionToState([this, userAirport]() {
                    return createSchedulesStartedState(userAirport);
                });
            }
        );
    }

    shared_ptr<PluginState> createSchedulesStartedState(shared_ptr<Airport> userAirport)
    {
        return make_shared<SchedulesStartedState>(
            m_hostServices,
            m_world,
            m_menu,
            userAirport,
            [this]() {
                transitionToState([this]() {
                    return createWorldAssembledState();
                });
            },
            [this]() {
                transitionToState([this]() {
                    return createSchedulesStartingState();
                });
            }
        );
    }

    shared_ptr<PluginState> createFailedState()
    {
        return make_shared<FailedState>();
    }

    void flightLoopTick()
    {
        if (!m_currentState)
        {
            return;
        }

        try
        {
            m_currentState->ping();
        }
        catch(const exception& e)
        {
            m_hostServices->writeLog(
                "PLUGIN|flightLoopTick ping state[%s] CRASHED!!! %s",
                m_currentState->name().c_str(),
                e.what());
        }
    }

private:

    static chrono::time_point<chrono::high_resolution_clock, chrono::microseconds> getNow()
    {
        return std::chrono::time_point_cast<std::chrono::microseconds>(chrono::high_resolution_clock::now());
    }

    static float pluginFlightLoopCallback(
        float  inElapsedSinceLastCall,
        float  inElapsedTimeSinceLastFlightLoop,
        int    inCounter,
        void * inRefcon)
    {
        if (inRefcon)
        {
            static_cast<PluginInstance*>(inRefcon)->flightLoopTick();
        }

        return -1;
    }

};
