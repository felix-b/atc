// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include "libspeech.h"

using namespace std;

template<typename TToken>
class PlatformVoiceDescriptor
{
private:
    TToken m_token;
    string m_description;
    string m_platformVoiceId;
    int m_gender;
    int m_voiceStyle = 0;
    int m_pitch = 0;
    bool m_supportsRate = false;
    bool m_supportsDisfluencies = false;
    int m_lowRate = 0;
    int m_mediumRate = 0;
    int m_highRate = 0;
public:
    PlatformVoiceDescriptor(const TToken& _token, const string& _description, const string& _platformVoiceId, int _gender) :
        m_token(_token),
        m_description(_description),
        m_platformVoiceId(_platformVoiceId),
        m_gender(_gender)
    {
    }
public:
    const TToken& token() const { return m_token; }
    const string& description() const { return m_description; }
    const string& platformVoiceId() const { return m_platformVoiceId; }
    int gender() const { return m_gender; }
    int voiceStyle() const { return m_voiceStyle; }
    int pitch() const { return m_pitch; }
    bool supportsRate() const { return m_supportsRate; }
    bool supportsDisfluencies() const { return m_supportsDisfluencies; }
    int lowRate() const { return m_lowRate; }
    int mediumRate() const { return m_mediumRate; }
    int highRate() const { return m_highRate; }
public:
    void configure(
        int _voiceStyle,
        int _pitch,
        bool _supportsRate,
        bool _supportsDisfluencies,
        int _lowRate,
        int _mediumRate,
        int _highRate)
    {
        m_voiceStyle = _voiceStyle;
        m_pitch = _pitch;
        m_supportsRate = _supportsRate;
        m_supportsDisfluencies = _supportsDisfluencies;
        m_lowRate = _lowRate;
        m_mediumRate = _mediumRate;
        m_highRate = _highRate;
    }
};
