//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <cstdarg>
#include <cstring>
#include <string>
#include <chrono>
#include "libworld.h"

#define LOG_BUFFER_SIZE 512
#define LOG_PREFIX_FORMAT "AT&C [+%10lld] "
#define LOG_PREFIX_LENGTH 19

namespace world
{
    HostServices::LogTimePoint HostServices::logStartTime = {}; // NOLINT(cert-err58-cpp)

    void HostServices::initLogString()
    {
        logStartTime = chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
    }

    void HostServices::formatLogString(char logString[LOG_BUFFER_SIZE], const char *format, va_list args)
    {
        auto now = std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
        auto millisecondsTimestamp = now - logStartTime;
        snprintf(logString, LOG_PREFIX_LENGTH + 1, LOG_PREFIX_FORMAT, millisecondsTimestamp.count());

        int messageLength = vsnprintf(logString + LOG_PREFIX_LENGTH, LOG_BUFFER_SIZE - LOG_PREFIX_LENGTH - 2, format, args);
        if (messageLength >= 0 && messageLength < LOG_BUFFER_SIZE - LOG_PREFIX_LENGTH - 2)
        {
            logString[LOG_PREFIX_LENGTH + messageLength] = '\n';
            logString[LOG_PREFIX_LENGTH + messageLength + 1] = '\0';
        }
        else
        {
            strncpy(logString, "WARNING: log message skipped, buffer overrun!", LOG_BUFFER_SIZE);
        }
    }
}
