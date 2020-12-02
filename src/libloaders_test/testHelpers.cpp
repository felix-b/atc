// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <fstream>
#include <sstream>
#include <vector>
#include <unordered_set>
#include "gtest/gtest.h"
#include "libworld.h"
#include "libdataxp.h"
#include "libworld_test.h"
#include "libdataxp_test.h"

using namespace world;
using namespace std;

shared_ptr<HostServices> makeHost()
{
    return make_shared<TestHostServices>();
}

shared_ptr<ControlledAirspace> makeAirspace(
    double centerLat, 
    double centerLon, 
    float radiusNm, 
    const string& name)
{
    GeoPolygon airspaceBounds({ 
        GeoPolygon::circleEdge(GeoPoint(centerLat, centerLon), radiusNm)
    });
    auto airspaceGeometry = shared_ptr<AirspaceGeometry>(new AirspaceGeometry(airspaceBounds, false, 0, true, 10000));
    auto airspace = shared_ptr<ControlledAirspace>(new ControlledAirspace(
        1, 
        "USA", 
        "TST", 
        name, 
        name, 
        ControlledAirspace::Type::ControlZone, 
        AirspaceClass::ClassB, 
        airspaceGeometry));
    return airspace;
}

stringstream makeAptDat(const vector<string>& lines)
{
    stringstream output;
    output.exceptions(ios::failbit | ios::badbit);

    for (const auto& line : lines)
    {
        output << line << endl;
    }

    output.seekg(0);
    return output;
}

string getTestInputFilePath(const string& fileName)
{
    return "../../src/libloaders_test/testInputs/" + fileName;
}

string getTestOutputFilePath(const string& fileName)
{
    return "../../src/libloaders_test/testOutputs/" + fileName;
}

void openTestInputStream(const string& fileName, ifstream& str)
{
    string fullPath = getTestInputFilePath(fileName);
    str.exceptions(ifstream::failbit | ifstream::badbit);
    str.open(fullPath.c_str());
}

void assertRunwaysExist(shared_ptr<Airport> airport, const vector<string>& names)
{
    for (const string& name : names)
    {
        try
        {
            auto runway = airport->getRunwayOrThrow(name);
            runway->getEndOrThrow(name);
        }
        catch (const exception& e)
        {
            stringstream message;
            message << "assertRunwaysExist FAILED name [" << name << "] error [" << e.what() << "]";
            throw runtime_error(message.str());
        }
    }
}

void assertGatesExist(shared_ptr<Airport> airport, const vector<string>& names)
{
    for (const string& name : names)
    {
        try
        {
            airport->getParkingStandOrThrow(name);
        }
        catch (const exception& e)
        {
            stringstream message;
            message << "assertGatesExist FAILED name [" << name << "] error [" << e.what() << "]";
            throw runtime_error(message.str());
        }
    }
}

void assertTaxiEdgesExist(shared_ptr<Airport> airport, const unordered_set<string>& names)
{
    unordered_set<string> remainingNames = names;

    for (auto edge : airport->taxiNet()->edges())
    {
        if (edge->type() == TaxiEdge::Type::Taxiway)
        {
            remainingNames.erase(edge->name());
        }
    }

    if (!remainingNames.empty())
    {
        stringstream message;
        message << "assertTaxiEdgesExist FAILED missing:";
        for (const auto& name : remainingNames)
        {
            message << " [" << name << "]";
        }
        throw runtime_error(message.str());
    }
}

// void writeAirportJson(shared_ptr<const Airport> airport, ostream& output)
// {
//     console::JsonWriter writer(output);
//     console::JsonProtocol protocol(writer);
//     protocol.writeAirport(airport);
// }

// void writeTaxiPathJson(shared_ptr<const TaxiPath> taxiPath, ostream& output)
// {
//     console::JsonWriter writer(output);
//     console::JsonProtocol protocol(writer);
//     protocol.writeTaxiPath(taxiPath);
// }
