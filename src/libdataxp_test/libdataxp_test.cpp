#include "libworld.h"
#include "libdataxp.h"
#include "gtest/gtest.h"

TEST(WorldTest, libWorldFunc) {
    int value = libWorldFunc();
    EXPECT_EQ(value, 123);
}

TEST(DataXPTest, libDataXPFunc) {
    int value = libDataXPFunc();
    EXPECT_EQ(value, 123 + 456);
}
