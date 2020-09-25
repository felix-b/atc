// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libworld_test.h"

using namespace world;

TEST(UniPointTest, initGeo) {
    auto host = make_shared<TestHostServices>();
    UniPoint p1(host, GeoPoint({32.123456, 34.234567, 100}));
    
    EXPECT_FLOAT_EQ(p1.geo().latitude, 32.123456);
    EXPECT_FLOAT_EQ(p1.geo().longitude, 34.234567);
    EXPECT_FLOAT_EQ(p1.geo().altitude, 100);

    EXPECT_FLOAT_EQ(p1.local().x, 3423.4567);
    EXPECT_FLOAT_EQ(p1.local().z, 3212.3456);
    EXPECT_FLOAT_EQ(p1.local().y, 10000);
}

TEST(UniPointTest, initLocal) {
    auto host = make_shared<TestHostServices>();
    UniPoint p1(host, LocalPoint({4000, 3000, 2000}));

    EXPECT_FLOAT_EQ(p1.local().x, 4000);
    EXPECT_FLOAT_EQ(p1.local().y, 3000);
    EXPECT_FLOAT_EQ(p1.local().z, 2000);

    EXPECT_FLOAT_EQ(p1.geo().latitude, 2.0);
    EXPECT_FLOAT_EQ(p1.geo().longitude, 4.0);
    EXPECT_FLOAT_EQ(p1.geo().altitude, 3.0);
}
