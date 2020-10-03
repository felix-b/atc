// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <chrono>

using namespace std;

typedef chrono::time_point<chrono::high_resolution_clock, chrono::milliseconds> LogTimePoint;

const char *getBuildPlatformId();
void initLogStartTime();
LogTimePoint getLogStartTime();
void PrintDebugString(const char* format, ...); //TODO: camelCase
string getPluginDirectory();
