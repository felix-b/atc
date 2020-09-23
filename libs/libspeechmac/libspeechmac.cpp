// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 

#include <string>
#include <iostream>
#include <sstream>
#include "libspeech.h"
#include "synthesizer.h"

using namespace std;

#define EXPORTED_FUNCTION extern "C" __attribute__((visibility("default")))
extern "C" int cocoaEnumSpeechVoices(CocoaEnumSpeechVoicesCallback callback);
extern "C" int cocoaSynthesizeSpeech(const char *voiceId, const char* text, const char* filePath);

EXPORTED_FUNCTION bool initSpeechThread(WriteLogCallback writeLog)
{
    writeLog("LIBSPEECHMAC::initSpeechThread(): OK");
    return true;
}

EXPORTED_FUNCTION void cleanupSpeechThread(WriteLogCallback writeLog)
{
    writeLog("LIBSPEECHMAC::cleanupSpeechThread(): OK");
   //cout << "LIBSPEECHMAC::cleanupSpeechThread()" << endl;
}

EXPORTED_FUNCTION bool synthesizeSpeech(WriteLogCallback writeLog, const SpeechSynthesisRequest* request, SpeechSynthesisReply *reply)
{
    const char *platformVoiceId = "com.apple.speech.synthesis.voice.karen.premium";

    stringstream log;
    log << "LIBSPEECHMAC::synthesizeSpeech(outputFilePath=" << request->outputFilePath << "): begin";
    writeLog(log.str().c_str());

    //cout << "LIBSPEECHMAC::synthesizeSpeech()" << endl;
    int result = cocoaSynthesizeSpeech(
        platformVoiceId,
        request->text, 
        request->outputFilePath);

    writeLog(("LIBSPEECHMAC::synthesizeSpeech(): end: result=" + to_string(result)).c_str());

    reply->errorCode = (result == 1 ? ERROR_CODE_NONE : ERROR_CODE_UNSPECIFIED);
    reply->platformVoiceId = platformVoiceId;
    
    return (result == 1);
}
