// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 

#include <memory>
#include <chrono>
#include <thread>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libserver.hpp"
#include "libworld_test.h"
#include "testClient.hpp"

using namespace std;
using namespace world;
using namespace server;

static void waitForState(
    const string& description,
    function<bool()> predicate,
    chrono::milliseconds timeout);

TEST(ServerE2ETest, sendHello)
{
    auto host = TestHostServices::create();
    host->enableLogs(true);

    auto controller = ServerControllerInterface::create(host);

    controller->start();
    this_thread::sleep_for(chrono::milliseconds(250));

    TestClient client;
    client.connect();

    waitForState("Connection open", [&]{ return client.openState().load(); }, chrono::milliseconds(3000));

    world_proto::ClientToServer envelope;
    envelope.mutable_connect()->set_token("HELLO");
    client.send(envelope);

    waitForState("Received 1 message", [&]{ return client.receivedMessagesCount() > 0; }, chrono::milliseconds(3000));

    vector<world_proto::ServerToClient> messages;
    client.copyReceivedMessagesTo(messages);

    EXPECT_EQ(messages[0].payload_case(), world_proto::ServerToClient::kReplyConnect);
    EXPECT_EQ(messages[0].reply_connect().server_banner(), "AT&C plugin");

    client.disconnect();

    waitForState("Connection closed", [&]{ return client.closedState().load(); }, chrono::milliseconds(3000));
    EXPECT_TRUE(client.closedState().load());

    controller->beginStop();

    EXPECT_TRUE(controller->waitUntilStopped(chrono::milliseconds(3000)));

    client.assertNoErrors();
    controller.reset();
}

static chrono::time_point<chrono::high_resolution_clock, chrono::milliseconds> getNow()
{
    return std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
}

static void waitForState(const string& description, function<bool()> predicate, chrono::milliseconds timeout)
{
    auto time0 = getNow();

    while (!predicate())
    {
        if ((getNow() - time0).count() > timeout.count())
        {
            EXPECT_EQ(description, "");
            return;
        }

        this_thread::sleep_for(chrono::milliseconds(100));
    }
}
