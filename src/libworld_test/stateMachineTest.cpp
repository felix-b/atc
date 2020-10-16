//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#if 0
#include <memory>
#include <string>
#include <algorithm>
#include <unordered_map>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "stateMachine.hpp"

using namespace std;
using namespace world;

TEST(StateMachineTest, simpleTransitions)
{
    auto host = TestHostServices::create();
    bool events[1] = { false };
    vector<string> log;

    const auto createGreenState = [&]()->shared_ptr

    StateMachine<string> machine;
    StateMachine<string>::State& redState = machine.addState(
        "RED",
        [&]{ log.push_back("RED:enter"); },
        [&]{ log.push_back("RED:exit"); },
        [&]{ log.push_back("RED:ping"); });
    StateMachine<string>::State& greenState = machine.addState(
        "GREEN",
        [&]{ log.push_back("GREEN:enter"); },
        [&]{ log.push_back("GREEN:exit"); },
        [&]{ log.push_back("GREEN:ping"); });
    redState.addTransition("");




    ASSERT_EQ(workItemLog.size(), 1);
    EXPECT_EQ(workItemLog[0], "workItemA");
}
#endif