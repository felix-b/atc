// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once
#define _USE_MATH_DEFINES

#include <sstream>
#include <vector>
#include <functional>
#include <cmath>
#include <chrono>
#include "alsoundbuffer.h"
#include "soundFileReader.hpp"

using namespace std;
using namespace PPL;

class SpeechSoundBuffer
{
private:
    string m_name;
    ALuint m_buffer;
    ALuint m_source;
    ALboolean m_loop;
    chrono::milliseconds m_playbackTime;
public:
    SpeechSoundBuffer(const std::string& filename, bool isRadio, float radioHighPassFrequency) :
        m_name(filename)
    {
        ALfloat source_position[] = { 0.0, 0.0, 0.0 };
        ALfloat source_velocity[] = { 0.0, 0.0, 0.0 };
        m_loop = AL_FALSE;
        // Clear present errors
        alGetError();
        m_buffer = loadSoundFile(filename, isRadio, radioHighPassFrequency, m_playbackTime);

        if( m_buffer == AL_NONE)
        {
            stringstream stream;
            stream << "ALUT: Buffer creation failed: "/*
                                        << alutGetErrorString(alutGetError())*/;
            throw ALSoundBuffer::SoundBufferError(stream.str());
        }
        else
        {
            if (alGetError() != AL_NO_ERROR)
            {
                throw ALSoundBuffer::SoundBufferError("Error in creating buffer");
            }
            alGenSources(1, &m_source);
            if (alGetError() != AL_NO_ERROR)
            {
                throw ALSoundBuffer::SoundSourceError("Error in creating source");
            }
            alSourcei (m_source, AL_BUFFER,   m_buffer   );
            alSourcef (m_source, AL_PITCH,    1.0      );
            alSourcef (m_source, AL_GAIN,     1.0      );
            alSourcefv(m_source, AL_POSITION, source_position);
            alSourcefv(m_source, AL_VELOCITY, source_velocity);
            alSourcei (m_source, AL_LOOPING,  m_loop     );
            if (alGetError() != AL_NO_ERROR)
            {
                throw ALSoundBuffer::SoundSourceError("Error in setting up source");
            }
        }
    }

    ~SpeechSoundBuffer()
    {
        alDeleteSources(1, &m_source);
        alDeleteBuffers(1, &m_buffer);
    }

    SpeechSoundBuffer(const ALSoundBuffer&) = delete;
    SpeechSoundBuffer& operator=(const ALSoundBuffer&) = delete;

    bool play(float volume)
    {
        if (!(alIsSource( m_source ) == AL_TRUE))
        {
            std::stringstream stream;
            stream << "Error: " << m_name << " has no source";
            throw ALSoundBuffer::SoundSourceError(stream.str());
        }
        ALint buf;
        alGetSourcei(m_source, AL_BUFFER, &buf);
        if (!(alIsBuffer(buf) == AL_TRUE))
        {
            std::stringstream stream;
            stream << "Error: " << m_name << " has no buffer";
            throw ALSoundBuffer::SoundBufferError(stream.str());
        }

        if (alGetError() != AL_NO_ERROR)
        {
            std::stringstream stream;
            stream << "Error, cannot play " << m_name;
            throw ALSoundBuffer::SoundPlayingError(stream.str());
        }
        ALfloat listener_position[]= { 0.0, 0.0, 0.0 };
        ALfloat listener_velocity[] = { 0.0, 0.0, 0.0 };
        ALfloat listener_orientation[] = { 0.0, 0.0, -1.0,  0.0, 1.0, 0.0 };
        alListenerfv(AL_POSITION,    listener_position);
        alListenerfv(AL_VELOCITY,    listener_velocity);
        alListenerfv(AL_ORIENTATION, listener_orientation);
        alSourcef(m_source, AL_GAIN, volume);
        if (alGetError() != AL_NO_ERROR)
        {
            std::stringstream stream;
            stream << "Error cannot play " << m_name <<
                    ". Setup of source and listener failed";
            throw ALSoundBuffer::SoundPlayingError(stream.str());
        }
        alSourcePlay( m_source );
        return true;
    }

    void setLoop(bool yes)
    {
        ALboolean loop = yes ? AL_TRUE : AL_FALSE;
        alSourcei( m_source, AL_LOOPING, loop );
    }

    void stop()
    {
        alSourceStop( m_source );
    }

    void rewind()
    {
        alSourceRewind( m_source );
    }

    bool isPlaying()
    {
        ALint state;
        alGetSourcei( m_source, AL_SOURCE_STATE, &state );
        return (state == AL_PLAYING);
    }

    chrono::milliseconds playbackTime() const
    {
        return m_playbackTime;
        // float seconds;
        // alGetSourcef( m_source, AL_SEC_OFFSET, &seconds);
        // return chrono::milliseconds((int)(seconds * 1000.0f));
    }

private:

    static ALuint loadSoundFile(
        const string& filePath, 
        bool shouldApplyRadioFilter, 
        float radioFilterCutffFrequency, 
        chrono::milliseconds& outPlaybackTime)
    {
        std::vector<char> data;
        ALenum format;
        ALsizei freq;
        ALuint buffer = AL_NONE;
        ALenum error;

        alGetError();

        try 
        {
            auto reader = SoundFileReaderFactory::createReader(filePath);
            reader->readHeader();

            const auto& header = reader->header();

            if (header.channelCount)
                format = (header.bytesPerSample == 1) ? AL_FORMAT_MONO8 : AL_FORMAT_MONO16;
            else
                format = (header.bytesPerSample == 1) ? AL_FORMAT_STEREO8 : AL_FORMAT_STEREO16;

            freq = header.frequency;
            if (sizeof(freq) != sizeof(header.frequency))
                throw ALSoundBuffer::SoundBufferError("LoadWav: freq and frequency different sizes");

            if (shouldApplyRadioFilter)
            {
                HighPassFilter radioFilter(header.frequency, header.bytesPerSample * 8, radioFilterCutffFrequency, 1.0f);
                reader->readSound(data, [&](char *buffer, size_t bufferSize) {
                    radioFilter.processBuffer(buffer, bufferSize);
                });
            }
            else
            {
                reader->readSound(data);
            }

            alGenBuffers(1, &buffer);
            
            if((error = alGetError()) != AL_NO_ERROR)
                throw ALSoundBuffer::SoundBufferError("LoadWav: Could not generate buffer: error=" + to_string(error));
            if(AL_NONE == buffer)
                throw ALSoundBuffer::SoundBufferError("LoadWav: Could not generate buffer: AL_NONE");

            alBufferData(buffer, format, &data[0], data.size(), freq);
            if((error = alGetError()) != AL_NO_ERROR)
                throw ALSoundBuffer::SoundBufferError("LoadWav: Could not load buffer data: error=" + to_string(error));

            outPlaybackTime = chrono::milliseconds((1000 * header.soundByteCount) / (header.bytesPerFrame * header.frequency) - 500);
            return buffer;
        }
        catch (std::runtime_error& er)
        {
            if (buffer && alIsBuffer(buffer) == AL_TRUE)
                alDeleteBuffers(1, &buffer);
            throw;
        }
    }

private:

    //reference: https://stackoverflow.com/a/29561548/4544845
    class HighPassFilter
    {
    private:
        float resonance;
        float frequency;
        uint32_t sampleRate;
        uint16_t bitsPerSample;
        float c, a1, a2, a3, b1, b2;
        float inputHistory[2] = { 0 };
        float outputHistory[3] = { 0 };
    public:
        HighPassFilter(uint32_t _sampleRate, uint16_t _bitsPerSample, float _frequency, float _resonance) :
            sampleRate(_sampleRate), 
            bitsPerSample(_bitsPerSample), 
            frequency(_frequency), 
            resonance(_resonance)
        {
            c = (float)tan(M_PI * frequency / sampleRate);
            a1 = 1.0f / (1.0f + resonance * c + c * c);
            a2 = -2.0f * a1;
            a3 = a1;
            b1 = 2.0f * (c * c - 1.0f) * a1;
            b2 = (1.0f - resonance * c + c * c) * a1;
        }
    public:
        void processBuffer(char *buffer, size_t size)
        {
            float inputSample = 0;
            float outputSample = 0;

            for (int i = 0 ; i < size ; )
            {
                switch (bitsPerSample)
                {
                // case 8:
                //     inputSample = (float)*((int8_t*)(buffer + i));
                //     outputSample = processNextSample(inputSample);
                //     *((int8_t*)(buffer + i)) = (int8_t)outputSample;
                //     i += 1;
                //     break;
                case 16:
                    inputSample = (float)*((int16_t*)(buffer + i));
                    outputSample = processNextSample(inputSample);
                    *((int16_t*)(buffer + i)) = (int16_t)outputSample;
                    i += 2;
                    break;
                default:
                    stringstream error;
                    error << "Unsupported bps: " << bitsPerSample;
                    throw ALSoundBuffer::SoundBufferError(error.str());
                }
            }
        }
    private:
        float processNextSample(float newSample)
        {
            float newOutput = 
                a1 * newSample + 
                a2 * inputHistory[0] + 
                a3 * inputHistory[1] - 
                b1 * outputHistory[0] - 
                b2 * outputHistory[1];

            inputHistory[1] = inputHistory[0];
            inputHistory[0] = newSample;

            outputHistory[2] = outputHistory[1];
            outputHistory[1] = outputHistory[0];
            outputHistory[0] = newOutput;

            return outputHistory[0] * 1.1;
        }
    };
};
