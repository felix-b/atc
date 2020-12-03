//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <fstream>
#include <sstream>
#include <vector>
#include <unordered_set>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libdataxp.h"
#include "libworld_test.h"
#include "libdataxp_test.h"

using namespace world;
using namespace std;


TEST(XPSceneryPacksIniReaderTest, readSceneryPacksIni) {
    auto host = TestHostServices::createWithWorld();
    ifstream iniFile;
    openTestInputStream("scenery_packs.ini", iniFile);

    vector<string> sceneryFolders;
    XPSceneryPacksIniReader reader(host);
    reader.readSceneryFolderList(iniFile, sceneryFolders);

    ASSERT_EQ(sceneryFolders.size(), 5);

    EXPECT_EQ(sceneryFolders.at(0), "Custom Scenery/My Scenery One/");
    EXPECT_EQ(sceneryFolders.at(1), "Custom Scenery/MySceneryThree/");
    EXPECT_EQ(sceneryFolders.at(2), "Custom Scenery/Global Airports/");
    EXPECT_EQ(sceneryFolders.at(3), "Custom Scenery/My Scenery Four/");
    EXPECT_EQ(sceneryFolders.at(4), "Custom Scenery/My Scenery Five/");
}
