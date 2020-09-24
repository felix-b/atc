// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 

#pragma warning(disable : 4996)

#include "pch.h"
#include "libspeech.h"
#include "libSpeechCommon.hpp"

using namespace std;

#define GLOBAL_STRING_LENGTH 2048

typedef PlatformVoiceDescriptor<CComPtr<ISpObjectToken>> SapiVoiceDescriptor;

static ISpVoice *globalVoice = NULL;
static unordered_map<string, SapiVoiceDescriptor> globalPlatformVoices;
static wchar_t globalText[GLOBAL_STRING_LENGTH] = { 0 };
static wchar_t globalFilePath[GLOBAL_STRING_LENGTH] = { 0 };

bool discoverPlatformVoices(WriteLogCallback writeLog);
bool addPlatformVoice(CComPtr<ISpObjectToken> token, const string& name, long long lcid, int gender);
const SapiVoiceDescriptor *findPlatformVoice(WriteLogCallback writeLog, const SpeechSynthesisRequest& request, SpeechSynthesisReply& reply);
const string pickPlatformVoice(WriteLogCallback writeLog, const SpeechSynthesisRequest& request);
const string getSpeechMarkup(const SpeechSynthesisRequest& request, const SapiVoiceDescriptor& voice);
void logEffectivePlatformVoices(WriteLogCallback writeLog);
int getGenderCode(const string& genderString);
string toNarrow(const wchar_t* s, char dfault = '?', const std::locale& loc = std::locale());
void writeLogWithHResult(WriteLogCallback writeLog, const string& message, HRESULT hr);
bool copyToGlobalString(const char* source, wchar_t* destination);
void logSynthesizeSpeechCall(WriteLogCallback writeLog, const SpeechSynthesisRequest* request, SpeechSynthesisReply* reply);

bool initSpeechThread(WriteLogCallback writeLog)
{
    HRESULT hr = ::CoInitialize(NULL);
    if (FAILED(hr))
    {
        writeLogWithHResult(writeLog, "CoInitialize FAILED", hr);
        return false;
    }

    hr = CoCreateInstance(CLSID_SpVoice, NULL, CLSCTX_ALL, IID_ISpVoice, (void**)&globalVoice);
    if (FAILED(hr))
    {
        writeLogWithHResult(writeLog, "CoCreateInstance(CLSID_SpVoice) FAILED", hr);
        return false;
    }

    if (!discoverPlatformVoices(writeLog))
    {
        writeLog("libspeechwin: failed to discover platform voices");
        return false;
    }

    logEffectivePlatformVoices(writeLog);
    writeLog("libspeechwin: successfully initialized");
    return true;
}

void cleanupSpeechThread(WriteLogCallback writeLog)
{
    writeLog("libspeechwin: cleanup");

    globalPlatformVoices.clear();
    if (globalVoice)
    {
        globalVoice->Release();
        globalVoice = NULL;
    }

    ::CoUninitialize();
}

bool synthesizeSpeech(WriteLogCallback writeLog, const SpeechSynthesisRequest *request, SpeechSynthesisReply *reply)
{
    //logSynthesizeSpeechCall(writeLog, request, reply);

    if (!globalVoice || !writeLog || !request || !request->text || !request->outputFilePath || !reply )
    {
        if (reply)
        {
            reply->errorCode = ERROR_CODE_INPUT;
        }
        return false;
    }

    if (!copyToGlobalString(request->outputFilePath, globalFilePath))
    {
        reply->errorCode = ERROR_CODE_TOO_LONG;
        return false;
    }

    const SapiVoiceDescriptor *voice = findPlatformVoice(writeLog, *request, *reply);
    if (!voice)
    {
        reply->errorCode = ERROR_CODE_NO_VOICE;
        return false;
    }

    string markup = getSpeechMarkup(*request, *voice);
    if (!copyToGlobalString(markup.c_str(), globalText))
    {
        reply->errorCode = ERROR_CODE_TOO_LONG;
        return false;
    }

    CSpStreamFormat format;
    format.AssignFormat(SPSF_16kHz16BitMono); //TODO: vary by request->quality value

    ISpStream* outputStream = nullptr;
    HRESULT hr = SPBindToFile(globalFilePath, SPFM_CREATE_ALWAYS, &outputStream, &format.FormatId(), format.WaveFormatExPtr());
    if (FAILED(hr))
    {
        reply->errorCode = ERROR_CODE_PREPARE_FAILED;
        writeLogWithHResult(writeLog, "SPBindToFile failed", hr);
        return false;
    }

    hr = globalVoice->SetOutput(outputStream, TRUE);
    if (FAILED(hr))
    {
        reply->errorCode = ERROR_CODE_PREPARE_FAILED;
        writeLogWithHResult(writeLog, "ISpVoice::SetOutput failed", hr);
        return false;
    }

    hr = globalVoice->SetVoice(voice->token());
    if (FAILED(hr))
    {
        reply->errorCode = ERROR_CODE_SELECT_VOICE_FAILED;
        writeLogWithHResult(writeLog, "ISpVoice::SetVoice failed (" + voice->description() + ")", hr);
        return false;
    }

    hr = globalVoice->Speak(
        globalText,
        SPF_PURGEBEFORESPEAK | SPF_IS_XML,
        NULL);

    if (SUCCEEDED(hr))
    {
        reply->errorCode = ERROR_CODE_NONE;
    }
    else
    {
        reply->errorCode = ERROR_CODE_SYNTHESIZER_FAILED;
        writeLogWithHResult(writeLog, "ISpVoice::Speak failed", hr);
    }

    if (outputStream)
    {
        outputStream->Close();
    }

    return SUCCEEDED(hr);
}

bool discoverPlatformVoices(WriteLogCallback writeLog)
{
    ULONG ulCount = 0;
    CComPtr<ISpObjectToken> cpVoiceToken;
    CComPtr<IEnumSpObjectTokens> cpEnum;
    HRESULT hr = 0;

    hr = SpEnumTokens(SPCAT_VOICES, NULL, NULL, &cpEnum);
    if (FAILED(hr))
    {
        writeLogWithHResult(writeLog, "SpEnumTokens failed", hr);
        return false;
    }

    hr = cpEnum->GetCount(&ulCount);
    if (FAILED(hr))
    {
        writeLogWithHResult(writeLog, "IEnumSpObjectTokens::getCount failed", hr);
        return false;
    }

    writeLogWithHResult(writeLog, to_string(ulCount) + " voices installed in total", 0);

    while (ulCount--)
    {
        cpVoiceToken.Release();

        string name;
        string description;
        string genderString;
        string languageString;
        int genderCode = 0;
        long long lcid = 0;
        CComPtr<ISpDataKey> attributes;
        WCHAR* wcharString;

        hr = cpEnum->Next(1, &cpVoiceToken, NULL);
        if (FAILED(hr))
        {
            writeLogWithHResult(writeLog, "IEnumSpObjectTokens::Next failed", hr);
            return false;
        }

        stringstream voiceLog;
        voiceLog << "found VOICE> ";

        if (SUCCEEDED(hr))
        {
            hr = SpGetDescription(cpVoiceToken, &wcharString);
            if (SUCCEEDED(hr))
            {
                description = toNarrow(wcharString);
            }
            else
            {
                voiceLog << "[error SpGetDescription]";
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = cpVoiceToken->OpenKey(L"Attributes", &attributes);
            if (FAILED(hr))
            {
                voiceLog << "[error Attributes regkey]";
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = attributes->GetStringValue(L"Name", &wcharString);
            if (SUCCEEDED(hr))
            {
                name = toNarrow(wcharString);
            }
            else
            {
                voiceLog << "[error Name regvalue]";
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = attributes->GetStringValue(L"Language", &wcharString);
            if (SUCCEEDED(hr))
            {
                languageString = toNarrow(wcharString);
                lcid = strtoll(languageString.c_str(), nullptr, 16);
            }
            else
            {
                voiceLog << "[error Locale regvalue]";
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = attributes->GetStringValue(L"Gender", &wcharString);
            if (SUCCEEDED(hr))
            {
                genderString = toNarrow(wcharString);
                genderCode = getGenderCode(genderString);
            }
            else
            {
                voiceLog << "[error Gender regvalue]";
            }
        }

        voiceLog << " name[" << name
                 << " locale[" << lcid << "|" << languageString << "]"
                 << " gender[" << genderCode << "|" << genderString << "]"
                 << " description[" << description << "]";
        writeLogWithHResult(writeLog, voiceLog.str(), hr);

        if (SUCCEEDED(hr))
        {
            addPlatformVoice(cpVoiceToken, name, lcid, genderCode);
        }
    }

    return true;
}

bool addPlatformVoice(CComPtr<ISpObjectToken> token, const string& name, long long lcid, int gender)
{
    bool isEnglish = (lcid & 0xFF) == 0x09;

    if (!isEnglish)
    {
        return false;
    }

    string platformId = "sapi/" + name;
    SapiVoiceDescriptor descriptor(token, name, platformId, gender);
    globalPlatformVoices.insert({ platformId, descriptor });
    
    return true;
}

const SapiVoiceDescriptor *findPlatformVoice(
    WriteLogCallback writeLog,
    const SpeechSynthesisRequest& request,
    SpeechSynthesisReply& reply)
{
    unordered_map<string, SapiVoiceDescriptor>::iterator it;
    
    bool hasAssignedVoice = (
        request.platformVoiceId && 
        *request.platformVoiceId != '\0' &&
        (it = globalPlatformVoices.find(request.platformVoiceId)) != globalPlatformVoices.end());
    
    if (!hasAssignedVoice)
    {
        string newVoicePlatformId = pickPlatformVoice(writeLog, request);
        it = globalPlatformVoices.find(newVoicePlatformId);
    }

    if (it == globalPlatformVoices.end())
    {
        writeLog("libspeechwin: no voice available for the request");
        return nullptr;
    }

    reply.platformVoiceId = it->second.platformVoiceId().c_str();
    return &(it->second);
}

const string pickPlatformVoice(WriteLogCallback writeLog, const SpeechSynthesisRequest& request)
{
    for (const auto& entry : globalPlatformVoices)
    {
        if (entry.second.gender() == request.gender)
        {
            stringstream log;
            log << "libspeechwin: picked voice [" << entry.first << "] for params: "
                << "gender[" << request.gender << "] voice[" << request.voice << "]";
            writeLog(log.str().c_str());
            
            return entry.first;
        }
    }
    return "";
}

const string getSpeechMarkup(const SpeechSynthesisRequest& request, const SapiVoiceDescriptor& voice)
{
    stringstream markup;

    switch (request.rate)
    {
    case RATE_FAST:
        markup << "<rate absspeed='2'/>";
        break;
    case RATE_SLOW:
        markup << "<rate absspeed='0'/>";
        break;
    default:
        markup << "<rate absspeed='1'/>";
        break;
    }

    switch (request.voice)
    {
    case VOICE_BASS:
    case VOICE_CONTRALTO:
        markup << "<pitch absmiddle='-10'/>";
        break;
    case VOICE_BARITONE:
    case VOICE_MEZZOSOPRANO:
        markup << "<pitch absmiddle='-3'/>";
        break;
    case VOICE_TENOR:
    case VOICE_SOPRANO:
        markup << "<pitch absmiddle='3'/>";
        break;
    case VOICE_COUNTERTENOR:
    case VOICE_TREBLE:
        markup << "<pitch absmiddle='10'/>";
        break;
    }

    markup << request.text;
    return markup.str();
}

void logEffectivePlatformVoices(WriteLogCallback writeLog)
{
    writeLog("--- effective platform voices ---");
    for (const auto& entry : globalPlatformVoices)
    {
        stringstream entryLog;
        entryLog << "[id=" << entry.first << "] gender[" << entry.second.gender() << "] " << entry.second.description();
        writeLog(entryLog.str().c_str());
    }
    writeLog("--- end of platform voices ---");
}

string toNarrow(const wchar_t* s, char dfault, const std::locale& loc)
{
    std::ostringstream stm;
    while (*s != L'\0')
    {
        stm << std::use_facet< std::ctype<wchar_t> >(loc).narrow(*s++, dfault);
    }
    return stm.str();
}

int getGenderCode(const string& genderString)
{
    if (genderString == "Male")
    {
        return GENDER_MALE;
    }
    if (genderString == "Female")
    {
        return GENDER_FEMALE;
    }
    return 0;
}

void writeLogWithHResult(WriteLogCallback writeLog, const string& message, HRESULT hr)
{
    stringstream str;
    str << "libspeechwin: " << message << " HRESULT=0x" << hex << hr;
    writeLog(str.str().c_str());
}

bool copyToGlobalString(const char* source, wchar_t* destination)
{
    size_t numCharsToConvert = strlen(source) + 1;
    if (numCharsToConvert > GLOBAL_STRING_LENGTH)
    {
        return false;
    }

    size_t numCharsConverted;
    if (mbstowcs_s(&numCharsConverted, destination, GLOBAL_STRING_LENGTH, source, numCharsToConvert) != 0)
    {
        return false;
    }

    return (numCharsConverted == numCharsToConvert);
}

void logSynthesizeSpeechCall(WriteLogCallback writeLog, const SpeechSynthesisRequest* request, SpeechSynthesisReply* reply)
{
    void* requestPtr = (void*)request;
    std::stringstream str1;
    str1 << "synthesizeSpeech starting, request address =" << requestPtr;
    writeLog(str1.str().c_str());

    void* textPtr = (void*)request->text;
    void* outputFilePathPtr = (void*)request->outputFilePath;

    std::stringstream str2;
    str2 << "synthesizeSpeech args"
        << " : size=" << request->size
        << " ; textPtr^=" << textPtr
        << " ; outputFilePathPtr=" << outputFilePathPtr
        << " ; gender=" << request->gender
        << " ; voice=" << request->voice
        << " ; rate=" << request->rate
        << " ; platformVoiceId=" << request->platformVoiceId;

    writeLog(str2.str().c_str());
}
