//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <algorithm>
#include <cctype>
#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <functional>
#include <memory>
#include <set>
#include <sstream>
#include <string>
#include <utility>
#include <unordered_set>
#include <vector>

#include "libworld.h"
#include "airlineReferenceTable.hpp"
#include "aircraftTypeReferenceTable.hpp"
#include "simpleJson.hpp"

using namespace std;
using namespace world;

struct Fr24ScheduleEntry
{
    string airlineIcao;
    string flightNumber;
    string callsign;
    string aircraftIcao;
    string originIcao;
    string destinationIcao;
    time_t scheduledTime = 0;
};

class Fr24AirportScheduleSource
{
private:
    shared_ptr<HostServices> m_host;

public:
    explicit Fr24AirportScheduleSource(shared_ptr<HostServices> host) : m_host(std::move(host))
    {
    }

    // Public helpers for other schedule sources that also want to use
    // undetected-chromedriver based fetching and JSON extraction.
    bool fetchPageWithUndetectedChromedriver(const string& url, string& responseText)
    {
        return fetchUrlTextWithUndetectedChromedriver(url, responseText);
    }

    static bool extractJsonPayloadFromText(string& text)
    {
        return extractJsonPayload(text);
    }

    bool tryLoadAirportSchedules(
        const string& airportIcao,
        vector<Fr24ScheduleEntry>& departures,
        vector<Fr24ScheduleEntry>& arrivals)
    {
        departures.clear();
        arrivals.clear();

        bool loadedDepartures = loadAirportScheduleMode(airportIcao, "departures", departures);
        bool loadedArrivals = loadAirportScheduleMode(airportIcao, "arrivals", arrivals);

        if (loadedDepartures)
        {
            sortEntries(departures);
        }
        if (loadedArrivals)
        {
            sortEntries(arrivals);
        }

        return loadedDepartures || loadedArrivals;
    }

private:
    static void sortEntries(vector<Fr24ScheduleEntry>& entries)
    {
        sort(entries.begin(), entries.end(), [](const Fr24ScheduleEntry& a, const Fr24ScheduleEntry& b) {
            if (a.scheduledTime != b.scheduledTime)
            {
                if (a.scheduledTime == 0) return false;
                if (b.scheduledTime == 0) return true;
                return a.scheduledTime < b.scheduledTime;
            }

            if (a.airlineIcao != b.airlineIcao)
            {
                return a.airlineIcao < b.airlineIcao;
            }

            return a.flightNumber < b.flightNumber;
        });
    }

    bool loadAirportScheduleMode(const string& airportIcao, const string& mode, vector<Fr24ScheduleEntry>& entries)
    {
        const string normalizedAirportIcao = normalizeCode(airportIcao);
        string url = buildAirportScheduleUrl(normalizedAirportIcao, mode);
        string responseText;

        if (!fetchUrlText(url, responseText))
        {
            m_host->writeLog("FR24|schedule[%s] airport[%s] fetch failed", mode.c_str(), normalizedAirportIcao.c_str());
            return false;
        }

        try
        {
            SimpleJson root = SimpleJson::parse(responseText);
            const SimpleJson* scheduleData = tryGetPath(root, {
                "result",
                "response",
                "airport",
                "pluginData",
                "schedule",
                mode,
                "data"
            });

            if (!scheduleData || !scheduleData->isArray())
            {
                m_host->writeLog("FR24|schedule[%s] airport[%s] did not contain a data array", mode.c_str(), normalizedAirportIcao.c_str());
                return false;
            }

            collectEntries(*scheduleData, normalizedAirportIcao, mode, entries);

            m_host->writeLog(
                "FR24|schedule[%s] airport[%s] loaded [%d] entries",
                mode.c_str(),
                normalizedAirportIcao.c_str(),
                (int)entries.size());
            return !entries.empty();
        }
        catch(const exception& e)
        {
            m_host->writeLog(
                "FR24|schedule[%s] airport[%s] parse failed: %s",
                mode.c_str(),
                normalizedAirportIcao.c_str(),
                e.what());
            return false;
        }
    }

    static string buildAirportScheduleUrl(const string& airportIcao, const string& mode)
    {
        string url = "https://api.flightradar24.com/common/v1/airport.json?code=";
        url += normalizeCode(airportIcao);
        url += "&plugin%5B%5D=&plugin-setting%5Bschedule%5D%5Bmode%5D=";
        url += mode;
        return url;
    }

    bool fetchUrlText(const string& url, string& responseText)
    {
        responseText.clear();

        // Prefer undetected-chromedriver for FR24 to reduce anti-bot blocks.
        if (fetchUrlTextWithUndetectedChromedriver(url, responseText))
        {
            if (extractJsonPayload(responseText))
            {
                return true;
            }
        }

        // Fallback to curl for environments without Python/uc/chrome.
        const string command = "curl -g -L --fail --silent --show-error \"" + url + "\"";

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

        return exitCode == 0 && extractJsonPayload(responseText);
    }

    static string escapeForPythonTripleQuoted(const string& value)
    {
        string escaped = value;
        size_t pos = 0;
        while ((pos = escaped.find("'''", pos)) != string::npos)
        {
            escaped.replace(pos, 3, "\\'\\'\\'");
            pos += 6;
        }
        return escaped;
    }

    bool runPythonScriptAndCapture(const string& scriptPath, const string& pythonBinary, string& responseText)
    {
        const string command = "\"" + pythonBinary + "\" \"" + scriptPath + "\"";

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
        responseText.clear();
        while (fgets(buffer, sizeof(buffer), pipe))
        {
            responseText.append(buffer);
        }

#if IBM
        const int exitCode = _pclose(pipe);
#else
        const int exitCode = pclose(pipe);
#endif

        return exitCode == 0;
    }

    bool fetchUrlTextWithUndetectedChromedriver(const string& url, string& responseText)
    {
        responseText.clear();

        const auto now = chrono::steady_clock::now().time_since_epoch().count();
        string scriptPath;
        if (const char* tempDir = getenv("TEMP"))
        {
            scriptPath = string(tempDir) + "\\\\mggxpatc_fr24_uc_" + to_string(static_cast<long long>(now)) + ".py";
        }
        else
        {
            scriptPath = "mggxpatc_fr24_uc_" + to_string(static_cast<long long>(now)) + ".py";
        }

        const string escapedUrl = escapeForPythonTripleQuoted(url);
        const string script =
            "import html\n"
            "import re\n"
            "import sys\n"
            "\n"
            "url = '''" + escapedUrl + "'''\n"
            "\n"
            "try:\n"
            "    import undetected_chromedriver as uc\n"
            "    from selenium.webdriver.chrome.options import Options\n"
            "except Exception as e:\n"
            "    sys.stderr.write('IMPORT_ERROR:' + str(e) + '\\n')\n"
            "    sys.exit(2)\n"
            "\n"
            "driver = None\n"
            "try:\n"
            "    options = Options()\n"
            "    options.add_argument('--headless=new')\n"
            "    options.add_argument('--disable-gpu')\n"
            "    options.add_argument('--no-sandbox')\n"
            "    options.add_argument('--disable-dev-shm-usage')\n"
            "    options.add_argument('--window-size=1200,800')\n"
            "    options.add_argument('--disable-blink-features=AutomationControlled')\n"
            "\n"
            "    driver = uc.Chrome(options=options, use_subprocess=True)\n"
            "    driver.set_page_load_timeout(45)\n"
            "    driver.get(url)\n"
            "\n"
            "    source = driver.page_source or ''\n"
            "    m = re.search(r'<pre[^>]*>(.*?)</pre>', source, re.IGNORECASE | re.DOTALL)\n"
            "    if m:\n"
            "        text = html.unescape(m.group(1)).strip()\n"
            "    else:\n"
            "        text = source.strip()\n"
            "\n"
            "    if not text:\n"
            "        sys.stderr.write('EMPTY_RESPONSE\\n')\n"
            "        sys.exit(3)\n"
            "\n"
            "    sys.stdout.write(text)\n"
            "except Exception as e:\n"
            "    sys.stderr.write('FETCH_ERROR:' + str(e) + '\\n')\n"
            "    sys.exit(1)\n"
            "finally:\n"
            "    try:\n"
            "        if driver:\n"
            "            driver.quit()\n"
            "    except Exception:\n"
            "        pass\n";

        {
            ofstream scriptFile(scriptPath, ios::out | ios::trunc | ios::binary);
            if (!scriptFile)
            {
                return false;
            }
            scriptFile << script;
        }

        string output;
        bool ok = runPythonScriptAndCapture(scriptPath, "python", output);
        if (!ok)
        {
            ok = runPythonScriptAndCapture(scriptPath, "python3", output);
        }

        remove(scriptPath.c_str());

        if (!ok)
        {
            return false;
        }

        responseText = output;
        return !responseText.empty();
    }

    static bool extractJsonPayload(string& text)
    {
        if (text.empty())
        {
            return false;
        }

        const auto firstCurly = text.find('{');
        const auto lastCurly = text.rfind('}');
        if (firstCurly == string::npos || lastCurly == string::npos || lastCurly <= firstCurly)
        {
            return false;
        }

        text = text.substr(firstCurly, lastCurly - firstCurly + 1);
        return !text.empty();
    }

    static string normalizeCode(string value)
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

    static const SimpleJson* tryGetPath(const SimpleJson& node, const vector<string>& path)
    {
        const SimpleJson* current = &node;
        for (const auto& key : path)
        {
            if (!current || !current->isObject())
            {
                return nullptr;
            }

            current = current->tryGet(key);
        }

        return current;
    }

    static string getStringPath(const SimpleJson& node, initializer_list<vector<string>> paths)
    {
        for (const auto& path : paths)
        {
            const SimpleJson* value = tryGetPath(node, path);
            if (value && !value->isNull())
            {
                const string text = value->asString();
                if (!text.empty())
                {
                    return text;
                }
            }
        }

        return "";
    }

    static long long getIntegerPath(const SimpleJson& node, initializer_list<vector<string>> paths)
    {
        for (const auto& path : paths)
        {
            const SimpleJson* value = tryGetPath(node, path);
            if (value && !value->isNull())
            {
                const long long integerValue = value->asInteger(0);
                if (integerValue != 0)
                {
                    return integerValue;
                }
            }
        }

        return 0;
    }

    static string firstNonEmpty(const string& a, const string& b)
    {
        return !a.empty() ? a : b;
    }

    static string stripLeadingFlightDesignator(const string& value)
    {
        size_t index = 0;
        while (index < value.size() && !isdigit(static_cast<unsigned char>(value.at(index))))
        {
            ++index;
        }

        return index < value.size()
            ? value.substr(index)
            : value;
    }

    static string extractFlightNumber(const SimpleJson& node)
    {
        string flightNumber = getStringPath(node, {
            { "flight", "identification", "number", "default" },
            { "flight", "identification", "number", "alternative" },
            { "identification", "number", "default" },
            { "identification", "number", "alternative" }
        });

        flightNumber = normalizeCode(flightNumber);
        if (!flightNumber.empty())
        {
            return stripLeadingFlightDesignator(flightNumber);
        }

        string callsign = getStringPath(node, {
            { "flight", "identification", "callsign" },
            { "identification", "callsign" },
            { "flight", "callsign" }
        });

        callsign = normalizeCode(callsign);
        if (!callsign.empty())
        {
            return stripLeadingFlightDesignator(callsign);
        }

        return "";
    }

    static string extractAirlineIcao(const SimpleJson& node, const string& flightNumber)
    {
        string airlineIcao = getStringPath(node, {
            { "flight", "airline", "code", "icao" },
            { "flight", "airline", "icao" },
            { "flight", "airline", "code" },
            { "flight", "owner", "code", "icao" },
            { "flight", "owner", "icao" },
            { "airline", "code", "icao" },
            { "airline", "icao" },
            { "airline", "code" },
            { "owner", "code", "icao" },
            { "owner", "icao" }
        });

        airlineIcao = normalizeCode(airlineIcao);
        if (airlineIcao.length() == 3)
        {
            AirlineReferenceTable::Entry airline;
            if (AirlineReferenceTable::tryFindByIcao(airlineIcao, airline))
            {
                return airlineIcao;
            }
        }

        if (!flightNumber.empty())
        {
            AirlineReferenceTable::Entry airline;
            string flightCallsign;
            if (AirlineReferenceTable::tryFindByFlightNumber(flightNumber, airline, flightCallsign))
            {
                return airline.icao;
            }
        }

        return "";
    }

    static string extractAircraftIcao(const SimpleJson& node)
    {
        string aircraftIcao = getStringPath(node, {
            { "flight", "aircraft", "model", "code" },
            { "flight", "aircraft", "model", "icao" },
            { "flight", "aircraft", "code", "icao" },
            { "flight", "aircraft", "icao" },
            { "flight", "aircraft", "type", "icao" },
            { "flight", "aircraft", "type" },
            { "aircraft", "model", "code" },
            { "aircraft", "model", "icao" },
            { "aircraft", "code", "icao" },
            { "aircraft", "icao" },
            { "aircraft", "type", "icao" },
            { "aircraft", "type" }
        });

        aircraftIcao = normalizeCode(aircraftIcao);
        if (aircraftIcao.empty())
        {
            return "";
        }

        AircraftTypeReferenceTable::Entry aircraft;
        if (AircraftTypeReferenceTable::tryFindByIcao(aircraftIcao, aircraft))
        {
            return aircraftIcao;
        }

        return aircraftIcao.length() == 4 ? aircraftIcao : "";
    }

    static string resolveCallsign(const string& airlineIcao, const string& flightNumber)
    {
        AirlineReferenceTable::Entry airline;
        if (!airlineIcao.empty() && AirlineReferenceTable::tryFindByIcao(airlineIcao, airline) && !airline.callsign.empty())
        {
            return flightNumber.empty() ? airline.callsign : airline.callsign + " " + flightNumber;
        }

        return !flightNumber.empty() ? flightNumber : airlineIcao;
    }

    static time_t extractDepartureScheduleTime(const SimpleJson& node)
    {
        const long long value = getIntegerPath(node, {
            { "flight", "time", "estimated", "departure" },
            { "flight", "time", "real", "departure" },
            { "flight", "time", "other", "etd" },
            { "flight", "time", "scheduled", "departure" },
            { "flight", "time", "departure" },
            { "time", "estimated", "departure" },
            { "time", "real", "departure" },
            { "time", "other", "etd" },
            { "time", "scheduled", "departure" },
            { "time", "departure" },
        });

        return value > 0 ? static_cast<time_t>(value) : 0;
    }

    static time_t extractArrivalScheduleTime(const SimpleJson& node)
    {
        const long long value = getIntegerPath(node, {
            { "flight", "time", "estimated", "arrival" },
            { "flight", "time", "other", "eta" },
            { "flight", "time", "real", "arrival" },
            { "flight", "time", "scheduled", "arrival" },
            { "flight", "time", "arrival" },
            { "time", "estimated", "arrival" },
            { "time", "other", "eta" },
            { "time", "real", "arrival" },
            { "time", "scheduled", "arrival" },
            { "time", "arrival" }
        });

        return value > 0 ? static_cast<time_t>(value) : 0;
    }

    static time_t extractScheduleTime(const SimpleJson& node, const string& mode)
    {
        return mode == "arrivals"
            ? extractArrivalScheduleTime(node)
            : extractDepartureScheduleTime(node);
    }

    static bool shouldSkipByStatus(const string& normalizedStatusText)
    {
        if (normalizedStatusText.empty())
        {
            return false;
        }

        // Landed/arrived/completed legs are stale for schedule-based spawning.
        if (normalizedStatusText.rfind("LANDED", 0) == 0 ||
            normalizedStatusText.rfind("ARRIVED", 0) == 0)
        {
            return true;
        }

        // Explicitly non-flyable statuses should be ignored as well.
        return normalizedStatusText.find("CANCEL") != string::npos ||
            normalizedStatusText.find("DIVERT") != string::npos;
    }

    void collectEntries(const SimpleJson& dataArray, const string& airportIcao, const string& mode, vector<Fr24ScheduleEntry>& entries)
    {
        unordered_set<string> seen;
        const bool isDepartureMode = (mode == "departures");
        const string normalizedAirportIcao = normalizeCode(airportIcao);

        if (!dataArray.isArray())
        {
            return;
        }

        for (const auto& item : dataArray.arrayValue())
        {
            if (!item.isObject())
            {
                continue;
            }

            const string statusText = normalizeCode(getStringPath(item, {
                { "flight", "status", "generic", "status", "text" },
                { "flight", "status", "text" },
                { "status", "generic", "status", "text" },
                { "status", "text" }
            }));
            if (shouldSkipByStatus(statusText))
            {
                continue;
            }

            const string flightNumber = extractFlightNumber(item);
            if (flightNumber.empty())
            {
                continue;
            }

            string originIcao = normalizeCode(getStringPath(item, {
                { "flight", "airport", "origin", "code", "icao" },
                { "flight", "airport", "origin", "icao" },
                { "flight", "origin", "code", "icao" },
                { "flight", "origin", "icao" },
                { "airport", "origin", "code", "icao" },
                { "airport", "origin", "icao" },
                { "origin", "code", "icao" },
                { "origin", "icao" }
            }));

            string destinationIcao = normalizeCode(getStringPath(item, {
                { "flight", "airport", "destination", "code", "icao" },
                { "flight", "airport", "destination", "icao" },
                { "flight", "destination", "code", "icao" },
                { "flight", "destination", "icao" },
                { "airport", "destination", "code", "icao" },
                { "airport", "destination", "icao" },
                { "destination", "code", "icao" },
                { "destination", "icao" }
            }));

            if (isDepartureMode)
            {
                if (originIcao.empty())
                {
                    originIcao = normalizedAirportIcao;
                }
            }
            else
            {
                if (destinationIcao.empty())
                {
                    destinationIcao = normalizedAirportIcao;
                }
            }

            if (originIcao.empty() && destinationIcao.empty())
            {
                continue;
            }

            const string airlineIcao = extractAirlineIcao(item, flightNumber);
            const string aircraftIcao = extractAircraftIcao(item);
            const string callsign = resolveCallsign(airlineIcao, flightNumber);
            const time_t scheduleTime = extractScheduleTime(item, mode);

            const string dedupeKey =
                mode + "|" + flightNumber + "|" + originIcao + "|" + destinationIcao + "|" +
                airlineIcao + "|" + aircraftIcao + "|" + callsign + "|" + to_string(static_cast<long long>(scheduleTime));
            if (!seen.insert(dedupeKey).second)
            {
                continue;
            }

            Fr24ScheduleEntry entry;
            entry.airlineIcao = airlineIcao;
            entry.flightNumber = flightNumber;
            entry.callsign = callsign;
            entry.aircraftIcao = aircraftIcao;
            entry.originIcao = originIcao;
            entry.destinationIcao = destinationIcao;
            entry.scheduledTime = scheduleTime;
            entries.push_back(entry);
        }
    }
};