//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <memory>
#include <chrono>
#include "libworld.h"

using namespace std;
using namespace world;

namespace server
{
    class ServerControllerInterface
    {
    protected:
        ServerControllerInterface() = default;
    public:
        virtual bool running() = 0;
        virtual void start(int listenPort = 9002) = 0;
        virtual void beginStop() = 0;
        virtual bool waitUntilStopped(chrono::milliseconds timeout) = 0;
    public:
        static shared_ptr<ServerControllerInterface> create(shared_ptr<HostServices> host);
    };
}
