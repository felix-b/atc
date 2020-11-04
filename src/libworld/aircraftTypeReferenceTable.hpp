//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include "libworld.h"

using namespace std;

namespace world
{
    class AircraftTypeReferenceTable
    {
    public:
        struct Entry
        {
            string icao;
            string name;
            string callsign;
        };
    public:
        static bool tryFindByIcao(const string& icao, Entry& entry);
    private:
        static void parseEntry(const char *rowPtr, Entry& entry);
    };
}
