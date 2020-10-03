// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <vector>
#include "libworld.h"
#include "stlhelpers.h"

using namespace std;

namespace world
{
    static vector<Actor::VoiceType> maleVoiceTypes = { 
        Actor::VoiceType::Bass, 
        Actor::VoiceType::Baritone, 
        Actor::VoiceType::Tenor, 
        Actor::VoiceType::Countertenor
    };

    static vector<Actor::VoiceType> femaleVoiceTypes = { 
        Actor::VoiceType::Contralto, 
        Actor::VoiceType::MezzoSoprano, 
        Actor::VoiceType::Soprano, 
        Actor::VoiceType::Treble
    };

    static vector<Actor::SpeechRate> speechRateRoundRobin = { 
        Actor::SpeechRate::Medium,
        Actor::SpeechRate::Slow,
        Actor::SpeechRate::Fast,
        Actor::SpeechRate::Slow,
        Actor::SpeechRate::Medium,
        Actor::SpeechRate::Fast,
        Actor::SpeechRate::Medium
    };

    static vector<Actor::RadioQuality> radioQualityRoundRobin = { 
        Actor::RadioQuality::Medium,
        Actor::RadioQuality::Good,
        Actor::RadioQuality::Poor,
    };

    static Actor::SpeechStyle defaultSpeechStyle = {
        true,                       // has style
        Actor::Gender::Male,
        Actor::VoiceType::Baritone,
        Actor::SpeechRate::Medium,
        0.4f,                       // selfCorrectionProbability;
        0.4f,                       // disfluencyProbability
        chrono::milliseconds(500),  // pttDelayBeforeSpeech;
        chrono::milliseconds(500),  // pttDelayAfterSpeech;
        Actor::RadioQuality::Medium,
        ""                          // platformVoiceId;
    };

    Actor::Actor(shared_ptr<HostServices> _host, int _id, Actor::Gender _gender) :
        m_host(_host),
        m_id(_id),
        m_nature(Actor::Nature::AI),
        m_gender(_gender)
    {
        m_name = "Larsen"; // TODO: randomize
        initRandomSpeechStyle();
    }

    void Actor::initRandomSpeechStyle()
    {
        m_speechStyle.hasStyle = true;
        m_speechStyle.gender = m_gender;
        m_speechStyle.voice = m_gender == Actor::Gender::Male 
            ? maleVoiceTypes[m_id % maleVoiceTypes.size()]
            : femaleVoiceTypes[m_id % femaleVoiceTypes.size()];
        m_speechStyle.rate = speechRateRoundRobin[m_id % speechRateRoundRobin.size()];
        m_speechStyle.radioQuality = radioQualityRoundRobin[m_id % radioQualityRoundRobin.size()];
        m_speechStyle.disfluencyProbability = m_host->getNextRandom(100) / 100.0f;
        m_speechStyle.selfCorrectionProbability = 
            0.5f * m_speechStyle.disfluencyProbability + 
            0.5f * m_host->getNextRandom(100) / 100.0f;

        if (m_speechStyle.disfluencyProbability > 0.5f)
        {
            m_speechStyle.pttDelayBeforeSpeech = chrono::milliseconds(m_host->getNextRandom(2000));
            m_speechStyle.pttDelayAfterSpeech = chrono::milliseconds(m_host->getNextRandom(1000));
        }
        else
        {
            m_speechStyle.pttDelayBeforeSpeech = chrono::milliseconds(m_host->getNextRandom(200));
            m_speechStyle.pttDelayAfterSpeech = chrono::milliseconds(m_host->getNextRandom(100));
        }

        m_speechStyle.platformVoiceId = "";
        // m_host->writeLog(
        //    "initRandomSpeechStyle voice[%d] rate[%d] rqlt[%d] Pdis[%f] Pcor[%f]",
        //    m_speechStyle.voice, m_speechStyle.rate, m_speechStyle.radioQuality, m_speechStyle.disfluencyProbability, m_speechStyle.selfCorrectionProbability);
    }

    const Actor::SpeechStyle& Actor::getDefaultSpeechStyle()
    {
        return defaultSpeechStyle;
    }
}
