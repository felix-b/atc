// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <vector>
#include <string>
#include "libworld.h"

using namespace std;

namespace world
{
    UtteranceBuilder::UtteranceBuilder()
    {
    }

    void UtteranceBuilder::addText(const string& text, bool slowDown)
    {
        if (slowDown)
        {
            addPart("<rate speed='-5'>" + text + "</rate>", Utterance::PartType::Text);
        }
        else
        {
            addPart(text, Utterance::PartType::Text);
        }
    }

    void UtteranceBuilder::addData(const string& text, bool slowDown)
    {
        if (slowDown)
        {
            addPart("<rate speed='-2'>" + text + "</rate>", Utterance::PartType::Data);
        }
        else
        {
            addPart("<rate speed='-1'>" + text + "</rate>", Utterance::PartType::Data);
        }
    }

    void UtteranceBuilder::addDisfluency(const string& text, bool skip)
    {
        if (!skip)
        {
            addPart("<rate speed='-7'><pitch middle='-1'><silence msec='1'/>uhm</pitch></rate>", Utterance::PartType::Disfluency);
        }
    }

    void UtteranceBuilder::addCorrection(const string& text, bool skip)
    {
        if (!skip)
        {
            addPart("<rate speed='1'><pitch middle='-1'><silence msec='100'/>err</pitch><silence msec='100'/><pitch middle='1'>correction</pitch></rate>", Utterance::PartType::Disfluency);
            addPart(text, Utterance::PartType::Correction);
        }
    }

    void UtteranceBuilder::addGreeting(const string& text)
    {
        addPart(text, Utterance::PartType::Greeting);
    }

    void UtteranceBuilder::addFarewell(const string& text)
    {
        addPart("<pitch middle='1'/><rate speed='1'/>" + text, Utterance::PartType::Farewell);
    }

    void UtteranceBuilder::addAffirmation(const string& text)
    {
        addPart(text, Utterance::PartType::Affirmation);
    }

    void UtteranceBuilder::addNegation(const string& text)
    {
        addPart(text, Utterance::PartType::Negation);
    }

    void UtteranceBuilder::addPunctuation()
    {
        m_parts.push_back({ (int)m_plainText.tellp(), 1, Utterance::PartType::Punctuation });
        m_plainText << ',';
    }

    shared_ptr<Utterance> UtteranceBuilder::getUtterance()
    {
        auto utterance = make_shared<Utterance>();
        utterance->m_plainText = m_plainText.str();
        utterance->m_parts = m_parts;
        return utterance;
    }

    Utterance::Part& UtteranceBuilder::addPart(const string& text, Utterance::PartType type)
    {
        if (m_parts.size() > 0)
        {
            m_plainText << " ";
        }

        m_parts.push_back({ (int)m_plainText.tellp(), (int)text.length(), type });
        m_plainText << text;

        return m_parts[m_parts.size() - 1];
    }
}
