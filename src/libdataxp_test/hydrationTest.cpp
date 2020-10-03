// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <string>
#include <functional>
#include "gtest/gtest.h"

using namespace std;

class ScalarFields
{
public:
    int n;
    char s5[5];
    bool b;
    float f;
};

TEST(HydrationTest, scalarFieldsObject) {
    
}