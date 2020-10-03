// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <memory>
#include <functional>
#include "libworld.h"

// #define LOCAL(host, x, y, z) UniPoint::fromLocal((host), {(x), (y), (z)})
// #define GEO(host, lat, lon, alt) UniPoint::fromGeo((host), {(lat), (lon), (alt)})

namespace world
{
    class TestHostServices : 
        public HostServices, 
        public enable_shared_from_this<TestHostServices>
    {
    public:
        class TestAIController : public Controller
        {
        public:
            TestAIController(shared_ptr<HostServices> _host, int _id, const string& _name, shared_ptr<ControllerPosition> _position) : 
                Controller(_host, _id, Actor::Gender::Female, _position)
            {
            }
        public:
            void receiveIntent(shared_ptr<Intent> intent) override
            {
            }
            void progressTo(chrono::microseconds timestamp) override
            {
            }
        };
        class TestAIPilot : public Pilot
        {
        public:
            TestAIPilot(shared_ptr<HostServices> _host, int _id, const string& _name, shared_ptr<Flight> _flight) :
                Pilot(_host, _id, Actor::Gender::Male, _flight)
            {
            }
        public:
            void progressTo(chrono::microseconds timestamp) override
            {
            }
            shared_ptr<Maneuver> getFlightCycle() override
            {
                return nullptr;
            }
            shared_ptr<Maneuver> getFinalToGate(const Runway::End& landingRunway) override
            {
                return nullptr;
            }
        };
        class TestAircraftObjectService : public AircraftObjectService
        {
        private:
            shared_ptr<World::ChangeSet> m_lastChangeSet;
            int m_callCount_clearAll = 0;
        public:
            void processEvents(shared_ptr<World::ChangeSet> changeSet) override
            {
                m_lastChangeSet = changeSet;
            }
            void clearAll() override
            {
                m_callCount_clearAll++;
            }
        public:
            shared_ptr<World::ChangeSet> lastChangeSet() const { return m_lastChangeSet; }
            int callCount_clearAll() const { return m_callCount_clearAll; }
        };
        class TestTtsService : public TextToSpeechService
        {
        private:
            vector<shared_ptr<Transmission>> m_transmissionHistory;
            int m_callCount_clearAll = 0;
        public:
            QueryCompletion vocalizeTransmission(shared_ptr<Frequency> frequency, shared_ptr<Transmission> transmission) override
            {
                m_transmissionHistory.push_back(transmission);
                return []{
                    return true;
                };
            }
            void clearAll() override
            {
                m_callCount_clearAll++;
            }
        public:
            vector<shared_ptr<Transmission>> takeTransmissionHistory()
            {
                vector<shared_ptr<Transmission>> result = m_transmissionHistory;
                m_transmissionHistory.clear();
                return result;
            }
        public:
            const vector<shared_ptr<Transmission>>& transmissionHistory() const { return m_transmissionHistory; }
            int callCount_clearAll() const { return m_callCount_clearAll; }
        };
    private:
        function<float(double geo)> m_geoToLocal;
        function<double(float local)> m_localToGeo;
        int m_nextAIControllerId = 1;
        int m_nextAIPilotId = 1;
        shared_ptr<TestAircraftObjectService> m_aircraftObjectService;
        shared_ptr<TestTtsService> m_textToSpeechService;
        vector<shared_ptr<TestAIController>> m_createdAIControllers;
        vector<shared_ptr<TestAIPilot>> m_createdAIPilots;
        shared_ptr<World> m_world;
    public:
        TestHostServices() :
            m_geoToLocal(defaultGeoToLocal),
            m_localToGeo(defaultLocalToGeo)
        {
        }
    public:
        shared_ptr<World> getWorld() override
        {
            if (m_world)
            {
                return m_world;
            }
            throw runtime_error("TestHostServices::getWorld() failed: world was not injected - call useWorld()");
        }
        LocalPoint geoToLocal(const GeoPoint& geo) override
        {
            return LocalPoint({
                m_geoToLocal(geo.longitude),
                m_geoToLocal(geo.altitude),
                m_geoToLocal(geo.latitude)
            });
        }
        GeoPoint localToGeo(const LocalPoint& local) override
        {
            return GeoPoint({
                m_localToGeo(local.z),
                m_localToGeo(local.x),
                m_localToGeo(local.y)
            });
        }
        int getNextRandom(int maxValue) override
        {
            return 123;
        }
        float queryTerrainElevationAt(const GeoPoint& location) override
        {
            return 123;
        }
        shared_ptr<Controller> createAIController(shared_ptr<ControllerPosition> position) override
        {
            int id = m_nextAIControllerId++;
            string name = "ai-controller-" + to_string(id);
            auto controller = shared_ptr<TestAIController>(new TestAIController(shared_from_this(), id, name, position));
            m_createdAIControllers.push_back(controller);
            return controller;
        }
        shared_ptr<Pilot> createAIPilot(shared_ptr<Flight> flight) override
        {
            int id = m_nextAIPilotId++;
            string name = "ai-pilot-" + to_string(id);
            auto pilot = shared_ptr<TestAIPilot>(new TestAIPilot(shared_from_this(), id, name, flight));
            m_createdAIPilots.push_back(pilot);
            return pilot;
        }
        void writeLog(const char* format, ...) override
        {
        }
        string getResourceFilePath(const vector<string>& relativePathParts) override
        {
            string fullPath = "PLUGIN_DIR";
            for (const string& part : relativePathParts)
            {
                fullPath.append("/");
                fullPath.append(part);
            }
            return fullPath;
        }
        string getHostFilePath(int numFoldersUp, const vector<string>& downPathParts) override
        {
            string fullPath = "PLUGIN_DIR";
            for (int i = 0 ; i < numFoldersUp ; i++)
            {
                fullPath.append("/..");
            }
            for (const string& part : downPathParts)
            {
                fullPath.append("/");
                fullPath.append(part);
            }
            return fullPath;
        }
        shared_ptr<istream> openFileForRead(const string& filePath) override
        {
            return shared_ptr<istream>(new stringstream());
        }
    public:
        void useWorld(shared_ptr<World> _world)
        {
            m_world = _world;
            m_world->onQueryTerrainElevation([](const GeoPoint& location){
                return 123.0;
            });
        }
        const vector<shared_ptr<TestAIController>>& createdAIControllers() const
        { 
            return m_createdAIControllers; 
        }
        const vector<shared_ptr<TestAIPilot>>& createdAIPilots() const
        {
            return m_createdAIPilots;
        }
    public:
        shared_ptr<TestAircraftObjectService> aircraftObjectService() const { return m_aircraftObjectService; }
        shared_ptr<TestTtsService> textToSpeechService() const { return m_textToSpeechService; }
    private:
        void initializeServices(shared_ptr<TestHostServices> me)
        {
            m_aircraftObjectService = make_shared<TestAircraftObjectService>();
            m_textToSpeechService = make_shared<TestTtsService>();
            services().use<AircraftObjectService>(m_aircraftObjectService);
            services().use<TextToSpeechService>(m_textToSpeechService);
        }
    public:
        static shared_ptr<TestHostServices> create()
        {
            auto testHost = make_shared<TestHostServices>();
            testHost->initializeServices(testHost);
            return testHost;
        }
    private:
        static float defaultGeoToLocal(double geo) 
        {
            return geo * 100;
        }
        static double defaultLocalToGeo(float local)
        {
            return local / 1000;
        }
    };
}
