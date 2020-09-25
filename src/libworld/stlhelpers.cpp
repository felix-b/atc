// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include <random>
#include "time.h"
#include "stlhelpers.h"

bool stringStartsWith(const string& s, const string& prefix)
{
    string::size_type index = s.find(prefix, 0);
    return (index == 0);
}

time_t initTime(int year, int month, int day, int hour, int min, int sec)
{
    struct tm timeinfo;
    
    timeinfo.tm_year = year - 1900;
    timeinfo.tm_mon = month - 1;
    timeinfo.tm_mday = day;
    timeinfo.tm_hour = hour;
    timeinfo.tm_min = min;
    timeinfo.tm_sec = sec;

    return timegm(&timeinfo);
}
