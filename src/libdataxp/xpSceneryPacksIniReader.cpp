//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <iostream>
#include <utility>
#include <system_error>
#include "stlhelpers.h"
#include "libworld.h"
#include "libdataxp.h"

using namespace std;
using namespace world;

XPSceneryPacksIniReader::XPSceneryPacksIniReader(shared_ptr<HostServices> _host) :
    m_host(_host)
{
}

void XPSceneryPacksIniReader::readSceneryFolderList(istream& input, vector<string>& sceneryFolders)
{
    vector<Line> iniLines;
    parseInputLines(input, iniLines);

    for (const Line& line : iniLines)
    {
        if (line.token == "SCENERY_PACK" && line.suffix.length() > 0)
        {
            sceneryFolders.push_back(line.suffix);
        }
    }
}

