// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 

#include <string>
#include <iostream>
#include <sstream>
#include "libspeech.h"

#define EXPORTED_FUNCTION extern "C" __attribute__((visibility("default")))

EXPORTED_FUNCTION bool initSpeechThread(WriteLogCallback writeLog)
{
    writeLog("libspeechlin::initSpeechThread(): OK");
    return true;
}

EXPORTED_FUNCTION void cleanupSpeechThread(WriteLogCallback writeLog)
{
    writeLog("libspeechlin::cleanupSpeechThread(): OK");
}

EXPORTED_FUNCTION bool synthesizeSpeech(WriteLogCallback writeLog, const SpeechSynthesisRequest* request, SpeechSynthesisReply *reply)
{
    writeLog("libspeechlin::synthesizeSpeech(): not implemented");
    return false;
}
