//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <string>
#include <fstream>
#include <sstream>
#include <vector>
#include <unordered_set>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libdataxp.h"
#include "libworld_test.h"

using namespace world;
using namespace std;

stringstream makeAptDat(const vector<string>& lines);
shared_ptr<HostServices> makeHost();
shared_ptr<ControlledAirspace> makeAirspace(double centerLat, double centerLon, float radiusNm, const string& name);
void openTestInputStream(const string& fileName, ifstream& str);
string getTestInputFilePath(const string& fileName);
string getTestOutputFilePath(const string& fileName);
void assertRunwaysExist(shared_ptr<Airport> airport, const vector<string>& names);
void assertGatesExist(shared_ptr<Airport> airport, const vector<string>& names);
void assertTaxiEdgesExist(shared_ptr<Airport> airport, const unordered_set<string>& names);
