// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#define _USE_MATH_DEFINES

#include <cstring>
#include <cstdarg>
#include <iostream>
#include <fstream>
#include <sstream>
#include <iomanip>
#include <functional>
#include <chrono>
#include <cmath>
#include <queue>
#include <vector>
#include <random>

// SDK
#include "XPLMUtilities.h"
#include "XPLMPlugin.h"
#include "XPLMGraphics.h"
#include "XPLMProcessing.h"

#include "XPCAircraft.h"
#include "XPMPAircraft.h"
#include "XPMPMultiplayer.h"

// PPL 
#include "log.h"
#include "owneddata.h"
#include "alsoundbuffer.h"

// concurrentqueue
#include "blockingconcurrentqueue.h"

// tnc
#include "utils.h"
#include "poc.h"
#include "libworld.h"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "libdataxp.h"
#include "libai.hpp"
#include "libspeech.h"
#include "simplePhraseologyService.hpp"
#include "nativeTextToSpeechService.hpp"

using namespace std;
using namespace PPL;
using namespace XPMP2;
using namespace world;
using namespace ai;

#define FEET_IN_1_METER 3.28084

class PluginHostServices : public HostServices
{
private:
    chrono::time_point<chrono::high_resolution_clock, chrono::milliseconds> m_startTime;
    string m_directorySeparator;
    string m_pluginDirectory;
    random_device m_randomDevice;
    mt19937 m_randomGenerator;
    XPLMProbeRef m_hTerrainProbe;
    shared_ptr<World> m_world;
public:
    PluginHostServices() :
        m_startTime(std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now())),
        m_hTerrainProbe(XPLMCreateProbe(xplm_ProbeY))
    {
        m_directorySeparator = XPLMGetDirectorySeparator();
        m_pluginDirectory = getPluginDirectory();
        m_randomGenerator = mt19937(m_randomDevice());
        writeLog("plugin directory = [%s]", m_pluginDirectory.c_str());
        writeLog("PluginHostServices initialized");
    }
public:
    shared_ptr<World> getWorld() override
    {
        if (m_world)
        {
            return m_world;
        }
        throw runtime_error("PluginHostServices::getWorld() failed: world was not injected");
    }

    int getNextRandom(int maxValue)
    {
        uniform_int_distribution<> distribution(0, maxValue - 1);
        return distribution(m_randomGenerator);
    }

    LocalPoint geoToLocal(const GeoPoint& geo) override
    {
        return LocalPoint({
            (float)geo.longitude,
            (float)geo.altitude,
            (float)geo.latitude
        });
    }
    GeoPoint localToGeo(const LocalPoint& local) override
    {
        return GeoPoint({
            local.z,
            local.x,
            local.y
        });
    }
    shared_ptr<Controller> createAIController(shared_ptr<ControllerPosition> position) override
    {
        return services().get<AIControllerFactory>()->createController(position);
    }
    shared_ptr<Pilot> createAIPilot(shared_ptr<Flight> flight) override
    {
        return services().get<AIPilotFactory>()->createPilot(flight);
    }
    string getResourceFilePath(const vector<string>& relativePathParts) override
    {
        string fullPath = m_pluginDirectory;
        for (const string& part : relativePathParts)
        {
            fullPath.append(m_directorySeparator);
            fullPath.append(part);
        }
        return fullPath;
    }
    shared_ptr<istream> openFileForRead(const string& filePath) override
    {
        auto file = shared_ptr<ifstream>(new ifstream());
        file->exceptions(ifstream::failbit | ifstream::badbit);
        file->open(filePath);
        return file;
    }
    void writeLog(const char* format, ...) override
    {
        const size_t bufferSize = 512;
        char buffer[bufferSize];
        va_list argptr;
        va_start(argptr, format);
        int messageLength = vsnprintf(buffer, bufferSize, format, argptr);
        va_end(argptr);

        if (messageLength < 0 || messageLength >= bufferSize)
        {
            strncpy(buffer, "WARNING: log message skipped, buffer overrun!", bufferSize);
        }
        
        auto now = std::chrono::time_point_cast<std::chrono::milliseconds>(chrono::high_resolution_clock::now());
        auto elapsedMilliseconds = now - m_startTime;
        stringstream s;
        s << "TNC> [+" << setw(10) << elapsedMilliseconds.count() << "] " << buffer << endl;

        XPLMDebugString(s.str().c_str());
    }
public:
    void useWorld(shared_ptr<World> _world)
    {
        m_world = _world;
        _world->onQueryTerrainElevation([this](const GeoPoint& location){
            return probeTerrainElevationAt(location);
        });
    }
private:
    float probeTerrainElevationAt(const GeoPoint& location)
    {
        if (!m_hTerrainProbe) 
        {
            writeLog("probeTerrainElevationAt ERROR! XPLMCreateProbe failed. Returning 0 MSL");
            return 0;
        }

        XPLMProbeInfo_t infoProbe = {
            sizeof(XPLMProbeInfo_t),            // structSIze
            0.0f, 0.0f, 0.0f,                   // location
            0.0f, 0.0f, 0.0f,                   // normal vector
            0.0f, 0.0f, 0.0f,                   // velocity vector
            0                                   // is_wet
        };
        double x, y, z;
        XPLMWorldToLocal(location.latitude, location.longitude, 0, &x, &y, &z);

        if (XPLMProbeTerrainXYZ(m_hTerrainProbe, x, 0.0f, z, &infoProbe) == xplm_ProbeHitTerrain)
        {
            double lat, lon, altMeters;
            XPLMLocalToWorld(x, infoProbe.locationY, z, &lat, &lon, &altMeters);
            return altMeters * FEET_IN_1_METER;
        }
        else
        {
            writeLog("probeTerrainElevationAt ERROR! XPLMProbeTerrainXYZ failed. Returning 0 MSL");
            return 0;
        }
    }
};

class XPLMSpeakStringTtsService : public TextToSpeechService
{
private:
    shared_ptr<HostServices> m_host;
    DataRef<int> m_com1Power;
    DataRef<int> m_com1FrequencyKhz;
public:
    XPLMSpeakStringTtsService(shared_ptr<HostServices> _host) :
        m_host(_host),
        m_com1Power("sim/cockpit2/radios/actuators/com1_power", PPL::ReadOnly),
        m_com1FrequencyKhz("sim/cockpit2/radios/actuators/com1_frequency_hz_833", PPL::ReadOnly)
    {
    }
public:
    QueryCompletion vocalizeTransmission(shared_ptr<Frequency> frequency, shared_ptr<Transmission> transmission) override
    {
        if (!transmission || !transmission->verbalizedUtterance())
        {
            throw runtime_error("vocalizeTransmission: transmission was not verbalized");
        }

        auto world = m_host->getWorld();
        string transmissionText = transmission->verbalizedUtterance()->plainText();
        chrono::milliseconds speechDuration = countSpeechDuration(transmissionText);
        chrono::microseconds completionTimestamp = world->timestamp() + speechDuration;

        int com1Power = m_com1Power;
        int com1FrequencyKhz = m_com1FrequencyKhz;
        bool isHeardByUser = (com1Power == 1 && com1FrequencyKhz == frequency->khz());
        m_host->writeLog(
            "XPLMSpeakStringTtsService::vocalizeTransmission : com1Power=%d, com1FrequencyKhz=%d, isHeardByUser=%s", 
            com1Power, com1FrequencyKhz, isHeardByUser ? "Yes" : "No");
        
        if (isHeardByUser)
        {
            XPLMSpeakString(transmissionText.c_str());
        }

        return [world, completionTimestamp]() {
            return (world->timestamp() >= completionTimestamp);
        };
    }
public:
    static chrono::milliseconds countSpeechDuration(const string& text)
    {
        int commaCount = 0;
        int periodCount = 0;

        for (int i = 0 ; i < text.length() ; i++)
        {
            char c = text[i];
            if (c == ',')
            {
                commaCount++;
            }
            else if (c == '.')
            {
                periodCount++;
            }
        }

        return chrono::milliseconds(100 * text.length() + 500 * commaCount + 750 * periodCount);
    }
};


class Poc1Aircraft : public XPMP2::Aircraft
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<Flight> m_flight;
    World::OnChangesCallback m_onQueryChanges;
    int m_frameCount;
public:
    Poc1Aircraft(
        shared_ptr<HostServices> _host, 
        shared_ptr<Flight> _flight
    ) : Aircraft(
            _flight->aircraft()->modelIcao(), 
            _flight->aircraft()->airlineIcao(), 
            "", 
            4333 + _flight->id(), // mode-S id
            ""
        ),
        m_host(_host),
        m_flight(_flight),
        m_onQueryChanges(World::onChangesUnassigned)
    {
        auto source = m_flight->aircraft();
        auto location = source->location();

        // Label
        label = m_flight->callSign();
        colLabel[0] = 0.0f;             // green
        colLabel[1] = 1.0f;
        colLabel[2] = 0.0f;

        // Radar
        acRadar.code = 4333 + _flight->id();
        acRadar.mode = xpmpTransponderMode_ModeC;

        // informational texts
        strcpy(acInfoTexts.icaoAcType, source->modelIcao().c_str());
        strcpy(acInfoTexts.icaoAirline, source->airlineIcao().c_str());
        strcpy(acInfoTexts.tailNum, m_flight->callSign().c_str());

        SetLocation(location.latitude, location.longitude, 0.0f);
        SetHeading(source->attitude().heading());
        SetPitch(0.0f);
        SetRoll(0.0f);
        SetGearRatio(1.0f);
        SetFlapRatio(0.0f);
        SetSlatRatio(0.0f);
        SetSpoilerRatio(0);
        SetSpeedbrakeRatio(0);
        SetWingSweepRatio(0.0f);
        SetThrustRatio(0.0f);
        SetYokePitchRatio(0.0f);
        SetYokeHeadingRatio(0.0f);
        SetYokeRollRatio(0.0f);
        SetLightsBeacon(false);
        SetLightsTaxi(false);
        SetLightsStrobe(false);
        SetLightsLanding(false);
        SetLightsNav(false);
        SetTouchDown(false);

        float groundPitch = 0.0f;
        safeClampToGround(groundPitch);
        SetPitch(groundPitch);
    }

    void UpdatePosition(float, int) override
    {
        //m_host->writeLog("Poc1Aircraft::UpdatePosition - enter");
        try
        {
            safeUpdatePosition();
        }
        catch (const exception& e)
        {
            m_host->writeLog("Poc1Aircraft::UpdatePosition CRASHED!!! %s", e.what());
        }
        //m_host->writeLog("Poc1Aircraft::UpdatePosition - exit");
    }

    void onQueryChanges(World::OnChangesCallback callback)
    {
        m_onQueryChanges = callback;
    }
private:
    void safeUpdatePosition()
    {
        m_frameCount++;

        auto changes = m_onQueryChanges();
        if (!changes || !hasKey(changes->flights().updated(), m_flight->id()))
        {
            return;
        }

        //m_host->writeLog("Flight %s: updating sim aircraft location", m_flight->callSign().c_str());

        auto source = m_flight->aircraft();
        const auto& location = source->location();
        const auto& attitude = source->attitude();
        const auto& altitude = source->altitude();
        
        float pitchAdjustment = 0.0f;

        SetLocation(location.latitude, location.longitude, altitude.isGroundBased() ? 0.0f : altitude.feet());
        
        if (altitude.isGroundBased())
        {
            safeClampToGround(pitchAdjustment);

            if (altitude.type() == Altitude::Type::AGL)
            {
                drawInfo.y += altitude.feet() / FEET_IN_1_METER;
            }
        }

        SetHeading(attitude.heading());
        SetPitch(attitude.pitch() + pitchAdjustment);
        SetRoll(attitude.roll());

        SetLightsBeacon(source->isLightsOn(world::Aircraft::LightBits::Beacon));
        SetLightsTaxi(source->isLightsOn(world::Aircraft::LightBits::Taxi)); //TODO: taxi lights not working?
        SetLightsStrobe(source->isLightsOn(world::Aircraft::LightBits::Strobe));
        SetLightsLanding(
            source->isLightsOn(world::Aircraft::LightBits::Taxi) ||
            source->isLightsOn(world::Aircraft::LightBits::Landing)); 
        SetLightsNav(source->isLightsOn(world::Aircraft::LightBits::Nav));

        SetGearRatio(source->gearState());
        SetFlapRatio(source->flapState());
        SetSlatRatio(source->flapState());
        SetSpeedbrakeRatio(source->spoilerState());
        SetSpoilerRatio(source->spoilerState());
    }

    void safeClampToGround(float& groundPitch)
    {
        ClampToGround();

        //TODO: use actual model matched by XPMP2
        groundPitch = -1.5;
        // const string& model = m_flight->aircraft()->modelIcao();

        // if (model.compare("B738") == 0)
        // {
        //     SetPitch(-1.5);
        // }
        // else if (model.compare("A320") == 0)
        // {
        //     drawInfo.y += 0.4;
        //     SetPitch(-0.5);
        // }
    }
};

class TncPoc1 : public TncPoc
{
private:
    shared_ptr<PluginHostServices> m_host;
    shared_ptr<World> m_world;
    chrono::time_point<chrono::high_resolution_clock, chrono::microseconds> m_lastTickTime;
    vector<shared_ptr<Poc1Aircraft>> m_simAircraft;
    shared_ptr<World::ChangeSet> m_lastChangeSet;
    uint64_t m_timeFactor;
public:
    TncPoc1(shared_ptr<PluginHostServices> _host) :
        m_host(_host),
        m_timeFactor(1)
    {
    }
    virtual ~TncPoc1()
    {
    }
public:
    void start() override 
    {
        m_host->writeLog("Starting POC # 1");
        m_lastTickTime = getNow();

        auto kjfkAirport = loadKjfkAirport();
        m_world = WorldBuilder::assembleSampleWorld(m_host, kjfkAirport);
        m_host->useWorld(m_world);
        m_host->writeLog("World initialized");

        initDemoFlights(kjfkAirport, 10, m_world->startTime() + 190, m_world->startTime() + 10);
        m_host->writeLog("AI flights initialized");

        XPLMRegisterFlightLoopCallback(&worldFlightLoopCallback, 0.5, this);
        m_host->writeLog("Registered flight loop callback");

        m_host->writeLog(
            "Initialized world with [%d] airports, [%d] control facilities, [%d] flights", 
            m_world->airports().size(), 
            m_world->controlFacilities().size(),
            m_world->flights().size());
    }
    void stop() override
    {
        m_host->writeLog("Stopping POC # 1");

        XPLMUnregisterFlightLoopCallback(&worldFlightLoopCallback, this);
        m_host->writeLog("Unregistered flight loop callback");

        m_world.reset();
        m_host->writeLog("World shut down");
    }
    void setTimeFactor(uint64_t factor) override 
    {
        m_timeFactor = factor;
    }
    void flightLoopTick()
    {
        try
        {
            auto now = getNow();
            auto microsecondsSinceLastTick = (now - m_lastTickTime) * m_timeFactor;
            auto newWorldTimestamp = m_world->timestamp() + microsecondsSinceLastTick;
            m_lastTickTime = now;
            
            m_world->progressTo(newWorldTimestamp);
            m_lastChangeSet = m_world->hasChanges()
                ? m_world->takeChanges()
                : nullptr;
            
            if (m_lastChangeSet)
            {
                processWorldChanges();
            }
        }
        catch(const exception& e)
        {
            m_host->writeLog("World::flightLoopTick CRASHED!!! %s", e.what());
        }
    }
private:
    void processWorldChanges()
    {
        //m_host->writeLog("Processing world changes.");

        for (const auto& addedFlight : m_lastChangeSet->flights().added())
        {
            auto newSimAircraft = shared_ptr<Poc1Aircraft>(new Poc1Aircraft(m_host, addedFlight));
            newSimAircraft->onQueryChanges([this, addedFlight](){
                //m_host->writeLog("onQueryChanges from %s", addedFlight->callSign().c_str());
                return m_lastChangeSet;
            });
            m_simAircraft.push_back(newSimAircraft);
        }
    }
    chrono::time_point<chrono::high_resolution_clock, chrono::microseconds> getNow() 
    {
        return std::chrono::time_point_cast<std::chrono::microseconds>(chrono::high_resolution_clock::now());
    }
    shared_ptr<Airport> loadKjfkAirport()
    {
        string kjfkFilePath = m_host->getResourceFilePath({ "airports", "kjfk.apt.dat" });
        m_host->writeLog("KJFK file path [%s]", kjfkFilePath.c_str());

        shared_ptr<istream> kjfkFile = m_host->openFileForRead(kjfkFilePath);
        XPAirportReader airportReader(m_host);

        airportReader.setAirspace(createKjfkAirspace());
        airportReader.readAptDat(*kjfkFile);
        auto kjfkAirport = airportReader.getAirport();

        m_host->writeLog("KJFK airport loaded");
        return kjfkAirport;
    }
    void initDemoFlights(shared_ptr<Airport> airport, int count, time_t firstDepartureTime, time_t firstArrivalTime)
    {
        unordered_map<string, string> callSignByAirline = {
            { "DAL", "Delta" },
            { "AAL", "American" },
            { "SWA", "Southwest" },
        };

        const auto addOutboundFlight = [this, airport, &callSignByAirline](
            const string& model, const string& airline, int flightId, const string& destination, time_t departureTime, shared_ptr<ParkingStand> gate
        ) {
            string callSign = getValueOrThrow(callSignByAirline, airline);
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(departureTime, departureTime + 60 * 60 * 3, airport->header().icao(), destination));
            flightPlan->setDepartureGate(gate->name());
            flightPlan->setDepartureRunway("13R");

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));
            int aircraftId = 1000 + flightId;
            
            auto aircraft = shared_ptr<world::Aircraft>(new world::Aircraft(m_host, aircraftId, model, airline, to_string(flightId), world::Aircraft::Category::Jet));
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);
            aircraft->setManeuver(pilot->getFlightCycle());

            m_world->addFlightColdAndDark(flight);
        };

        const auto addInboundFlight = [this, airport, &callSignByAirline](
            const string& model, const string& airline, int flightId, const string& origin, time_t arrivalTime, shared_ptr<ParkingStand> gate
        ) {
            m_host->writeLog("adding inbound flight id=%d", flightId);

            string callSign = getValueOrThrow(callSignByAirline, airline);
            auto flightPlan = shared_ptr<FlightPlan>(new FlightPlan(arrivalTime - 60 * 60 * 3, arrivalTime, origin, airport->header().icao()));
            flightPlan->setArrivalGate(gate->name());
            flightPlan->setArrivalRunway("13L");

            auto flight = shared_ptr<Flight>(new Flight(m_host, flightId, Flight::RulesType::IFR, airline, to_string(flightId), callSign + " " + to_string(flightId), flightPlan));
            int aircraftId = 1000 + flightId;
            
            auto aircraft = shared_ptr<world::Aircraft>(new world::Aircraft(m_host, aircraftId, model, airline, to_string(flightId), world::Aircraft::Category::Jet));
            flight->setAircraft(aircraft);

            auto pilot = m_host->createAIPilot(flight);
            flight->setPilot(pilot);

            m_world->deferUntil(arrivalTime, [=](){
                const auto& landingRunwayEnd = m_world->getRunwayEnd(airport->header().icao(), "13L");
                m_world->addFlight(flight);
                aircraft->setOnFinal(landingRunwayEnd);
                aircraft->setManeuver(pilot->getFinalToGate(landingRunwayEnd));
            });
        };

        vector<shared_ptr<ParkingStand>> gates;
        findGatesForFlights(airport, gates, count);

        int index = 0;
        time_t nextDepartureTime = firstDepartureTime;
        time_t nextArrivalTime = firstArrivalTime;
        vector<string> airlineOptions = { "DAL", "AAL", "SWA" };
        vector<string> modelOptions = { "B738" /*, "A320"*/ };

        for (const auto& gate : gates)
        {
            index++;
            int flightId = 100 + index;
            const string& airline = airlineOptions[index % airlineOptions.size()];
            const string& model = modelOptions[index % modelOptions.size()];
            
            try
            {
                if ((index % 2) == 1)
                {
                    time_t departureTime = nextDepartureTime;
                    nextDepartureTime += 180;
                    addOutboundFlight(model, airline, flightId, "KMIA", departureTime, gate);
                }
                else
                {
                    time_t arrivalTime = nextArrivalTime;
                    nextArrivalTime += 45;
                    addInboundFlight(model, airline, flightId, "KJFK", arrivalTime, gate);
                }
            }
            catch(const std::exception& e)
            {
                m_host->writeLog("CRASHED while adding AI flight!!! %s", e.what());
            }
        }
    }
    void findGatesForFlights(shared_ptr<Airport> airport, vector<shared_ptr<ParkingStand>>& found, int count)
    {
        int skipCount = 1;

        for (const auto& gate : airport->parkingStands())
        {
            m_host->writeLog("Checking gate [%s]", gate->name().c_str());
            if (gate->type() == ParkingStand::Type::Gate && gate->hasOperationType(world::Aircraft::OperationType::Airline) && !gate->hasOperationType(world::Aircraft::OperationType::Cargo))
            {
                if (skipCount-- > 0)
                {
                    m_host->writeLog("Skipping gate [%s]", gate->name().c_str());
                    continue;
                }

                m_host->writeLog("Will use gate [%s] for AI flights", gate->name().c_str());
                found.push_back(gate);
                if (found.size() >= count)
                {
                    m_host->writeLog("Found %d gates for AI flights.", found.size());
                    return;
                }
            }
        }
    }
private:
    static float worldFlightLoopCallback(
        float  inElapsedSinceLastCall,    
        float  inElapsedTimeSinceLastFlightLoop,    
        int    inCounter,    
        void * inRefcon)
    {
        if (inRefcon) 
        {
            static_cast<TncPoc1*>(inRefcon)->flightLoopTick();
        }
        return -1;
    }
    static shared_ptr<ControlledAirspace> createKjfkAirspace()
    {
        auto airspace = WorldBuilder::assembleSimpleAirspace(
            AirspaceClass::ClassB,
            ControlledAirspace::Type::TerminalControlArea,
            GeoPoint(40.639925000, -73.778694444),
            10, 
            ALTITUDE_GROUND,
            18000,
            "USA",
            "K6",
            "JFK",
            "JFK");
        return airspace;
    }
};

shared_ptr<TncPoc> createPoc1()
{
    try
    {
#if APL        
        XPLMDebugString("------ BEGIN ALTEST ------\n");
        NativeTextToSpeechService::init_sound();
        XPLMDebugString("------ END ALTEST ------\n");
#endif

        auto hostServices = shared_ptr<PluginHostServices>(new PluginHostServices());
        auto phraseologyService = shared_ptr<PhraseologyService>(new SimplePhraseologyService(hostServices));
        
        //auto pluginTts = shared_ptr<XPLMSpeakStringTtsService>(new XPLMSpeakStringTtsService(hostServices));
        auto pluginTts = shared_ptr<NativeTextToSpeechService>(new NativeTextToSpeechService(hostServices));
        
        auto intentFactory = shared_ptr<IntentFactory>(new IntentFactory(hostServices));

        hostServices->services().use<TextToSpeechService>(pluginTts);
        hostServices->services().use<IntentFactory>(intentFactory);
        hostServices->services().use<PhraseologyService>(phraseologyService);

        ai::contributeComponents(hostServices);

        hostServices->writeLog("AI factories initialized");

        auto result = shared_ptr<TncPoc1>(new TncPoc1(hostServices));
        hostServices->writeLog("Succesfully initialized POC # 1");
        return result;
    }
    catch(const exception& e)
    {
        PrintDebugString("TNC> POC # 1 CRASHED while initializing!!! %s\r\n", e.what());
        return nullptr;
    }
}
