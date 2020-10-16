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
            typedef function<void(vector<string>& departure, vector<string>& arrival)> OnSelectActiveRunways;
        private:
            OnSelectActiveRunways m_onSelectActiveRunways;
        public:
            TestAIController(shared_ptr<HostServices> _host, int _id, const string& _name, shared_ptr<ControllerPosition> _position) : 
                Controller(_host, _id, Actor::Gender::Female, _position),
                m_onSelectActiveRunways(noopSelectActiveRunways)
            {
            }
        public:
            void receiveIntent(shared_ptr<Intent> intent) override
            {
            }
            void progressTo(chrono::microseconds timestamp) override
            {
            }
            void selectActiveRunways(vector<string>& departure, vector<string>& arrival) override
            {
                m_onSelectActiveRunways(departure, arrival);
            }
            void onSelectActiveRunways(OnSelectActiveRunways callback)
            {
                m_onSelectActiveRunways = callback;
            }
        public:
            static void noopSelectActiveRunways(vector<string>& departure, vector<string>& arrival)
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
        class TestAIAircraft : public Aircraft
        {
        public:
            TestAIAircraft(
                shared_ptr<HostServices> _host,
                int _id,
                const string& _modelIcao,
                const string& _airlineIcao,
                const string& _tailNo,
                Category _category
            ) : Aircraft(
                _host,
                _id,
                Actor::Nature::AI,
                _modelIcao,
                _airlineIcao,
                _tailNo,
                _category)
            {
            }
        public:
            const GeoPoint& location() const override { throw runtime_error("TestAIAircraft"); }
            const AircraftAttitude& attitude() const override { throw runtime_error("TestAIAircraft"); }
            double track() const override { throw runtime_error("TestAIAircraft"); }
            const Altitude& altitude() const override { throw runtime_error("TestAIAircraft"); }
            double groundSpeedKt() const override { throw runtime_error("TestAIAircraft"); }
            double verticalSpeedFpm() const override { throw runtime_error("TestAIAircraft"); }
            const string& squawk() const override { throw runtime_error("TestAIAircraft"); }
            LightBits lights() const override { throw runtime_error("TestAIAircraft"); }
            bool isLightsOn(LightBits bits) const override { throw runtime_error("TestAIAircraft"); }
            float gearState() const override { throw runtime_error("TestAIAircraft"); }
            float flapState() const override { throw runtime_error("TestAIAircraft"); }
            float spoilerState() const override { throw runtime_error("TestAIAircraft"); }
            bool justTouchedDown(chrono::microseconds timestamp) override { throw runtime_error("TestAIAircraft"); }
            void park(shared_ptr<ParkingStand> parkingStand) override { throw runtime_error("TestAIAircraft"); }
            void setOnFinal(const Runway::End& runwayEnd) override { throw runtime_error("TestAIAircraft"); }
            void notifyChanges() override {}
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
        int m_nextAIAircraftId = 1;
        shared_ptr<TestAircraftObjectService> m_aircraftObjectService;
        shared_ptr<TestTtsService> m_textToSpeechService;
        vector<shared_ptr<TestAIController>> m_createdAIControllers;
        vector<shared_ptr<TestAIPilot>> m_createdAIPilots;
        shared_ptr<World> m_world;
        bool m_quiet;
    public:
        TestHostServices() :
            TestHostServices(false)
        {
        }
        explicit TestHostServices(bool shouldWriteLogs) :
            m_quiet(!shouldWriteLogs),
            m_geoToLocal(defaultGeoToLocal),
            m_localToGeo(defaultLocalToGeo)
        {
            if (shouldWriteLogs)
            {
                HostServices::initLogString();
            }
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
        shared_ptr<Aircraft> createAIAircraft(
            const string& modelIcao,
            const string& operatorIcao,
            const string& tailNo,
            Aircraft::Category category) override
        {
            int id = m_nextAIAircraftId++;
            auto aircraft = shared_ptr<Aircraft>(new TestAIAircraft(
                shared_from_this(), id, modelIcao, operatorIcao, tailNo, category));
            return aircraft;
        }
        void writeLog(const char* format, ...) override
        {
            if (m_quiet)
            {
                return;
            }
            char buffer[512];
            va_list args;
            va_start(args, format);
            HostServices::formatLogString(buffer, format, args);
            va_end(args);
            cout << buffer;
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
        string getHostFilePath(const vector<string>& relativePathParts) override
        {
            string fullPath = "HOST_DIR";
            for (const string& part : relativePathParts)
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
            return createWithLogs(false);
        }
        static shared_ptr<TestHostServices> createWithLogs(bool shouldWriteLogs = true)
        {
            auto testHost = make_shared<TestHostServices>(shouldWriteLogs);
            testHost->initializeServices(testHost);
            return testHost;
        }
        static shared_ptr<TestHostServices> createWithWorld(bool shouldWriteLogs = false)
        {
            auto testHost = createWithLogs(shouldWriteLogs);
            testHost->useWorld(shared_ptr<World>(new World(testHost, 0)));
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
