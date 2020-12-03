// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <memory>
#include <functional>
#include <fstream>
#include "libworld.h"
#include "intentFactory.hpp"
#include "clearanceTypes.hpp"

// #define LOCAL(host, x, y, z) UniPoint::fromLocal((host), {(x), (y), (z)})
// #define GEO(host, lat, lon, alt) UniPoint::fromGeo((host), {(lat), (lon), (alt)})

namespace world
{
    class TestHostServices : 
        public HostServices, 
        public enable_shared_from_this<TestHostServices>
    {
    public:
        typedef function<shared_ptr<Airport>(shared_ptr<TestHostServices> host)> AirportFactoryCallback;
        class TestAIController : public Controller
        {
        public:
            typedef function<void(vector<string>& departure, vector<string>& arrival)> OnSelectActiveRunways;
            typedef function<void(shared_ptr<Intent> intent)> OnReceiveIntent;
        private:
            OnSelectActiveRunways m_onSelectActiveRunways;
            OnReceiveIntent m_onReceiveIntent;
        public:
            TestAIController(shared_ptr<HostServices> _host, int _id, const string& _name, shared_ptr<ControllerPosition> _position) : 
                Controller(_host, _id, Actor::Gender::Female, _position),
                m_onSelectActiveRunways(noopSelectActiveRunways),
                m_onReceiveIntent(noopReceiveIntent)
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
            void clearFlights() override
            {
            }
        public:
            void onSelectActiveRunways(OnSelectActiveRunways callback)
            {
                m_onSelectActiveRunways = callback;
            }
            void onReceiveIntent(OnReceiveIntent callback)
            {
                m_onReceiveIntent = callback;
            }
        public:
            static void noopSelectActiveRunways(vector<string>& departure, vector<string>& arrival)
            {
            }
            static void noopReceiveIntent(shared_ptr<Intent> intent)
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
        private:
            GeoPoint m_location;
            Altitude m_altitude;
            LightBits m_lights = LightBits::None;
            double m_verticalSpeedFpm = 0;
            double m_groundSpeedKt = 0;
            string m_squawk;
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
                    _category
                ),
                m_altitude(Altitude::ground()),
                m_location(0,0)
            {
            }
        public:
            const Altitude& altitude() const override { return m_altitude; }
            void setAltitude(const Altitude& _altitude) { m_altitude = _altitude; }

            const GeoPoint& location() const override { return m_location; }
            void setLocation(const GeoPoint& _location) { m_location  = _location; }

            LightBits lights() const override { return m_lights; }
            void setLights(LightBits _lights) { m_lights = _lights; }
            bool isLightsOn(LightBits bits) const override
            {
                return ((m_lights & bits) == bits);
            }

            double verticalSpeedFpm() const override { return m_verticalSpeedFpm; }
            void setVerticalSpeedFpm(double value) { m_verticalSpeedFpm = value;}

            double groundSpeedKt() const override { return m_groundSpeedKt; }
            void setGroundSpeedKt(double value) { m_groundSpeedKt = value;}

            const string& squawk() const override { return m_squawk; }
            void setSquawk(const string& value) { m_squawk = value; }

            const AircraftAttitude& attitude() const override { throw runtime_error("TestAIAircraft"); }
            double track() const override { throw runtime_error("TestAIAircraft"); }
            float gearState() const override { throw runtime_error("TestAIAircraft"); }
            float flapState() const override { throw runtime_error("TestAIAircraft"); }
            float spoilerState() const override { throw runtime_error("TestAIAircraft"); }
            bool justTouchedDown(chrono::microseconds timestamp) override { throw runtime_error("TestAIAircraft"); }
            void park(shared_ptr<ParkingStand> parkingStand) override { throw runtime_error("TestAIAircraft"); }
            void setOnFinal(const Runway::End& runwayEnd) override { throw runtime_error("TestAIAircraft"); }
            void notifyChanges() override {}
        };
        struct TestFlight
        {
        public:
            shared_ptr<Flight> ptr;
            shared_ptr<TestAIPilot> pilot;
            shared_ptr<TestAIAircraft> aircraft;
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
        chrono::milliseconds m_timeForLog;
        bool m_timeForLogWasSet;
    public:
        TestHostServices() :
            TestHostServices(false)
        {
        }
        explicit TestHostServices(bool shouldWriteLogs) :
            m_quiet(!shouldWriteLogs),
            m_geoToLocal(defaultGeoToLocal),
            m_localToGeo(defaultLocalToGeo),
            m_timeForLog(chrono::milliseconds(0)),
            m_timeForLogWasSet(false)
        {
            HostServices::initLogString();
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
            chrono::milliseconds timestamp = m_timeForLogWasSet ? m_timeForLog : HostServices::getLogTimestamp();
            char buffer[512];
            va_list args;
            va_start(args, format);
            HostServices::formatLogString(timestamp, buffer, format, args);
            va_end(args);
            cout << buffer;
            cout.flush();
        }

        string pathAppend(const string &rootPath, const vector<string>& relativePathParts)
        {
            string fullPath = rootPath;
            for (const string& part : relativePathParts)
            {
                fullPath.append("/");
                fullPath.append(part);
            }
            return fullPath;
        }

        string getResourceFilePath(const vector<string>& relativePathParts) override
        {
            return pathAppend("PLUGIN_DIR", relativePathParts);
        }
        string getHostFilePath(const vector<string>& relativePathParts) override
        {
            return pathAppend("HOST_DIR", relativePathParts);
        }
        vector<string> findFilesInHostDirectory(const vector<string>& relativePathParts) override
        {
            return {};
        }
        shared_ptr<istream> openFileForRead(const string& filePath) override
        {
            auto file = shared_ptr<ifstream>(new ifstream());
            file->exceptions(ifstream::failbit | ifstream::badbit);
            file->open(filePath);
            return file;
        }
        bool checkFileExists(const string& filePath) override
        {
            return true;
        }
        void showMessageBox(const string& title, const char *format, ...) override
        {
        }
    public:
        void useWorld(shared_ptr<World> _world)
        {
            m_world = _world;
            m_world->onQueryTerrainElevation([](const GeoPoint& location){
                return 123.0;
            });
        }
        TestFlight addIfrFlight(
            int flightNo,
            const string& fromIcao,
            const string& toIcao,
            const GeoPoint& location,
            const Altitude& altitude,
            const string& typeIcao = "B738")
        {
            shared_ptr<TestAIAircraft> aircraft = shared_ptr<TestAIAircraft>(new TestAIAircraft(
                shared_from_this(),
                12345,
                typeIcao,
                "TES",
                to_string(flightNo),
                Aircraft::Category::Jet));
            shared_ptr<FlightPlan> flightPlan(new FlightPlan(
                1000,
                2000,
                fromIcao,
                toIcao));
            shared_ptr<Flight> flight(new Flight(
                shared_from_this(),
                flightNo,
                Flight::RulesType::IFR,
                "TES",
                to_string(flightNo),
                "TES " + to_string(flightNo),
                flightPlan));
            shared_ptr<TestAIPilot> pilot(new TestAIPilot(
                shared_from_this(),
                flightNo,
                "Tes",
                flight));

            flight->setAircraft(aircraft);
            flight->setPilot(pilot);
            aircraft->setLocation(location);
            aircraft->setAltitude(altitude);

            m_world->addFlight(flight);
            return { flight, pilot, aircraft };
        }
        const vector<shared_ptr<TestAIController>>& getCreatedAIControllers() const
        { 
            return m_createdAIControllers; 
        }
        const vector<shared_ptr<TestAIPilot>>& getCreatedAIPilots() const
        {
            return m_createdAIPilots;
        }
        shared_ptr<ControllerPosition> getAirportControl(const string& icao, ControllerPosition::Type type, shared_ptr<Flight> flight)
        {
            return m_world->getAirport(icao)->getControllerPositionOrThrow(type, flight->aircraft()->location());
        }
        shared_ptr<Frequency> getAirportFrequency(const string& icao, ControllerPosition::Type type, shared_ptr<Flight> flight)
        {
            return getAirportControl(icao, type, flight)->frequency();
        }
        shared_ptr<IntentFactory> intentFactory()
        {
            return services().get<IntentFactory>();
        }
        void enableLogs(bool enable)
        {
            m_quiet = !enable;
        }
        void setTimeForLog(chrono::milliseconds time)
        {
            m_timeForLog = time;
            m_timeForLogWasSet = true;
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
        static shared_ptr<TestHostServices> createWithWorld()
        {
            auto testHost = create();
            testHost->useWorld(shared_ptr<World>(new World(testHost, 0)));
            return testHost;
        }
        static shared_ptr<TestHostServices> createWithWorldAirports(const vector<AirportFactoryCallback>& airportFactories)
        {
            auto testHost = create();

            vector<shared_ptr<Airport>> airports;
            for (const auto& factory : airportFactories)
            {
                airports.push_back(factory(testHost));
            }

            auto world = WorldBuilder::assembleSampleWorld(testHost, airports, nullptr);
            testHost->useWorld(world);

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
