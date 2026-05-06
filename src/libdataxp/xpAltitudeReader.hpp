// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <algorithm>
#include <array>
#include <cctype>
#include <cmath>
#include <cstdlib>
#include <limits>
#include <memory>
#include <sstream>
#include <string>
#include <unordered_map>
#include <utility>
#include <vector>

#include "../libworld/libworld.h"

using namespace std;
using namespace world;

class XPMinimumAltitudeReader
{
private:
    struct MoraRow
    {
    public:
        int latitude = 0;
        int longitude = 0;
        array<int, 30> altitudesHundreds{};
    };

    struct MsaSector
    {
    public:
        char bearingMode = 'M';
        int bearingDegrees = 0;
        int altitudeHundreds = 0;
        int radiusNm = 0;
    };

    struct MsaRecord
    {
    public:
        int pointType = 0;
        string ident;
        string region;
        string airportIcao;
        char bearingMode = 'M';
        vector<MsaSector> sectors;
    };

    struct Cache
    {
        unordered_map<int, vector<MoraRow>> moraRowsByLatitude;
        unordered_map<string, vector<MsaRecord>> msaRecordsByKey;
    };

private:
    shared_ptr<HostServices> m_host;
    mutable shared_ptr<Cache> m_cache;

public:
    explicit XPMinimumAltitudeReader(shared_ptr<HostServices> host) :
        m_host(std::move(host)),
        m_cache()
    {
    }

public:
    float minimumAltitudeForLeg(
        const string& contextIcao,
        const string& fromName,
        const GeoPoint& fromLocation,
        const string& toName,
        const GeoPoint& toLocation,
        float baseAltitudeFeet) const
    {
        ensureLoaded();

        float result = baseAltitudeFeet;

        float moraFeet = 0.0f;
        if (tryGetMoraFloorAlongSegment(fromLocation, toLocation, moraFeet))
        {
            result = max(result, moraFeet);
        }

        float msaFeet = 0.0f;
        if (tryGetMsaFloor(contextIcao, fromName, msaFeet))
        {
            result = max(result, msaFeet);
        }

        if (tryGetMsaFloor(contextIcao, toName, msaFeet))
        {
            result = max(result, msaFeet);
        }

        return result;
    }

    bool tryGetMoraFloorAt(const GeoPoint& location, float& altitudeFeet) const
    {
        ensureLoaded();
        if (!m_cache)
        {
            return false;
        }

        const int latitude = floorCoordinate(location.latitude);
        const int longitude = floorCoordinate(location.longitude);
        const auto foundLatitude = m_cache->moraRowsByLatitude.find(latitude);
        if (foundLatitude == m_cache->moraRowsByLatitude.end())
        {
            return false;
        }

        for (const auto& row : foundLatitude->second)
        {
            if (longitude < row.longitude || longitude >= row.longitude + static_cast<int>(row.altitudesHundreds.size()))
            {
                continue;
            }

            const size_t columnIndex = static_cast<size_t>(longitude - row.longitude);
            altitudeFeet = hundredsToFeet(row.altitudesHundreds.at(columnIndex));
            return true;
        }

        return false;
    }

    bool tryGetMsaFloor(const string& contextIcao, const string& pointName, float& altitudeFeet) const
    {
        ensureLoaded();
        if (!m_cache)
        {
            return false;
        }

        const auto getRecords = [&](const string& key) -> const vector<MsaRecord>* {
            const auto found = m_cache->msaRecordsByKey.find(key);
            if (found == m_cache->msaRecordsByKey.end())
            {
                return nullptr;
            }
            return &found->second;
        };

        const string airportKey = makeMsaKey(contextIcao, pointName);
        const vector<MsaRecord>* records = getRecords(airportKey);
        if (!records && !pointName.empty())
        {
            records = getRecords(normalizeKey(pointName));
        }

        if (!records)
        {
            return false;
        }

        float bestAltitude = -1.0f;
        for (const auto& record : *records)
        {
            for (const auto& sector : record.sectors)
            {
                if (sector.altitudeHundreds <= 0)
                {
                    continue;
                }

                bestAltitude = max(bestAltitude, hundredsToFeet(sector.altitudeHundreds));
            }
        }

        if (bestAltitude < 0.0f)
        {
            return false;
        }

        altitudeFeet = bestAltitude;
        return true;
    }

private:
    static string cacheKeyForPath(const shared_ptr<HostServices>& host, const vector<string>& relativePathParts)
    {
        if (!host)
        {
            return "";
        }

        return host->getHostFilePath(relativePathParts);
    }

    string cacheKey() const
    {
        const string moraPath = cacheKeyForPath(m_host, { "Custom Data", "earth_mora.dat" });
        const string msaPath = cacheKeyForPath(m_host, { "Custom Data", "earth_msa.dat" });
        return moraPath + "|" + msaPath;
    }

    void ensureLoaded() const
    {
        if (m_cache)
        {
            return;
        }

        const string key = cacheKey();
        if (key.empty())
        {
            return;
        }

        const auto found = cacheStore().find(key);
        if (found != cacheStore().end())
        {
            m_cache = found->second.lock();
            if (m_cache)
            {
                return;
            }
        }

        auto cache = shared_ptr<Cache>(new Cache());
        try
        {
            loadMoraData(*cache);
            loadMsaData(*cache);
        }
        catch (const exception&)
        {
            // Best-effort only; missing or partial files should simply leave the cache sparse.
        }

        m_cache = cache;
        cacheStore()[key] = cache;
    }

    void loadMoraData(Cache& cache) const
    {
        if (!m_host)
        {
            return;
        }

        try
        {
            const string filePath = m_host->getHostFilePath({ "Custom Data", "earth_mora.dat" });
            auto input = m_host->openFileForRead(filePath);
            if (!input)
            {
                return;
            }

            string line;
            while (getline(*input, line))
            {
                const auto fields = splitWhitespace(line);
                if (fields.empty())
                {
                    continue;
                }

                if (fields.size() == 1 && fields.front() == "99")
                {
                    break;
                }

                int latitude = 0;
                int longitude = 0;
                if (fields.size() < 32 || !tryParseInt(fields.at(0), latitude) || !tryParseInt(fields.at(1), longitude))
                {
                    continue;
                }

                MoraRow row;
                row.latitude = latitude;
                row.longitude = longitude;

                bool valid = true;
                for (size_t i = 0; i < row.altitudesHundreds.size(); ++i)
                {
                    int altitudeHundreds = 0;
                    if (!tryParseInt(fields.at(i + 2), altitudeHundreds))
                    {
                        valid = false;
                        break;
                    }

                    row.altitudesHundreds.at(i) = altitudeHundreds;
                }

                if (valid)
                {
                    cache.moraRowsByLatitude[latitude].push_back(row);
                }
            }
        }
        catch (const exception&)
        {
        }
    }

    void loadMsaData(Cache& cache) const
    {
        if (!m_host)
        {
            return;
        }

        try
        {
            const string filePath = m_host->getHostFilePath({ "Custom Data", "earth_msa.dat" });
            auto input = m_host->openFileForRead(filePath);
            if (!input)
            {
                return;
            }

            string line;
            while (getline(*input, line))
            {
                const auto fields = splitWhitespace(line);
                if (fields.empty())
                {
                    continue;
                }

                if (fields.size() == 1 && fields.front() == "99")
                {
                    break;
                }

                int pointType = 0;
                if (fields.size() < 8 || !tryParseInt(fields.at(0), pointType))
                {
                    continue;
                }

                MsaRecord record;
                record.pointType = pointType;
                record.ident = fields.at(1);
                record.region = fields.at(2);
                record.airportIcao = fields.at(3);
                record.bearingMode = fields.at(4).empty() ? 'M' : static_cast<char>(toupper(static_cast<unsigned char>(fields.at(4).front())));

                for (size_t i = 5; i + 2 < fields.size(); i += 3)
                {
                    if (fields.at(i) == "000" && fields.at(i + 1) == "000" && fields.at(i + 2) == "0")
                    {
                        break;
                    }

                    int bearingDegrees = 0;
                    int altitudeHundreds = 0;
                    int radiusNm = 0;
                    if (!tryParseInt(fields.at(i), bearingDegrees) ||
                        !tryParseInt(fields.at(i + 1), altitudeHundreds) ||
                        !tryParseInt(fields.at(i + 2), radiusNm))
                    {
                        break;
                    }

                    record.sectors.push_back({ record.bearingMode, bearingDegrees, altitudeHundreds, radiusNm });
                }

                registerMsaRecord(cache, record);
            }
        }
        catch (const exception&)
        {
        }
    }

    static void registerMsaRecord(Cache& cache, const MsaRecord& record)
    {
        const string airportKey = makeMsaKey(record.airportIcao, record.ident);
        const string identKey = normalizeKey(record.ident);

        if (!airportKey.empty())
        {
            cache.msaRecordsByKey[airportKey].push_back(record);
        }

        if (!identKey.empty())
        {
            cache.msaRecordsByKey[identKey].push_back(record);
        }
    }

    bool tryGetMoraFloorAlongSegment(const GeoPoint& fromLocation, const GeoPoint& toLocation, float& altitudeFeet) const
    {
        if (fromLocation == GeoPoint::empty || toLocation == GeoPoint::empty)
        {
            return false;
        }

        const float totalDistanceMeters = GeoMath::getDistanceMeters(fromLocation, toLocation);
        if (totalDistanceMeters <= 0.0f)
        {
            return tryGetMoraFloorAt(fromLocation, altitudeFeet);
        }

        const float totalDistanceNm = totalDistanceMeters / METERS_IN_1_NAUTICAL_MILE;
        const int sampleCount = max(2, min(32, static_cast<int>(totalDistanceNm / 10.0f) + 2));
        const float heading = GeoMath::getHeadingFromPoints(fromLocation, toLocation);

        float bestAltitude = -1.0f;
        for (int i = 0; i < sampleCount; ++i)
        {
            const float fraction = sampleCount <= 1
                ? 0.0f
                : static_cast<float>(i) / static_cast<float>(sampleCount - 1);
            const GeoPoint samplePoint = GeoMath::getPointAtDistance(fromLocation, heading, totalDistanceMeters * fraction);

            float sampleAltitude = 0.0f;
            if (tryGetMoraFloorAt(samplePoint, sampleAltitude))
            {
                bestAltitude = max(bestAltitude, sampleAltitude);
            }
        }

        if (bestAltitude < 0.0f)
        {
            return false;
        }

        altitudeFeet = bestAltitude;
        return true;
    }

    static unordered_map<string, weak_ptr<Cache>>& cacheStore()
    {
        static unordered_map<string, weak_ptr<Cache>> s_cacheStore;
        return s_cacheStore;
    }

    static vector<string> splitWhitespace(const string& value)
    {
        vector<string> result;
        istringstream input(value);
        string field;
        while (input >> field)
        {
            result.push_back(field);
        }
        return result;
    }

    static bool tryParseInt(const string& value, int& parsedValue)
    {
        if (value.empty())
        {
            return false;
        }

        char* endPtr = nullptr;
        const long result = strtol(value.c_str(), &endPtr, 10);
        if (!endPtr || *endPtr != '\0')
        {
            return false;
        }

        parsedValue = static_cast<int>(result);
        return true;
    }

    static int floorCoordinate(double value)
    {
        return static_cast<int>(floor(value));
    }

    static float hundredsToFeet(int valueHundreds)
    {
        return static_cast<float>(valueHundreds) * 100.0f;
    }

    static string normalizeKey(string value)
    {
        value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
            return !isalnum(c);
        }), value.end());

        transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
            return static_cast<char>(toupper(c));
        });

        return value;
    }

    static string makeMsaKey(const string& contextIcao, const string& pointName)
    {
        const string normalizedContext = normalizeKey(contextIcao);
        const string normalizedPoint = normalizeKey(pointName);

        if (normalizedContext.empty())
        {
            return normalizedPoint;
        }

        if (normalizedPoint.empty())
        {
            return normalizedContext;
        }

        return normalizedContext + "|" + normalizedPoint;
    }
};