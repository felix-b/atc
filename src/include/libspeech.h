// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once
#include <cstdlib>

//===========================================================================
// C API for integration of platform-specific speech libraries
// No C++ or STL is allowed - avoid compiler lock-in
//===========================================================================

// all constants must match values from libworld.h

#define GENDER_MALE 1
#define GENDER_FEMALE 2

#define VOICE_BASS 1
#define VOICE_BARITONE 2
#define VOICE_TENOR 3
#define VOICE_COUNTERTENOR 4
#define VOICE_CONTRALTO 5
#define VOICE_MEZZOSOPRANO 6
#define VOICE_SOPRANO 7
#define VOICE_TREBLE 8

#define RATE_SLOW 1
#define RATE_MEDIUM 2
#define RATE_FAST 3

#define QUALITY_POOR 1
#define QUALITY_MEDIUM 2
#define QUALITY_GOOD 3

#define SPEECH_PART_TEXT 1
#define SPEECH_PART_DATA 2
#define SPEECH_PART_DISFLUENCY 3
#define SPEECH_PART_CORRECTION 4

#define FILE_FORMAT_RIFF 1
#define FILE_FORMAT_AIFF 2

#define ERROR_CODE_NONE 0
#define ERROR_CODE_UNSPECIFIED 1
#define ERROR_CODE_INPUT 2
#define ERROR_CODE_TOO_LONG 3
#define ERROR_CODE_NO_VOICE 4
#define ERROR_CODE_PREPARE_FAILED 5
#define ERROR_CODE_SELECT_VOICE_FAILED 6
#define ERROR_CODE_SYNTHESIZER_FAILED 7

typedef struct {
    size_t size;
    const char* text;
    const char* outputFilePath;
    const char* platformVoiceId;
    int gender;
    int voice;        
    int rate;
    int quality;
} SpeechSynthesisRequest;

typedef struct {
    size_t size;
    int errorCode;
    const char *platformVoiceId;
} SpeechSynthesisReply;

typedef void (*WriteLogCallback)(const char* message);

#if IBM == 1
    #define DECLSPEC __declspec(dllexport)
#else
    #define DECLSPEC
#endif

extern "C" DECLSPEC bool initSpeechThread(WriteLogCallback writeLog);
extern "C" DECLSPEC void cleanupSpeechThread(WriteLogCallback writeLog);
extern "C" DECLSPEC bool synthesizeSpeech(WriteLogCallback writeLog, const SpeechSynthesisRequest *request, SpeechSynthesisReply *reply);
