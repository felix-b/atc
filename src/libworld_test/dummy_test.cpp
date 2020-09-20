#include "dummy.h"
#include "gtest/gtest.h"

TEST(Dummy, libWorldFunc) {
    int value = libWorldFunc();
    EXPECT_EQ(value, 123);
}
