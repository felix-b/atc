// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <memory>
#include <iostream>
#include <utility>
#include "stlhelpers.h"
#include "libworld.h"
#include "libdataxp.h"

using namespace world;
using namespace std;

static const unordered_map<string, ParkingStand::Type> parkingStandTypeLookup = {
    {"gate", ParkingStand::Type::Gate},
    {"hangar", ParkingStand::Type::Hangar},
    {"tie_down", ParkingStand::Type::Remote},
    {"misc", ParkingStand::Type::Unknown},
};

static const unordered_map<string, Aircraft::Category> aircraftCategoryLookup = {
    {"heavy", Aircraft::Category::Heavy},
    {"jets", Aircraft::Category::Jet},
    {"turboprops", Aircraft::Category::Turboprop},
    {"props", Aircraft::Category::Prop},
    {"helos", Aircraft::Category::Helicopter},
    {"fighters", Aircraft::Category::Fighter},
    {"all", Aircraft::Category::All},
};

static const unordered_map<string, Aircraft::OperationType> aircraftOperationTypeLookup = {
    {"none", Aircraft::OperationType::None},
    {"general_aviation", Aircraft::OperationType::GA},
    {"airline", Aircraft::OperationType::Airline},
    {"cargo", Aircraft::OperationType::Cargo},
    {"military", Aircraft::OperationType::Military},
};

// Approximate width of a taxiway
static const unordered_map<string, int> taxiwayWidthHint = {
    {"taxiway_A", 10},
    {"taxiway_B", 20},
    {"taxiway_C", 30},
    {"taxiway_D", 40},
    {"taxiway_E", 50},
    {"taxiway_F", 60},
};

static constexpr int DATUM_UNSPECIFIED = -10000;

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

XPAirportReader::XPAirportReader(
    shared_ptr<HostServices> _host,
    int _unparsedLineCode,
    QueryAirspaceCallback _onQueryAirspace,
    FilterAirportCallback _onFilterAirport
) : m_host(std::move(_host)),
    m_onQueryAirspace(std::move(_onQueryAirspace)),
    m_onFilterAirport(std::move(_onFilterAirport)),
    m_unparsedLineCode(_unparsedLineCode),
    m_nextEdgeId(1001),
    m_nextParkingStandId(301),
    m_datumLatitude(DATUM_UNSPECIFIED),
    m_datumLongitude(DATUM_UNSPECIFIED),
    m_elevation(0),
    m_skippingAirport(false),
    m_isLandAirport(false),
    m_headerWasRead(false),
    m_filterWasQueried(false)
{
}

void XPAirportReader::readAirport(istream& input)
{
    readAptDatInContext(input, [&](int lineCode) {
        return rootContextParser(lineCode, input);
    });
}

bool XPAirportReader::validate(vector<string>& diagnostics)
{
    return true;
}

shared_ptr<Airport> XPAirportReader::getAirport()
{
    if (!m_skippingAirport)
    {
        try
        {
            return assembleAirportOrThrow();
        }
        catch (const exception &e)
        {
            m_host->writeLog("APTDAT|FAILED to assemble airport [%s]: %s", m_icao.c_str(), e.what());
        }
    }

    return nullptr;
}

shared_ptr<Airport> XPAirportReader::assembleAirportOrThrow()
{
    GeoPoint datum(
        m_datumLatitude != DATUM_UNSPECIFIED ? m_datumLatitude : 0,
        m_datumLongitude != DATUM_UNSPECIFIED ? m_datumLongitude : 0);

    Airport::Header header(m_icao, m_name, datum, m_elevation);
    m_airspace = m_onQueryAirspace(header);
    
    shared_ptr<ControlFacility> tower = m_airspace
        ? WorldBuilder::assembleAirportTower(m_host, header, m_airspace, m_controllerPositions)
        : nullptr;

    auto airport = WorldBuilder::assembleAirport(
        m_host,
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
    while (!input.eof() && !input.bad())
    {
        int saveLineCode = m_unparsedLineCode;
        streampos saveInputPosition = input.tellg();

        try
        {
            if (!readAptDatLineInContext(input, parser))
            {
                break;
            }
        }
        catch (const exception& e)
        {
            string errorMessage = formatErrorMessage(input, saveInputPosition, saveLineCode, e.what());
            m_host->writeLog("APTDAT|%s", errorMessage.c_str());
            //throw runtime_error(errorMessage);
            m_skippingAirport = true;
        }
    }
}

bool XPAirportReader::readAptDatLineInContext(istream &input, XPAirportReader::ContextualParser parser)
{
    int lineCode = m_unparsedLineCode >= 0
       ? m_unparsedLineCode
       : extractNextLineCode(input);

    if (lineCode < 0)
    {
        return false;
    }

    m_unparsedLineCode = -1;

    bool accepted = parser(lineCode);
    if (!accepted)
    {
        m_unparsedLineCode = lineCode;
        return false;
    }

    return true;
} 


bool XPAirportReader::rootContextParser(int lineCode, istream& input)
{
    bool isAirportHeaderLine = isAirportHeaderLineCode(lineCode);

    if (m_skippingAirport)
    {
        if (isAirportHeaderLine)
        {
            return false;
        }
        skipToNextLine(input);
        return true;
    }

    if (m_headerWasRead)
    {
        if (isAirportHeaderLine)
        {
            return false; // we're at the beginning of the next airport
        }
        if (lineCode != 1302 && !m_filterWasQueried)
        {
            m_skippingAirport = !invokeFilterCallback();
            m_filterWasQueried = true;
            if (m_skippingAirport)
            {
                m_host->writeLog("APTDAT|will skip airport [%s] according to filter", m_icao.c_str());
            }
        }
    }

    switch (lineCode)
    {
    case 1:
        parseHeader1(input);
        m_headerWasRead = true;
        m_isLandAirport = true;
        break;
    case 16:
    case 17:
        m_skippingAirport = true;
        skipToNextLine(input);
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
    case 15:
        parseStartupLocation15(input);
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

    Airport::Header header(m_icao, m_name, GeoPoint::empty, m_elevation);
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
    int widthHint = 0;
    string direction;
    string typeString;
    string name;

    input >> nodeId1 >> nodeId2 >> direction >> typeString;
    name = readToEndOfLine(input);

    bool isOneWay = (direction.compare("oneway") == 0);
    TaxiEdge::Type type = (stringStartsWith(typeString, "runway")
        ? TaxiEdge::Type::Runway 
        : TaxiEdge::Type::Taxiway);

    tryGetValue(taxiwayWidthHint, typeString, widthHint);
    
    int edgeId = m_nextEdgeId++;
    auto edge = shared_ptr<TaxiEdge>(new TaxiEdge(
        edgeId, 
        name, 
        nodeId1, 
        nodeId2, 
        type, 
        isOneWay,
        {},
        widthHint));
    
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
        isOneWay,
        {}));
    
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

void XPAirportReader::parseStartupLocation15(istream &input)
{
    double latitude;
    double longitude;
    float heading;
    string name;

    input >> latitude >> longitude >> heading;
    name = readToEndOfLine(input);

    auto parkingStand = shared_ptr<ParkingStand>(new ParkingStand(
        m_nextParkingStandId++,
        name,
        ParkingStand::Type::Unknown,
        UniPoint(GeoPoint(latitude, longitude)),
        heading,
        "F",
        Aircraft::Category::All,
        Aircraft::OperationType::All,
        {}));

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
        string callSign = readToEndOfLine(input);
        
        if (tryInsertKey(m_parsedFrequencyKhz, khz))
        {
            ControllerPosition::Structure position = { positionType, khz, GeoPolygon::empty(), callSign };
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

int XPAirportReader::extractNextLineCode(istream &input)
{
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
}

string XPAirportReader::formatErrorMessage(istream &input, const streampos& position, int extractedLineCode, const char *what)
{
    stringstream message;
    message << "FAILED to read apt.dat: airport[" << m_icao << "] error [" << what << "] line [";
    if (extractedLineCode >= 0)
    {
        message << "code[" << extractedLineCode << "] > ";
    }

    input.clear();
    input.seekg(position);
    string line;
    getline(input, line);
    message << line;
    message << ']';

    return message.str();
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

shared_ptr<ControlledAirspace> XPAirportReader::noopQueryAirspace(const Airport::Header& header)
{
    return nullptr;
}

bool XPAirportReader::noopFilterAirport(const Airport::Header &header)
{
    return true;
}

bool XPAirportReader::invokeFilterCallback()
{
    Airport::Header header(m_icao, m_name, GeoPoint(m_datumLatitude, m_datumLongitude), m_elevation);
    return m_onFilterAirport(header);
}

bool XPAirportReader::isAirportHeaderLineCode(int lineCode)
{
    return (lineCode == 1 || lineCode == 16 || lineCode == 17);
}

XPAptDatReader::XPAptDatReader(shared_ptr<HostServices> _host) :
    m_host(std::move(_host))
{
}

void XPAptDatReader::readAptDat(
    istream &input,
    const XPAirportReader::QueryAirspaceCallback& onQueryAirspace,
    const XPAirportReader::FilterAirportCallback& onFilterAirport,
    const XPAptDatReader::AirportLoadedCallback& onAirportLoaded)
{
    int loadedCount = 0;
    int skippedCount = 0;
    int unparsedLineCode = -1;
    string lastLoadedAirportIcao;

    do {
        XPAirportReader airportReader(m_host, unparsedLineCode, onQueryAirspace, onFilterAirport);
        airportReader.readAirport(input);
        unparsedLineCode = airportReader.unparsedLineCode();

        auto airport = airportReader.getAirport();
        if (airport)
        {
            //m_host->writeLog("Airport loaded: %s", airport->header().icao().c_str());
            lastLoadedAirportIcao = airport->header().icao();
            onAirportLoaded(airport);
            loadedCount++;
        }
        else if (airportReader.headerWasRead() && airportReader.isLandAirport())
        {
            m_host->writeLog("APTDAT|skipped airport [%s]", airportReader.icao().c_str());
            skippedCount++;
        }
    } while (XPAirportReader::isAirportHeaderLineCode(unparsedLineCode));

    //m_host->writeLog("APTDAT|done loading airports, %d loaded, %d skipped.", loadedCount, skippedCount);
}

XPSceneryAptDatReader::XPSceneryAptDatReader(shared_ptr<HostServices> _host) :
    m_host(_host)
{
}

void XPSceneryAptDatReader::readSceneryAirports(
    const XPAirportReader::QueryAirspaceCallback& onQueryAirspace,
    const XPAirportReader::FilterAirportCallback& onFilterAirport,
    const XPAptDatReader::AirportLoadedCallback& onAirportLoaded)
{
    vector<string> sceneryFolders;
    loadSceneryFolderList(sceneryFolders);

    for (const string& folder : sceneryFolders)
    {
        auto aptDatFile = tryOpenAptDat(folder);
        if (!aptDatFile)
        {
            continue;
        }

        int loadedCount = 0;

        XPAptDatReader aptDatReader(m_host);
        aptDatReader.readAptDat(
            *aptDatFile,
            onQueryAirspace,
            onFilterAirport,
            [&](shared_ptr<Airport> airport) {
                loadedCount++;
                onAirportLoaded(airport);
            }
        );

        m_host->writeLog("LSCNRY|Loaded [%d] airport(s) from [%s]", loadedCount, folder.c_str());
    }
}

void XPSceneryAptDatReader::loadSceneryFolderList(vector<string>& list)
{
    string sceneryPacksIniPath = m_host->getHostFilePath({
        "Custom Scenery", "scenery_packs.ini"
    });

    m_host->writeLog("LSCNRY|scenery_packs.ini file path [%s]", sceneryPacksIniPath.c_str());

    shared_ptr<istream> iniFile = m_host->openFileForRead(sceneryPacksIniPath);
    XPSceneryPacksIniReader iniReader(m_host);
    iniReader.readSceneryFolderList(*iniFile, list);
}

shared_ptr<istream> XPSceneryAptDatReader::tryOpenAptDat(const string& sceneryFolder)
{
    string aptDatFilePath = m_host->getHostFilePath({ sceneryFolder, "Earth nav data", "apt.dat" });
    if (m_host->checkFileExists(aptDatFilePath))
    {
        m_host->writeLog("LSCNRY|will load airports from [%s]", aptDatFilePath.c_str());
        return m_host->openFileForRead(aptDatFilePath);
    }

    return nullptr;
}
