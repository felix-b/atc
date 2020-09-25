// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <cstdarg>
#include <string>
#include <fstream>
#include <streambuf>
#include <algorithm> 
#include <cctype>
#include <locale>
#include <random>
#include <string.h>
#include "XPLMUtilities.h"
#include "XPLMPlugin.h"

void PrintDebugString(const char* format, ...)
{
	char buffer[512];
	va_list argptr;
	va_start(argptr, format);
	vsnprintf(buffer, 512, format, argptr);
	va_end(argptr);
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

