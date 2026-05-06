// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <algorithm>
#include <cctype>
#include <cstdlib>
#include <fstream>
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

class XPNavaidReader
{
private:
    enum class SourceKind
    {
        Fix,
        Nav
    };

    struct Record
    {
        GeoPoint location;
        string anchorIcao;
        SourceKind sourceKind;
    };

    struct Cache
    {
        unordered_map<string, vector<Record>> recordsByKey;
    };

    shared_ptr<HostServices> m_host;
    mutable shared_ptr<Cache> m_cache;

public:
    explicit XPNavaidReader(shared_ptr<HostServices> host) :
        m_host(std::move(host)),
        m_cache()
    {
    }

public:
    bool tryResolveWaypoint(const string& waypointName, GeoPoint& location) const
    {
        return tryResolveWaypoint("", waypointName, GeoPoint::empty, location);
    }

    bool tryResolveWaypoint(const string& contextIcao, const string& waypointName, GeoPoint& location) const
    {
        return tryResolveWaypoint(contextIcao, waypointName, GeoPoint::empty, location);
    }

    bool tryResolveWaypoint(
        const string& contextIcao, 
        const string& waypointName, 
        const GeoPoint& contextLocation,
        GeoPoint& location) const
    {
        ensureLoaded();

        if (!m_cache)
        {
            return false;
        }

        const string normalizedName = normalizeKey(waypointName);
        if (normalizedName.empty())
        {
            return false;
        }

        vector<string> candidateKeys;
        appendCandidateKeys(candidateKeys, normalizedName);
        if (normalizedName != waypointName)
        {
            appendCandidateKeys(candidateKeys, normalizeKey(waypointName));
        }

        const string normalizedContext = normalizeKey(contextIcao);
        const bool hasContextLocation = (contextLocation != GeoPoint::empty);

        const Record* bestRecord = nullptr;
        int bestScore = numeric_limits<int>::min();

        for (const auto& candidateKey : candidateKeys)
        {
            const auto found = m_cache->recordsByKey.find(candidateKey);
            if (found == m_cache->recordsByKey.end())
            {
                continue;
            }

            for (const auto& record : found->second)
            {
                int score = candidateKey == normalizedName ? 1000 : 850;

                if (!normalizedContext.empty() && normalizeKey(record.anchorIcao) == normalizedContext)
                {
                    score += 500;
                }

                if (record.sourceKind == SourceKind::Fix)
                {
                    score += 10;
                }

                // Distance-based scoring: prefer waypoints close to context location
                // This is crucial for STAR/SID waypoints which should be near the airport
                if (hasContextLocation && record.location != GeoPoint::empty)
                {
                    float distanceNm = GeoMath::getDistanceMeters(contextLocation, record.location) / 1852.0f;
                    if (distanceNm < 50.0f)  // Within 50 NM
                    {
                        score += 400;  // Significant bonus for nearby waypoints
                    }
                    else if (distanceNm < 100.0f)  // Within 100 NM
                    {
                        score += 200;
                    }
                    else if (distanceNm < 200.0f)  // Within 200 NM
                    {
                        score += 100;
                    }
                    // Waypoints beyond 200 NM get no distance bonus
                }

                if (!bestRecord || score > bestScore)
                {
                    bestScore = score;
                    bestRecord = &record;
                }
            }
        }

        if (!bestRecord)
        {
            return false;
        }

        location = bestRecord->location;
        return true;
    }

private:
    static unordered_map<string, weak_ptr<Cache>>& cacheStore()
    {
        static unordered_map<string, weak_ptr<Cache>> s_cacheStore;
        return s_cacheStore;
    }

    string cacheKey() const
    {
        if (!m_host)
        {
            return "";
        }

        return m_host->getHostFilePath({ "Custom Data", "earth_fix.dat" }) + "|" +
            m_host->getHostFilePath({ "Custom Data", "earth_nav.dat" });
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
            loadFixData(*cache);
            loadNavData(*cache);
        }
        catch (const exception&)
        {
            // Best-effort lookup only; individual file failures should simply leave the cache partial.
        }

        m_cache = cache;
        cacheStore()[key] = cache;
    }

    void loadFixData(Cache& cache) const
    {
        if (!m_host)
        {
            return;
        }

        try
        {
            const string filePath = m_host->getHostFilePath({ "Custom Data", "earth_fix.dat" });
            auto input = m_host->openFileForRead(filePath);
            if (!input)
            {
                return;
            }

            string line;
            while (getline(*input, line))
            {
                auto fields = splitWhitespace(line);
                if (fields.size() < 3 || !looksLikeNumber(fields.at(0)) || !looksLikeNumber(fields.at(1)))
                {
                    continue;
                }

                try
                {
                    const GeoPoint location(stod(fields.at(0)), stod(fields.at(1)));
                    registerLocation(cache, fields.at(2), location, "", SourceKind::Fix);
                }
                catch (const exception&)
                {
                }
            }
        }
        catch (const exception&)
        {
        }
    }

    void loadNavData(Cache& cache) const
    {
        if (!m_host)
        {
            return;
        }

        try
        {
            const string filePath = m_host->getHostFilePath({ "Custom Data", "earth_nav.dat" });
            auto input = m_host->openFileForRead(filePath);
            if (!input)
            {
                return;
            }

            string line;
            while (getline(*input, line))
            {
                auto fields = splitWhitespace(line);
                if (fields.size() < 4 || !looksLikeNumber(fields.at(1)) || !looksLikeNumber(fields.at(2)))
                {
                    continue;
                }

                const string ident = findNavIdent(fields);
                if (ident.empty())
                {
                    continue;
                }

                string anchorIcao;
                for (size_t i = 0; i < fields.size(); ++i)
                {
                    if (fields.at(i) == ident)
                    {
                        for (size_t j = i + 1; j < fields.size(); ++j)
                        {
                            if (looksLikeIcao(fields.at(j)))
                            {
                                anchorIcao = fields.at(j);
                                break;
                            }
                        }
                        break;
                    }
                }

                try
                {
                    const GeoPoint location(stod(fields.at(1)), stod(fields.at(2)));
                    registerLocation(cache, ident, location, anchorIcao, SourceKind::Nav);
                }
                catch (const exception&)
                {
                }
            }
        }
        catch (const exception&)
        {
        }
    }

    static vector<string> splitWhitespace(const string& text)
    {
        vector<string> result;
        istringstream input(text);
        string field;
        while (input >> field)
        {
            result.push_back(field);
        }
        return result;
    }

    static bool looksLikeNumber(const string& value)
    {
        if (value.empty())
        {
            return false;
        }

        char* endPtr = nullptr;
        strtod(value.c_str(), &endPtr);
        return endPtr && *endPtr == '\0';
    }

    static bool looksLikeIcao(const string& value)
    {
        if (value.length() != 4)
        {
            return false;
        }

        return all_of(value.begin(), value.end(), [](unsigned char c) {
            return isalnum(static_cast<unsigned char>(c)) != 0;
        });
    }

    static string normalizeKey(string value)
    {
        value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
            return !isalnum(static_cast<unsigned char>(c));
        }), value.end());

        transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
            return static_cast<char>(toupper(c));
        });
        return value;
    }

    static void appendCandidateKeys(vector<string>& candidateKeys, const string& normalizedName)
    {
        if (normalizedName.empty())
        {
            return;
        }

        const auto addKey = [&candidateKeys](const string& key) {
            if (key.empty())
            {
                return;
            }

            if (find(candidateKeys.begin(), candidateKeys.end(), key) == candidateKeys.end())
            {
                candidateKeys.push_back(key);
            }
        };

        addKey(normalizedName);

        string alias = normalizedName;
        while (alias.length() > 1)
        {
            const char prefix = alias.front();
            if (prefix != 'C' && prefix != 'F' && prefix != 'I' && prefix != 'R')
            {
                break;
            }

            alias = alias.substr(1);
            addKey(alias);
        }
    }

    static vector<string> aliasKeys(const string& normalizedKey)
    {
        vector<string> keys;
        appendCandidateKeys(keys, normalizedKey);
        return keys;
    }

    static string findNavIdent(const vector<string>& fields)
    {
        for (size_t i = 3; i < fields.size(); ++i)
        {
            const string& token = fields.at(i);
            if (looksLikeNumber(token))
            {
                continue;
            }

            const string normalized = normalizeKey(token);
            if (!normalized.empty() && any_of(normalized.begin(), normalized.end(), [](unsigned char c) {
                return isalpha(static_cast<unsigned char>(c)) != 0;
            }))
            {
                return normalized;
            }
        }

        return "";
    }

    static void registerLocation(
        Cache& cache,
        const string& key,
        const GeoPoint& location,
        const string& anchorIcao,
        SourceKind sourceKind)
    {
        const string normalizedKey = normalizeKey(key);
        if (normalizedKey.empty())
        {
            return;
        }

        const Record record = { location, anchorIcao, sourceKind };
        for (const auto& alias : aliasKeys(normalizedKey))
        {
            cache.recordsByKey[alias].push_back(record);
        }
    }
};