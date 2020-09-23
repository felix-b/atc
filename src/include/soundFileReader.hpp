// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 

#pragma once

#include <cstdlib>
#include <string>
#include <sstream>
#include <vector>
#include <memory>

using namespace std;

class SoundFileReader
{
public:
    class SoundFileReaderError : public runtime_error
    {
    public:
        SoundFileReaderError(
            const string& filePath, 
            FILE *file, 
            const string& errorMessage
        ) : runtime_error(formatMessage(filePath, file, errorMessage))
        {
        }
    public:
        static string formatMessage(const string& filePath, FILE *file, const string& errorMessage)
        {
            stringstream str;
            fpos_t position;

            str << "Error loading sound. "
                << errorMessage
                << " at [" 
#if LIN
                << (fgetpos(file, &position) == 0 ? to_string(position.__pos) : string("??"))
#else
                << (fgetpos(file, &position) == 0 ? to_string(position) : string("??"))
#endif
                << "] in file [" 
                << filePath
                << "]";

            return str.str();
        }
    };
    struct Header
    {
    public:
        uint16_t channelCount = 0;
        uint16_t bytesPerSample = 0;
        uint16_t bytesPerFrame = 0;
        uint32_t frameCount = 0;
        uint32_t soundByteCount = 0;
        uint32_t frequency = 0;
        uint32_t byteRate = 0;
    };
public:
    typedef function<void(char *buffer, size_t size)> ProcessFunc;
protected:
    string m_filePath;
    FILE *m_file;
    Header m_header;
    bool m_firstMagicWasRead;
protected:
    SoundFileReader(const string& _filePath) :
        m_filePath(_filePath),
        m_firstMagicWasRead(false)
    {
        m_file = fopen(_filePath.c_str(), "rb");
    }
    SoundFileReader(const string& _filePath, FILE *_file, bool _firstMagicWasRead) :
        m_filePath(_filePath),
        m_file(_file),
        m_firstMagicWasRead(_firstMagicWasRead)
    {
        m_firstMagicWasRead = true;
    }
    virtual ~SoundFileReader()
    {
        if (m_file)
        {
            fclose(m_file);
            m_file = nullptr;
        }
    }
public:
    const string& filePath() const { return m_filePath; }
    const Header& header() const { return m_header; }
public: 
    virtual void readHeader() = 0;
    virtual void readSound(vector<char>& destination, ProcessFunc process = noopProcess) = 0;
protected:
    SoundFileReaderError error(const string& message)
    {
        return SoundFileReaderError(m_filePath, m_file, message);
    }
protected:
    void readOrThrow(unsigned char* destination, size_t count)
    {
        if (fread(destination, count, 1, m_file) != 1)
        {
            throw error("Unexpected end of file");
        }
    }
    string readMagic(size_t size)
    {
        char magic[size + 1];
        readOrThrow((unsigned char*)magic, size);
        magic[size] = '\0';
        return string(magic);
    }
    void readMagicOrThrow(const string& magic)
    {
        if (readMagic(magic.length()) != magic)
        {
            throw error("Expected magic [" + magic + "]");
        }
    }
    void skipBytes(size_t count)
    {
        if (fseek(m_file, count, SEEK_CUR) != 0)
        {
            throw error("Unexpected end of file");
        }
    }
    uint16_t readBigUInt16()
    {
        unsigned char buffer16[2];
        readOrThrow(buffer16, 2);
        return (buffer16[0] << 8) + buffer16[1];
    }
    uint32_t readBigUInt32()
    {
        unsigned char buffer32[4];
        readOrThrow(buffer32, 4);
        return (buffer32[0] << 24) + (buffer32[1] << 16) + (buffer32[2] << 8) + buffer32[3];
    }
    long double readBigDouble80()
    {
        unsigned char buffer80[10];
        readOrThrow(buffer80, 10);
        
        unsigned char reverseBuffer80[10];
        for (int i = 0 ; i < 10 ; i++)
        {
            reverseBuffer80[i] = buffer80[9-i];
        }
        
        long double value;
        memcpy(&value, reverseBuffer80, 10);
        return value;
    }
    void readBigSampleBytes(vector<char>& destination, uint32_t count, ProcessFunc process = noopProcess)
    {
        readLittleSampleBytes(destination, count, [=](char *buffer, size_t size) {
            flipSampleEndianess(buffer, size, m_header.bytesPerSample);
            process(buffer, size);
        });
    }
    uint16_t readLittleUInt16()
    {
        unsigned char buffer16[2];
        readOrThrow(buffer16, 2);
        return (buffer16[1] << 8) + buffer16[0];
    }
    uint32_t readLittleUInt32()
    {
        unsigned char buffer32[4];
        readOrThrow(buffer32, 4);
        return (buffer32[3] << 24) + (buffer32[2] << 16) + (buffer32[1] << 8) + buffer32[0];
    }
    void readLittleSampleBytes(vector<char>& destination, uint32_t count, ProcessFunc process = noopProcess)
    {
        constexpr size_t BUFFER_SIZE = 32768;     
        auto array = std::vector<char>(BUFFER_SIZE);

        while (destination.size() != count)
        {
            size_t bytesRead = fread(array.data(), 1, array.size(), m_file);
            if (bytesRead <= 0)
            {
                break;
            }

            if (destination.size() + bytesRead > count)
            {
                bytesRead = count - destination.size();
            }

            process(array.data(), bytesRead);

            destination.insert(
                destination.end(), 
                array.begin(), 
                array.begin() + bytesRead);
        };
    }
    void flipSampleEndianess(char *buffer, size_t bufferSize, uint16_t bytesPerSample)
    {
        if (bytesPerSample == 1)
        {
            return;
        }
        if (bytesPerSample > 4)
        {
            throw error("Sample size > 32-bit is not supported");
        }

        size_t sampleSize = m_header.bytesPerSample;
        if ((bufferSize % sampleSize) != 0)
        {
            throw error("Buffer size is not multiple of sample size");
        }

        char *sample;
        char temp;

        for (size_t index = 0 ; index < bufferSize ; index += sampleSize)
        {
            sample = buffer + index;

            switch (sampleSize)
            {
            case 2:
                temp = sample[0];
                sample[0] = sample[1];
                sample[1] = temp;
                break;
            case 3:
                temp = sample[0];
                sample[0] = sample[2];
                sample[2] = temp;
                break;
            case 4:
                temp = sample[0];
                sample[0] = sample[3];
                sample[3] = temp;
                temp = sample[1];
                sample[1] = sample[2];
                sample[2] = temp;
                break;
            }
        }
    }
public:
    static void noopProcess(char *buffer, size_t size)
    {
    }
};

class WavFileReader : public SoundFileReader
{
public:

    WavFileReader(const string& _filePath) : 
        SoundFileReader(_filePath)
    {
    }

    WavFileReader(const string& _filePath, FILE *_file, bool _firstMagicWasRead) :
        SoundFileReader(_filePath, _file, _firstMagicWasRead)
    {
    }

public:

    void readHeader() override
    {
        if (!m_firstMagicWasRead)
        {
            readMagicOrThrow("RIFF");
        }

        skipBytes(4);

        readMagicOrThrow("WAVE");
        readMagicOrThrow("fmt ");

        uint32_t fmtChunkSize = readLittleUInt32();
        if (fmtChunkSize < 16)
        {
            throw error("fmt chunk too short, size=" + to_string(fmtChunkSize));
        }

        uint16_t audioFormat = readLittleUInt16();
        if (audioFormat != 1)
        {
            throw error("Not a PCM format");
        }

        m_header.channelCount = readLittleUInt16();
        m_header.frequency = readLittleUInt32();

        skipBytes(6); // byte rate; block align

        uint16_t bitsPerSample = readLittleUInt16();
        if ((bitsPerSample % 8) != 0)
        {
            throw error("BPS is not supported: " + to_string(bitsPerSample));
        }

        m_header.bytesPerSample = bitsPerSample / 8;
        m_header.bytesPerFrame = m_header.channelCount * m_header.bytesPerSample;
        m_header.byteRate = m_header.bytesPerFrame * m_header.frequency;
        m_header.soundByteCount = m_header.frameCount * m_header.bytesPerFrame;

        if (fmtChunkSize > 16)
        {
            skipBytes(fmtChunkSize - 16);
        }

        readMagicOrThrow("data");
        m_header.soundByteCount = readLittleUInt32();
        m_header.frameCount = m_header.soundByteCount / m_header.bytesPerFrame;
    }

    void readSound(vector<char>& destination, ProcessFunc process = noopProcess) override
    {
        readLittleSampleBytes(destination, m_header.soundByteCount, process);
    }

};

class AiffFileReader : public SoundFileReader
{
private:

    uint32_t m_commChunkSize = 0;

public:

    AiffFileReader(const string& filePath) : 
        SoundFileReader(filePath)
    {
    }

    AiffFileReader(const string& _filePath, FILE *_file, bool _firstMagicWasRead) :
        SoundFileReader(_filePath, _file, _firstMagicWasRead)
    {
    }

public:

    void readHeader() override
    {
        if (!m_firstMagicWasRead)
        {
            readMagicOrThrow("FORM");
        }

        skipBytes(4);
        readMagicOrThrow("AIFF");
        readMagicOrThrow("COMM");
        
        m_commChunkSize = readBigUInt32();
        if (m_commChunkSize < 18)
        {
            throw error("COMM chunk too short, size=" + to_string(m_commChunkSize));
        }

        m_header.channelCount = readBigUInt16();
        m_header.frameCount = readBigUInt32();
        
        uint16_t bitsPerSample = readBigUInt16();
        if ((bitsPerSample % 8) != 0)
        {
            throw error("BPS is not supported: " + to_string(bitsPerSample));
        }

        m_header.bytesPerSample = bitsPerSample / 8;
        m_header.frequency = (uint32_t)readBigDouble80();
        m_header.bytesPerFrame = m_header.channelCount * m_header.bytesPerSample;
        m_header.byteRate = m_header.bytesPerFrame * m_header.frequency;
        m_header.soundByteCount = m_header.frameCount * m_header.bytesPerFrame;

        if (m_commChunkSize > 18)
        {
            skipBytes(m_commChunkSize - 18);
        }
    }

    void readSound(vector<char>& destination, ProcessFunc process = noopProcess) override
    {
        skipToSsndChunk();
        uint32_t ssndChunkSize = readBigUInt32();
        uint32_t dataOffset = readBigUInt32();
        uint32_t dataBlockSize = readBigUInt32();

        if (dataOffset != 0 || dataBlockSize != 0)
        {
            throw error("Non-zero dataOffset/dataBlockSize are not supported");
        }

        if (ssndChunkSize != m_header.soundByteCount + 8)
        {
            throw error(
                "Unexpected SSND chunk size [" + 
                to_string(ssndChunkSize) + 
                "] expected [" + 
                to_string(m_header.soundByteCount + 8) + 
                "]");
        }

        readBigSampleBytes(destination, m_header.soundByteCount, process);
    }

public:

    uint32_t commChunkSize() const { return m_commChunkSize; }

private:

    void skipToSsndChunk()
    {
        while (readMagic(4) != "SSND")
        {
            // unknown chunk - read its size and skip
            uint32_t chunkSize = readBigUInt32();
            skipBytes(chunkSize);
        }
    }
};

class SoundFileReaderFactory
{
private:
    SoundFileReaderFactory() {}
public:
    static shared_ptr<SoundFileReader> createReader(const string& filePath)
    {
        FILE *file = fopen(filePath.c_str(), "rb");
        char magicBuffer[4 + 1];

        if (fread(magicBuffer, 4, 1, file) != 1)
        {
            throw runtime_error("Failed to open sound file: " + filePath);
        }
        
        magicBuffer[4] = '\0';
        string magicString(magicBuffer);

        if (magicString == "RIFF")
        {
            return shared_ptr<SoundFileReader>(new WavFileReader(filePath, file, true));
        }
        
        if (magicString == "FORM")
        {
            return shared_ptr<SoundFileReader>(new AiffFileReader(filePath, file, true));
        }

        throw runtime_error("Unrecognized file format: " + filePath);
    }
};