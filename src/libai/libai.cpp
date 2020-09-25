// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"
#include "libai.hpp"
#include "aiPilot.hpp"
#include "aiController.hpp"
#include "maneuverFactory.hpp"

using namespace std;
using namespace world;

namespace ai
{
    class ConcreteAIControllerFactory : public AIControllerFactory
    {
    private:
        vector<Actor::Gender> m_genderRoundRobin = { Actor::Gender::Female, Actor::Gender::Male };
        shared_ptr<HostServices> m_host;
        unsigned long long m_nextControllerId;
    public:
        ConcreteAIControllerFactory(shared_ptr<HostServices> _host) :
            m_host(_host),
            m_nextControllerId(1)
        {
        }
    public:
        shared_ptr<Controller> createController(shared_ptr<ControllerPosition> position) override
        {
            //Actor::Gender newControllerGender = m_genderRoundRobin[m_nextControllerId % m_genderRoundRobin.size()];
            Actor::Gender newControllerGender = m_nextControllerId == 2 ? Actor::Gender::Female : Actor::Gender::Male;
            int newControllerId = m_nextControllerId++;

            switch (position->type())
            {
            case ControllerPosition::Type::ClearanceDelivery:
            case ControllerPosition::Type::Ground:
            case ControllerPosition::Type::Local:
            case ControllerPosition::Type::Approach:
            case ControllerPosition::Type::Departure:
                m_host->writeLog("Creating AI controller for position: %s", position->callSign().c_str());
                return shared_ptr<Controller>(new DummyAIController(m_host, newControllerId, newControllerGender, position));
            }

            throw runtime_error("AIControllerFactory::createController: unsupported type for position: " + position->callSign());
        }
    };

    class ConcreteAIPilotFactory : public AIPilotFactory
    {
    private:
        vector<Actor::Gender> m_genderRoundRobin = { Actor::Gender::Male, Actor::Gender::Male, Actor::Gender::Male, Actor::Gender::Female };
        shared_ptr<HostServices> m_host;
        shared_ptr<IntentFactory> m_intentFactory;
        shared_ptr<ManeuverFactory> m_maneuverFactory;
        unsigned long long m_nextPilotId;
    public:
        ConcreteAIPilotFactory(shared_ptr<HostServices> _host) :
            m_host(_host),
            m_nextPilotId(1)
        {
            m_maneuverFactory = _host->services().get<ManeuverFactory>();
            m_intentFactory = _host->services().get<IntentFactory>();
        }
    public:
        shared_ptr<Pilot> createPilot(shared_ptr<Flight> flight) override
        {
            m_host->writeLog("Creating AI pilot for flight: %s", flight->callSign().c_str());
            
            //Actor::Gender newPilotGender = m_genderRoundRobin[m_nextPilotId % m_genderRoundRobin.size()];
            Actor::Gender newPilotGender = m_nextPilotId == 3 ? Actor::Gender::Female : Actor::Gender::Male;
            int newPilotId = m_nextPilotId++;

            return shared_ptr<Pilot>(new AIPilot(
                m_host, 
                newPilotId, 
                newPilotGender, 
                flight,
                m_maneuverFactory, 
                m_intentFactory));
        }
    };

    void contributeComponents(shared_ptr<HostServices> host)
    {
        host->writeLog("libai::contributeComponents - entered");

        auto clearanceFactory = shared_ptr<ClearanceFactory>(new ClearanceFactory(host));
        auto maneuverFactory = shared_ptr<ManeuverFactory>(new ManeuverFactory(host));

        host->services().use<ClearanceFactory>(clearanceFactory);
        host->services().use<ManeuverFactory>(maneuverFactory);

        auto aiPilotFactory = shared_ptr<AIPilotFactory>(new ConcreteAIPilotFactory(host));
        auto aiControllerFactory = shared_ptr<AIControllerFactory>(new ConcreteAIControllerFactory(host));

        host->services().use<AIPilotFactory>(aiPilotFactory);
        host->services().use<AIControllerFactory>(aiControllerFactory);
    }
}
