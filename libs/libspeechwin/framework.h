// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 

#pragma once
#pragma warning(disable : 4996)

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <string>
#include <locale>
#include <iostream>
#include <sstream>
#include <unordered_map>
#define _ATL_APARTMENT_THREADED
#include <windows.h>
#include <atlbase.h>
#include <atlcom.h>
#include <sapi.h>
#include <sphelper.h>
