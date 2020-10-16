//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

// STL
#include <string>
#include <utility>
#include <vector>
#include <algorithm>
#include <functional>

// PPL
#include "owneddata.h"

// AT&C
#include "utils.h"
#include "libworld.h"

using namespace std;
using namespace world;

class UserPilot : public Pilot
{
private:
public:
    UserPilot(
        shared_ptr<HostServices> _host,
        const string& _name,
        Gender _gender,
        shared_ptr<Flight> _flight,
        const SpeechStyle& _speechStyle
    ) : Pilot(
            _host,
            1,
            _name,
            Actor::Nature::Human,
            _gender,
            _flight,
            _speechStyle
        )
    {
    }
public:
    void progressTo(chrono::microseconds timestamp) override
    {
    }
    shared_ptr<Maneuver> getFlightCycle() override
    {
        throw runtime_error("UserPilot::getFlightCycle is not supported");
    }
    shared_ptr<Maneuver> getFinalToGate(const Runway::End& landingRunway) override
    {
        throw runtime_error("UserPilot::getFinalToGate is not supported");
    }
public:
    static shared_ptr<UserPilot> create(
        shared_ptr<HostServices> host, const string& name, Actor::Gender gender, shared_ptr<Flight> flight)
    {
        SpeechStyle speechStyle;

        speechStyle.hasStyle = true;
        speechStyle.gender = Gender::Male;
        speechStyle.voice = VoiceType::Baritone;
        speechStyle.rate = SpeechRate::Medium;
        speechStyle.selfCorrectionProbability = 0;
        speechStyle.disfluencyProbability = 0;
        speechStyle.pttDelayBeforeSpeech = chrono::milliseconds(500);
        speechStyle.pttDelayAfterSpeech = chrono::milliseconds(250);
        speechStyle.radioQuality = RadioQuality::Good;
        speechStyle.platformVoiceId = "";

        return shared_ptr<UserPilot>(new UserPilot(host, name, gender, flight, speechStyle));
    }
};
