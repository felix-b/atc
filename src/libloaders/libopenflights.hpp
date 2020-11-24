//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <fstream>
#include <cstring>
#include "libworld.h"

using namespace std;
using namespace world;

// from https://stackoverflow.com/a/1120224
class CSVRow
{
private:
    string m_line;
    vector<string> m_data;

private:
    // from https://stackoverflow.com/a/6089413
    static istream &safeGetline(istream &is, string &t)
    {
        t.clear();

        // The characters in the stream are read one-by-one using a std::streambuf.
        // That is faster than reading them one-by-one using the std::istream.
        // Code that uses streambuf this way must be guarded by a sentry object.
        // The sentry object performs various tasks,
        // such as thread synchronization and updating the stream state.

        istream::sentry se(is, true);
        streambuf *sb = is.rdbuf();

        for (;;)
        {
            int c = sb->sbumpc();
            switch (c)
            {
            case '\n':
                return is;
            case '\r':
                if (sb->sgetc() == '\n')
                {
                    sb->sbumpc();
                }
                return is;
            case streambuf::traits_type::eof():
                // Also handle the case when the last line has no line ending
                is.setstate(ios::eofbit);
                if (t.empty())
                {
                    // No data, row is not valid : stream extraction has to fail
                    is.setstate(ios::failbit);
                }
                return is;
            default:
                t += (char)c;
            }
        }
    }

public:
    const string &operator[](size_t index) const
    {
        if (index >= size())
        {
            throw out_of_range{"CSVRow : index out of bounds"};
        }
        return m_data[index];
    }
    size_t size() const
    {
        return m_data.size();
    }
    void readNextRow(istream &str)
    {
        safeGetline(str, m_line);

        m_data = split(m_line, ',');
        // Add empty last field if needed
        if (!m_line.empty() && (m_line.back() == ','))
        {
            m_data.push_back("");
        }
    }
};

istream &operator>>(istream &str, CSVRow &data)
{
    data.readNextRow(str);
    return str;
}

class CsvParser
{
public:
    typedef function<void(CSVRow &)> LineRedCallback;

    static void parse(istream &input, LineRedCallback callback)
    {
        CSVRow row;

        try
        {
            while (input >> row)
            {
                callback(row);
            }
        }
        // catch the fail exception here if thz stream has the failbit exception set
        catch (ifstream::failure e)
        {
            if (!input.eof())
            {
                // rethrow the exception if we are not at eof
                throw;
            }
        }
    }
};

class OpenFlightDataReader
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<WorldRoutes> m_routes;
    unordered_map<string, string> m_airportIata2Icao;
    unordered_map<string, string> m_airframeIata2Icao;
    unordered_map<int, string> m_airlinesId2Icao;

public:
    explicit OpenFlightDataReader(shared_ptr<HostServices> _host) : m_host(_host){};

public:
    shared_ptr<WorldRoutes> getRoutes(const string &dataDirectoryPath, const vector<string>& allowedAircrafts)
    {
        default_random_engine rng = default_random_engine(chrono::system_clock::now().time_since_epoch().count());
        return getRoutes(dataDirectoryPath, allowedAircrafts, rng);

    } 
    // This one should only ber used directly for testing purposes
    shared_ptr<WorldRoutes> getRoutes(const string &dataDirectoryPath, const vector<string>& allowedAircrafts, default_random_engine &rng)
    {
        // If this is gonna last in the plugin, it might be better
        // to generate a binary version of all this at build time.
        if (m_routes == nullptr)
        {
            // Initialize airport iata -> icao conversion
            string filePath = m_host->pathAppend(dataDirectoryPath, {"airports.dat"});
            m_host->writeLog("OPNFLT|Reading [%s]", filePath.c_str());
            shared_ptr<istream> input = m_host->openFileForRead(filePath);
            readAirports(*input);

            filePath = m_host->pathAppend(dataDirectoryPath, {"planes.dat"});
            m_host->writeLog("OPNFLT|Reading [%s]", filePath.c_str());
            input = m_host->openFileForRead(filePath);
            readPlanes(*input);

            filePath = m_host->pathAppend(dataDirectoryPath, {"airlines.dat"});
            m_host->writeLog("OPNFLT|Reading [%s]", filePath.c_str());
            input = m_host->openFileForRead(filePath);
            readAirlines(*input);

            filePath = m_host->pathAppend(dataDirectoryPath, {"routes.dat"});
            m_host->writeLog("OPNFLT|Reading [%s]", filePath.c_str());
            input = m_host->openFileForRead(filePath);
            readRoutes(*input, allowedAircrafts, rng);
        }
        if (m_routes == nullptr)
        {
            throw runtime_error("OPNFLT : unread routes");
        }
        return m_routes;
    }

    // Parsing OpenFlights airpors.dat file.
    // Generates a mapping from iata to icao
    //
    // Columns are :
    //
    // - Airport ID Unique OpenFlights identifier for this airport.
    // - Name       Name of airport. May or may not contain the City name.
    // - City       Main city served by airport. May be spelled differently from Name.
    // - Country    Country or territory where airport is located. See Countries to cross-reference to ISO 3166-1 codes.
    // - IATA       3-letter IATA code. Null if not assigned/unknown.
    // - ICAO       4-letter ICAO code. Null if not assigned.
    // - Latitude   Decimal degrees, usually to six significant digits. Negative is South, positive is North.
    // - Longitude  Decimal degrees, usually to six significant digits. Negative is West, positive is East.
    // - Altitude   In feet.
    // - Timezone   Hours offset from UTC. Fractional hours are expressed as decimals, eg. India is 5.5.
    // - DST        Daylight savings time. One of E (Europe), A (US/Canada), S (South America), O (Australia), Z (New Zealand), N (None) or U (Unknown). See also: Help: Time
    // - Tz database time zone Timezone in "tz" (Olson) format, eg. "America/Los_Angeles".
    // - Type       Type of the airport. Value "airport" for air terminals, "station" for train stations, "port" for ferry terminals and "unknown" if not known. In airports.csv, only type=airport is included.
    // - Source     Source of this data. "OurAirports" for data sourced from OurAirports, "Legacy" for old data not matched to OurAirports (mostly DAFIF), "User" for unverified user contributions. In airports.csv, only source=OurAirports is included.
    void readAirports(istream &input)
    {
        CsvParser::parse(input,
                         [this](CSVRow &row) {
                             if ((row.size() == 14) && isValidString(row[4]) && isValidString(row[5]))
                             {
                                 string iata(row[4].substr(1, row[4].size() - 2));
                                 string icao(row[5].substr(1, row[5].size() - 2));
                                 m_airportIata2Icao[iata] = icao;
                             }
                         });
        m_host->writeLog("OPNFLT| Airports parsed : %d association IATA->ICAO found", m_airportIata2Icao.size());
    }

    // Parsing OpenFlights planes.dat file.
    // Generates a mapping from iata to icao
    //
    // Columns are :
    //
    // Name         Full name of the aircraft.
    // IATA code    Unique three-letter IATA identifier for the aircraft.
    // ICAO code    Unique four-letter ICAO identifier for the aircraft.
    void readPlanes(istream &input)
    {
        CsvParser::parse(input,
                         [this](CSVRow &row) {
                             if ((row.size() == 3) && isValidString(row[1]) && isValidString(row[2]))
                             {
                                 string iata(row[1].substr(1, row[1].size() - 2));
                                 string icao(row[2].substr(1, row[2].size() - 2));
                                 m_airframeIata2Icao[iata] = icao;
                             }
                         });
        m_host->writeLog("OPNFLT| Planes parsed : %d association IATA->ICAO found", m_airframeIata2Icao.size());
    }

    // Parsing OpenFlights airlines.dat file.
    // Stores ICAO and callsign for an airline.
    //
    // Columns are :
    //
    // Airline  ID Unique OpenFlights identifier for this airline.
    // Name     Name of the airline.
    // Alias    Alias of the airline. For example, All Nippon Airways is commonly known as "ANA".
    // IATA     2-letter IATA code, if available.
    // ICAO     3-letter ICAO code, if available.
    // Callsign Airline callsign.
    // Country  Country or territory where airport is located. See Countries to cross-reference to ISO 3166-1 codes.
    // Active   "Y" if the airline is or has until recently been operational, "N" if it is defunct. This field is not reliable: in particular, major airlines that stopped flying long ago, but have not had their IATA code reassigned (eg. Ansett/AN), will incorrectly show as "Y".
    void readAirlines(istream &input)
    {
        int lineCount = 0;
        CsvParser::parse(input,
                         [&, this](CSVRow &row) {
                             lineCount++;
                             if ((row.size() == 8) && isValidString(row[4]) && isValidString(row[5]))
                             {
                                 try
                                 {
                                     int openflightAirlineId = stoi(row[0]);
                                     string airlineIcao = (row[4].substr(1, row[4].size() - 2));
                                     m_airlinesId2Icao[openflightAirlineId] = airlineIcao;
                                 }
                                 catch (const exception &e)
                                 {
                                     m_host->writeLog("OPNFLT| Exception [%s] airline route at line %d", e.what(), lineCount);
                                 }
                             }
                         });
        // An airline can be invalid if the ICAO or callsign is invalid
        m_host->writeLog("OPNFLT| Airlines parsed : %d/%d valid airlines found", m_airlinesId2Icao.size(), lineCount);
    }

    // Parsing OpenFlights routes.dat file.
    // Stores routes cf. world::Route.
    //
    // Columns are :
    //
    // Airline                  2-letter (IATA) or 3-letter (ICAO) code of the airline.
    // Airline ID               Unique OpenFlights identifier for airline (see Airline).
    // Source airport           3-letter (IATA) or 4-letter (ICAO) code of the source airport.
    // Source airport ID        Unique OpenFlights identifier for source airport (see Airport)
    // Destination airport      3-letter (IATA) or 4-letter (ICAO) code of the destination airport.
    // Destination airport ID   Unique OpenFlights identifier for destination airport (see Airport)
    // Codeshare                "Y" if this flight is a codeshare (that is, not operated by Airline, but another carrier), empty otherwise.
    // Stops                    Number of stops on this flight ("0" for direct)
    // Equipment                3-letter codes for plane type(s) generally used on this flight, separated by spaces
    void readRoutes(istream &input, const vector<string>& allowedAircrafts, default_random_engine &rng)
    {
        int lineCount = 0;
        vector<shared_ptr<world::Route>> routes;
        CsvParser::parse(input,
                         [&, this](CSVRow &row) {
                             lineCount++;
                             if ((row.size() == 9) && isValidEntry(row[1]) && isValidEntry(row[2]) && isValidEntry(row[4]))
                             {
                                 try
                                 {
                                     int openflightAirlineId = stoi(row[1]);
                                     auto icaoDeparture = getValueOrThrow(m_airportIata2Icao, row[2]);
                                     auto icaoArrival = getValueOrThrow(m_airportIata2Icao, row[4]);
                                     auto iataAirframes = split(string(row[8]), ' ');
                                     vector<string> icaoAirframes;
                                     for (string iataAirframe : iataAirframes)
                                     {
                                         try
                                         {
                                             icaoAirframes.push_back(getValueOrThrow(m_airframeIata2Icao, iataAirframe));
                                         }
                                         catch (const exception &e)
                                         {
                                             // Nothing to do if the iata code for one of the planes operating the routes
                                             // has no ICAO equivalent. but the route might still be valid
                                         }
                                     }
                                     auto airlineIcao = getValueOrThrow(m_airlinesId2Icao, openflightAirlineId);

                                     // Compare list of allowed aircrafts if neither of them is empty
                                     bool aircraftValid = !((allowedAircrafts.size() > 0) && (icaoAirframes.size() > 0));

                                     for (auto allowedAtAirport : allowedAircrafts)
                                     {
                                         if (hasStringInsensitive(icaoAirframes, allowedAtAirport))
                                         {
                                             aircraftValid = true;
                                             break;
                                         }
                                     }
                                     if (aircraftValid)
                                     {
                                         auto route = make_shared<world::Route>(
                                             icaoDeparture,
                                             icaoArrival,
                                             airlineIcao,
                                             std::move(icaoAirframes));

                                         routes.push_back(route);
                                     }
                                 }
                                 catch (const exception &e)
                                 {
                                     //  m_host->writeLog("OPNFLT| Exception [%s] parsing route at line %d", e.what(), lineCount);
                                 }
                             }
                         });
        // A route can be invalid if we cannot find the icao code of one of the airports (stored as iata in openflights files)
        m_host->writeLog("OPNFLT| Routes parsed : %d/%d valid routes found", routes.size(), lineCount);

        unordered_map<string, vector<shared_ptr<Route>>> routesFrom;
        unordered_map<string, vector<shared_ptr<Route>>> routesTo;
        // Build the route indexes
        for (auto route : routes)
        {
            routesFrom[route->departure()].push_back(route);
            routesTo[route->destination()].push_back(route);
        }

        // And shuffle them
        for (auto entry : routesFrom)
        {
            auto airport = entry.first;
            shuffle(routesFrom[airport].begin(), routesFrom[airport].end(), rng);
            shuffle(routesTo[airport].begin(), routesTo[airport].end(), rng);
        }

        m_routes = make_shared<WorldRoutes>(m_host, routes, routesFrom, routesTo, true);
    }

private:
    // A value of \N means an invalid/unknown field  in openflights files
    bool isValidEntry(const string &value)
    {
        return (value.compare("\\N") != 0);
    }
    bool isValidString(const string &value)
    {
        return (isValidEntry(value) && (value.front() == '"') && (value.back() == '"'));
    }

    static bool iequals(const string &a, const string &b)
    {
        if (a.length() == b.length())
        {
            return equal(b.begin(), b.end(), a.begin(),
                         [](unsigned char ca, unsigned char cb) {
                             return (tolower(ca) == tolower(cb));
                         });
        }
        return false;
    }

    static bool hasStringInsensitive(const vector<string> v, const string &s)
    {
        // An empty string is always valid
        if (s.size() == 0)
            return true;

        // An empty vector is valid : there are no constraints
        if (v.size() == 0)
            return true;

        return hasAny<string>(v, [s](string test) {
            return (iequals(s, test));
        });
    }

private:
};
