//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <memory>
#include <system_error>
#include "stlhelpers.h"
#include "libworld.h"
#include "libdataxp.h"

using namespace std;
using namespace world;

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
    for (int i = 0 ; i < lines.size() ; i++)
    {
        const Line& line = lines.at(i);

        if (line.token != "NUMENR")
        {
            addValue(plan, line.token, line.suffix);
        }
        else
        {
            break;
        }
    }
}

void XPFmsxReader::parseFmxFormat(shared_ptr<FlightPlan> plan, const vector<Line> &lines)
{
    bool isEnrouteSection = true;

    for (int i = 0 ; i < lines.size() ; i++)
    {
        const Line& line = lines.at(i);

        if (isEnrouteSection)
        {
            bool continueEnrouteSection = (countCharOccurrences(line.suffix, ',') == 3);
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
