// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 

#include <iostream>
#include <vector>
#include "synthesizer.h"
//#include "soundFileReader.hpp"
#include "../../src/include/soundFileReader.hpp"

extern "C" int cocoaEnumSpeechVoices(CocoaEnumSpeechVoicesCallback callback);
extern "C" int cocoaSynthesizeSpeech(const char *voiceId, const char* text, const char* filePath);

using namespace std;

void addVoice(const char *name, int gender)
{
    cout << "adding voice [" << name << "], gender [" << gender << "]" << endl;
}

void convertAiffToRiff(const string& aiffFilePath, const string& riffFilePath);
void convertRiffToRiff(const string& fromFilePath, const string& toFilePath);

int main(int argc, char *argv[]) 
{
    cout << "---test---" << endl;
    
    int enumResult = cocoaEnumSpeechVoices(&addVoice);
    if (enumResult != 1)
    {
        cout << "Error: failed to enum voices, code " << enumResult << endl;
        return 1;
    }
    
    int synthResult = cocoaSynthesizeSpeech(
        "com.apple.speech.synthesis.voice.karen.premium",
        "Hi there, I'm just testing my voice",
        "/Users/felixb/Documents/test1.aiff"
    );
    if (synthResult != 1)
    {
        cout << "Error: failed to synthesize, code " << synthResult << endl;
        return 1;
    }

    cout << "------ AIFF -> RIFF ------" << endl;
    convertAiffToRiff(
        "/Users/felixb/Desktop/xp/Resources/plugins/tnc/speech/com1.wav",
        "/Users/felixb/Desktop/xp/Resources/plugins/tnc/speech/com1-cvt.wav"
        // "/Users/felixb/Documents/test1.aiff", 
        // "/Users/felixb/Documents/test1-cvt.wav"
    );
    cout << "------ RIFF -> RIFF ------" << endl;
    convertRiffToRiff("/Users/felixb/Documents/test1-cvt.wav", "/Users/felixb/Documents/test1-cvt-2.wav");

    return 0;
}

void convertAiffToRiff(const string& aiffFilePath, const string& riffFilePath)
{
    FILE *fOut = nullptr;

    try 
    {
        auto reader = SoundFileReaderFactory::createReader(aiffFilePath);
        reader->readHeader();

        fOut = fopen(riffFilePath.c_str(), "wb");
        if (!fOut)
        {
            throw runtime_error("convertAiffToRiff : 1");
        }

        const auto& header = reader->header();

        cout << "(v2) sound file path = " << aiffFilePath << endl;
        //cout << "COMM chunk size = " << reader->commChunkSize() << endl;
        cout << "channels = " << header.channelCount << endl;
        cout << "numSampleFrames = " << header.frameCount << endl;
        cout << "sampleSize (bytes) = " << header.bytesPerSample << endl;
        cout << "sampleRate (frequency) = " << header.frequency << endl;

        auto playbackTime = chrono::milliseconds((1000 * header.soundByteCount) / (header.bytesPerFrame * header.frequency));
        cout << "playback time (ms) = " << playbackTime.count() << endl;

        uint32_t riffChunkSize = 0x20 + header.bytesPerFrame * header.frameCount;
        fwrite("RIFF", 4, 1, fOut);
        fwrite(&riffChunkSize, 4, 1, fOut);
        fwrite("WAVEfmt ", 8, 1, fOut);
        uint32_t fmtChunkSize = 16;
        fwrite(&fmtChunkSize, 4, 1, fOut);
        uint16_t pcmFormatId = 1;
        fwrite(&pcmFormatId, 2, 1, fOut);
        fwrite(&header.channelCount, 2, 1, fOut);
        fwrite(&header.frequency, 4, 1, fOut);
        fwrite(&header.byteRate, 4, 1, fOut);
        uint16_t blockAlign = header.channelCount * header.bytesPerSample;
        fwrite(&blockAlign, 2, 1, fOut);
        uint16_t bitsPerSample = header.bytesPerSample * 8;
        fwrite(&bitsPerSample, 2, 1, fOut);
        fwrite("data", 4, 1, fOut);
        fwrite(&header.soundByteCount, 4, 1, fOut);

        vector<char> soundBytes;
        reader->readSound(soundBytes);
        fwrite(soundBytes.data(), soundBytes.size(), 1, fOut);

        fclose(fOut);
    }
    catch (std::runtime_error& er)
    {
        if (fOut)
        {
            fclose(fOut);
        }
        cout << "AIFF-to-RIFF CONVERSION FAILED! " << er.what() << endl;
    }
}

void convertRiffToRiff(const string& fromFilePath, const string& toFilePath)
{
    FILE *fOut = nullptr;

    try 
    {
        WavFileReader reader(fromFilePath);
        reader.readHeader();

        fOut = fopen(toFilePath.c_str(), "wb");
        if (!fOut)
        {
            throw runtime_error("convertRiffToRiff : 1");
        }

        const auto& header = reader.header();

        //cout << "COMM chunk size = " << reader.commChunkSize() << endl;
        cout << "channels = " << header.channelCount << endl;
        cout << "numSampleFrames = " << header.frameCount << endl;
        cout << "sampleSize (bytes) = " << header.bytesPerSample << endl;
        cout << "sampleRate (frequency) = " << header.frequency << endl;

        uint32_t riffChunkSize = 0x20 + header.bytesPerFrame * header.frameCount;
        fwrite("RIFF", 4, 1, fOut);
        fwrite(&riffChunkSize, 4, 1, fOut);
        fwrite("WAVEfmt ", 8, 1, fOut);
        uint32_t fmtChunkSize = 16;
        fwrite(&fmtChunkSize, 4, 1, fOut);
        uint16_t pcmFormatId = 1;
        fwrite(&pcmFormatId, 2, 1, fOut);
        fwrite(&header.channelCount, 2, 1, fOut);
        fwrite(&header.frequency, 4, 1, fOut);
        fwrite(&header.byteRate, 4, 1, fOut);
        uint16_t blockAlign = header.channelCount * header.bytesPerSample;
        fwrite(&blockAlign, 2, 1, fOut);
        uint16_t bitsPerSample = header.bytesPerSample * 8;
        fwrite(&bitsPerSample, 2, 1, fOut);
        fwrite("data", 4, 1, fOut);
        fwrite(&header.soundByteCount, 4, 1, fOut);

        vector<char> soundBytes;
        reader.readSound(soundBytes);
        fwrite(soundBytes.data(), soundBytes.size(), 1, fOut);

        fclose(fOut);
    }
    catch (std::runtime_error& er)
    {
        if (fOut)
        {
            fclose(fOut);
        }
        cout << "RIFF-to-RIFF CONVERSION FAILED! " << er.what() << endl;
    }
}
