// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 

#include <string>
#include <functional>
#include <chrono>

// SDK
#include "XPLMUtilities.h"
#include "XPLMPlugin.h"

// PPL
#include "log.h"
#include "owneddata.h"

// tnc
#include "utils.h"
#include "libworld.h"

using namespace std;
using namespace PPL;
using namespace world;

class XPLMSpeakStringTtsService : public TextToSpeechService
{
private:
    shared_ptr<HostServices> m_host;
    DataRef<int> m_com1Power;
    DataRef<int> m_com1FrequencyKhz;
public:
    XPLMSpeakStringTtsService(shared_ptr<HostServices> _host) :
        m_host(_host),
        m_com1Power("sim/cockpit2/radios/actuators/com1_power", PPL::ReadOnly),
        m_com1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadOnly)
    {
    }
public:
    QueryCompletion vocalizeTransmission(shared_ptr<Frequency> frequency, shared_ptr<Transmission> transmission) override
    {
        if (!transmission || !transmission->verbalizedUtterance())
        {
            throw runtime_error("vocalizeTransmission: transmission was not verbalized");
        }

        auto world = m_host->getWorld();
        string transmissionText = transmission->verbalizedUtterance()->plainText();
        chrono::milliseconds speechDuration = countSpeechDuration(transmissionText);
        chrono::microseconds completionTimestamp = world->timestamp() + speechDuration;

        int com1Power = m_com1Power;
        int com1FrequencyKhz = m_com1FrequencyKhz;
        bool isHeardByUser = (com1Power == 1 && com1FrequencyKhz == frequency->khz());
        m_host->writeLog(
            "XPLMSpeakStringTtsService::vocalizeTransmission : com1Power=%d, com1FrequencyKhz=%d, isHeardByUser=%s", 
            com1Power, com1FrequencyKhz, isHeardByUser ? "Yes" : "No");
        
        if (isHeardByUser)
        {
            XPLMSpeakString(transmissionText.c_str());
        }

        return [world, completionTimestamp]() {
            return (world->timestamp() >= completionTimestamp);
        };
    }

    void clearAll() override
    {
        XPLMSpeakString("");
    }
public:
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
};
