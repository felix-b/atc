// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#define _USE_MATH_DEFINES
#include <cmath>
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "basicManeuverTypes.hpp"
#include "maneuverFactory.hpp"
#include "clearanceFactory.hpp"
#include "libworld_test.h"

using namespace world;
using namespace ai;

TEST(ManeuverFactoryTest, animation_scalar) {
    vector<tuple<double, double>> log;
    
    auto animation = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 
        100,
        200,
        chrono::seconds(10),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ progress, value });
        }
    ));

    EXPECT_EQ(animation->state(), Maneuver::State::NotStarted);

    animation->progressTo(chrono::seconds(10));
    EXPECT_EQ(animation->state(), Maneuver::State::InProgress);

    animation->progressTo(chrono::seconds(15));
    EXPECT_EQ(animation->state(), Maneuver::State::InProgress);

    animation->progressTo(chrono::seconds(20));
    EXPECT_EQ(animation->state(), Maneuver::State::Finished);


    EXPECT_FLOAT_EQ(get<0>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<1>(log[0]), 100.0);

    EXPECT_FLOAT_EQ(get<0>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<1>(log[1]), 150.0);

    EXPECT_FLOAT_EQ(get<0>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<1>(log[2]), 200.0);

    EXPECT_EQ(animation->startTimestamp().count(), 10000000);
    EXPECT_EQ(animation->finishTimestamp().count(), 20000000);
}

TEST(ManeuverFactoryTest, animation_shiftedTicks) {
    vector<tuple<double, double>> log;
    
    auto animation = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 
        100,
        200,
        chrono::seconds(10),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ progress, value });
        }
    ));

    EXPECT_EQ(animation->state(), Maneuver::State::NotStarted);

    animation->progressTo(chrono::seconds(10));
    EXPECT_EQ(animation->state(), Maneuver::State::InProgress);

    animation->progressTo(chrono::seconds(15));
    EXPECT_EQ(animation->state(), Maneuver::State::InProgress);

    animation->progressTo(chrono::seconds(22));
    EXPECT_EQ(animation->state(), Maneuver::State::Finished);


    EXPECT_FLOAT_EQ(get<0>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<1>(log[0]), 100.0);

    EXPECT_FLOAT_EQ(get<0>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<1>(log[1]), 150.0);

    EXPECT_FLOAT_EQ(get<0>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<1>(log[2]), 200.0);

    EXPECT_EQ(animation->startTimestamp().count(), 10000000);
    EXPECT_EQ(animation->finishTimestamp().count(), 22000000);
}

TEST(ManeuverFactoryTest, animation_geoPoint) {
    vector<tuple<double, double, double>> log;

    auto animation = shared_ptr<Maneuver>(new AnimationManeuver<GeoPoint>(
        "", 
        GeoPoint(10, -70),
        GeoPoint(20, -90),
        chrono::seconds(100),
        [](const GeoPoint& from, const GeoPoint& to, double progress, GeoPoint& value) {
            value.latitude = from.latitude + progress * (to.latitude - from.latitude);
            value.longitude = from.longitude + progress * (to.longitude - from.longitude);
        },
        [&log](const GeoPoint& value, double progress) {
            log.push_back({ progress, value.latitude, value.longitude });
        }
    ));

    animation->progressTo(chrono::seconds(10));
    animation->progressTo(chrono::seconds(60));
    animation->progressTo(chrono::seconds(110));

    ASSERT_EQ(log.size(), 3);
    EXPECT_FLOAT_EQ(get<0>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<1>(log[0]), 10.0);
    EXPECT_FLOAT_EQ(get<2>(log[0]), -70.0);

    EXPECT_FLOAT_EQ(get<0>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<1>(log[1]), 15.0);
    EXPECT_FLOAT_EQ(get<2>(log[1]), -80.0);

    EXPECT_FLOAT_EQ(get<0>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<1>(log[2]), 20.0);
    EXPECT_FLOAT_EQ(get<2>(log[2]), -90.0);
}

TEST(ManeuverFactoryTest, animation_startValueFactory) {
    vector<tuple<double, double, double>> log;

    auto animation = shared_ptr<Maneuver>(new AnimationManeuver<GeoPoint>(
        "", 
        GeoPoint(10, -70),
        GeoPoint(20, -90),
        chrono::seconds(100),
        [](const GeoPoint& from, const GeoPoint& to, double progress, GeoPoint& value) {
            value.latitude = from.latitude + progress * (to.latitude - from.latitude);
            value.longitude = from.longitude + progress * (to.longitude - from.longitude);
        },
        [&log](const GeoPoint& value, double progress) {
            log.push_back({ progress, value.latitude, value.longitude });
        }
    ));

    animation->progressTo(chrono::seconds(10));
    animation->progressTo(chrono::seconds(60));
    animation->progressTo(chrono::seconds(110));

    ASSERT_EQ(log.size(), 3);
    EXPECT_FLOAT_EQ(get<0>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<1>(log[0]), 10.0);
    EXPECT_FLOAT_EQ(get<2>(log[0]), -70.0);

    EXPECT_FLOAT_EQ(get<0>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<1>(log[1]), 15.0);
    EXPECT_FLOAT_EQ(get<2>(log[1]), -80.0);

    EXPECT_FLOAT_EQ(get<0>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<1>(log[2]), 20.0);
    EXPECT_FLOAT_EQ(get<2>(log[2]), -90.0);
}

TEST(ManeuverFactoryTest, awaitManeuver) {
    bool ready = false;
    AwaitManeuver maneuver(
        TestHostServices::create(),
        Maneuver::Type::Unspecified,
        "", 
        [&ready]() { return ready; }
    );

    EXPECT_EQ(maneuver.state(), Maneuver::State::NotStarted);

    maneuver.progressTo(chrono::seconds(1));
    EXPECT_EQ(maneuver.state(), Maneuver::State::InProgress);
    
    maneuver.progressTo(chrono::seconds(2));
    EXPECT_EQ(maneuver.state(), Maneuver::State::InProgress);
    
    ready = true;

    maneuver.progressTo(chrono::seconds(3));
    EXPECT_EQ(maneuver.state(), Maneuver::State::Finished);
}

TEST(ManeuverFactoryTest, awaitManeuver_inSequence) {
    bool ready = false;
    int actionCount = 0;

    auto delay = shared_ptr<Maneuver>(new AwaitManeuver(
        TestHostServices::create(),
        Maneuver::Type::Unspecified, "", [&ready]() {
            return ready;
        }
    ));
    auto action = shared_ptr<Maneuver>(new InstantActionManeuver(
        Maneuver::Type::Unspecified, "", [&actionCount](){
            actionCount++;
        }
    ));
    auto sequence = shared_ptr<SequentialManeuver>(new SequentialManeuver(
        Maneuver::Type::Unspecified, "", {  
            delay,
            action
        }
    ));

    sequence->progressTo(chrono::seconds(1));
    EXPECT_EQ(delay->state(), Maneuver::State::InProgress);
    EXPECT_EQ(action->state(), Maneuver::State::NotStarted);

    ready = true;
    sequence->progressTo(chrono::seconds(2));

    EXPECT_EQ(delay->state(), Maneuver::State::Finished);
    EXPECT_EQ(action->state(), Maneuver::State::Finished);
}

TEST(ManeuverFactoryTest, sequential_singleChild) {
    vector<tuple<double, double>> log;

    auto animation1 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 100, 200, chrono::seconds(5),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ progress, value });
        }
    ));
    auto sequential = shared_ptr<SequentialManeuver>(new SequentialManeuver(
        Maneuver::Type::Unspecified, "", {  
            animation1
        }
    ));

    EXPECT_EQ(sequential->state(), Maneuver::State::NotStarted);
    
    sequential->progressTo(chrono::milliseconds(10000));
    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);

    sequential->progressTo(chrono::milliseconds(12500));
    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);

    sequential->progressTo(chrono::milliseconds(15000));
    EXPECT_EQ(sequential->state(), Maneuver::State::Finished);

    sequential->progressTo(chrono::milliseconds(16000));
    EXPECT_EQ(sequential->state(), Maneuver::State::Finished);

    ASSERT_EQ(log.size(), 3);

    EXPECT_FLOAT_EQ(get<0>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<1>(log[0]), 100.0);

    EXPECT_FLOAT_EQ(get<0>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<1>(log[1]), 150.0);

    EXPECT_FLOAT_EQ(get<0>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<1>(log[2]), 200.0);
}

TEST(ManeuverFactoryTest, sequential_multipleChildren) {
    vector<tuple<string, double, double>> log;

    auto animation1 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 100, 200, chrono::seconds(5),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ "1", progress, value });
        }
    ));
    auto animation2 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 1000, 2000, chrono::seconds(10),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ "2", progress, value });
        }
    ));
    auto sequential = shared_ptr<SequentialManeuver>(new SequentialManeuver(
        Maneuver::Type::Unspecified, "", {  
            animation1, 
            animation2
        }
    ));

    EXPECT_EQ(sequential->firstChild(), animation1);
    EXPECT_EQ(animation1->nextSibling(), animation2);
    EXPECT_EQ(animation2->nextSibling(), nullptr);

    EXPECT_EQ(sequential->state(), Maneuver::State::NotStarted);
    EXPECT_EQ(animation1->state(), Maneuver::State::NotStarted);
    EXPECT_EQ(animation2->state(), Maneuver::State::NotStarted);
    EXPECT_EQ(log.size(), 0);
    
    sequential->progressTo(chrono::seconds(10));
    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation1->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation2->state(), Maneuver::State::NotStarted);
    EXPECT_EQ(log.size(), 1);

    sequential->progressTo(chrono::milliseconds(12500));
    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation1->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation2->state(), Maneuver::State::NotStarted);
    EXPECT_EQ(log.size(), 2);

    sequential->progressTo(chrono::seconds(15));
    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation1->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation2->state(), Maneuver::State::InProgress);
    EXPECT_EQ(log.size(), 4);

    sequential->progressTo(chrono::seconds(20));
    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation1->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation2->state(), Maneuver::State::InProgress);
    EXPECT_EQ(log.size(), 5);

    sequential->progressTo(chrono::seconds(25));
    EXPECT_EQ(sequential->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation1->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation2->state(), Maneuver::State::Finished);
    EXPECT_EQ(log.size(), 6);

    sequential->progressTo(chrono::seconds(26));
    EXPECT_EQ(sequential->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation1->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation2->state(), Maneuver::State::Finished);
    EXPECT_EQ(log.size(), 6);

    EXPECT_EQ(sequential->startTimestamp(), chrono::milliseconds(10000));
    EXPECT_EQ(sequential->finishTimestamp(), chrono::milliseconds(25000));

    ASSERT_EQ(log.size(), 6);

    EXPECT_EQ(get<0>(log[0]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<2>(log[0]), 100.0);

    EXPECT_EQ(get<0>(log[1]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<2>(log[1]), 150.0);

    EXPECT_EQ(get<0>(log[2]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<2>(log[2]), 200.0);

    EXPECT_EQ(get<0>(log[3]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[3]), 0.0);
    EXPECT_FLOAT_EQ(get<2>(log[3]), 1000.0);

    EXPECT_EQ(get<0>(log[4]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[4]), 0.5);
    EXPECT_FLOAT_EQ(get<2>(log[4]), 1500.0);

    EXPECT_EQ(get<0>(log[5]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[5]), 1.0);
    EXPECT_FLOAT_EQ(get<2>(log[5]), 2000.0);
}

TEST(ManeuverFactoryTest, sequential_shiftedTicks) {
    vector<tuple<string, double, double>> log;

    auto animation1 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 100, 200, chrono::seconds(5),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ "1", progress, value });
        }
    ));
    auto animation2 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 1000, 2000, chrono::seconds(10),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ "2", progress, value });
        }
    ));
    auto sequential = shared_ptr<SequentialManeuver>(new SequentialManeuver(
        Maneuver::Type::Unspecified, "", {  
            animation1, 
            animation2
        }
    ));

    sequential->progressTo(chrono::seconds(10));
    sequential->progressTo(chrono::milliseconds(12500));
    sequential->progressTo(chrono::seconds(16));

    EXPECT_EQ(sequential->state(), Maneuver::State::InProgress);
    EXPECT_EQ(animation1->state(), Maneuver::State::Finished);
    EXPECT_EQ(animation2->state(), Maneuver::State::InProgress);
    EXPECT_EQ(log.size(), 4);

    sequential->progressTo(chrono::seconds(21));
    sequential->progressTo(chrono::seconds(27));

    EXPECT_EQ(sequential->startTimestamp(), chrono::milliseconds(10000));
    EXPECT_EQ(animation1->startTimestamp(), chrono::milliseconds(10000));
    EXPECT_EQ(animation2->startTimestamp(), chrono::milliseconds(16000));

    EXPECT_EQ(sequential->finishTimestamp(), chrono::milliseconds(27000));
    EXPECT_EQ(animation1->finishTimestamp(), chrono::milliseconds(16000));
    EXPECT_EQ(animation2->finishTimestamp(), chrono::milliseconds(27000));

    ASSERT_EQ(log.size(), 6);

    EXPECT_EQ(get<0>(log[0]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<2>(log[0]), 100.0);

    EXPECT_EQ(get<0>(log[1]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<2>(log[1]), 150.0);

    EXPECT_EQ(get<0>(log[2]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<2>(log[2]), 200.0);

    EXPECT_EQ(get<0>(log[3]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[3]), 0.0);
    EXPECT_FLOAT_EQ(get<2>(log[3]), 1000.0);

    EXPECT_EQ(get<0>(log[4]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[4]), 0.5);
    EXPECT_FLOAT_EQ(get<2>(log[4]), 1500.0);

    EXPECT_EQ(get<0>(log[5]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[5]), 1.0);
    EXPECT_FLOAT_EQ(get<2>(log[5]), 2000.0);
}

TEST(ManeuverFactoryTest, parallel_singleChild) {
    vector<tuple<double, double>> log;

    auto animation1 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 100, 200, chrono::seconds(5),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ progress, value });
        }
    ));
    auto parallel = shared_ptr<ParallelManeuver>(new ParallelManeuver(
        Maneuver::Type::Unspecified, "", {  
            animation1
        }
    ));

    EXPECT_EQ(parallel->firstChild(), animation1);
    EXPECT_EQ(animation1->nextSibling(), nullptr);

    EXPECT_EQ(parallel->state(), Maneuver::State::NotStarted);
    
    parallel->progressTo(chrono::milliseconds(10000));
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    parallel->progressTo(chrono::milliseconds(12500));
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    parallel->progressTo(chrono::milliseconds(15000));
    EXPECT_EQ(parallel->state(), Maneuver::State::Finished);

    parallel->progressTo(chrono::milliseconds(16000));
    EXPECT_EQ(parallel->state(), Maneuver::State::Finished);

    EXPECT_EQ(parallel->startTimestamp(), chrono::milliseconds(10000));
    EXPECT_EQ(parallel->finishTimestamp(), chrono::milliseconds(15000));

    ASSERT_EQ(log.size(), 3);

    EXPECT_FLOAT_EQ(get<0>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<1>(log[0]), 100.0);

    EXPECT_FLOAT_EQ(get<0>(log[1]), 0.5);
    EXPECT_FLOAT_EQ(get<1>(log[1]), 150.0);

    EXPECT_FLOAT_EQ(get<0>(log[2]), 1.0);
    EXPECT_FLOAT_EQ(get<1>(log[2]), 200.0);
}

TEST(ManeuverFactoryTest, parallel_multipleChildren) {
    vector<tuple<string, double, double>> log;

    auto animation1 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 100, 200, chrono::seconds(5),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ "1", progress, value });
        }
    ));
    auto animation2 = shared_ptr<AnimationManeuver<double>>(new AnimationManeuver<double>(
        "", 1000, 2000, chrono::seconds(10),
        [](const double& from, const double& to, double progress, double& value) {
            value = from + progress * (to - from);
        },
        [&log](const double& value, double progress) {
            log.push_back({ "2", progress, value });
        }
    ));
    auto parallel = shared_ptr<ParallelManeuver>(new ParallelManeuver(
        Maneuver::Type::Unspecified, "", {  
            animation1, 
            animation2
        }
    ));

    EXPECT_EQ(parallel->firstChild(), animation1);
    EXPECT_EQ(animation1->nextSibling(), animation2);
    EXPECT_EQ(animation2->nextSibling(), nullptr);

    EXPECT_EQ(parallel->state(), Maneuver::State::NotStarted);
    
    parallel->progressTo(chrono::milliseconds(10000));
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    parallel->progressTo(chrono::milliseconds(12500));
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    parallel->progressTo(chrono::milliseconds(15000));
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    parallel->progressTo(chrono::milliseconds(17500));
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    parallel->progressTo(chrono::milliseconds(20000));
    EXPECT_EQ(parallel->state(), Maneuver::State::Finished);

    parallel->progressTo(chrono::milliseconds(21000));
    EXPECT_EQ(parallel->state(), Maneuver::State::Finished);

    EXPECT_EQ(parallel->startTimestamp(), chrono::milliseconds(10000));
    EXPECT_EQ(parallel->finishTimestamp(), chrono::milliseconds(20000));

    ASSERT_EQ(log.size(), 8);

    EXPECT_EQ(get<0>(log[0]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[0]), 0.0);
    EXPECT_FLOAT_EQ(get<2>(log[0]), 100.0);

    EXPECT_EQ(get<0>(log[1]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[1]), 0.0);
    EXPECT_FLOAT_EQ(get<2>(log[1]), 1000.0);

    EXPECT_EQ(get<0>(log[2]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[2]), 0.5);
    EXPECT_FLOAT_EQ(get<2>(log[2]), 150.0);

    EXPECT_EQ(get<0>(log[3]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[3]), 0.25);
    EXPECT_FLOAT_EQ(get<2>(log[3]), 1250.0);

    EXPECT_EQ(get<0>(log[4]), "1");
    EXPECT_FLOAT_EQ(get<1>(log[4]), 1.0);
    EXPECT_FLOAT_EQ(get<2>(log[4]), 200.0);

    EXPECT_EQ(get<0>(log[5]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[5]), 0.5);
    EXPECT_FLOAT_EQ(get<2>(log[5]), 1500.0);

    EXPECT_EQ(get<0>(log[6]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[6]), 0.75);
    EXPECT_FLOAT_EQ(get<2>(log[6]), 1750.0);

    EXPECT_EQ(get<0>(log[7]), "2");
    EXPECT_FLOAT_EQ(get<1>(log[7]), 1.0);
    EXPECT_FLOAT_EQ(get<2>(log[7]), 2000.0);
}

TEST(ManeuverFactoryTest, parallel_sequencesWithDelays) {
    auto host = TestHostServices::create();

    bool delay1Ready = false;
    bool delay2Ready = false;
    int action1Count = 0;
    int action2Count = 0;

    auto parallel = shared_ptr<ParallelManeuver>(new ParallelManeuver(Maneuver::Type::Unspecified, "", {  
        shared_ptr<Maneuver>(new SequentialManeuver(Maneuver::Type::Unspecified, "", {  
            shared_ptr<Maneuver>(new AwaitManeuver(host, Maneuver::Type::Unspecified, "",
                [&]() { return delay1Ready; }
            )),
            shared_ptr<Maneuver>(new InstantActionManeuver(Maneuver::Type::Unspecified, "", 
                [&]() { action1Count++; }
            )),
        })),
        shared_ptr<Maneuver>(new SequentialManeuver(Maneuver::Type::Unspecified, "", {  
            shared_ptr<Maneuver>(new AwaitManeuver(host, Maneuver::Type::Unspecified, "",
                [&]() { return delay2Ready; }
            )),
            shared_ptr<Maneuver>(new InstantActionManeuver(Maneuver::Type::Unspecified, "", 
                [&]() { action2Count++; }
            )),
        }))
    }));

    EXPECT_EQ(parallel->state(), Maneuver::State::NotStarted);
    
    parallel->progressTo(chrono::milliseconds(1000));

    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);
    EXPECT_EQ(action1Count, 0);
    EXPECT_EQ(action2Count, 0);

    delay1Ready = true;
    parallel->progressTo(chrono::milliseconds(2000));

    EXPECT_EQ(action1Count, 1);
    EXPECT_EQ(action2Count, 0);

    delay2Ready = true;
    parallel->progressTo(chrono::milliseconds(3000));

    EXPECT_EQ(action1Count, 1);
    EXPECT_EQ(action2Count, 1);
    EXPECT_EQ(parallel->state(), Maneuver::State::Finished);
}

TEST(ManeuverFactoryTest, parallel_sequencesWithDeferredDelays) {
    auto host = TestHostServices::create();

    int delay0Ready = false;
    int delay1Ready = -1000;
    int delay2Ready = -1000;
    int action1Count = 0;
    int action2Count = 0;

    const auto getDelay1Ready = [&](){ return delay1Ready; };
    const auto getDelay2Ready = [&](){ return delay2Ready; };

    auto parallel = shared_ptr<ParallelManeuver>(new ParallelManeuver(Maneuver::Type::Unspecified, "", {  
        shared_ptr<Maneuver>(new SequentialManeuver(Maneuver::Type::Unspecified, "", {  
            shared_ptr<Maneuver>(new AwaitManeuver(host, Maneuver::Type::Unspecified, "",
                [&]() { return delay0Ready; }
            )),
            DeferredManeuver::create(Maneuver::Type::Unspecified, "", [=]() {
                int readyValue1 = getDelay1Ready();
                return shared_ptr<Maneuver>(new AwaitManeuver(host, Maneuver::Type::Unspecified, "",
                    [&]() { return (++readyValue1 > 0); }
                ));
            }),
            shared_ptr<Maneuver>(new InstantActionManeuver(Maneuver::Type::Unspecified, "", 
                [&]() { action1Count++; }
            )),
        })),
        shared_ptr<Maneuver>(new SequentialManeuver(Maneuver::Type::Unspecified, "", {  
            shared_ptr<Maneuver>(new AwaitManeuver(host, Maneuver::Type::Unspecified, "",
                [&]() { return delay0Ready; }
            )),
            DeferredManeuver::create(Maneuver::Type::Unspecified, "", [=]() {
                int readyValue2 = getDelay2Ready();
                return shared_ptr<Maneuver>(new AwaitManeuver(host, Maneuver::Type::Unspecified, "",
                    [&]() { return (++readyValue2 > 0); }
                ));
            }),
            shared_ptr<Maneuver>(new InstantActionManeuver(Maneuver::Type::Unspecified, "", 
                [&]() { action2Count++; }
            )),
        }))
    }));

    EXPECT_EQ(parallel->state(), Maneuver::State::NotStarted);
    
    parallel->progressTo(chrono::milliseconds(1000));

    EXPECT_EQ(action1Count, 0);
    EXPECT_EQ(action2Count, 0);
    EXPECT_EQ(parallel->state(), Maneuver::State::InProgress);

    delay0Ready = true;
    delay1Ready = 0;
    delay2Ready = 0;

    parallel->progressTo(chrono::milliseconds(2000));

    EXPECT_EQ(action1Count, 1);
    EXPECT_EQ(action2Count, 1);
    EXPECT_EQ(parallel->state(), Maneuver::State::Finished);
}

TEST(ManeuverFactoryTest, deferredManeuver_instantAction) {
    int actionCount = 0;

    auto deferred = DeferredManeuver::create(Maneuver::Type::Unspecified, "", [&]() {
        return shared_ptr<Maneuver>(new InstantActionManeuver(Maneuver::Type::Unspecified, "", [&] {
            actionCount++;
        }));
    });

    EXPECT_EQ(deferred->state(), Maneuver::State::NotStarted);
    EXPECT_EQ(actionCount, 0);

    deferred->progressTo(chrono::seconds(10));

    EXPECT_EQ(deferred->state(), Maneuver::State::Finished);
    EXPECT_EQ(actionCount, 1);

    deferred->progressTo(chrono::seconds(20));

    EXPECT_EQ(deferred->state(), Maneuver::State::Finished);
    EXPECT_EQ(actionCount, 1);
}

shared_ptr<Airport> createTestAirport(shared_ptr<HostServices> host)
{
    //auto host = TestHostServices::create();

    auto n1 = shared_ptr<TaxiNode>(new TaxiNode(111, UniPoint::fromGeo(host, GeoPoint(10, 40))));
    auto n2 = shared_ptr<TaxiNode>(new TaxiNode(222, UniPoint::fromGeo(host, GeoPoint(10, 50))));
    auto n3 = shared_ptr<TaxiNode>(new TaxiNode(333, UniPoint::fromGeo(host, GeoPoint(10, 60))));
    auto n4 = shared_ptr<TaxiNode>(new TaxiNode(444, UniPoint::fromGeo(host, GeoPoint(20, 60))));
    auto n5 = shared_ptr<TaxiNode>(new TaxiNode(555, UniPoint::fromGeo(host, GeoPoint(30, 60))));
    auto n6 = shared_ptr<TaxiNode>(new TaxiNode(666, UniPoint::fromGeo(host, GeoPoint(40, 50))));
    auto n7 = shared_ptr<TaxiNode>(new TaxiNode(777, UniPoint::fromGeo(host, GeoPoint(40, 40))));

    auto e12 = shared_ptr<TaxiEdge>(new TaxiEdge(1200, "E12", 111, 222));
    auto e23 = shared_ptr<TaxiEdge>(new TaxiEdge(2300, "E23", 222, 333));
    auto e34 = shared_ptr<TaxiEdge>(new TaxiEdge(3400, "E34", 333, 444));
    auto e45 = shared_ptr<TaxiEdge>(new TaxiEdge(4500, "E45", 444, 555));
    auto e56 = shared_ptr<TaxiEdge>(new TaxiEdge(5600, "E56", 555, 666));
    auto e67 = shared_ptr<TaxiEdge>(new TaxiEdge(6700, "E67", 666, 777));

    Airport::Header testHeader("KJFK", "JFK", GeoPoint(10,40), 0);
    auto airport = WorldBuilder::assembleAirport(
        host,
        testHeader,
        {},
        {},
        { n1, n2, n3, n4, n5, n6, n7 }, 
        { e12, e23, e34, e45, e56, e67 }
    );

    return airport;
}

shared_ptr<Flight> createTestFlight(shared_ptr<HostServices> host)
{
    shared_ptr<Aircraft> aircraft(new Aircraft(
        host, 
        12345, 
        "B738", 
        "DAL", 
        "12345", 
        Aircraft::Category::Jet));
    shared_ptr<FlightPlan> flightPlan(new FlightPlan(
        1000, 
        2000, 
        "KJFK", 
        "KMIA"));
    shared_ptr<Flight> flight(new Flight(
        host, 
        123, 
        Flight::RulesType::IFR, 
        "DAL", 
        "12345", 
        "DAL 12345", 
        flightPlan));
    flight->setAircraft(aircraft);
    return flight;
}

shared_ptr<DepartureTaxiClearance> createTaxiClearance(shared_ptr<Airport> airport, int fromNodeId, int toNodeId)
{
    auto taxiNet = airport->taxiNet();
    auto taxiPath = TaxiPath::find(taxiNet, taxiNet->getNodeById(fromNodeId), taxiNet->getNodeById(toNodeId));
    
    Clearance::Header header;
    header.id = 123456;
    header.type = Clearance::Type::DepartureTaxiClearance;
    header.issuedTimestamp = chrono::microseconds(0);
 
    return shared_ptr<DepartureTaxiClearance>(new DepartureTaxiClearance(header, "10", taxiPath));
}

#define EXPECT_AIRCRAFT_POSITION(flight, lat, lon, hdg)  \
    EXPECT_NEAR((flight)->aircraft()->location().latitude, (lat), 0.1); \
    EXPECT_NEAR((flight)->aircraft()->location().longitude, (lon), 0.1); \
    EXPECT_NEAR((flight)->aircraft()->attitude().heading(), (hdg), 0.1);

TEST(ManeuverFactoryTest, taxiTurn_rightAngleCounterClockwise) {
    shared_ptr<TestHostServices> host(new TestHostServices());
    shared_ptr<ManeuverFactory> factory(new ManeuverFactory(host));

    auto airport = createTestAirport(host);
    auto flight = createTestFlight(host);
    auto node222 = airport->taxiNet()->getNodeById(222);
    auto node333 = airport->taxiNet()->getNodeById(333);
    auto node444 = airport->taxiNet()->getNodeById(444);

    auto changeSet = make_shared<World::ChangeSet>();
    flight->onChanges([=](){ return changeSet; });

    GeoMath::TurnData turn;
    turn.e1p0 = node222->location().geo();
    turn.e1p1 = node333->location().geo();
    turn.e1HeadingRad = GeoMath::getRadiansFromPoints(turn.e1p0, turn.e1p1);
    turn.e2p0 = node333->location().geo();
    turn.e2p1 = node444->location().geo();
    turn.e2HeadingRad = GeoMath::getRadiansFromPoints(turn.e2p0, turn.e2p1);
    turn.radius = 5;

    GeoMath::TurnArc arc;
    GeoMath::calculateTurn(turn, arc, host);

    EXPECT_EQ(arc.p0.latitude, 10); //10,55 15,60
    EXPECT_EQ(arc.p0.longitude, 55); //10,55 15,60
    EXPECT_EQ(arc.p1.latitude, 15); 
    EXPECT_EQ(arc.p1.longitude, 60); 
    EXPECT_EQ(arc.arcRadius, 5); 
    EXPECT_EQ(arc.arcStartAngle, -GeoMath::pi() / 2); 
    EXPECT_EQ(arc.arcEndAngle, 0); 
    EXPECT_EQ(arc.arcDeltaAngle, GeoMath::pi() / 2); 
    EXPECT_FALSE(arc.arcClockwise); 

    auto maneuver = factory->taxiTurn(flight, arc, chrono::milliseconds(900), ManeuverFactory::TaxiType::Normal);
    
    maneuver->progressTo(chrono::milliseconds(1000));
    EXPECT_AIRCRAFT_POSITION(flight, 10, 55, 90);

    maneuver->progressTo(chrono::milliseconds(1300));
    EXPECT_AIRCRAFT_POSITION(flight, 10 + 0.134 * 5, 55 + 0.5 * 5, 60);

    maneuver->progressTo(chrono::milliseconds(1600));
    EXPECT_AIRCRAFT_POSITION(flight, 10 + 0.5 * 5, 55 + 0.866 * 5, 30);

    maneuver->progressTo(chrono::milliseconds(1900));
    EXPECT_AIRCRAFT_POSITION(flight, 15, 60, 0);
}

TEST(ManeuverFactoryTest, taxiTurn_obtuseAngleClockwise) {
    shared_ptr<TestHostServices> host(new TestHostServices());
    shared_ptr<ManeuverFactory> factory(new ManeuverFactory(host));

    auto airport = createTestAirport(host);
    auto flight = createTestFlight(host);
    auto node777 = airport->taxiNet()->getNodeById(777);
    auto node666 = airport->taxiNet()->getNodeById(666);
    auto node555 = airport->taxiNet()->getNodeById(555);

    auto changeSet = make_shared<World::ChangeSet>();
    flight->onChanges([=](){ return changeSet; });

    GeoMath::TurnData turn;
    turn.e1p0 = node777->location().geo();
    turn.e1p1 = node666->location().geo();
    turn.e1HeadingRad = GeoMath::getRadiansFromPoints(turn.e1p0, turn.e1p1);
    turn.e2p0 = node666->location().geo();
    turn.e2p1 = node555->location().geo();
    turn.e2HeadingRad = GeoMath::getRadiansFromPoints(turn.e2p0, turn.e2p1);
    turn.radius = 5;

    GeoMath::TurnArc arc;
    GeoMath::calculateTurn(turn, arc, host);

    EXPECT_EQ(arc.p0.latitude, 40); 
    EXPECT_NEAR(arc.p0.longitude, 47.9289, 0.01); 
    EXPECT_NEAR(arc.p1.latitude, 38.5355, 0.01); 
    EXPECT_NEAR(arc.p1.longitude, 51.4645, 0.01); 
    EXPECT_EQ(arc.arcRadius, 5); 
    EXPECT_EQ(arc.arcStartAngle, GeoMath::pi() / 2); 
    EXPECT_EQ(arc.arcEndAngle, GeoMath::pi() / 4); 
    EXPECT_EQ(arc.arcDeltaAngle, -GeoMath::pi() / 4); 
    EXPECT_TRUE(arc.arcClockwise); 

    auto maneuver = factory->taxiTurn(flight, arc, chrono::milliseconds(900), ManeuverFactory::TaxiType::Normal);
    
    maneuver->progressTo(chrono::milliseconds(1000));
    cout << "loc(0/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    cout << "hdg(0/3)   = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 40, 47.9289, 90);

    maneuver->progressTo(chrono::milliseconds(1300));
    cout << "loc(1/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    cout << "hdg(1/3) = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 39.8296, 49.223, 105);

    maneuver->progressTo(chrono::milliseconds(1600));
    cout << "loc(2/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    cout << "hdg(2/3) = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 39.3301, 50.4289, 120);

    maneuver->progressTo(chrono::milliseconds(1900));
    cout << "loc(3/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    cout << "hdg(3/3)   = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 38.5355, 51.4645, 135);
}

TEST(ManeuverFactoryTest, taxiTurn_kjfk_1) {
    shared_ptr<TestHostServices> host(new TestHostServices());
    shared_ptr<ManeuverFactory> factory(new ManeuverFactory(host));

    auto flight = createTestFlight(host);
    auto changeSet = make_shared<World::ChangeSet>();
    flight->onChanges([=](){ return changeSet; });

    GeoMath::TurnData turn;
    turn.e1p0 = GeoPoint(40.64606073,-73.79528695);
    turn.e1p1 = GeoPoint(40.64567687, -73.79617209);
    turn.e1HeadingRad = GeoMath::getRadiansFromPoints(turn.e1p0, turn.e1p1);
    turn.e2p0 = GeoPoint(40.64567687, -73.79617209);
    turn.e2p1 = GeoPoint(40.64540975, -73.79605748);
    turn.e2HeadingRad = GeoMath::getRadiansFromPoints(turn.e2p0, turn.e2p1);
    turn.radius = 0.00015;

    GeoMath::TurnArc arc;
    GeoMath::calculateTurn(turn, arc, host);

    auto maneuver = factory->taxiTurn(flight, arc, chrono::milliseconds(900), ManeuverFactory::TaxiType::Normal);
    
    maneuver->progressTo(chrono::milliseconds(1000));
    // cout << "loc(0/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    // cout << "hdg(0/3)   = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 40.6457,-73.796, 246.555);

    maneuver->progressTo(chrono::milliseconds(1300));
    // cout << "loc(1/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    // cout << "hdg(1/3) = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 40.6457,-73.7961, 216.629);

    maneuver->progressTo(chrono::milliseconds(1600));
    // cout << "loc(2/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    // cout << "hdg(2/3) = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 40.6456,-73.7961, 186.704);

    maneuver->progressTo(chrono::milliseconds(1900));
    // cout << "loc(3/3)   = " << flight->aircraft()->location().latitude << "," << flight->aircraft()->location().longitude << endl;
    // cout << "hdg(3/3)   = " << flight->aircraft()->attitude().heading() << endl;
    EXPECT_AIRCRAFT_POSITION(flight, 40.6455,-73.7961, 156.778);
}

TEST(ManeuverFactoryTest, taxiByPath) {
    shared_ptr<TestHostServices> host(new TestHostServices());
    shared_ptr<ManeuverFactory> factory(new ManeuverFactory(host));
    
    // shared_ptr<World> world(new World(host, 0));
    // host->useWorld(world);

    auto airport = createTestAirport(host);
    auto taxiNet = airport->taxiNet();
    auto flight = createTestFlight(host);
    auto changeSet = make_shared<World::ChangeSet>();
    flight->onChanges([=](){ return changeSet; });

    // auto taxiClearance = createTaxiClearance(airport, 111, 777);
    // flight->addClearance(taxiClearance);

    auto taxiPath = TaxiPath::find(taxiNet, taxiNet->getNodeById(111), taxiNet->getNodeById(777));
    auto maneuver = factory->taxiByPath(flight, taxiPath, ManeuverFactory::TaxiType::Normal);

    //world->progressTo(chrono::seconds(1000000));

    for (int t = 1000 ; t < 100000 ; t += 1000)
    {
        maneuver->progressTo(chrono::milliseconds(t));
        cout << "t=" << t << " > "
             << flight->aircraft()->location().latitude  << ", " 
             << flight->aircraft()->location().longitude << ", "
             << flight->aircraft()->attitude().heading() 
             << endl;
    }
}

