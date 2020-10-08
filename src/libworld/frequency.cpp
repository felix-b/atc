// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"

using namespace std;

namespace world
{
    void Frequency::logTransmission(const string& message, shared_ptr<Transmission> transmission)
    {
        const auto& intent = transmission->intent();

        string fromCallSign = (intent->direction() == Intent::Direction::PilotToController 
            ? intent->subjectFlight()->callSign() 
            : intent->subjectControl()->callSign());

        string toCallSign = (intent->direction() == Intent::Direction::PilotToController 
            ? intent->subjectControl()->callSign()
            : intent->subjectFlight()->callSign());

        m_host->writeLog(
            "%d|%s [%s]->[%s] %s",
            m_khz,
            message.c_str(),
            fromCallSign.c_str(),
            toCallSign.c_str(),
            transmission->verbalizedUtterance()->plainText().c_str());
    }

    shared_ptr<Transmission> Frequency::enqueueTransmission(const shared_ptr<Intent> intent)
    {
        if (m_pendingTransmissions.size() >= 1000)
        {
            m_host->writeLog("%d|ERROR transmission queue full, cannot enqueue intent code[%d]", m_khz, intent->code());
            return nullptr;
        }

        auto transmission = shared_ptr<Transmission>(new Transmission(m_nextTransmissionId++, intent));
        auto utterance = m_host->services().get<PhraseologyService>()->verbalizeIntent(intent);
        transmission->setVerbalizedUtterance(utterance);

        logTransmission("ENQEUE TRANSMISSION", transmission);

        m_pendingTransmissions.push(transmission);
        return transmission;
    }

    void Frequency::beginTransmission(shared_ptr<Transmission> transmission, chrono::microseconds timestamp)
    {
        auto intent = transmission->intent();
        transmission->m_state = Transmission::State::InProgress;
        transmission->m_startTimestamp = timestamp;

        logTransmission("BEGIN TRANSMISSION", transmission);

        auto tts = m_host->services().get<TextToSpeechService>();

        m_queryTransmissionCompletion = tts->vocalizeTransmission(shared_from_this(), transmission); 
        m_transmissionInProgress = transmission;
    }

    void Frequency::endTransmission(chrono::microseconds timestamp)
    {
        logTransmission("DISPATCH COMPLETED TRANSMISSION", m_transmissionInProgress);

        auto intent = m_transmissionInProgress->intent();

        m_transmissionInProgress->m_endTimestamp = timestamp;
        m_transmissionInProgress->m_state = Transmission::State::Completed;
        m_transmissionInProgress.reset();
        m_queryTransmissionCompletion = TextToSpeechService::noopQueryCompletion;
        m_lastTransmissionEndTimestamp = timestamp;

        for (const auto& pair : m_listenerById)
        {
            try
            {
                pair.second(intent);
            }
            catch(const exception& e)
            {
                m_host->writeLog("%d|FREQUENCY LISTENER CRASHED!!! %s", m_khz, e.what());
            }
        }
    }

    int Frequency::addListener(Frequency::Listener callback)
    {
        int newId = m_nextListenerId++;
        m_listenerById.insert({ newId, callback });
        return newId;
    }
    
    void Frequency::removeListener(int listenerId)
    {
        m_listenerById.erase(listenerId);
    }

    void Frequency::progressTo(chrono::microseconds timestamp)
    {
        if (m_transmissionInProgress && m_queryTransmissionCompletion())
        {
            endTransmission(timestamp);
        }

        //m_host->writeLog("Frequency[%d]::progressTo(%lld), silence=[%lld] queue-len[%d]", m_khz, timestamp, m_silenceTimestamp, m_pendingTransmissions.size());
        if (!m_transmissionInProgress && !m_pendingTransmissions.empty())
        {
            auto nextTransmission = m_pendingTransmissions.front();
            m_pendingTransmissions.pop();
            beginTransmission(nextTransmission, timestamp);
        }
    }

    void Frequency::clearTransmissions()
    {
        m_pendingTransmissions = queue<shared_ptr<Transmission>>();
        m_transmissionInProgress.reset();
        m_queryTransmissionCompletion = TextToSpeechService::noopQueryCompletion;
    }

    bool Frequency::wasSilentFor(chrono::milliseconds duration)
    {
        if (m_transmissionInProgress || !m_pendingTransmissions.empty())
        {
            return false;
        }

        auto now = m_host->getWorld()->timestamp();
        return (now - m_lastTransmissionEndTimestamp >= duration);
    }
}
