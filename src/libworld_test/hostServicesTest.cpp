// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <functional>
#include <typeinfo>
#include <string>
#include <unordered_map>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

class ServiceA
{
private:
    shared_ptr<vector<string>> m_log;
public:
    ServiceA(shared_ptr<vector<string>> _log) : m_log(_log) {} 
    ~ServiceA() { m_log->push_back("~ServiceA()"); }
    string sayWho() { return "SERVICE A"; }
};

class ServiceB
{
protected:
    shared_ptr<vector<string>> m_log;
    string m_name;
public:
    ServiceB(shared_ptr<vector<string>> _log) : m_log(_log), m_name("1st") {} 
    virtual ~ServiceB() { m_log->push_back(m_name + ":~ServiceB()"); }
    virtual string sayWho() { return "SERVICE B"; }
};

class ServiceB2 : public ServiceB
{
public:
    ServiceB2(shared_ptr<vector<string>> _log) : ServiceB(_log) 
    {
        m_name = "2nd";
    }
    virtual ~ServiceB2() { m_log->push_back(m_name + ":~ServiceB2()"); }
    string sayWho() override { return "SERVICE B2"; }
};

// class ExampleDIContainer
// {
// private:
//     unordered_map<string, shared_ptr<void>> m_servicePtrByTypeKey;
// public:
//     template<class TService>
//     shared_ptr<TService> getService()
//     {
//         string typeKey(typeid(TService).name());
//         shared_ptr<void> servicePtr;
//         if (tryGetValue(m_servicePtrByTypeKey, typeKey, servicePtr))
//         {
//             return dynamic_cast<TService*>(servicePtr);
//         }
//         throw runtime_error("Service not found in container: " + serviceKey);
//     }

//     template<class TService>
//     void useService(shared_ptr<TService> service)
//     {
//         string typeKey(typeid(TService).name());
//         m_servicePtrByTypeKey.insert({ 
//             typeKey,
//             service
//         });
//     }
// };

template<class T>
string exampleGetRuntimeTypeName()
{
    return typeid(T).name();
}

class TestPtr
{
private:
    shared_ptr<void> m_ptr;
public:
    TestPtr(shared_ptr<void> _ptr) : m_ptr(_ptr) { } 
    template<class T> shared_ptr<T> getAs() { 
        return static_pointer_cast<T>(m_ptr);
    }
};


TEST(HostServicesTest, canGetTypeNameAtRuntime) {
    EXPECT_STREQ(typeid(ServiceA).name(), exampleGetRuntimeTypeName<ServiceA>().c_str());
    EXPECT_STREQ(typeid(ServiceB).name(), exampleGetRuntimeTypeName<ServiceB>().c_str());
}

TEST(HostServicesTest, canUseSharedPtrOfVoid) {

    auto log = make_shared<vector<string>>();

    {
        unordered_map<string, TestPtr> serviceMap;

        {
            auto initA = shared_ptr<ServiceA>(new ServiceA(log));
            auto initB = shared_ptr<ServiceB>(new ServiceB(log));
            auto initB2 = shared_ptr<ServiceB2>(new ServiceB2(log));
            serviceMap.insert({ "A", TestPtr(initA) });
            serviceMap.insert({ "B", TestPtr(initB) });
            serviceMap.insert({ "B2", TestPtr(initB2) });
        }

        TestPtr ptrA = getValueOrThrow(serviceMap, string("A"));
        shared_ptr<ServiceA> serviceA = ptrA.getAs<ServiceA>();
        EXPECT_EQ(serviceA->sayWho(), string("SERVICE A"));

        TestPtr ptrB2 = getValueOrThrow(serviceMap, string("B2"));
        shared_ptr<ServiceB> serviceB = ptrB2.getAs<ServiceB>();
        EXPECT_EQ(serviceB->sayWho(), string("SERVICE B2"));
    }

    const auto logContains = [log](const string& s) {
        return hasAny<string>(*log, [&s](const string& item) { 
            return s.compare(item) == 0; 
        });
    };

    EXPECT_TRUE(logContains("~ServiceA()"));
    EXPECT_TRUE(logContains("1st:~ServiceB()"));
    EXPECT_TRUE(logContains("2nd:~ServiceB()"));
    EXPECT_TRUE(logContains("2nd:~ServiceB2()"));
}

TEST(HostServicesTest, servicesContainer) {

    auto log = make_shared<vector<string>>();

    {
        TestHostServices host;
        unordered_map<string, TestPtr> serviceMap;

        {
            auto initA = shared_ptr<ServiceA>(new ServiceA(log));
            auto initB = shared_ptr<ServiceB>(new ServiceB(log));
            auto initB2 = shared_ptr<ServiceB2>(new ServiceB2(log));
            host.services().use(initA);
            host.services().use(initB);
            host.services().use(initB2);
        }

        auto serviceA = host.services().get<ServiceA>();
        EXPECT_EQ(serviceA->sayWho(), string("SERVICE A"));

        auto serviceB2 = host.services().get<ServiceB2>();
        EXPECT_EQ(serviceB2->sayWho(), string("SERVICE B2"));
    }

    const auto logContains = [log](const string& s) {
        return hasAny<string>(*log, [&s](const string& item) { 
            return s.compare(item) == 0; 
        });
    };

    EXPECT_TRUE(logContains("~ServiceA()"));
    EXPECT_TRUE(logContains("1st:~ServiceB()"));
    EXPECT_TRUE(logContains("2nd:~ServiceB()"));
    EXPECT_TRUE(logContains("2nd:~ServiceB2()"));
}

