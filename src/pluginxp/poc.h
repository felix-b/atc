// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#pragma once
#include <memory>

using namespace std;

class TncPoc
{
public:
    virtual void start() = 0;
    virtual void stop() = 0;
    virtual void setTimeFactor(uint64_t factor) = 0;
};

shared_ptr<TncPoc> createPoc0();
shared_ptr<TncPoc> createPoc1();
