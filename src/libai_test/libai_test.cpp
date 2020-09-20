#include "libworld.h"
#include "libai.h"
#include "gtest/gtest.h"

TEST(WorldTest, libWorldFunc) {
    int value = libWorldFunc();
    EXPECT_EQ(value, 123);
}

TEST(AITest, libAIFunc) {
    int value = libAIFunc();
    EXPECT_EQ(value, 123 + 789);
}
