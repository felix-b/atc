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

        auto verbalizedUtterance = transmission->verbalizedUtterance();

        m_host->writeLog(
            "%d|%s [%s]->[%s] intent id[%d] code[%d] crit[%d] state[%d] reply-to[%d] %s",
            m_khz,
            message.c_str(),
            fromCallSign.c_str(),
            toCallSign.c_str(),
            intent->id(),
            intent->code(),
            intent->isCritical() ? 1 : 0,
            intent->conversationState(),
            intent->replyToId(),
            verbalizedUtterance ? verbalizedUtterance->plainText().c_str() : "");
    }

    void Frequency::logIntent(const string& message, shared_ptr<Intent> intent)
    {
        string fromCallSign = (intent->direction() == Intent::Direction::PilotToController
            ? intent->subjectFlight()->callSign()
            : intent->subjectControl()->callSign());

        string toCallSign = (intent->direction() == Intent::Direction::PilotToController
            ? intent->subjectControl()->callSign()
            : intent->subjectFlight()->callSign());

        m_host->writeLog(
            "%d|%s [%s]->[%s] intent id[%d] code[%d] crit[%d] state[%d] reply-to[%d]",
            m_khz,
            message.c_str(),
            fromCallSign.c_str(),
            toCallSign.c_str(),
            intent->id(),
            intent->code(),
            intent->isCritical() ? 1 : 0,
            intent->conversationState(),
            intent->replyToId());
    }


    //    class SilenceAwaiter
//    {
//    private:
//        shared_ptr<HostServices> m_host;
//        shared_ptr<Frequency> m_frequency;
//        shared_ptr<Intent> m_intent;
//        shared_ptr<Transmission> m_transmission;
//        chrono::microseconds m_awaitStartedAt = chrono::microseconds(0);
//        chrono::milliseconds m_updatedSilence = chrono::milliseconds(0);
//        bool m_silenceWasUpdated = false;
//        string m_name;
//    public:
//        SilenceAwaiter(
//            shared_ptr<HostServices> _host,
//            shared_ptr<Frequency> _frequency,
//            shared_ptr<Intent> _intent,
//            chrono::milliseconds silence
//        ) : m_host(_host),
//            m_frequency(_frequency),
//            m_intent(_intent),
//            m_updatedSilence(silence)
//        {
//            m_name = "silence-awaiter/" + to_string(_frequency->khz()) + "/" + to_string(silence.count());
//            m_awaitStartedAt = m_host->getWorld()->timestamp();
//        }
//    public:
//        shared_ptr<Transmission> transmission() const { return m_transmission; }
//    public:
//        void check()
//        {
//            if (m_frequency->wasSilentFor(m_updatedSilence))
//            {
//                m_transmission = m_frequency->enqueueTransmission(m_intent);
//            }
//            else
//            {
//                chrono::microseconds timeElapsed = m_host->getWorld()->timestamp() - m_awaitStartedAt;
//                if (!m_silenceWasUpdated && timeElapsed >= m_updatedSilence * 2)
//                {
//                    m_updatedSilence /= 2;
//                    m_silenceWasUpdated = true;
//                }
//                m_host->getWorld()->deferBy(m_name, chrono::microseconds(100000), [this]{
//                    check();
//                });
//            }
//        };
//    };

    void Frequency::enqueuePushToTalk(
        chrono::milliseconds silence,
        const shared_ptr<Intent> intent,
        TransmissionCallback onTransmission,
        CancellationQueryCallback onQueryCancel)
    {
        if (m_regularAwaiters.size() >= 1000)
        {
            m_host->writeLog("%d|ERROR push-to-talk queue full, cannot enqueue intent code[%d]", m_khz, intent->code());
            return;
        }

        logIntent("ENQEUE PTT silence[" + to_string(silence.count()) + "]", intent);

        int id = m_nextPushToTalkId++;
        if (intent->isCritical())
        {
            m_criticalAwaiters.push_back({ id, silence, intent, onTransmission, onQueryCancel});
        }
        else
        {
            m_regularAwaiters.push_back({id, silence, intent, onTransmission, onQueryCancel});
        }
    }

    shared_ptr<Transmission> Frequency::enqueueTransmission(const shared_ptr<Intent> intent)
    {
        if (m_pendingTransmissions.size() >= 1000)
        {
            m_host->writeLog("%d|ERROR transmission queue full, cannot enqueue intent code[%d]", m_khz, intent->code());
            return nullptr;
        }

        auto now =  m_host->getWorld()->timestamp();
        auto transmission = shared_ptr<Transmission>(new Transmission(m_nextTransmissionId++, intent));
        auto utterance = m_host->services().get<PhraseologyService>()->verbalizeIntent(intent);
        transmission->setVerbalizedUtterance(utterance);

        logTransmission("ENQEUE TRANSMISSION", transmission);

        m_pendingTransmissions.push(transmission);
        m_lastTransmittedIntentId = intent->id();
        m_lastConversationState = intent->conversationState();
        m_conversationStateExpiryTimestamp = now + chrono::minutes(10);

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

        if (intent->id() == m_lastTransmittedIntentId)
        {
            m_conversationStateExpiryTimestamp = timestamp + chrono::seconds(5);
        }

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
        if (m_transmissionInProgress)
        {
            return;
        }

        if (m_pendingTransmissions.empty())
        {
            checkConversationStateExpiry(timestamp);

            PushToTalkAwaiter awaiter;
            bool dequeued =
                    tryDequeueAwaiter(m_criticalAwaiters, awaiter) ||
                    tryDequeueAwaiter(m_regularAwaiters, awaiter);

            if (dequeued)
            {
                auto transmission = enqueueTransmission(awaiter.intent);
                awaiter.onTransmission(transmission); //TODO: try/catch
            }
        }

        if (!m_pendingTransmissions.empty())
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
        m_regularAwaiters.clear();
        m_lastTransmittedIntentId = 0;
        m_lastConversationState = Intent::ConversationState::End;
        m_conversationStateExpiryTimestamp = chrono::microseconds(0);
    }

    bool Frequency::wasSilentFor(chrono::milliseconds duration, uint64_t replyToId)
    {
        if (m_transmissionInProgress || !m_pendingTransmissions.empty())
        {
            return false;
        }

        auto now = m_host->getWorld()->timestamp();
        auto wasSilentFor = now - m_lastTransmissionEndTimestamp;

        if (m_lastConversationState == Intent::ConversationState::Continue)
        {
            return (replyToId == m_lastTransmittedIntentId);// || wasSilentFor.count() > 2000);
        }

        return (wasSilentFor >= duration);
    }

    bool Frequency::tryDequeueAwaiter(list<PushToTalkAwaiter>& queue, PushToTalkAwaiter& dequeued)
    {
        if (!m_transmissionInProgress)
        {
            //int index = 0;

            for (auto it = queue.begin() ; it != queue.end() ; )
            {
                bool match = wasSilentFor(it->silence, it->intent->replyToId());
//                m_host->writeLog(
//                    "%d|tryDequeuePTT at index[%d]%s intent: silence[%d] reply-do[%d] code[%d] id[%d] crit[%d]",
//                    m_khz,
//                    index,
//                    (match ? " MATCHED" : ""),
//                    it->silence.count(),
//                    it->intent->replyToId(),
//                    it->intent->code(),
//                    it->intent->id(),
//                    it->intent->isCritical() ? 1 : 0);

                auto currentIt = it;
                it++;

                if (match)
                {
                    bool cancelled = currentIt->onQueryCancel();
                    if (cancelled)
                    {
                        cancelAwaiter(*currentIt);
                    }
                    else
                    {
                        dequeued = *currentIt;
                    }

                    queue.erase(currentIt);

                    if (!cancelled)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    void Frequency::cancelAwaiter(PushToTalkAwaiter& awaiter)
    {
        auto transmission = shared_ptr<Transmission>(new Transmission(m_nextTransmissionId++, awaiter.intent));
        transmission->m_endTimestamp = m_host->getWorld()->timestamp();
        transmission->m_state = Transmission::State::Cancelled;
        logTransmission("CANCEL TRANSMISSION", transmission);
        awaiter.onTransmission(transmission); //TODO: try/catch
    }

    bool Frequency::wasPushToTalkDequeued(int pushToTalkId)
    {
        for (auto it = m_regularAwaiters.begin() ; it != m_regularAwaiters.end() ; it++)
        {
            if (it->id == pushToTalkId)
            {
                return false;
            }
        }
        return true;
    }

    void Frequency::checkConversationStateExpiry(chrono::microseconds timestamp)
    {
        if (timestamp >= m_conversationStateExpiryTimestamp)
        {
            if (m_lastConversationState == Intent::ConversationState::Continue)
            {
                m_host->writeLog(
                    "%d|WARNING: conversation state expired! state[%d] intent id[%d]",
                    m_khz,
                    m_lastConversationState,
                    m_lastTransmittedIntentId);
            }

            m_lastTransmittedIntentId = 0;
            m_lastConversationState = Intent::ConversationState::End;
            m_conversationStateExpiryTimestamp = timestamp + chrono::hours(1);
        }
    }
}

