// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "state.h"
#include "libworld_test.h"

using namespace world;

TEST(StateTest, assign_raw_value) {
    StateVariable<int> n1 = 10;
    int n2 = n1;
}
