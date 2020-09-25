// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <iostream>
#include "stlhelpers.h"
#include "libworld.h"
#include "libdataxp.h"

using namespace world;
using namespace std;

static const unordered_map<string, ParkingStand::Type> parkingStandTypeLookup = {
    {"gate", ParkingStand::Type::Gate},
    {"hangar", ParkingStand::Type::Hangar},
    {"tie_down", ParkingStand::Type::Remote},
};

static const unordered_map<string, Aircraft::Category> aircraftCategoryLookup = {
    {"heavy", Aircraft::Category::Heavy},
    {"jets", Aircraft::Category::Jet},
    {"turboprops", Aircraft::Category::Turboprop},
    {"props", Aircraft::Category::Prop},
    {"helos", Aircraft::Category::Helicopter},
    {"all", Aircraft::Category::All},
};

static const unordered_map<string, Aircraft::OperationType> aircraftOperationTypeLookup = {
    {"none", Aircraft::OperationType::None},
    {"general_aviation", Aircraft::OperationType::GA},
    {"airline", Aircraft::OperationType::Airline},
    {"cargo", Aircraft::OperationType::Cargo},
    {"military", Aircraft::OperationType::Military},
};

static void parseSeparatedList(
    const string& listText, 
    const string& delimiters, 
    function<void(const string& item)> parseItem)
{
    int lastDelimiterIndex = -1;
    
    for (int i = 0 ; i < listText.length(); i++)
    {
        char c = listText[i];
        bool isDelimiter = (delimiters.find(c) != string::npos);
        if (isDelimiter)
        {
            if (i > lastDelimiterIndex + 1)
            {
                string itemText = listText.substr(lastDelimiterIndex + 1, i - lastDelimiterIndex - 1);
                parseItem(itemText);
            }
            lastDelimiterIndex = i;
        }
    }

    if (lastDelimiterIndex < (int)listText.length() - 1)
    {
        string itemText = listText.substr(lastDelimiterIndex + 1, listText.length() - lastDelimiterIndex - 1);
        parseItem(itemText);
    }
}

XPAirportReader::XPAirportReader(const shared_ptr<HostServices> _host) :
    m_host(_host), 
    m_unparsedLineCode(-1),
    m_nextEdgeId(1001),
    m_nextParkingStandId(301),
    m_datumLatitude(0),
    m_datumLongitude(0),
    m_elevation(0)
{
}

void XPAirportReader::readAptDat(istream& input)
{
    readAptDatInContext(input, [&](int lineCode) {
        rootContextParser(lineCode, input);
        return true;
    });
}

void XPAirportReader::setAirspace(shared_ptr<ControlledAirspace> airspace)
{
    m_airspace = airspace;
}

bool XPAirportReader::validate(vector<string>& diagnostics)
{
    return true;
}

shared_ptr<Airport> XPAirportReader::getAirport()
{
    Airport::Header header(m_icao, m_name, GeoPoint(m_datumLatitude, m_datumLongitude), m_elevation);
    
    shared_ptr<ControlFacility> tower = m_airspace
        ? WorldBuilder::assembleAirportTower(m_host, header, m_airspace, m_controllerPositions)
        : nullptr;

    auto airport = WorldBuilder::assembleAirport(
        header,
        m_runways, 
        m_parkingStands, 
        m_taxiNodes, 
        m_taxiEdges,
        tower, 
        m_airspace);

    return airport;
}

void XPAirportReader::readAptDatInContext(istream& input, ContextualParser parser)
{   
    const auto extractNextLineCode = [&]() {
        while (!input.eof() && input.peek() >= 0)
        {
            string firstToken = readFirstToken(input);
            if (firstToken.length() == 0 || firstToken[0] < '0' || firstToken[0] > '9')
            {
                getline(input, firstToken);
                continue;
            }
            return stoi(firstToken);
        }
        return -1;
    };

    while (true)
    {
        int lineCode = m_unparsedLineCode >= 0 
            ? m_unparsedLineCode 
            : extractNextLineCode();

        if (lineCode < 0)
        {
            break;
        }

        m_unparsedLineCode = -1;
        bool accepted = parser(lineCode);
        if (!accepted)
        {
            m_unparsedLineCode = lineCode;
            break;
        }
    }
}

bool XPAirportReader::rootContextParser(int lineCode, istream& input)
{
    switch (lineCode)
    {
    case 1:
        parseHeader1(input);
        break;
    case 100:
        parseRunway100(input);
        break;
    case 1201:
        parseTaxiNode1201(input);
        break;
    case 1202:
        parseTaxiEdge1202(input);
        break;
    case 1206:
        parseGroundEdge1206(input);
        break;
    case 1300:
        parseStartupLocation1300(input);
        break;
    case 1302:
        parseMetadata1302(input);
        break;
    default:
        if (isControlFrequencyLine(lineCode))
        {
            parseControlFrequency(lineCode, input);
        }
        else
        {
            skipToNextLine(input);
        }
        break;
    }

    return true;
}

void XPAirportReader::parseHeader1(istream &input)
{
    int deprecated;
    input >> m_elevation >> deprecated >> deprecated >> m_icao;
    m_name = readToEndOfLine(input);
}

void XPAirportReader::parseRunway100(istream& input)
{
    const auto parseEnd = [this,&input](){
        string name;
        float displasedThresholdMeters;
        float overrunAreaMeters;
        GeoPoint centerlinePoint = {0,0,0};
        int unusedInt;

        input >> name >> centerlinePoint.latitude >> centerlinePoint.longitude;
        input >> displasedThresholdMeters >> overrunAreaMeters;
        input >> unusedInt >> unusedInt >> unusedInt >> unusedInt;

        
        return Runway::End(
            name, 
            displasedThresholdMeters, 
            overrunAreaMeters, 
            UniPoint(m_host, centerlinePoint));
    };

    float widthMeters;
    int unusedInt;
    float unusedFloat;
    input >> widthMeters >> unusedInt >> unusedInt;
    input >> unusedFloat >> unusedInt >> unusedInt >> unusedInt;
    
    auto end1 = parseEnd();
    auto end2 = parseEnd();
    auto runway = make_shared<Runway>(end1, end2, widthMeters);
    
    m_runways.push_back(runway);
}

void XPAirportReader::parseTaxiNode1201(istream& input)
{
    double latitude;
    double longitude;
    string usage;
    int id;
    string name;

    input.precision(11);
    input >> latitude >> longitude >> usage >> id;
    name = readToEndOfLine(input);

    UniPoint location(m_host, GeoPoint({latitude, longitude, 0}));
    auto node = make_shared<TaxiNode>(id, location);
    
    m_taxiNodes.push_back(node);
    m_taxiNodeById.insert({ id, node });
}

void XPAirportReader::parseTaxiEdge1202(istream& input)
{
    int nodeId1;
    int nodeId2;
    string direction;
    string typeString;
    string name;

    input >> nodeId1 >> nodeId2 >> direction >> typeString;
    name = readToEndOfLine(input);

    bool isOneWay = (direction.compare("oneway") == 0);
    TaxiEdge::Type type = (stringStartsWith(typeString, "runway")
        ? TaxiEdge::Type::Runway 
        : TaxiEdge::Type::Taxiway);

    int edgeId = m_nextEdgeId++;
    auto edge = shared_ptr<TaxiEdge>(new TaxiEdge(
        edgeId, 
        name, 
        nodeId1, 
        nodeId2, 
        type, 
        {},
        isOneWay));
    
    m_taxiEdges.push_back(edge);

    readAptDatInContext(input, [this, &input, edge](int lineCode) {
        if (lineCode == 1204)
        {
            parseRunwayActiveZone1204(input, edge);
            return true;
        }
        return false;
    });
}

void XPAirportReader::parseGroundEdge1206(istream &input)
{
    int nodeId1;
    int nodeId2;
    string direction;
    string name;

    input >> nodeId1 >> nodeId2 >> direction;
    name = readToEndOfLine(input);

    bool isOneWay = (direction.compare("oneway") == 0);
    int edgeId = m_nextEdgeId++;

    auto edge = shared_ptr<TaxiEdge>(new TaxiEdge(
        edgeId, 
        name, 
        nodeId1, 
        nodeId2, 
        TaxiEdge::Type::Groundway, 
        {},
        isOneWay));
    
    m_taxiEdges.push_back(edge);
}

void XPAirportReader::parseRunwayActiveZone1204(istream& input, shared_ptr<TaxiEdge> edge)
{
    string classification;
    string runwayIdList;

    input >> classification >> runwayIdList;
    
    bool isDeparture = (classification.compare("departure") == 0);
    bool isArrival = (classification.compare("arrival") == 0);
    bool isIls = (classification.compare("ils") == 0);

    int lastCommaIndex = -1;

    for (int i = 0 ; i < runwayIdList.length(); i++)
    {
        if (runwayIdList[i] == ',')
        {
            string runwayId = runwayIdList.substr(lastCommaIndex + 1, i - lastCommaIndex - 1);
            WorldBuilder::addActiveZone(edge,  runwayId, isDeparture, isArrival, isIls);
            lastCommaIndex = i;
        }
    }
}

void XPAirportReader::parseStartupLocation1300(istream &input)
{
    double latitude;
    double longitude;
    float heading;
    string typeText;
    string categoriesText;
    string operationTypesText;
    string name;
    string widthCode;
    string airlinesText;

    input >> latitude >> longitude >> heading >> typeText >> categoriesText;
    name = readToEndOfLine(input);

    readAptDatInContext(input, [&](int lineCode){
        if (lineCode == 1301)
        {
            input >> widthCode >> operationTypesText;
            airlinesText = readToEndOfLine(input);
            return true;
        }
        return false;
    });

    UniPoint location = UniPoint::fromGeo(m_host, latitude, longitude, 0);
    ParkingStand::Type type = getValueOrThrow(parkingStandTypeLookup, typeText);
    Aircraft::Category categories = Aircraft::Category::None;
    Aircraft::OperationType operationTypes = Aircraft::OperationType::None;
    vector<string> airlines;

    parseSeparatedList(airlinesText, ",;:| \t", [&airlines](const string& item) {
        airlines.push_back(item);
    });
    parseSeparatedList(categoriesText, ",;:| \t", [&categories](const string& item) {
        categories = categories | getValueOrThrow(aircraftCategoryLookup, item);
    });
    parseSeparatedList(operationTypesText, ",;:| \t", [&operationTypes](const string& item) {
        operationTypes = operationTypes | getValueOrThrow(aircraftOperationTypeLookup, item);
    });

    auto parkingStand = shared_ptr<ParkingStand>(new ParkingStand(
        m_nextParkingStandId++, name, type, location, heading, widthCode, categories, operationTypes, airlines));

    m_parkingStands.push_back(parkingStand);
}

void XPAirportReader::parseMetadata1302(istream &input)
{
    string fieldName;
    input >> fieldName;

    if (fieldName.compare("datum_lat") == 0)
    {
        input >> m_datumLatitude;
    }
    else if (fieldName.compare("datum_lon") == 0)
    {
        input >> m_datumLongitude;
    }
    else if (fieldName.compare("icao_code") == 0)
    {
        input >> m_icao;
    }
    else
    {
        readToEndOfLine(input);
    }
}

bool XPAirportReader::isControlFrequencyLine(int lineCode)
{
    return ((lineCode >= 50 && lineCode <= 56) || (lineCode >= 1050 && lineCode <= 1056));
}

void XPAirportReader::parseControlFrequency(int lineCode, istream &input)
{
    if (hasKey(m_parsedFrequencyLineCodes, lineCode))
    {
        return;
    }

    const auto getPositionType = [lineCode]() {
        switch (lineCode)
        {
            case 52:
            case 1052:
                return ControllerPosition::Type::ClearanceDelivery;
            case 53:
            case 1053:
                return ControllerPosition::Type::Ground;
            case 54:
            case 1054:
                return ControllerPosition::Type::Local;
            case 55:
            case 1055:
                return ControllerPosition::Type::Approach;
            case 56:
            case 1056:
                return ControllerPosition::Type::Departure;
            default:
                return ControllerPosition::Type::Unknown;
        }
    };

    const auto positionType = getPositionType();
    if (positionType != ControllerPosition::Type::Unknown)
    {
        int khz;
        input >> khz; 
        
        if (tryInsertKey(m_parsedFrequencyKhz, khz))
        {
            ControllerPosition::Structure position = { positionType, khz, GeoPolygon::empty() };
            m_controllerPositions.push_back(position);
            m_parsedFrequencyLineCodes.insert(lineCode);
        }
    }
}

string XPAirportReader::readFirstToken(istream& input)
{
    bool isAtLeadingSpace = true;

    string s;
    s.reserve(16);
    
    while (!input.eof() && input.peek() > -1)
    {
        char c = input.peek();
        bool isAtWhitespace = (c <= 0x20);
        bool isAtEndOfLine = (c == '\n' || c == '\r');
    
        if (isAtEndOfLine)
        {
            break;
        }
        
        if (isAtLeadingSpace)
        {
            if (isAtWhitespace)
            {
                input.get();
            }
            else 
            {
                isAtLeadingSpace = false;
            }
        }
        else        
        {
            if (!isAtWhitespace)
            {
                s.push_back(input.get());
            }
            else 
            {
                break;
            }
        }
    }

    return s;
}

string XPAirportReader::readToEndOfLine(istream& input)
{
    const int stateLeadingSpace = 0;
    const int stateContents = 1;
    const int stateMaybeTrailingSpace = 2;
    const int stateEndOfLine = 3;
    const int stateStop = 4;

    int state = stateLeadingSpace;
    string s;
    s.reserve(16);

    while (!input.eof() && input.peek() > -1 && state != stateStop)
    {
        char c = input.peek();
        bool isWhitespace = (c <= 0x20);
        bool isEndOfLine = (c == '\n' || c == '\r');
        if (isEndOfLine)
        {
            state = stateEndOfLine;
        }

        switch (state)
        {
        case stateLeadingSpace:
            if (isWhitespace)
            {
                input.get();
            }
            else 
            {
                state = stateContents;
            }
            break;
        case stateContents:
            if (!isWhitespace)
            {
                s.push_back(input.get());
            }
            else 
            {
                state = stateMaybeTrailingSpace;
            }
            break;
        case stateMaybeTrailingSpace:
            if (isWhitespace)
            {
                input.get();
            }
            else 
            {
                s.push_back(' ');
                state = stateContents;
            }
            break;
        case stateEndOfLine:
            if (isEndOfLine)
            {
                input.get();
            }
            else 
            {
                state = stateStop;
            }
            break;
        }
    }

    return s;
}

void XPAirportReader::skipToNextLine(istream& input)
{
    bool atEndOfLine = false;

    while (!input.eof() && input.peek() > -1)
    {
        char c = input.peek();
        bool isEolChar = (c == '\r' || c == '\n');

        if (atEndOfLine && !isEolChar)
        {
            break;
        }

        input.get();

        if (isEolChar)
        {
            atEndOfLine = true;
        }
    }
}

