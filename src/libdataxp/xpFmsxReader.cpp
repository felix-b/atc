//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <iostream>
#include <utility>
#include <system_error>
#include <cctype>
#include <cstdlib>
#include "stlhelpers.h"
#include "libworld.h"
#include "libdataxp.h"

using namespace std;
using namespace world;

namespace
{
    string trimCopy(const string& value)
    {
        const auto begin = value.find_first_not_of(" \t\r\n");
        if (begin == string::npos)
        {
            return "";
        }

        const auto end = value.find_last_not_of(" \t\r\n");
        return value.substr(begin, end - begin + 1);
    }

    vector<string> splitWhitespaceCopy(const string& value)
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

    vector<string> splitCommaCopy(const string& value)
    {
        vector<string> result;
        string current;
        for (char ch : value)
        {
            if (ch == ',')
            {
                result.push_back(trimCopy(current));
                current.clear();
            }
            else if (ch != '\r' && ch != '\n')
            {
                current.push_back(ch);
            }
        }

        result.push_back(trimCopy(current));
        return result;
    }

    bool tryParseDouble(const string& value, double& parsedValue)
    {
        if (value.empty())
        {
            return false;
        }

        char* endPtr = nullptr;
        parsedValue = strtod(value.c_str(), &endPtr);
        return endPtr && *endPtr == '\0';
    }

    string normalizeWaypointKey(string value)
    {
        value.erase(remove_if(value.begin(), value.end(), [](unsigned char c) {
            return !isalnum(c);
        }), value.end());

        transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
            return static_cast<char>(toupper(c));
        });
        return value;
    }

    bool appendUniqueRouteWaypoint(
        vector<FlightPlan::RouteWaypoint>& waypoints,
        const string& name,
        const string& airway,
        const GeoPoint& parsedLocation,
        XPNavaidReader& navaidReader,
        const string& contextIcao)
    {
        const string trimmedName = trimCopy(name);
        if (trimmedName.empty())
        {
            return false;
        }

        if (!waypoints.empty() && normalizeWaypointKey(waypoints.back().name) == normalizeWaypointKey(trimmedName))
        {
            return false;
        }

        GeoPoint location = parsedLocation;
        if (location == GeoPoint::empty)
        {
            navaidReader.tryResolveWaypoint(contextIcao, trimmedName, location);
        }

        waypoints.push_back(FlightPlan::RouteWaypoint(trimmedName, trimCopy(airway), location));
        return true;
    }

    bool tryParseLocationFromTailFields(const vector<string>& fields, GeoPoint& location)
    {
        if (fields.size() < 2)
        {
            return false;
        }

        double latitude = 0.0;
        double longitude = 0.0;
        if (!tryParseDouble(fields.at(fields.size() - 2), latitude) || !tryParseDouble(fields.back(), longitude))
        {
            return false;
        }

        location = GeoPoint(latitude, longitude);
        return true;
    }
}

XPFmsxReader::XPFmsxReader(shared_ptr<HostServices> _host) :
    m_host(_host)
{
}

shared_ptr<FlightPlan> XPFmsxReader::readFrom(istream &input)
{
    time_t departureTime = m_host->getWorld()->currentTime() + 45 * 60;
    time_t arrivalTime = departureTime + 180 * 60;
    auto plan = shared_ptr<FlightPlan>(new FlightPlan(departureTime, arrivalTime, "", ""));

    vector<Line> lines;
    parseInputLines(input, lines);

    if (isFmsFormat(lines))
    {
        parseFmsFormat(plan, lines);
    }
    else if (isFmxFormat(lines))
    {
        parseFmxFormat(plan, lines);
    }
    else
    {
        throw runtime_error("Flight plan file format not recognized");
    }

    return plan;
}

void XPFmsxReader::parseInputLines(istream &input, vector<Line> &lines)
{
    while (!input.eof() && !input.bad())
    {
        string text;

        try
        {
            getline(input, text);
        }
        catch(const exception &e)
        {
            break;
        }

        size_t delimitierIndex = text.find_first_of(",: ");
        if (delimitierIndex != text.npos)
        {
            string token = text.substr(0, delimitierIndex);
            string suffix = text.substr(delimitierIndex + 1);

            if (!token.empty())
            {
                lines.push_back({ token, suffix, text.at(delimitierIndex) });
            }
        }
    }
}

bool XPFmsxReader::isFmsFormat(const vector<Line> &lines)
{
    auto v11it = find_if(lines.begin(), lines.end(), [](const Line& line){
        return (line.token == "1100" && line.suffix == "Version");
    });
    bool foundV11 = (v11it != lines.end());
    return foundV11;
}

bool XPFmsxReader::isFmxFormat(const vector<Line> &lines)
{
    if (lines.empty())
    {
        return false;
    }

    const string& firstLineSuffix = lines.at(0).suffix;
    int commaCount = countCharOccurrences(firstLineSuffix, ',');
    return commaCount == 3;
}

void XPFmsxReader::parseFmsFormat(shared_ptr<FlightPlan> plan, const vector<Line> &lines)
{
    XPNavaidReader navaidReader(m_host);
    vector<FlightPlan::RouteWaypoint> routeWaypoints;
    bool inRouteSection = false;

    for (int i = 0 ; i < lines.size() ; i++)
    {
        const Line& line = lines.at(i);

        if (!inRouteSection)
        {
            if (line.token == "NUMENR")
            {
                inRouteSection = true;
                continue;
            }

            addValue(plan, line.token, line.suffix);
            continue;
        }

        const vector<string> fields = splitWhitespaceCopy(line.suffix);
        if (fields.size() < 2)
        {
            continue;
        }

        const string name = trimCopy(fields.at(0));
        const string marker = fields.size() > 1 ? trimCopy(fields.at(1)) : "";

        if (marker == "ADEP")
        {
            if (plan->departureAirportIcao().empty())
            {
                plan->setDepartureAirportIcao(name);
            }
            continue;
        }

        if (marker == "ADES")
        {
            if (plan->arrivalAirportIcao().empty())
            {
                plan->setArrivalAirportIcao(name);
            }
            continue;
        }

        GeoPoint location = GeoPoint::empty;
        if (!tryParseLocationFromTailFields(fields, location))
        {
            navaidReader.tryResolveWaypoint(plan->departureAirportIcao(), name, location);
        }

        const string airway = fields.size() > 2 ? trimCopy(fields.at(2)) : "";
        appendUniqueRouteWaypoint(routeWaypoints, name, airway, location, navaidReader, plan->departureAirportIcao());
    }

    plan->setRouteWaypoints(routeWaypoints);
}

void XPFmsxReader::parseFmxFormat(shared_ptr<FlightPlan> plan, const vector<Line> &lines)
{
    XPNavaidReader navaidReader(m_host);
    vector<FlightPlan::RouteWaypoint> routeWaypoints;
    bool isEnrouteSection = true;

    for (int i = 0 ; i < lines.size() ; i++)
    {
        const Line& line = lines.at(i);

        if (isEnrouteSection)
        {
            bool continueEnrouteSection = (countCharOccurrences(line.suffix, ',') == 3);

            if (continueEnrouteSection)
            {
                const vector<string> fields = splitCommaCopy(line.suffix);
                const string name = trimCopy(line.token);
                const string airway = fields.size() > 0 ? trimCopy(fields.at(0)) : "";
                const string routeType = fields.size() > 1 ? trimCopy(fields.at(1)) : "";

                GeoPoint location = GeoPoint::empty;
                if (!tryParseLocationFromTailFields(fields, location))
                {
                    navaidReader.tryResolveWaypoint(plan->departureAirportIcao(), name, location);
                }

                if (!name.empty() && (!airway.empty() || !routeType.empty()))
                {
                    appendUniqueRouteWaypoint(routeWaypoints, name, airway, location, navaidReader, plan->departureAirportIcao());
                }

                if (plan->departureAirportIcao().empty() && !name.empty() && airway.empty() && routeType.empty())
                {
                    plan->setDepartureAirportIcao(name);
                }
            }

            if (!continueEnrouteSection && plan->arrivalAirportIcao().empty() && i > 0)
            {
                plan->setArrivalAirportIcao(lines.at(i - 1).token);
            }
            isEnrouteSection = continueEnrouteSection;
        }

        if (isEnrouteSection && plan->departureAirportIcao().empty())
        {
            plan->setDepartureAirportIcao(line.token);
        }

        if (!isEnrouteSection)
        {
            addValue(plan, line.token, line.suffix);
        }
    }

    plan->setRouteWaypoints(routeWaypoints);
}

void XPFmsxReader::addValue(shared_ptr<FlightPlan> plan, const string &key, const string &value)
{
    if (key == "ADEP")
    {
        plan->setDepartureAirportIcao(value);
    }
    else if (key == "ADES")
    {
        plan->setArrivalAirportIcao(value);
    }
    else if (key == "DEPRWY")
    {
        plan->setDepartureRunway(trimLead(value, "RW"));
    }
    else if (key == "DESRWY")
    {
        plan->setArrivalRunway(trimLead(value, "RW"));
    }
    else if (key == "SID")
    {
        plan->setSid(value);
    }
    else if (key == "SIDTRANS")
    {
        plan->setSidTransition(value);
    }
    else if (key == "STAR")
    {
        plan->setStar(value);
    }
    else if (key == "STARTRANS")
    {
        plan->setStarTransition(value);
    }
    else if (key == "APP")
    {
        plan->setApproach(value);
        if (plan->arrivalRunway().empty())
        {
            plan->setArrivalRunway(getRunwayFromApproachName(value));
        }
    }
    else if (key == "FLIGHT_NUM")
    {
        plan->setFlightNo(value);
    }
}

int XPFmsxReader::countCharOccurrences(const string& s, char c)
{
    return count_if(s.begin(), s.end(), [c](char ci){
        return (ci == c);
    });
}

string XPFmsxReader::trimLead(const string &s, const string& prefix)
{
    size_t pos = s.find(prefix);
    if (pos == 0)
    {
        string copy = s;
        copy.erase(0, prefix.length());
        return copy;
    }
    return s;
}

string XPFmsxReader::getRunwayFromApproachName(const string& approachName)
{
    string runwayName;
    bool copiedAnyDigits = false;

    for (int i = 0 ; i < approachName.length() ; i++)
    {
        char c = approachName.at(i);

        if (isdigit(c))
        {
            runwayName += c;
            copiedAnyDigits = true;
        }
        else if (copiedAnyDigits)
        {
            if (c == 'L' || c == 'R' || c == 'C')
            {
                runwayName += c;
            }
            break;
        }
    }

    return runwayName;
}
