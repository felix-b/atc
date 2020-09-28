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
    private:
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
    private:
        function<float(double geo)> m_geoToLocal;
        function<double(float local)> m_localToGeo;
        int m_nextAIControllerId = 1;
        vector<shared_ptr<Controller>> m_createdAIControllers;
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
        shared_ptr<Controller> createAIController(shared_ptr<ControllerPosition> position) override
        {
            int id = m_nextAIControllerId++;
            string name = "ai-controller-" + to_string(id);
            auto controller = shared_ptr<Controller>(new TestAIController(shared_from_this(), id, name, position));
            m_createdAIControllers.push_back(controller);
            return controller;
        }
        shared_ptr<Pilot> createAIPilot(shared_ptr<Flight> flight) override
        {
            throw runtime_error("TestHostServices::createAIPilot is not implemented");
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
        }
        const vector<shared_ptr<Controller>>& createdAIControllers() const 
        { 
            return m_createdAIControllers; 
        }
    public:
        static shared_ptr<TestHostServices> create()
        {
            return make_shared<TestHostServices>();
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
