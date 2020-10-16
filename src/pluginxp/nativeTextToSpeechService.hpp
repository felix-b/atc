// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <sstream>
#include <iomanip>
#include <functional>
#include <chrono>
#include <cmath>
#include <queue>
#include <vector>
#include <atomic>
#include <random>
#include <thread>

// PPL 
#include "log.h"
#include "owneddata.h"

// concurrentqueue
#include "blockingconcurrentqueue.h"

// tnc
#include "libworld.h"
#include "stlhelpers.h"
#include "utils.h"
#include "libspeech.h"
#include "speechSoundBuffer.hpp"

#define CHECK_ERR() __CHECK_ERR(__FILE__,__LINE__)

using namespace std;
using namespace world;

class NativeTextToSpeechService : public TextToSpeechService
{
private:
    enum class ThreadMessageType
    {
        SynthesizeSpeech = 1,
        PlayRadioStaticPttOn = 2,
        PlayRadioSpeech = 3,
        PlayRadioStaticPttOff = 4,
        PlayRadioStaticMutualCancel = 5,
        AbortTransmission = 9,
        TerminateThread = 10
    };
    struct RadioSpeechStyle
    {
        chrono::milliseconds delayBeforePtt;
        chrono::milliseconds delayAfterPtt;
        chrono::milliseconds delayAfterSpeech;
        float staticVolume;
        float highPassFrequency;
    };
    struct ThreadMessage 
    {
        ThreadMessageType type;
        int requestId;
        shared_ptr<Frequency> frequency;
        shared_ptr<Actor> speaker;
        shared_ptr<Transmission> transmission;
        RadioSpeechStyle radioStyle;
    };
private:
    shared_ptr<HostServices> m_host;
    DataRef<int> m_com1Power;
    DataRef<int> m_com1FrequencyKhz;
    moodycamel::BlockingConcurrentQueue<ThreadMessage> m_messageQueue;
    shared_ptr<thread> m_synthesizerThread;
    shared_ptr<SpeechSoundBuffer> m_currentSpeech;
    int m_nextRequestId;
    int m_activeRequestId;
    ALSoundBuffer m_radioStaticEdgeLong;
    ALSoundBuffer m_radioStaticEdgeMedium;
    ALSoundBuffer m_radioStaticEdgeShort;
    ALSoundBuffer m_radioStaticBackgroundLoop;
    //string m_tempSpeechFilePath;
public:
    NativeTextToSpeechService(shared_ptr<HostServices> _host) :
        m_host(_host),
        m_nextRequestId(1),
        m_activeRequestId(0),
        m_com1Power("sim/cockpit2/radios/actuators/com1_power", PPL::ReadOnly),
        m_com1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadOnly),
        m_radioStaticEdgeLong(_host->getResourceFilePath({ "sounds", "radio-static-edge-l.wav"})),
        m_radioStaticEdgeMedium(_host->getResourceFilePath({ "sounds", "radio-static-edge-m.wav"})),
        m_radioStaticEdgeShort(_host->getResourceFilePath({ "sounds", "radio-static-edge-s.wav"})),
        m_radioStaticBackgroundLoop(_host->getResourceFilePath({ "sounds", "radio-static-loop-1.wav"}))
    {
        m_radioStaticBackgroundLoop.setLoop(true);
        m_synthesizerThread = shared_ptr<thread>(new thread([this](){ 
            runSynthesizerThread();
        }));
        //m_tempSpeechFilePath = m_host->getResourceFilePath({ "speech", "tempgen.wav" });
    }

    virtual ~NativeTextToSpeechService()
    {
        m_host->writeLog("NATT2S|NativeTextToSpeechService::~ctor");
        
        m_messageQueue.enqueue({ ThreadMessageType::TerminateThread });
        m_synthesizerThread->join();
    }
public:

    QueryCompletion vocalizeTransmission(shared_ptr<Frequency> frequency, shared_ptr<Transmission> transmission) override
    {
        m_host->writeLog("NATT2S|vocalizeTransmission(frequency=%p, transmission=%p)", frequency.get(), transmission.get());

        if (!transmission || !transmission->verbalizedUtterance())
        {
            m_host->writeLog("NATT2S|vocalizeTransmission ERROR: transmission was not verbalized!");
            throw runtime_error("vocalizeTransmission: transmission was not verbalized");
        }

        auto world = m_host->getWorld();

        int com1Power = m_com1Power;
        int com1FrequencyKhz = m_com1FrequencyKhz;
        bool isHeardByUser = (com1Power == 1 && com1FrequencyKhz == frequency->khz());
        
        m_host->writeLog(
            "NATT2S|vocalizeTransmission : com1Power=%d, com1FrequencyKhz=%d, isHeardByUser=%s",
            com1Power, com1FrequencyKhz, (isHeardByUser ? "Y" : "N"));

        auto speaker = transmission->intent()->getSpeakingActor();
        if (!speaker)
        {
            m_host->writeLog("NATT2S|vocalizeTransmission ERROR: transmission has no assigned actor!");
            throw runtime_error("Speaking actor could not be obtained for this transmission");
        }

        int newRequestId = m_nextRequestId;
        m_nextRequestId += 2;

        if (isHeardByUser)
        {
            m_host->writeLog("NATT2S|enqueue to SPECHW: (SynthesizeSpeech, requestId=%d), m_nextRequestId=%d", newRequestId, m_nextRequestId);

            RadioSpeechStyle styleToRender;
            setSpeechStyleToRender(*speaker, styleToRender, speaker->role());

            m_messageQueue.enqueue({
                ThreadMessageType::SynthesizeSpeech,
                newRequestId,
                frequency,
                speaker,
                transmission,
                styleToRender
            });

            return [this, newRequestId](){
                return (m_activeRequestId > newRequestId);
            };
        }

        chrono::milliseconds speechDuration = countSpeechDuration(transmission->verbalizedUtterance()->plainText());
        chrono::microseconds completionTimestamp = world->timestamp() + speechDuration;
        return [world, completionTimestamp]() {
            return (world->timestamp() >= completionTimestamp);
        };

        // handleSynthesizeSpeechRequest({ 
        //     ThreadMessageType::SynthesizeSpeech,
        //     requestId, 
        //     frequencyKhz, 
        //     speaker, 
        //     intent
        // });

        // if (!isHeardByUser)
        // {
        // chrono::milliseconds speechDuration = countSpeechDuration(intent->transmissionText());
        // chrono::microseconds completionTimestamp = world->timestamp() + speechDuration;
        // return [world, completionTimestamp]() {
        //     return (world->timestamp() >= completionTimestamp);
        // };
        //}
    }

    void clearAll() override
    {
        m_host->writeLog("NATT2S|clearing all transmissions");
        m_messageQueue.enqueue({ ThreadMessageType::AbortTransmission, m_activeRequestId });
    }

private:

    void runSynthesizerThread()
    {
        m_host->writeLog("SPECHW|entering worker thread");
        if (!initializeSynthesizerThread())
        {
            return;
        }

        while (true)
        {
            m_host->writeLog("SPECHW|worker thread is waiting for a message");

            ThreadMessage message;
            if (m_messageQueue.wait_dequeue_timed(message, 5000000))
            {
                m_host->writeLog(
                    "SPECHW|dequeued (type=%d, requestId=%d), m_activeRequestId=%d",
                    message.type, message.requestId, m_activeRequestId);

                if (message.type == ThreadMessageType::TerminateThread)
                {
                    break;
                }

                try
                {
                    handleSynthesizerThreadMessage(message);
                }
                catch (const exception &e)
                {
                    m_host->writeLog("SPECHW|handleSynthesizerThreadMessage CRASHED!!! %s", e.what());
                }
            }
        }

        finalizeSynthesizerThread();
        m_host->writeLog("SPECHW|exiting worker thread");
    }

    bool initializeSynthesizerThread()
    {
        try
        {
            bool initResult = initSpeechThread(&logSpeechLibraryMessage);
            m_host->writeLog("SPECHW|initSpeechThread - %s", initResult ? "SUCCESS" : "FAILURE");
            return initResult;
        }
        catch (const exception& e)
        {
            m_host->writeLog("SPECHW|initSpeechThread CRASHED!!! %s", e.what());
            return false;
        }
    }

    void finalizeSynthesizerThread()
    {
        try
        {
            cleanupSpeechThread(&logSpeechLibraryMessage);
            m_host->writeLog("SPECHW|cleanupSpeechThread finished.");
        }
        catch(const exception& e)
        {
            m_host->writeLog("SPECHW|cleanupSpeechThread CRASHED!!! %s", e.what());
        }
    }

    void handleSynthesizerThreadMessage(const ThreadMessage& message)
    {
        switch (message.type)
        {
        case ThreadMessageType::AbortTransmission:
            if (message.requestId == m_activeRequestId && m_currentSpeech)
            {
                m_host->writeLog("SPECHW|aborting current transmission request id[%d]", message.requestId);
                stopAllSounds();
            }
            else
            {
                m_host->writeLog(
                    "SPECHW|WARNING: could not abort transmission request id[%d], m_activeRequestId=%d",
                    message.requestId, m_activeRequestId);
            }
            return;
        case ThreadMessageType::SynthesizeSpeech:
            try
            {
                handleSynthesizeSpeechRequest(message);
            }
            catch(const exception& e)
            {
                m_host->writeLog("SPECHW|synthesis failed: %s", e.what());
            }
            break;
        case ThreadMessageType::PlayRadioStaticPttOn:
            if (isReqeustStillActive(message.requestId) && m_currentSpeech)
            {
                m_host->writeLog("SPECHW|request id[%d], playing PTT-PUSH", message.requestId);
                ALSoundBuffer& pttOnSound = message.radioStyle.delayAfterPtt < chrono::milliseconds(1)
                                            ? m_radioStaticEdgeShort
                                            : message.radioStyle.delayAfterPtt <= chrono::milliseconds(500) ? m_radioStaticEdgeMedium : m_radioStaticEdgeLong;
                pttOnSound.play(message.radioStyle.staticVolume);
                m_radioStaticBackgroundLoop.play(max(0.1f, message.radioStyle.staticVolume - 0.1f));
            }
            break;
        case ThreadMessageType::PlayRadioSpeech:
            if (isReqeustStillActive(message.requestId) && m_currentSpeech)
            {
                float volumeBumpForHighPassFilter = message.radioStyle.highPassFrequency / 1000.0f;
                m_host->writeLog("SPECHW|request id[%d], playing radio speech", message.requestId);
                m_currentSpeech->play(1.0f + volumeBumpForHighPassFilter + message.radioStyle.staticVolume);
            }
            break;
        case ThreadMessageType::PlayRadioStaticPttOff:
            if (isReqeustStillActive(message.requestId) && m_currentSpeech)
            {
                m_host->writeLog("SPECHW|request id[%d], playing PTT-RELEASE", message.requestId);
                m_radioStaticEdgeShort.play(message.radioStyle.staticVolume);
                m_currentSpeech->stop();
                m_radioStaticBackgroundLoop.stop();
                m_activeRequestId++;
                m_host->writeLog("SPECHW|COMPLETED request id[%d], m_activeRequestId=%d", m_activeRequestId);
            }
            break;
        default:
            m_host->writeLog("SPECHW|WARNING! message type unknown: %d", (int)message.type);
        }
    }

    void handleSynthesizeSpeechRequest(const ThreadMessage& message)
    {
        m_host->writeLog(
            "SPECHW|synthesizing speech request id[%d][speaker=%p]: %s",
            message.requestId, message.speaker.get(),  message.transmission->verbalizedUtterance()->plainText().c_str());

        string speechFilePath = m_host->getResourceFilePath({ "speech", "temp.wav" });

        SpeechSynthesisRequest request;
        request.size = sizeof(request);
        request.text = message.transmission->verbalizedUtterance()->plainText().c_str();
        request.outputFilePath = speechFilePath.c_str();
        
        const auto& style = message.speaker->speechStyle();

        request.gender = (int)style.gender;
        request.voice = (int)style.voice;
        request.rate = (int)style.rate;
        request.quality = (int)style.radioQuality;
        request.platformVoiceId = style.platformVoiceId.c_str();
        m_host->writeLog(
            "SPECHW|request id[%d]: request prepared, speech style> gender[%d] voice[%d] rate[%d] quality[%d] platformVoiceId[%s]",
            message.requestId, request.gender, request.voice, request.rate, request.quality, request.platformVoiceId);
        SpeechSynthesisReply reply = { sizeof(reply), ERROR_CODE_UNSPECIFIED, nullptr };

        bool result = synthesizeSpeech(&logSpeechLibraryMessage, &request, &reply);
        if (!result)
        {
            m_host->writeLog("SPECHW|FAILED to synthesize speech, error code [%d]", reply.errorCode);
            return;
        }

        m_host->writeLog("SPECHW|successfully synthesized speech");
        this_thread::sleep_for(chrono::milliseconds(500));

        m_activeRequestId = message.requestId;
        m_currentSpeech = make_shared<SpeechSoundBuffer>(speechFilePath, true, message.radioStyle.highPassFrequency);

        if (message.speaker && style.platformVoiceId.length() == 0 && reply.platformVoiceId)
        {
            m_host->writeLog("SPECHW|actor [%d] assigned platform voice id [%s]", message.speaker->id(), reply.platformVoiceId);
            message.speaker->setPlatformVoiceId(reply.platformVoiceId);
        }
        
        playRadioSpeech(message);
    }

    void playRegularSpeech(const ThreadMessage& message)
    {
        m_currentSpeech->play(1.0f);
    }

    void playRadioSpeech(const ThreadMessage& message)
    {
        stopAllSounds();
//        m_radioStaticEdgeMedium.stop();
//        m_radioStaticEdgeMedium.rewind();
//        m_radioStaticEdgeShort.stop();
//        m_radioStaticEdgeShort.rewind();
//        m_radioStaticEdgeLong.stop();
//        m_radioStaticEdgeLong.rewind();

        auto speechPlaybackTime = m_currentSpeech->playbackTime() - chrono::milliseconds(100);
        m_host->writeLog("SPECHW|speech playback time, ms: %lld", speechPlaybackTime.count());

        int requestIdCopy = m_activeRequestId;
        const auto& radioStyle = message.radioStyle;
        chrono::milliseconds timePttAt = radioStyle.delayBeforePtt;
        chrono::milliseconds timeSpeakAt = timePttAt + radioStyle.delayAfterPtt;
        chrono::milliseconds timePttOffAt = timeSpeakAt + speechPlaybackTime + radioStyle.delayAfterSpeech;

        m_host->writeLog(
            "SPECHW|radio style: beforeptt=%d, afterptt=%d, afterspch=%d static=%f highpass=%f",
            radioStyle.delayBeforePtt.count(), radioStyle.delayAfterPtt.count(), radioStyle.delayAfterSpeech.count(), radioStyle.staticVolume, radioStyle.highPassFrequency);

        m_host->getWorld()->deferBy("SYNTH/ptton/reqid=" + to_string(requestIdCopy), timePttAt, [=](){
            m_host->writeLog("SPECHW|m_messageQueue.enqueue(PlayRadioStaticPttOn, requestId=%d)", requestIdCopy);
            m_messageQueue.enqueue({ 
                ThreadMessageType::PlayRadioStaticPttOn, requestIdCopy, message.frequency, message.speaker, message.transmission, radioStyle 
            });
        });
        m_host->getWorld()->deferBy("SYNTH/speak/reqid=" + to_string(requestIdCopy), timeSpeakAt, [=](){
            m_host->writeLog("SPECHW|m_messageQueue.enqueue(PlayRadioSpeech, requestId=%d)", requestIdCopy);
            m_messageQueue.enqueue({ 
                ThreadMessageType::PlayRadioSpeech, requestIdCopy, message.frequency, message.speaker, message.transmission, radioStyle 
            });
        });
        m_host->getWorld()->deferBy("SYNTH/pttoff/reqid=" + to_string(requestIdCopy), timePttOffAt, [=](){
            m_host->writeLog("SPECHW|m_messageQueue.enqueue(PlayRadioStaticPttOff, requestId=%d)", requestIdCopy);
            m_messageQueue.enqueue({ 
                ThreadMessageType::PlayRadioStaticPttOff, requestIdCopy, message.frequency, message.speaker, message.transmission, radioStyle 
            });
        });
    }

    bool isReqeustStillActive(int requestId)
    {
        return m_activeRequestId == requestId;
    }

    void setSpeechStyleToRender(
        const Actor& actor,
        RadioSpeechStyle& styleToRender,
        Actor::Role role)
    {
        switch (actor.nature())
        {
        case Actor::Nature::AI:
            setRadioRenderSpeechStyle(actor, styleToRender, role);
            break;
        case Actor::Nature::Human:
            setCoPilotRenderSpeechStyle(actor, styleToRender, role);
            break;
        }
    }

    void setCoPilotRenderSpeechStyle(
        const Actor& actor,
        RadioSpeechStyle& radioStyle,
        Actor::Role role)
    {
        radioStyle.delayBeforePtt = chrono::milliseconds(0);
        radioStyle.delayAfterPtt = chrono::milliseconds(500);
        radioStyle.delayAfterSpeech = chrono::milliseconds(500);
        radioStyle.staticVolume = 0.1f;
        radioStyle.highPassFrequency = 250;
    }

    void setRadioRenderSpeechStyle(
        const Actor& actor,
        RadioSpeechStyle& radioStyle,
        Actor::Role role)
    {
        radioStyle.delayBeforePtt = chrono::milliseconds(250 + m_host->getNextRandom(750));
        const auto& regularStyle = actor.speechStyle();

        switch (regularStyle.rate)
        {
        case Actor::SpeechRate::Slow:
            radioStyle.delayAfterPtt = chrono::milliseconds(750);
            radioStyle.delayAfterSpeech = chrono::milliseconds(325);
            break;
        case Actor::SpeechRate::Fast:
            radioStyle.delayAfterPtt = chrono::milliseconds(0);
            radioStyle.delayAfterSpeech = chrono::milliseconds(0);
            break;
        default:
            radioStyle.delayAfterPtt = chrono::milliseconds(300);
            radioStyle.delayAfterSpeech = chrono::milliseconds(150);
        }

        switch (regularStyle.radioQuality)
        {
        case Actor::RadioQuality::Good:
            radioStyle.staticVolume = 0.25f;
            radioStyle.highPassFrequency = 2500;
            break;
        case Actor::RadioQuality::Poor:
            radioStyle.staticVolume = 0.35f;
            radioStyle.highPassFrequency = 1500;
            break;
        default:
            radioStyle.staticVolume = 0.30f;
            radioStyle.highPassFrequency = 2000;
        }

        if (role == Actor::Role::Pilot)
        {
            radioStyle.highPassFrequency += 500;
        }
        else
        {
            radioStyle.staticVolume -= 0.1f;
            radioStyle.highPassFrequency -= 500;
        }
    }

    void stopAllSounds()
    {
        m_radioStaticEdgeMedium.stop();
        m_radioStaticEdgeMedium.rewind();
        m_radioStaticEdgeShort.stop();
        m_radioStaticEdgeShort.rewind();
        m_radioStaticEdgeLong.stop();
        m_radioStaticEdgeLong.rewind();
        m_radioStaticBackgroundLoop.stop();

        if (m_currentSpeech)
        {
            m_currentSpeech->stop();
        }
    }

public:

    static void init_sound()
    {
        ALCdevice *		my_dev		= NULL;			// We make our own device and context to play sound through.
        ALCcontext *		my_ctx		= NULL;
        ALuint			snd_src		=0;				// Sample source and buffer - this is one "sound" we play.

        XPLMDebugString("ALTEST> init_sound ENTERED\n");

        CHECK_ERR();

        char buf[2048];
        
        // We have to save the old context and restore it later, so that we don't interfere with X-Plane
        // and other plugins.

        ALCcontext * old_ctx = alcGetCurrentContext();
        
        if(old_ctx == NULL)
        {
            PrintDebugString("ALTEST> 0x%08x: I found no OpenAL, I will be the first to init.\n",XPLMGetMyID());
            XPLMDebugString("ALTEST> will open device\n");
            my_dev = alcOpenDevice(NULL);
            if(my_dev == NULL)
            {
                XPLMDebugString("ALTEST> Could not open the default OpenAL device.\n");
                return;		
            }	
            XPLMDebugString("ALTEST> will create context\n");
            my_ctx = alcCreateContext(my_dev, NULL);
            if(my_ctx == NULL)
            {
                if(old_ctx)
                    alcMakeContextCurrent(old_ctx);
                alcCloseDevice(my_dev);
                my_dev = NULL;
                XPLMDebugString("ALTEST> Could not create a context.\n");
                return;				
            }
            
            // Make our context current, so that OpenAL commands affect our, um, stuff.
            
            XPLMDebugString("ALTEST> will make context current\n");
            alcMakeContextCurrent(my_ctx);
            CHECK_ERR();

            PrintDebugString("ALTEST> 0x%08x: I created the context.\n",XPLMGetMyID(), my_ctx);

            ALCint		major_version, minor_version;
            const char * al_hw=alcGetString(my_dev,ALC_DEVICE_SPECIFIER	);
            const char * al_ex=alcGetString(my_dev,ALC_EXTENSIONS		
    );
            alcGetIntegerv(NULL,ALC_MAJOR_VERSION,sizeof(major_version),&major_version);
            alcGetIntegerv(NULL,ALC_MINOR_VERSION,sizeof(minor_version),&minor_version);
            
            PrintDebugString("ALTEST> OpenAL version   : %d.%d\n",major_version,minor_version);
            PrintDebugString("ALTEST> OpenAL hardware  : %s\n", (al_hw?al_hw:"(none)"));
            PrintDebugString("ALTEST> OpenAL extensions: %s\n", (al_ex?al_ex:"(none)"));
            CHECK_ERR();

            XPLMDebugString("ALTEST> init_sound: CREATED NEW CONTEXT\n");
        } 
        else
        {
            XPLMDebugString("ALTEST> init_sound: FOUND EXISTING CONTEXT\n");
            PrintDebugString("ALTEST> 0x%08x: I found someone else's context 0x%08x.\n",XPLMGetMyID(), old_ctx);
        }
        
        ALfloat	zero[3] = { 0 } ;

        // char dirchar = *XPLMGetDirectorySeparator();
        // XPLMGetPluginInfo(XPLMGetMyID(), NULL, buf, NULL, NULL);
        // char * p = buf;
        // char * slash = p;
        // while(*p)
        // {
        //     if(*p==dirchar) slash = p;
        //     ++p;
        // }
        // ++slash;
        // *slash=0;
        // strcat(buf,"sound.wav");
        // #if APL
        //     ConvertPath(buf,buf,sizeof(buf));
        // #endif
        
        // Generate 1 source and load a buffer of audio.
        XPLMDebugString("ALTEST> will generate sources\n");
        alGenSources(1,&snd_src);
        CHECK_ERR();
        // snd_buffer = load_wave(buf);
        // PrintDebugString("ALTEST> 0x%08x: Loaded %d from %s\n", XPLMGetMyID(), snd_buffer,buf);
        // CHECK_ERR();
        
        // Basic initializtion code to play a sound: specify the buffer the source is playing, as well as some 
        // sound parameters. This doesn't play the sound - it's just one-time initialization.
        
        //alSourcei(snd_src,AL_BUFFER,snd_buffer);
        
        XPLMDebugString("ALTEST> will set source parameters\n");
        alSourcef(snd_src,AL_PITCH,1.0f);
        alSourcef(snd_src,AL_GAIN,1.0f);	
        alSourcei(snd_src,AL_LOOPING,0);
        alSourcefv(snd_src,AL_POSITION, zero);
        alSourcefv(snd_src,AL_VELOCITY, zero);
        CHECK_ERR();
    }

private:

    static void __CHECK_ERR(const char * f, int l)
    {
        ALuint e = alGetError();
        if (e != AL_NO_ERROR)
        {
            PrintDebugString("ALTEST> ERROR: %d (%s:%d)\n", e, f, l);
        }
        else
        {
            XPLMDebugString("ALTEST> OK\n");
        }
    }

    static chrono::milliseconds countSpeechDuration(const string& text)
    {
        int commaCount = 0;
        int periodCount = 0;

        for (int i = 0 ; i < text.length() ; i++)
        {
            char c = text[i];
            if (c == ',')
            {
                commaCount++;
            }
            else if (c == '.')
            {
                periodCount++;
            }
        }

        return chrono::milliseconds(100 * text.length() + 500 * commaCount + 750 * periodCount);
    }

    static void logSpeechLibraryMessage(const char *text)
    {
        stringstream str;
        str << text;
        str << endl;
        XPLMDebugString(str.str().c_str());
    }
};
