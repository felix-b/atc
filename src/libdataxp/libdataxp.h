// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <sstream>
#include <memory>
#include <vector>
#include <unordered_map>
#include <unordered_set>
#include <functional>
#include "libworld.h"

using namespace std;
using namespace world;

class XPAirportReader
{
private:
    typedef function<bool(int lineCode)> ContextualParser;
public:
    typedef function<bool(const Airport::Header& header)> FilterAirportCallback;
    typedef function<shared_ptr<ControlledAirspace>(const Airport::Header& header)> QueryAirspaceCallback;
private:
    const shared_ptr<HostServices> m_host;
    const QueryAirspaceCallback m_onQueryAirspace;
    const FilterAirportCallback m_onFilterAirport;
    bool m_headerWasRead;
    bool m_filterWasQueried;
    bool m_isLandAirport;
    bool m_skippingAirport;
    int m_unparsedLineCode;
    int m_nextEdgeId;
    int m_nextParkingStandId;
    string m_icao;
    string m_name;
    float m_elevation;
    double m_datumLatitude;
    double m_datumLongitude;
    vector<shared_ptr<TaxiNode>> m_taxiNodes;
    vector<shared_ptr<TaxiEdge>> m_taxiEdges;
    vector<shared_ptr<Runway>> m_runways;
    vector<shared_ptr<ParkingStand>> m_parkingStands;
    unordered_map<int, shared_ptr<TaxiNode>> m_taxiNodeById;
    vector<ControllerPosition::Structure> m_controllerPositions;
    unordered_set<int> m_parsedFrequencyKhz;
    unordered_set<int> m_parsedFrequencyLineCodes;
    shared_ptr<ControlledAirspace> m_airspace;
public:
    explicit XPAirportReader(
        shared_ptr<HostServices> _host,
        int _unparsedLineCode = -1,
        QueryAirspaceCallback _onQueryAirspace = noopQueryAirspace,
        FilterAirportCallback _onFilterAirport = noopFilterAirport);
public:
    int unparsedLineCode() const { return m_unparsedLineCode; }
    bool headerWasRead() const { return m_headerWasRead; }
    bool isLandAirport() const { return m_isLandAirport; }
    const string& icao() const { return m_icao; }
public:
    void readAirport(istream &input);
    bool validate(vector<string> &diagnostics);
    shared_ptr<Airport> getAirport();
private:
    void readAptDatInContext(istream &input, ContextualParser parser);
    bool readAptDatLineInContext(istream &input, ContextualParser parser);
    bool rootContextParser(int lineCode, istream &input);
    void parseHeader1(istream &input);
    void parseRunway100(istream &input);
    void parseTaxiNode1201(istream &input);
    void parseTaxiEdge1202(istream &input);
    void parseGroundEdge1206(istream &input);
    void parseRunwayActiveZone1204(istream& input, shared_ptr<TaxiEdge> edge);
    void parseStartupLocation1300(istream &input);
    void parseStartupLocation15(istream &input);
    void parseMetadata1302(istream &input);
    void parseControlFrequency(int lineCode, istream &input);
    bool isControlFrequencyLine(int lineCode);
    bool invokeFilterCallback();
    shared_ptr<Airport> assembleAirportOrThrow();
    string formatErrorMessage(istream &input, const streampos& position, int extractedLineCode, const char *what);
public:
    static string readFirstToken(istream &input);
    static string readToEndOfLine(istream &input);
    static int extractNextLineCode(istream &input);
    static void skipToNextLine(istream &input);
    static shared_ptr<ControlledAirspace> noopQueryAirspace(const Airport::Header& header);
    static bool noopFilterAirport(const Airport::Header& header);
    static bool isAirportHeaderLineCode(int lineCode);
};

class XPAptDatReader
{
public:
    typedef function<void(shared_ptr<Airport> airport)> AirportLoadedCallback;
private:
    const shared_ptr<HostServices> m_host;
public:
    explicit XPAptDatReader(shared_ptr<HostServices> _host);
public:
    void readAptDat(
        istream &input,
        const XPAirportReader::QueryAirspaceCallback& onQueryAirspace,
        const XPAirportReader::FilterAirportCallback& onFilterAirport,
        const AirportLoadedCallback& onAirportLoaded);
};

class XPSceneryAptDatReader
{
private:
    const shared_ptr<HostServices> m_host;
public:
    explicit XPSceneryAptDatReader(shared_ptr<HostServices> _host);
public:
    void readSceneryAirports(
        const XPAirportReader::QueryAirspaceCallback& onQueryAirspace,
        const XPAirportReader::FilterAirportCallback& onFilterAirport,
        const XPAptDatReader::AirportLoadedCallback& onAirportLoaded);
private:
    void loadSceneryFolderList(vector<string>& list);
    shared_ptr<istream> tryOpenAptDat(const string& sceneryFolder);
};

class TokenValueFileReaderBase
{
protected:
    struct Line
    {
        string token;
        string suffix;
        char delimiter;
    };
protected:
    void parseInputLines(istream& input, vector<Line>& lines);
};

class XPFmsxReader : TokenValueFileReaderBase
{
private:
    shared_ptr<HostServices> m_host;
public:
    XPFmsxReader(shared_ptr<HostServices> _host);
public:
    shared_ptr<FlightPlan> readFrom(istream& input);
private:
    void addValue(shared_ptr<FlightPlan> plan, const string& key, const string& value);
    bool isFmsFormat(const vector<Line>& lines);
    bool isFmxFormat(const vector<Line>& lines);
    void parseFmsFormat(shared_ptr<FlightPlan> plan, const vector<Line>& lines);
    void parseFmxFormat(shared_ptr<FlightPlan> plan, const vector<Line>& lines);
private:
    static int countCharOccurrences(const string& s, char c);
    static string trimLead(const string& s, const string& prefix);
    static string getRunwayFromApproachName(const string &approachName);
};

class XPSceneryPacksIniReader : TokenValueFileReaderBase
{
private:
    shared_ptr<HostServices> m_host;
public:
    XPSceneryPacksIniReader(shared_ptr<HostServices> _host);
public:
    void readSceneryFolderList(istream& input, vector<string>& sceneryFolders);
};
