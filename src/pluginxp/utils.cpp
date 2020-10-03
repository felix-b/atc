// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <cstdarg>
#include <cstring>
#include <string>
#include <chrono>
#include "XPLMUtilities.h"
#include "XPLMPlugin.h"
#include "utils.h"

#define LOG_BUFFER_SIZE 512
#define LOG_PREFIX_FORMAT "AT&C [+%10lld] "
#define LOG_PREFIX_LENGTH 19

using namespace std;

static LogTimePoint globalLogStartTime = {}; // NOLINT(cert-err58-cpp)
static const char *globalBuildPlatformId =
#if APL
    "APL"
#elif IBM
    "IBM"
#elif LIN
    "LIN"
#else
    "???"
#endif
;

void initLogStartTime()
{
    globalLogStartTime = chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
}

LogTimePoint getLogStartTime()
{
    return globalLogStartTime;
}

const char *getBuildPlatformId()
{
    return globalBuildPlatformId;
}

void PrintDebugString(const char* format, ...)
{
    char buffer[LOG_BUFFER_SIZE];

    auto now = std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
    auto millisecondsTimestamp = now - globalLogStartTime;
    snprintf(buffer, LOG_PREFIX_LENGTH + 1, LOG_PREFIX_FORMAT, millisecondsTimestamp.count());

	va_list argptr;
	va_start(argptr, format);
    int messageLength = vsnprintf(buffer + LOG_PREFIX_LENGTH, LOG_BUFFER_SIZE - LOG_PREFIX_LENGTH - 2, format, argptr);
	va_end(argptr);

    if (messageLength >= 0 && messageLength < LOG_BUFFER_SIZE - LOG_PREFIX_LENGTH - 2)
    {
        buffer[LOG_PREFIX_LENGTH + messageLength] = '\n';
        buffer[LOG_PREFIX_LENGTH + messageLength + 1] = '\0';
    }
    else
    {
        strncpy(buffer, "WARNING: log message skipped, buffer overrun!", LOG_BUFFER_SIZE);
    }

    XPLMDebugString(buffer);
}

std::string getPluginDirectory()
{
    char name[256];
    char filePath[256];
    char signature[256];
    char description[256];
    XPLMGetPluginInfo(XPLMGetMyID(), name, filePath, signature, description);
    XPLMExtractFileAndPath(filePath);
    XPLMExtractFileAndPath(filePath);
    
    return std::string(filePath);
}
