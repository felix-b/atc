//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <algorithm>
#include <cctype>
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <map>
#include <memory>
#include <regex>
#include <sstream>
#include <string>
#include <utility>
#include <vector>

#include "libworld.h"

using namespace std;
using namespace world;

namespace ai
{
    struct AircraftPerformanceProfile
    {
        float approachSpeedKt = 145.0f;
        float descentRateFpm = 1000.0f;
        float minutesToThreshold = 4.0f;
        float flareBufferFeet = 40.0f;
        string classification;
        string wakeCategory;
        bool hasEurocontrolData = false;
    };

    class AircraftPerformanceTable
    {
    private:
        static map<string, AircraftPerformanceProfile>& cache()
        {
            static map<string, AircraftPerformanceProfile> s_cache;
            return s_cache;
        }

        static bool& loaded()
        {
            static bool s_loaded = false;
            return s_loaded;
        }

    public:
        static AircraftPerformanceProfile lookup(shared_ptr<HostServices> host, const string& modelIcao, Aircraft::Category category)
        {
            ensureLoaded(host);

            const string key = normalizeIcao(modelIcao);
            if (key.empty())
            {
                return defaultProfile(category);
            }

            auto it = cache().find(key);
            if (it != cache().end())
            {
                return it->second;
            }

            AircraftPerformanceProfile profile = defaultProfile(category);

            if (host && loadFromEurocontrolDetailsPage(host, key, profile))
            {
                cache()[key] = profile;
                return profile;
            }

            cache()[key] = profile;
            return profile;
        }

    private:
        static void ensureLoaded(shared_ptr<HostServices> host)
        {
            if (loaded())
            {
                return;
            }

            loaded() = true;
            if (host)
            {
                loadFromCsvResource(host, cache());
            }
        }

        static bool loadFromCsvResource(shared_ptr<HostServices> host, map<string, AircraftPerformanceProfile>& out)
        {
            try
            {
                const string resourcePath = host->getResourceFilePath({ "Resources", "aircraft-performance.csv" });
                auto file = host->openFileForRead(resourcePath);

                string line;
                int lineNo = 0;
                while (std::getline(*file, line))
                {
                    ++lineNo;
                    line = trim(line);
                    if (line.empty() || line[0] == '#')
                    {
                        continue;
                    }

                    vector<string> columns;
                    splitCsvLine(line, columns);
                    if (columns.size() < 2)
                    {
                        continue;
                    }

                    AircraftPerformanceProfile profile = defaultProfile(Aircraft::Category::Jet);
                    const string modelIcao = normalizeIcao(columns.at(0));
                    profile.approachSpeedKt = static_cast<float>(parseFloat(columns, 1, profile.approachSpeedKt));
                    profile.descentRateFpm = static_cast<float>(parseFloat(columns, 2, profile.descentRateFpm));
                    profile.minutesToThreshold = static_cast<float>(parseFloat(columns, 3, profile.minutesToThreshold));
                    profile.flareBufferFeet = static_cast<float>(parseFloat(columns, 4, profile.flareBufferFeet));
                    profile.hasEurocontrolData = true;
                    out[modelIcao] = profile;
                }

                if (!out.empty())
                {
                    host->writeLog("PERF|Loaded [%d] aircraft performance profiles from resource CSV", (int)out.size());
                    return true;
                }
            }
            catch(const exception&)
            {
            }

            return false;
        }

        static bool loadFromEurocontrolDetailsPage(shared_ptr<HostServices> host, const string& modelIcao, AircraftPerformanceProfile& out)
        {
            string html;
            if (!fetchUrlText(buildEurocontrolDetailsUrl(modelIcao), html))
            {
                host->writeLog("PERF|Eurocontrol details page fetch failed for [%s]", modelIcao.c_str());
                return false;
            }

            string text = stripMarkup(std::move(html));
            normalizeWhitespace(text);

            AircraftPerformanceProfile profile = out;
            smatch match;
            bool parsedAny = false;

            const regex typePattern(
                R"(Type\s+([A-Z0-9]+)\s+APC\s+([A-Z])\s+WTC\s+([A-Z])\s+RECAT-EU\s+([A-Za-z0-9\- ]+))",
                regex_constants::icase);
            if (regex_search(text, match, typePattern))
            {
                profile.classification = trim(match[1].str());
                profile.wakeCategory = trim(match[3].str());
                parsedAny = true;
            }

            const regex approachSpeedPattern(R"(Approach\s+IAS:?\s*([0-9]+)\s*kts?)", regex_constants::icase);
            if (regex_search(text, match, approachSpeedPattern))
            {
                profile.approachSpeedKt = static_cast<float>(stoi(match[1].str()));
                parsedAny = true;
            }

            const regex approachDescentPattern(R"(Approach\s+IAS:?\s*[0-9]+\s*kts?.*?ROD:?\s*([0-9]+)\s*ft/min)", regex_constants::icase);
            if (regex_search(text, match, approachDescentPattern))
            {
                profile.descentRateFpm = static_cast<float>(stoi(match[1].str()));
                parsedAny = true;
            }

            const regex landingSpeedPattern(R"(Vat\s*\(IAS\):?\s*([0-9]+)\s*kts?)", regex_constants::icase);
            if (regex_search(text, match, landingSpeedPattern))
            {
                profile.flareBufferFeet = max(profile.flareBufferFeet, static_cast<float>(max(20, stoi(match[1].str()) / 4)));
                parsedAny = true;
            }

            if (!parsedAny)
            {
                host->writeLog("PERF|Eurocontrol details page did not yield parseable data for [%s]", modelIcao.c_str());
                return false;
            }

            profile.hasEurocontrolData = true;
            out = profile;
            return true;
        }

        static string buildEurocontrolDetailsUrl(const string& modelIcao)
        {
            return "https://learningzone.eurocontrol.int/ilp/customs/ATCPFDB/details.aspx?ICAO=" + normalizeIcao(modelIcao);
        }

        static AircraftPerformanceProfile defaultProfile(Aircraft::Category category)
        {
            AircraftPerformanceProfile profile;

            switch (category)
            {
            case Aircraft::Category::Heavy:
                profile.approachSpeedKt = 160.0f;
                profile.descentRateFpm = 1200.0f;
                profile.minutesToThreshold = 15.0f;  // Increased from 4.5 for realistic descent from cruise
                profile.flareBufferFeet = 50.0f;
                break;
            case Aircraft::Category::Jet:
                profile.approachSpeedKt = 145.0f;
                profile.descentRateFpm = 1000.0f;
                profile.minutesToThreshold = 12.0f;  // Increased from 4.0 for realistic descent from cruise
                profile.flareBufferFeet = 40.0f;
                break;
            case Aircraft::Category::Turboprop:
                profile.approachSpeedKt = 125.0f;
                profile.descentRateFpm = 700.0f;
                profile.minutesToThreshold = 10.0f;  // Increased from 3.5
                profile.flareBufferFeet = 35.0f;
                break;
            case Aircraft::Category::Prop:
            case Aircraft::Category::LightProp:
                profile.approachSpeedKt = 95.0f;
                profile.descentRateFpm = 500.0f;
                profile.minutesToThreshold = 8.0f;  // Increased from 3.0
                profile.flareBufferFeet = 30.0f;
                break;
            case Aircraft::Category::Helicopter:
                profile.approachSpeedKt = 70.0f;
                profile.descentRateFpm = 300.0f;
                profile.minutesToThreshold = 5.0f;  // Increased from 2.0
                profile.flareBufferFeet = 20.0f;
                break;
            case Aircraft::Category::Fighter:
                profile.approachSpeedKt = 170.0f;
                profile.descentRateFpm = 1500.0f;
                profile.minutesToThreshold = 6.0f;  // Increased from 3.0 (fighters descend fast but need more time)
                profile.flareBufferFeet = 50.0f;
                break;
            default:
                break;
            }

            return profile;
        }

        static AircraftPerformanceProfile defaultProfileFromClassification(const string& classification)
        {
            AircraftPerformanceProfile profile = defaultProfile(Aircraft::Category::Jet);
            if (classification.find('H') != string::npos)
            {
                profile = defaultProfile(Aircraft::Category::Helicopter);
            }
            else if (classification.find('T') != string::npos)
            {
                profile = defaultProfile(Aircraft::Category::Turboprop);
            }
            else if (classification.find('P') != string::npos)
            {
                profile = defaultProfile(Aircraft::Category::Prop);
            }

            profile.classification = classification;
            return profile;
        }

        static string normalizeIcao(string value)
        {
            value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
                return isspace(c);
            }), value.end());

            for (char& c : value)
            {
                c = static_cast<char>(toupper(static_cast<unsigned char>(c)));
            }

            return value;
        }

        static string trim(const string& value)
        {
            size_t begin = 0;
            while (begin < value.size() && isspace(static_cast<unsigned char>(value.at(begin))))
            {
                ++begin;
            }

            size_t end = value.size();
            while (end > begin && isspace(static_cast<unsigned char>(value.at(end - 1))))
            {
                --end;
            }

            return value.substr(begin, end - begin);
        }

        static void splitCsvLine(const string& line, vector<string>& columns)
        {
            columns.clear();
            string current;
            bool inQuotes = false;

            for (size_t i = 0 ; i < line.size() ; ++i)
            {
                const char ch = line.at(i);
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    columns.push_back(trim(current));
                    current.clear();
                    continue;
                }

                current.push_back(ch);
            }

            columns.push_back(trim(current));
        }

        static double parseFloat(const vector<string>& columns, size_t index, double fallback)
        {
            if (index >= columns.size())
            {
                return fallback;
            }

            const string& value = columns.at(index);
            if (value.empty())
            {
                return fallback;
            }

            char* endPtr = nullptr;
            double parsedValue = strtod(value.c_str(), &endPtr);
            return endPtr && *endPtr == '\0' ? parsedValue : fallback;
        }

        static string stripMarkup(string html)
        {
            replaceAll(html, "</tr>", "\n");
            replaceAll(html, "</p>", "\n");
            replaceAll(html, "<br>", "\n");
            replaceAll(html, "<br/>", "\n");
            replaceAll(html, "<br />", "\n");
            replaceAll(html, "&nbsp;", " ");
            replaceAll(html, "&amp;", "&");

            string text;
            text.reserve(html.size());
            bool inTag = false;

            for (size_t i = 0 ; i < html.size() ; ++i)
            {
                const char ch = html.at(i);
                if (ch == '<')
                {
                    inTag = true;
                    continue;
                }
                if (ch == '>')
                {
                    inTag = false;
                    continue;
                }
                if (!inTag)
                {
                    text.push_back(ch);
                }
            }

            return text;
        }

        static void normalizeWhitespace(string& text)
        {
            string result;
            result.reserve(text.size());
            bool previousWasSpace = false;

            for (char ch : text)
            {
                if (isspace(static_cast<unsigned char>(ch)))
                {
                    if (!previousWasSpace)
                    {
                        result.push_back(' ');
                        previousWasSpace = true;
                    }
                }
                else
                {
                    result.push_back(ch);
                    previousWasSpace = false;
                }
            }

            text.swap(result);
        }

        static void replaceAll(string& value, const string& from, const string& to)
        {
            if (from.empty())
            {
                return;
            }

            size_t start = 0;
            while ((start = value.find(from, start)) != string::npos)
            {
                value.replace(start, from.length(), to);
                start += to.length();
            }
        }

        static bool fetchUrlText(const string& url, string& responseText)
        {
            responseText.clear();
            const string command =
                "curl -g -L --fail --silent --show-error --compressed "
                "--connect-timeout 8 --max-time 20 --retry 2 --retry-delay 1 "
                "--user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
                "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36\" "
                "\"" + url + "\"";

#if IBM
            FILE* pipe = _popen(command.c_str(), "r");
#else
            FILE* pipe = popen(command.c_str(), "r");
#endif

            if (!pipe)
            {
                return false;
            }

            char buffer[4096];
            while (fgets(buffer, sizeof(buffer), pipe))
            {
                responseText.append(buffer);
            }

#if IBM
            const int exitCode = _pclose(pipe);
#else
            const int exitCode = pclose(pipe);
#endif

            return exitCode == 0 && !responseText.empty();
        }
    };
}