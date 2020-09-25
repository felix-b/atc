// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
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
private:
    const shared_ptr<HostServices> m_host;
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
    XPAirportReader(const shared_ptr<HostServices> _host);
    void readAptDat(istream &input);
    void setAirspace(shared_ptr<ControlledAirspace> airspace);
    bool validate(vector<string> &diagnostics);
    shared_ptr<Airport> getAirport();
private:
    void readAptDatInContext(istream &input, ContextualParser parser);
    bool rootContextParser(int lineCode, istream &input);
    void parseHeader1(istream &input);
    void parseRunway100(istream &input);
    void parseTaxiNode1201(istream &input);
    void parseTaxiEdge1202(istream &input);
    void parseGroundEdge1206(istream &input);
    void parseRunwayActiveZone1204(istream& input, shared_ptr<TaxiEdge> edge);
    void parseStartupLocation1300(istream &input);
    void parseMetadata1302(istream &input);
    void parseControlFrequency(int lineCode, istream &input);
    bool isControlFrequencyLine(int lineCode);
public:
    static string readFirstToken(istream &input);
    static string readToEndOfLine(istream &input);
    static void skipToNextLine(istream &input);
};
