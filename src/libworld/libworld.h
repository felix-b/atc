// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once
#define _USE_MATH_DEFINES

#include <cstdarg>
#include <cstring>
#include <string>
#include <memory>
#include <tuple>
#include <vector>
#include <unordered_map>
#include <list>
#include <queue>
#include <functional>
#include <chrono>
#include "stlhelpers.h"

using namespace std;

// lowest elevation in the world is -1355 MSL at Dead Sea
#define ALTITUDE_GROUND -10000
#define ALTITUDE_UNASSIGNED -11000
#define FREQUENCY_UNICOM_1228 122800
#define FREQUENCY_UNICOM_1227 122700
#define FREQUENCY_UNICOM_1230 123000
#define FEET_IN_1_METER 3.28084
#define KNOT_IN_1_METER_PER_SEC 1.9438444942
#define METERS_IN_1_NAUTICAL_MILE 1852

namespace world
{
    struct GeoPoint;
    struct LocalPoint;
    struct UniPoint;
    struct GeoPolygon;
    struct Vector3d;
    struct AircraftAttitude;
    struct Altitude;
    class Actor;
    class World;
    class ControlFacility;
    class ControlledAirspace;
    class AirspaceClass;
    class AirspaceGeometry;
    class RadarScope;
    class ControllerPosition;
    class Controller;
    class InformationService;
    class Frequency;
    class Utterance;
    class UtteranceBuilder;
    class PhraseologyService;
    class Transmission;
    class Intent;
    class Pilot;
    class Aircraft;
    class Flight;
    class FlightPlan;
    class Clearance;
    class Maneuver;
    class Airport;
    class Runway;
    class ParkingStand;
    class TaxiNet;
    class TaxiNode;
    class TaxiEdge;
    class TaxiPath;
    struct ActiveZoneMask;
    struct ActiveZoneMatrix;
    class WorldBuilder;
    class AIPilotFactory;
    class AIControllerFactory;
    class AIAircraftFactory;
    class TextToSpeechService;
    class AircraftObjectService;
    class HostServices;

    struct GeoPoint
    {
    public:
        double latitude;
        double longitude;
        double altitude; //to be removed
    public:
        GeoPoint() :
            latitude(0), longitude(0), altitude(0)
        {
        }
        GeoPoint(double _latitude, double _longitude, double _altitude = 0) :
            latitude(_latitude), longitude(_longitude), altitude(_altitude)
        {
        }
    public:
        static const GeoPoint empty;
    public:
        friend bool operator== (const GeoPoint& p1, const GeoPoint& p2);
        friend bool operator!= (const GeoPoint& p1, const GeoPoint& p2);    
    };

    struct GeoVector
    {
    public:
        GeoPoint p1;
        GeoPoint p2;
        double latitude;
        double longitude;
    public:
        GeoVector() :
            GeoVector({ 0, 0 }, {0, 0})
        {
        }
        GeoVector(const GeoPoint& _p1, const GeoPoint& _p2) :
            p1(_p1), p2(_p2)
        {
            longitude = p2.longitude - p1.longitude;
            latitude = p2.latitude - p1.latitude;
        }
    public:
        static const GeoVector empty;
    public:
        friend bool operator== (const GeoVector& u, const GeoVector& v);
        friend bool operator!= (const GeoVector& u, const GeoVector& v);
        friend double operator* (const GeoVector& u, const GeoVector& v);
    };

    struct LocalPoint
    {
        float x;
        float y;
        float z;
    };

    class UniPoint
    {
    private:
        enum class Type
        {
            local,
            geo
        };
    private:
        const shared_ptr<HostServices> m_services;
        const Type m_assignedType;
        LocalPoint m_local;
        GeoPoint m_geo;
    public:
        UniPoint(const GeoPoint& _geo);
        UniPoint(shared_ptr<HostServices> _services, const LocalPoint& _local);
        UniPoint(shared_ptr<HostServices> _services, const GeoPoint& _geo);
    public:
        // void moveByLocal(const LocalPoint& delta);
        // void moveByGeo(const GeoPoint& delta);
        const LocalPoint& local() const { return m_local; }
        const GeoPoint& geo() const { return m_geo; }
        double latitude() const { return m_geo.latitude; }
        double longitude() const { return m_geo.longitude; }
        double altitude() const { return m_geo.altitude; }
        float x() const { return m_local.x; }
        float y() const { return m_local.y; }
        float z() const { return m_local.z; }
    public:
        static UniPoint fromLocal(shared_ptr<HostServices> _services, const LocalPoint& _local);
        static UniPoint fromLocal(shared_ptr<HostServices> _services, float _x, float _y, float _z);
        static UniPoint fromGeo(shared_ptr<HostServices> _services, const GeoPoint& _geo);
        static UniPoint fromGeo(
            shared_ptr<HostServices> _services, 
            double _latitude, 
            double _longitude, 
            double _altitude);
    };

    struct Vector3d
    {
        const double latitude;
        const double longitude;
        const double altitude;
    };

    class GeoPolygon
    {
    public:
        enum class GeoEdgeType
        {
            Unknown = 0,
            ArcByEdge = 1,
            Circle = 2,
            GreatCircle = 3,
            RhumbLine = 4,
            ClockwiseArc = 5,
            CounterClockwiseArc = 6
        };
    public:
        struct GeoEdge 
        {
        public:
            const GeoEdgeType type;
            const GeoPoint fromPoint;
            const GeoPoint arcOrigin;
            const float arcDistance;
            const float arcBearing;
        };
    public:
        const vector<GeoEdge> edges;
    public:
        GeoPolygon(const vector<GeoEdge>& _edges) :
            edges(_edges)
        {
        }
    public:
        bool isEmpty() const { 
            return edges.size() == 0; 
        }
    public:
        static GeoPolygon empty()
        {
            return GeoPolygon({});
        }

        static GeoEdge circleEdge(const GeoPoint& arcOrigin, const float arcDistance)
        {
            return { GeoEdgeType::Circle, GeoPoint::empty, arcOrigin, arcDistance, 0 };
        }
    };

    class GeoMath
    {
    public:
        struct TurnData
        {
        public:
            GeoPoint e1p0;
            GeoPoint e1p1;
            double e1HeadingRad;
            GeoPoint e2p0;
            GeoPoint e2p1;
            double e2HeadingRad;
            float radius;
        };
        struct TurnArc
        {
        public:
            GeoPoint p0;
            GeoPoint p1;
            float heading0;
            float heading1;
            GeoPoint arcCenter;
            double arcStartAngle;
            double arcEndAngle;
            double arcDeltaAngle;
            double arcRadius;
            float arcLengthMeters;
            bool arcClockwise;
        };
    public:
        static double pi();
        static double twoPi();
        static double degreesToRadians(double degrees);
        static double radiansToDegrees(double degrees);
        static double headingToAngleDegrees(double headingDegrees);
        static double headingToAngleRadians(double headingDegrees);
        static double radiansToHeading(double radians);
        static GeoPoint getPointAtDistance(const GeoPoint& origin, float headingDegrees, float distanceMeters);
        static float getHeadingFromPoints(const GeoPoint& origin, const GeoPoint& destination);
        static double getRadiansFromPoints(const GeoPoint& origin, const GeoPoint& destination);
        static float getDistanceMeters(const GeoPoint& p1, const GeoPoint& p2);
        static double distanceToRadians(float distanceMeters);
        static float flipHeading(float headingDegrees);
        static void calculateTurn(const GeoMath::TurnData& input, GeoMath::TurnArc& output, shared_ptr<HostServices> host);
        static float getTurnDegrees(float fromHeading, float toHeading);
        static float addTurnToHeading(float heading, float turnDegrees);
        static bool isPointInRectangle(const GeoPoint& p, const GeoPoint& A, const GeoPoint& B, const GeoPoint& C, const GeoPoint& D);
        static double hypotenuse(double side);
    };

    template<class TKey>
    class HaveKey
    {
    public:
        virtual const TKey& getKey() = 0;
    };

    struct AircraftAttitude
    {
    private:
        double m_heading;
        double m_pitch;
        double m_roll;
    public:
        AircraftAttitude(double _heading, double _pitch, double _roll) :
            m_heading(_heading),
            m_pitch(_pitch),
            m_roll(_roll)
        {
        }
    public:
        const double heading() const { return m_heading; }
        const double pitch() const { return m_pitch; }
        const double roll() const { return m_roll; }
    public:
        AircraftAttitude withHeading(double newHeading) const { return AircraftAttitude(newHeading, m_pitch, m_roll); }
        AircraftAttitude withPitch(double newPitch) const { return AircraftAttitude(m_heading, newPitch, m_roll); }
        AircraftAttitude withRoll(double newRoll) const { return AircraftAttitude(m_heading, m_pitch, newRoll); }
    };

    struct Altitude
    {
    public:
        enum class Type
        {
            Ground = 0,
            AGL = 1,
            MSL = 2
        };
    private:
        float m_feet;
        Type m_type;
    private:
        Altitude(float _feet, Type _type) :
            m_feet(_feet),
            m_type(_type)
        {
        }
    public:
        const float feet() const { return m_feet; }
        Type type() const { return m_type; }
        bool isGround() const { return m_type == Type::Ground; }
        bool isGroundBased() const { return m_type == Type::Ground || m_type == Type::AGL; }
        string toString() const;
    public:
        static Altitude ground() { return Altitude(0, Type::Ground); }
        static Altitude agl(float feet) { return Altitude(feet, Type::AGL); }
        static Altitude msl(float feet) { return Altitude(feet, Type::MSL); }
    };

    /*
    struct Distance
    {
    private:
        double m_value;
        int m_ratio;
    public:
        Distance(double _value, int _ratio) :
            m_value(_value),
            m_ratio(_ratio)
        {
        }
    public:
        double value() { return m_value; }
        int ratio() { return m_ratio; }
    public:
        static Distance meters(double count) { return Distance(count, 10000); }
        static Distance feet(double count) { return Distance(count, 3048); }
        static Distance nauticalMiles(double count) { return Distance(count, 18520000); }
    };

    class Velocity
    { // a = (v1^2 - v0^2) / (2 s), s=distance
    private:
        Distance m_distanceUnit;
        chrono::microseconds m_timeUnit;
        double m_momentary;
        double m_acceleration;
        chrono::microseconds m_timestamp;
    public:
        Velocity(
            Distance _distanceUnit, 
            chrono::microseconds _timeUnit, 
            double _momentary, 
            chrono::microseconds _timestamp
        ) : m_distanceUnit(_distanceUnit),
            m_timeUnit(_timeUnit),
            m_momentary(_momentary),
            m_timestamp(_timestamp),
            m_acceleration(0.0)
        {
        }
    public:
        double momentary() const { return m_momentary; }
        double acceleration() const { return m_acceleration; }
        chrono::microseconds timestamp() const { return m_timestamp; }
    public:
        void progressTo(chrono::microseconds futureTimestamp);
        double calcDisplacement(chrono::microseconds futureTimestamp);
    public:
        static Velocity knotsAt(double value, chrono::microseconds timestamp) { 
            return Velocity(Distance::nauticalMiles(1), chrono::hours(1), value, timestamp);
        }
    };
    */

    template<class TKey, class TEntity>
    class EntityRef
    {
    private:
        friend class WorldBuilder;
    private:
        const TKey m_key;
        shared_ptr<TEntity> m_entity;
    public:
        EntityRef(const TKey& _key) : m_key(_key)
        {
        }
    public:
        const TKey& key() const
        {
            return m_key;
        }
        const shared_ptr<TEntity>& entity() const
        {
            return m_entity;
        }
        bool isResolved() const
        {
            return !!m_entity;
        }
        TEntity* operator->() const 
        {
            if (!m_entity)
            {
                throw runtime_error("EntityRef not resolved, cannot dereference");
            }
            return m_entity.get();
        }
    private:
        void resolve(shared_ptr<TEntity> _entity)
        {
            m_entity = _entity;
        }
    };

    class AirspaceGeometry
    {
    private:
        friend class WorldBuilder;
    private:
        GeoPolygon m_lateralBounds;
        bool m_hasLowerBound;
        float m_lowerBoundFeet;
        bool m_hasUpperBound;
        float m_upperBoundFeet;
    public:
        AirspaceGeometry(
            const GeoPolygon& _lateralBounds, 
            bool _hasLowerBound, 
            float _lowerBoundFeet,
            bool _hasUpperBound, 
            float _upperBoundFeet) :
            m_lateralBounds(_lateralBounds),
            m_hasLowerBound(_hasLowerBound),
            m_lowerBoundFeet(_lowerBoundFeet),
            m_hasUpperBound(_hasUpperBound),
            m_upperBoundFeet(_upperBoundFeet)
        {
        }
    public:
        const GeoPolygon& lateralBounds() const { return m_lateralBounds; }
        bool hasUpperBound() const { return m_hasUpperBound; }
        float upperBoundFeet() const { return m_upperBoundFeet; }
        bool hasLowerBound() const { return m_hasLowerBound; }
        float lowerBoundFeet() const { return m_lowerBoundFeet; }
    };

    class Runway
    {
    private:
        friend class WorldBuilder;
    public:
        typedef unsigned long Bitmask;
        class End
        {
        private:
            friend class WorldBuilder;
        private:
            string m_name; // name in HHS format: HH=heading/10 (e.g. 117 -> 12), S=suffix L/R/C/empty
            int m_number;  // runway number: heading/10
            char m_suffix; // L/R/C or 0 if none
            float m_displacedThresholdMeters;
            float m_overrunAreaMeters;
            UniPoint m_centerlinePoint;
            float m_heading; //exact heading
            shared_ptr<TaxiNode> m_centerlineNode;
            float m_elevationFeet;
        public:
            End(
                const string& _name,
                float _displacedThresholdMeters,
                float _overrunAreaMeters,
                const UniPoint& _centerlinePoint
            ) : m_name(_name),
                m_number(getRunwayEndNumber(_name)),
                m_suffix(getRunwayEndSuffix(_name)),
                m_displacedThresholdMeters(_displacedThresholdMeters),
                m_overrunAreaMeters(_overrunAreaMeters),
                m_centerlinePoint(_centerlinePoint),
                m_heading(0),
                m_elevationFeet(0)
            {
            }
        public:
            float heading() const { return m_heading; }
            const string& name() const { return m_name; }
            int number() const { return m_number; }
            char suffix() const { return m_suffix; }
            float displacedThresholdMeters() const { return m_displacedThresholdMeters; }
            float overrunAreaMeters() const { return m_overrunAreaMeters; }
            shared_ptr<TaxiNode> centerlineNode() const { return m_centerlineNode; }
            const UniPoint& centerlinePoint() const { return m_centerlinePoint; }
            float elevationFeet() const { return m_elevationFeet; }
        };
        struct Bounds
        {
            GeoPoint A;
            GeoPoint B;
            GeoPoint C;
            GeoPoint D;
            double minLatitude = 0;
            double maxLatitude = 0;
            double minLongitude = 0;
            double maxLongitude = 0;
        public:
            bool contains(const GeoPoint& p) const;
        };
    private:
        string m_name; // <end1_name>/<end2_name>
        float m_widthMeters;
        float m_lengthMeters;
        End m_end1; //the lesser heading end
        End m_end2; //the greater heading end
        vector<shared_ptr<TaxiEdge>> m_edges;
        Bitmask m_maskBit;
        Bounds m_bounds;
    public:
        Runway(const Runway::End& end1, const Runway::End& end2, float m_widthMeters);
    public:
        const string& name() const { return m_name; }
        const End& end1() const { return m_end1; }
        const End& end2() const { return m_end2; }
        const End& getEndOrThrow(const string& name);
        float widthMeters() const { return m_widthMeters; }
        float lengthMeters() const { return m_lengthMeters; }
        Bitmask maskBit() const { return m_maskBit; }
        const vector<shared_ptr<TaxiEdge>>& edges() const { return m_edges; }
        const Bounds& bounds() const { return m_bounds; }
    public:
        void calculateBounds();
    private:
        static int getRunwayEndNumber(const string& name);
        static char getRunwayEndSuffix(const string& name);
    };

    class World
    {
    private:
        friend class WorldBuilder;
    public:
        template<class T, class TKey>
        class EntityChangeSet
        {
        private:
            friend class World;
        private:
            vector<shared_ptr<T>> m_added;
            unordered_map<TKey, shared_ptr<T>> m_updated;
            vector<shared_ptr<T>> m_removed;
        public:
            EntityChangeSet()
            {
                static_assert(std::is_base_of<HaveKey<TKey>, T>::value, "T must implement HaveKey<TKey>");
            }
        public:
            const vector<shared_ptr<T>>& added() const { return m_added; }
            const unordered_map<TKey, shared_ptr<T>>& updated() const { return m_updated; }
            const vector<shared_ptr<T>>& removed() const { return m_removed; }
            bool empty() const { 
                return m_added.empty() && m_updated.empty() && m_removed.empty(); 
            }
        public:
            void added(shared_ptr<T> item) { 
                if (item)
                {
                    m_added.push_back(item); 
                }
            }
            void updated(shared_ptr<T> item) { 
                if (item)
                {
                    m_updated.emplace(item->getKey(), item); 
                }
            }
            void removed(shared_ptr<T> item) { 
                if (item)
                {
                    m_removed.push_back(item); 
                }
            }
        };
        class ChangeSet
        {
        private:
            friend class World;
        private:
            EntityChangeSet<Flight, int> m_flights;
            bool m_configurationChanged = false;
        public:
            bool empty() const { return m_flights.empty() && !m_configurationChanged; }
            bool configurationChanged() const { return m_configurationChanged; }
            const EntityChangeSet<Flight, int>& flights() const { return m_flights; }
        public:
            EntityChangeSet<Flight, int>& mutableFlights() { return m_flights; }
            void setConfigurationChanged() { m_configurationChanged = true; }
        };
        typedef function<shared_ptr<World::ChangeSet>()> OnChangesCallback;
        typedef function<float(const GeoPoint& location)> OnQueryElevationCallback;
    private:
        struct WorkItem
        {
            string description;
            chrono::microseconds timestamp;
            function<void()> callback;
        };
        typedef priority_queue<
            WorkItem, 
            vector<WorkItem>, 
            function<bool(const WorkItem&, const WorkItem&)>
        > WorkItemPriorityQueue;
    private:
        time_t m_startTime;
        unsigned long long m_heartbeatCount;
        chrono::microseconds m_lastHearbeatTimestamp;
        chrono::microseconds m_lastTimestampDelta;
        chrono::microseconds m_timestamp;
        WorkItemPriorityQueue m_workItemQueue;
        shared_ptr<ChangeSet> m_changeSet;
        shared_ptr<HostServices> m_host;
    private:
        vector<shared_ptr<ControlledAirspace>> m_airspaces;
        vector<shared_ptr<Airport>> m_airports;
        vector<shared_ptr<Flight>> m_flights;
        vector<shared_ptr<ControlFacility>> m_controlFacilities;
        unordered_map<int, shared_ptr<ControlledAirspace>> m_airspaceById;
        unordered_map<string, shared_ptr<Airport>> m_airportByIcao;
        unordered_map<int, shared_ptr<Flight>> m_flightById;
        OnQueryElevationCallback m_onQueryTerrainElevation;
    public:
        World(const shared_ptr<HostServices> _host, time_t _startTime) :
            m_startTime(_startTime), 
            m_timestamp(0),
            m_lastHearbeatTimestamp(0),
            m_heartbeatCount(0),
            m_host(_host),
            m_workItemQueue(compareWorkItems),
            m_changeSet(make_shared<ChangeSet>()),
            m_onQueryTerrainElevation(onQueryTerrainElevationUnassigned)
        {
        }
    public:
        void progressTo(chrono::microseconds futureTimestamp);
        void addFlight(shared_ptr<Flight> flight);
        void addFlightColdAndDark(shared_ptr<Flight> flight);
        void clearAllFlights();
        void clearWorkItems();
        void notifyConfigurationChanged();
        shared_ptr<World::ChangeSet> takeChanges();
        void deferUntilNextTick(const string& description, function<void()> callback);
        void deferUntil(const string& description, time_t time, function<void()> callback);
        void deferBy(const string& description, chrono::microseconds microseconds, function<void()> callback);
        // shared_ptr<ControlledAirspace> findAirspaceById(int id) const;
        shared_ptr<Flight> getFlightById(int id) const { return getValueOrThrow(m_flightById, id); }
        shared_ptr<Airport> getAirport(const string& icaoCode) const { return getValueOrThrow(m_airportByIcao, icaoCode); }
        shared_ptr<Runway> getRunway(const string& airportIcao, const string& runwayName) const;
        const Runway::End& getRunwayEnd(const string& airportIcao, const string& runwayName) const;
        shared_ptr<Frequency> tryFindCommFrequency(shared_ptr<Flight> flight, int frequencyKhz);
        float queryTerrainElevationAt(const GeoPoint& location) { return m_onQueryTerrainElevation(location); }
        bool detectAircraftInRect(
            const GeoPoint& topLeft,
            const GeoPoint& bottomRight,
            function<bool(shared_ptr<Aircraft> aircraft)> predicate);
    public:
        time_t startTime() const { return m_startTime; }
        chrono::microseconds timestamp() const { return m_timestamp; }
        time_t currentTime() const { return time_t(m_startTime + m_timestamp.count() / 1000000); }
        bool hasChanges() const { return !m_changeSet->empty(); }
        const vector<shared_ptr<ControlledAirspace>>& airspaces() const { return m_airspaces; }
        const vector<shared_ptr<Airport>>& airports() const { return m_airports; }
        const vector<shared_ptr<Flight>>& flights() const { return m_flights; }
        const vector<shared_ptr<ControlFacility>>& controlFacilities() const { return m_controlFacilities; }
    public:
        void onQueryTerrainElevation(OnQueryElevationCallback callback) { m_onQueryTerrainElevation = callback; }
    public:
        static shared_ptr<ChangeSet> onChangesUnassigned() { throw runtime_error("onChanges callback was not assigned"); }
    private:
        void processDueWorkItems();
        void processFlights();
        void processControlFacilities();
        void processHeartbeat();
    private:
        static bool compareWorkItems(const WorkItem& left, const WorkItem& right);
        static float onQueryTerrainElevationUnassigned(const GeoPoint&) { throw runtime_error("onQueryTerrainElevation callback was not assigned"); }
    };

    class Actor
    {
    public:
        enum class Nature
        {
            Unknown = 0,
            Human = 1,
            AI = 2
        };
        enum class Gender
        {
            Unknown = 0,
            Male = 1,
            Female = 2
        };
        enum class Role
        {
            Unknown = 0,
            Pilot = 1,
            Controller = 2
        };
        enum class VoiceType
        {
            Unknown = 0,
            Bass = 1,
            Baritone = 2,
            Tenor = 3,
            Countertenor = 4,
            Contralto = 5,
            MezzoSoprano = 6,
            Soprano = 7,
            Treble = 8
        };
        enum class SpeechRate
        {
            Unknown = 0,
            Slow = 1,
            Medium = 2,
            Fast = 3,
            MaxValue = 3
        };
        enum class RadioQuality
        {
            Unknown = 0,
            Poor = 1,
            Medium = 2,
            Good = 3,
            MaxValue = 3
        };
        struct SpeechStyle
        {   
        public:
            bool hasStyle;
            Gender gender;
            VoiceType voice;
            SpeechRate rate;
            float selfCorrectionProbability;
            float disfluencyProbability;
            chrono::milliseconds pttDelayBeforeSpeech;
            chrono::milliseconds pttDelayAfterSpeech;
            RadioQuality radioQuality;
            string platformVoiceId;
        };
    private:
        shared_ptr<HostServices> m_host;
        int m_id;
        string m_name;
        Nature m_nature;
        Role m_role;
        Gender m_gender;
    protected:
        SpeechStyle m_speechStyle;
    protected:
        Actor(
            shared_ptr<HostServices> _host,
            int _id,
            const string& _name,
            Nature _nature,
            Role _role,
            Gender _gender,
            const SpeechStyle& _speechStyle
        ) : m_host(_host),
            m_id(_id),
            m_name(_name),
            m_nature(_nature),
            m_role(_role),
            m_gender(_gender),
            m_speechStyle(_speechStyle)
        {
        }
        Actor(shared_ptr<HostServices> _host, int _id, Role _role, Gender _gender);
    public:
        shared_ptr<HostServices> host () const { return m_host; }
        int id() const { return m_id; }
        const string& name() const { return m_name; }
        Nature nature() const { return m_nature; }
        Role role() const { return m_role; }
        Gender gender() const { return m_gender; }
        const SpeechStyle& speechStyle() const { return m_speechStyle; }
    public:
        void setPlatformVoiceId(const string& voiceId) { m_speechStyle.platformVoiceId = voiceId; }
    private:
        void initRandomSpeechStyle();
    public:
        static const SpeechStyle& getDefaultSpeechStyle();
    };

    class ControllerPosition
    {
    private:
        friend class WorldBuilder;
    public:
        enum class Type
        {
            Unknown = 0,
            FlightData = 1,
            ClearanceDelivery = 2,
            Ground = 3,
            Local = 4,
            Departure = 5,            
            Approach = 6,
            Area = 7,
            Oceanic = 8
        };
        struct Structure
        {
            Type type;
            int frequencyKhz;
            GeoPolygon scopeLimit;
            string callSign;
        };
    private:
        shared_ptr<HostServices> m_host;
        Type m_type;
        shared_ptr<ControlFacility> m_facility;
        string m_callSign;
        shared_ptr<Frequency> m_frequency;
        vector<shared_ptr<Flight>> m_stripBoard;
        shared_ptr<RadarScope> m_radarScope;
        shared_ptr<Controller> m_controller;
        vector<shared_ptr<ControllerPosition>> m_handoffControllers;
    public:
        ControllerPosition(
            shared_ptr<HostServices> _host,
            Type _type,
            shared_ptr<ControlFacility> _facility,
            const string& _callSign,
            shared_ptr<Frequency> _frequency,
            shared_ptr<RadarScope> _radarScope
        ) : m_host(_host),
            m_type(_type),
            m_facility(_facility),
            m_callSign(_callSign),
            m_frequency(_frequency),
            m_radarScope(_radarScope)
        {
            startListenOnFrequency();
        }
    public:
        Type type() const { return m_type; }
        shared_ptr<ControlFacility> facility() const { return m_facility; }
        const string& callSign() const { return m_callSign; }
        shared_ptr<Controller> controller() const { return m_controller; }
        shared_ptr<Frequency> frequency() const { return m_frequency; }
        const vector<shared_ptr<Flight>>& stripBoard() const { return m_stripBoard; }
        shared_ptr<RadarScope> radarScope() const { return m_radarScope; }
        const vector<shared_ptr<ControllerPosition>>& handoffControllers() const { return m_handoffControllers; }
    public:
        void progressTo(chrono::microseconds timestamp);
        void clearFlights();
        void selectActiveRunways(vector<string>& departureRunways, vector<string>& arrivalRunways);
    private:
        void startListenOnFrequency();
    };

    class ControlFacility
    {
    private:
        friend class WorldBuilder;
    public:
        enum class Type
        {
            Unknown = 0,
            Tower = 1,
            Terminal = 2,
            Center = 3,
            Oceanic = 4
        };
    private:
        string m_callSign;
        string m_name;
        Type m_type;
        shared_ptr<ControlledAirspace> m_airspace;
        shared_ptr<Airport> m_airport;
        vector<shared_ptr<ControllerPosition>> m_positions;
        vector<shared_ptr<InformationService>> m_services;
    public:
        const string& callSign() const { return m_callSign; }
        const string& name() const { return m_name; }
        const Type type() const { return m_type; }
        const shared_ptr<ControlledAirspace> airspace() const { return m_airspace; }
        const shared_ptr<Airport> airport() const { return m_airport; }
        const vector<shared_ptr<ControllerPosition>>& positions() const { return m_positions; }
        const vector<shared_ptr<InformationService>>& services() const { return m_services; }
        shared_ptr<ControllerPosition> tryFindPosition(ControllerPosition::Type type, const GeoPoint& location) const;
        shared_ptr<ControllerPosition> findPositionOrThrow(ControllerPosition::Type type, const GeoPoint& location) const;
    public:
        void progressTo(chrono::microseconds timestamp);
        void clearFlights();
    };

    class ControlledAirspace
    {
    private:
        friend class WorldBuilder;
    public:
        enum class Type
        {
            Unspecified = 0,
            ControlZone = 1,
            ControlArea = 2,
            TerminalControlArea = 3,
            AreaFIR = 4,
            OceanicFIR = 5
        };
    private:
        int m_id;
        string m_areaCode;
        string m_icaoCode;
        string m_centerName; //TODO: what is this?
        string m_name;
        ControlledAirspace::Type m_type;
        const AirspaceClass& m_classification;
        shared_ptr<AirspaceGeometry> m_geometry;
        shared_ptr<ControlFacility> m_controllingFacility;
        shared_ptr<Airport> m_airport;
    public:
        ControlledAirspace(
            int _id,
            string _areaCode,
            string _icaoCode,
            string _centerName,
            string _name,
            ControlledAirspace::Type _type,
            const AirspaceClass& _classification,
            shared_ptr<AirspaceGeometry> _geometry) :
            m_id(_id),
            m_areaCode(_areaCode),
            m_icaoCode(_icaoCode),
            m_centerName(_centerName),
            m_name(_name),
            m_type(_type),
            m_classification(_classification),
            m_geometry(_geometry)
        {
        }
    public:
        int id() const { return m_id; }
        const string& areaCode() const { return m_areaCode; }
        const string& icaoCode() const { return m_icaoCode; }
        const string& centerName() const { return m_centerName;  }
        const string& name() const { return m_name; }
        ControlledAirspace::Type type() const { return m_type; }
        const AirspaceClass& classification() const { return m_classification; }
        const shared_ptr<AirspaceGeometry> geometry() const { return m_geometry; }
        const shared_ptr<ControlFacility> controllingFacility() const { return m_controllingFacility; }
        const shared_ptr<Airport> airport() const { return m_airport; }
    };

    class AirspaceClass
    {
    public:
        enum class Letter
        {
            Unknown = 0,
            A = 1,
            B = 2,
            C = 3,
            D = 4,
            E = 5,
            G = 6
        };
    public:
        const Letter letter;
    public:
        AirspaceClass(Letter _letter) : 
            letter(_letter)
        {
        }
    public:
        static const AirspaceClass ClassA;
        static const AirspaceClass ClassB;
        static const AirspaceClass ClassC;
        static const AirspaceClass ClassD;
        static const AirspaceClass ClassE;
        static const AirspaceClass ClassG;
    };

    class RadarScope
    {
    private:
        shared_ptr<ControlledAirspace> m_airspace;
        GeoPolygon m_scopeLimit;
    public:
        RadarScope(shared_ptr<ControlledAirspace> _airspace) :
            m_airspace(_airspace),
            m_scopeLimit({})
        {
        }
        RadarScope(shared_ptr<ControlledAirspace> _airspace, const GeoPolygon& _scopeLimit) :
            m_airspace(_airspace),
            m_scopeLimit(_scopeLimit)
        {
        }
    public:
        shared_ptr<ControlledAirspace> airspace() const { return m_airspace; }
        const GeoPolygon& scopeLimit() const { return m_scopeLimit; }
    };
    
    class Controller : public Actor
    {
    private:
        shared_ptr<ControllerPosition> m_position;
    public:
        Controller(
            shared_ptr<HostServices> _host,
            int _id,
            const string& _name,
            Nature _nature,
            Gender _gender,
            shared_ptr<ControllerPosition> _position
        ) : Actor(_host, _id, _name, _nature, Actor::Role::Controller, _gender, { false }),
            m_position(_position)
        {
        }
        Controller(
            shared_ptr<HostServices> _host,
            int _id,
            Gender _gender,
            shared_ptr<ControllerPosition> _position
        ) : Actor(_host, _id, Actor::Role::Controller, _gender),
            m_position(_position)
        {
        }
        virtual void selectActiveRunways(vector<string>& departure, vector<string>& arrival) { }
    public:
        shared_ptr<ControllerPosition> position() const { return m_position; }
        shared_ptr<ControlFacility> facility() const { return m_position->facility(); }
        shared_ptr<Airport> airport() const { return m_position->facility()->airport(); }
    public:
        virtual void receiveIntent(shared_ptr<Intent> intent) = 0;
        virtual void progressTo(chrono::microseconds timestamp) = 0;
        virtual void clearFlights() = 0;
    };

    class InformationService
    {
    public:
        enum class Type
        {
            Atis,
        };
    public:
        const Type type;
        const shared_ptr<Frequency> frequency;
    };

    class TextToSpeechService
    {
    public:
        typedef function<bool()> QueryCompletion;
    public:
        virtual QueryCompletion vocalizeTransmission(shared_ptr<Frequency> frequency, shared_ptr<Transmission> transmission) = 0;
        virtual void clearAll() = 0;
    public:
        static bool noopQueryCompletion()
        {
            return true;
        }
    };

    //TODO: abstract XPMP2
    class AircraftObjectService
    {
    public:
        virtual void processEvents(shared_ptr<World::ChangeSet> changeSet) = 0;
        virtual void clearAll() = 0;
    };

    class Intent
    {
    public:
        enum class Direction
        {
            Unknown = 0,
            PilotToController = 1,
            ControllerToPilot = 2,
            ControllerToController = 3,
            PilotToPilot = 4,
        };
        enum class Type
        {
            Unknown = 0,
            Question = 1,
            Information = 2,
            Affirmation = 3,
            Negation = 4,
            Request = 5,
            Report = 6,
            RequestApproval = 7,
            RequestRejection = 8,
            Clearance = 9,
            ClearanceReadback = 10,
            ClearanceUnable = 11
        };
        enum class ConversationState
        {
            End = 0,
            Continue = 1
        };
    private:
        uint64_t m_id;
        uint64_t m_replyToId;
        Direction m_direction;
        Type m_type;
        int m_code;
        ConversationState m_conversationState;
        shared_ptr<ControllerPosition> m_subjectControl;
        shared_ptr<ControllerPosition> m_subjectControl2;
        shared_ptr<Flight> m_subjectFlight;
        shared_ptr<Flight> m_subjectFlight2;
    protected:
        Intent(
            uint64_t _id,
            uint64_t _replyToId,
            Direction _direction,
            Type _type,
            int _code,
            ConversationState _conversationState,
            shared_ptr<ControllerPosition> _subjectControl,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl2 = nullptr,
            shared_ptr<Flight> _subjectFlight2 = nullptr
        ) : m_id(_id),
            m_replyToId(_replyToId),
            m_direction(_direction),
            m_type(_type),
            m_code(_code),
            m_conversationState(_conversationState),
            m_subjectControl(_subjectControl),
            m_subjectFlight(_subjectFlight),
            m_subjectControl2(_subjectControl2),
            m_subjectFlight2(_subjectFlight2)
        {
        }
    public:
        uint64_t id() const { return m_id; }
        uint64_t replyToId() const { return m_replyToId; }
        Direction direction() const { return m_direction; }
        Type type() const { return m_type; }
        int code() const { return m_code; }
        ConversationState conversationState() const { return m_conversationState; }
        shared_ptr<ControllerPosition> subjectControl() const { return m_subjectControl; }
        shared_ptr<ControllerPosition> subjectControl2() const { return m_subjectControl2; }
        shared_ptr<Flight> subjectFlight() const { return m_subjectFlight; }
        shared_ptr<Flight> subjectFlight2() const { return m_subjectFlight2; }
        bool isReply() const { return (m_replyToId > 0); }
        //const string& transmissionText() const { return m_transmissionText; }
    public:
        virtual bool isCritical() const { return false; }
        virtual shared_ptr<Actor> getSpeakingActor() const;
        //virtual void setTransmissionText(const string& text) { m_transmissionText = text; }
    };

    class Frequency : public enable_shared_from_this<Frequency>
    {
    private:
        friend class WorldBuilder;
    public:
        typedef function<void(shared_ptr<Intent> intent)> Listener;
        typedef function<void(shared_ptr<Transmission> transmission)> TransmissionCallback;
        typedef function<bool()> CancellationQueryCallback;
        struct PushToTalkAwaiter
        {
            int id;
            chrono::milliseconds silence;
            shared_ptr<Intent> intent;
            TransmissionCallback onTransmission;
            CancellationQueryCallback onQueryCancel;
        };
    private:
        shared_ptr<HostServices> m_host;
        int m_khz; //e.g. 118325
        GeoPoint m_antennaLocation;
        float m_radiusNm;
        long long m_nextTransmissionId;
        int m_nextListenerId;
        int m_nextPushToTalkId;
        list<PushToTalkAwaiter> m_regularAwaiters;
        list<PushToTalkAwaiter> m_criticalAwaiters;
        queue<shared_ptr<Transmission>> m_pendingTransmissions;
        shared_ptr<Transmission> m_transmissionInProgress;
        TextToSpeechService::QueryCompletion m_queryTransmissionCompletion;
        unordered_map<int, Listener> m_listenerById;
        weak_ptr<ControllerPosition> m_controllerPosition;
        uint64_t m_lastTransmittedIntentId;
        Intent::ConversationState m_lastConversationState;
        chrono::microseconds m_conversationStateExpiryTimestamp;
        chrono::microseconds m_lastTransmissionEndTimestamp;
    public:
        Frequency(
            shared_ptr<HostServices> _host,
            int _khz, 
            const GeoPoint& _antennaLocation, 
            float _radiusNm
        ) : m_host(_host),
            m_khz(_khz),
            m_antennaLocation(_antennaLocation),
            m_radiusNm(_radiusNm),
            m_nextTransmissionId(1),
            m_nextListenerId(1),
            m_nextPushToTalkId(1),
            m_queryTransmissionCompletion(TextToSpeechService::noopQueryCompletion),
            m_lastTransmittedIntentId(0),
            m_lastConversationState(Intent::ConversationState::End),
            m_lastTransmissionEndTimestamp(0),
            m_conversationStateExpiryTimestamp(0)
        {
        }
    public:
        int khz() const { return m_khz; }
        const GeoPoint& antennaLocation() const { return m_antennaLocation; }
        float radiusNm() const { return m_radiusNm; }
        shared_ptr<ControllerPosition> controllerPosition() { return m_controllerPosition.lock(); }
    public:
        void enqueuePushToTalk(
            chrono::milliseconds silence,
            const shared_ptr<Intent> intent,
            TransmissionCallback onTransmission = noopTRansmissionCallback,
            CancellationQueryCallback onQueryCancel = noopQueryCancelCallback);
        shared_ptr<Transmission> enqueueTransmission(const shared_ptr<Intent> intent);
        int addListener(Listener callback);
        void removeListener(int listenerId);
        void progressTo(chrono::microseconds timestamp);
        void clearTransmissions();
        bool wasSilentFor(chrono::milliseconds duration, uint64_t replyToId = 0);
    private:
        bool tryDequeueAwaiter(list<PushToTalkAwaiter>& queue, PushToTalkAwaiter& dequeued);
        void cancelAwaiter(PushToTalkAwaiter& awaiter);
        void beginTransmission(shared_ptr<Transmission> transmission, chrono::microseconds timestamp);
        void endTransmission(chrono::microseconds timestamp);
        void logTransmission(const string& message, shared_ptr<Transmission> transmission);
        void logIntent(const string& message, shared_ptr<Intent> intent);
        bool wasPushToTalkDequeued(int id);
        void checkConversationStateExpiry(chrono::microseconds timestamp);
    public:
        static void noopListener(shared_ptr<Intent> intent) { }
        static void noopTRansmissionCallback(shared_ptr<Transmission> transmission) { }
        static bool noopQueryCancelCallback() { return false; }
    };

    class Utterance
    {
    public:
        friend class UtteranceBuilder;
    public:
        enum class PartType
        {
            Unknown = 0,
            Punctuation = 1,
            Text = 2,
            Data = 3,
            Disfluency = 4,
            Correction = 5,
            Greeting = 6,
            Farewell = 7,
            Affirmation = 8,
            Negation = 9
        };
        // enum class DataType
        // {
        //     Unknown = 0,
        //     Callsign = 1,
        //     Frequency = 2,
        //     Taxi = 3,
        //     Altitude = 4
        // };
        // enum class Intonation
        // {
        //     Unknown = 0,
        //     Relaxed = 1,
        //     Neutral = 2,
        //     Stressed = 3
        // };
        struct Part
        {
            int startIndex;
            int length;
            PartType type;
        };
    private:
        string m_plainText;
        vector<Part> m_parts;
    public:
        const string& plainText() const { return m_plainText; }
        const vector<Part>& parts() const { return m_parts; }
    };

    class UtteranceBuilder
    {
    private:
        stringstream m_plainText;
        vector<Utterance::Part> m_parts;
    public:
        UtteranceBuilder();
    public:
        void addText(const string& text, bool slowDown = false);
        void addData(const string& text, bool slowDown = false);
        void addDisfluency(const string& text, bool skip = false);
        void addCorrection(const string& text, bool skip = false);
        void addGreeting(const string& text);
        void addFarewell(const string& text);
        void addAffirmation(const string& text);
        void addNegation(const string& text);
        void addPunctuation();
        shared_ptr<Utterance> getUtterance();
    private:
        Utterance::Part& addPart(const string& text, Utterance::PartType type);
    };

    class PhraseologyService
    {
    public:
        virtual shared_ptr<Utterance> verbalizeIntent(shared_ptr<Intent> intent) = 0;
    };

    class Transmission
    {
    private:
        friend class Frequency;
    public:
        enum class State
        {
            NotStarted = 0,
            InProgress = 1,
            Completed = 2,
            Cancelled = 3,
        };
    private:
        uint64_t m_id;
        shared_ptr<Intent> m_intent;
        State m_state;
        chrono::microseconds m_startTimestamp;
        chrono::microseconds m_endTimestamp;
        shared_ptr<Utterance> m_verbalizedUtterance;
    public:
        Transmission(
            uint64_t _id,
            shared_ptr<Intent> _intent
        ) : m_id(_id),
            m_intent(_intent),
            m_state(State::NotStarted),
            m_startTimestamp(0),
            m_endTimestamp(0)
        {
        }
    public:
        uint64_t id() const { return m_id; }
        shared_ptr<Intent> intent() const { return m_intent; }
        State state() const { return m_state; }
        chrono::microseconds startTimestamp() const { return m_startTimestamp; }
        chrono::microseconds endTimestamp() const { return m_endTimestamp; }
        shared_ptr<Utterance> verbalizedUtterance() const { return m_verbalizedUtterance; }
    public:
        void setVerbalizedUtterance(shared_ptr<Utterance> utterance) { m_verbalizedUtterance = utterance; }
    };


    class Clearance
    {
    public:
        enum class Type
        {   
            Unknown = 0,
            IfrClearance = 1,
            IfrReadbackConfirmation = 2,
            PushAndStartApproval = 3,
            DepartureTaxiClearance = 4,
            RunwayCrossClearance = 5,
            LineUpAndWait = 6,
            TakeoffClearance = 7,
            EnRouteClearance = 8,
            ApproachClearance = 9,
            LandingClearance = 10,
            ArrivalTaxiClearance = 11,
            GoAroundRequest = 12
        };
        struct Header
        {
            long long id;
            Type type;
            chrono::microseconds issuedTimestamp;
            shared_ptr<ControllerPosition> issuedBy;
            shared_ptr<Flight> issuedTo;
        };
    private:
        Header m_header;
    protected:
        Clearance(const Header& _header) :
            m_header(_header) 
        {
        }
        virtual ~Clearance() { } 
    public:
        const Header& header() const { return m_header; }
        long long id() const { return m_header.id; }
        Type type() const { return m_header.type; }
    };

    class Maneuver
    {
    public:
        enum class Type
        {
            Unspecified = 0,
            Flight = 1,
            DepartureAwaitIfrClearance = 5,
            DepartureAwaitPushback = 10,
            DeparturePushbackAndStart = 20,
            DepartureAwaitTaxi = 30,
            DepartureTaxi = 40,
            DepartureAwaitTakeOff = 50,
            DepartureLineUpAndWait = 45,
            DepartureTakeOffRoll = 60,
            DepartureAbortedTakeOff = 70,
            DepartureClimbInitialHeading = 80,
            DepartureClimbBySid = 90,
            DepartureClimbToTop = 100,
            FlyCruize = 110,
            ArrivalDescentFromTop = 120,
            ArrivalDescentByStar = 130,
            ArrivalApproach = 140,
            ArrivalHoldingPattern = 260,
            ArrivalGoAround = 270,
            ArrivalLanding = 150,
            ArrivalLandingRoll = 160,
            ArrivalVacateRunway = 165,
            ArrivalTaxi = 170,
            ArrivalParking = 180,
            TaxiByPath = 185,
            TaxiStraight = 190,
            TaxiTurn = 200,
            TaxiStop = 210,
            TaxiHoldShort = 230,
            FlyVector = 240,
            FlyDirect = 250,
            FlySpinOnBoundary = 280,
            Animation = 300,
            AwaitClearance = 310,
            AwaitSilenceOnFrequency = 311
        };
        enum class State
        {
            Unknown = 0,
            NotStarted = 1,
            InProgress = 2,
            Finished = 3
        };
        enum class SemaphoreState
        {
            NotInitialized = 0,
            Open = 1,
            Closed = 2
        };
        class ParameterDictionary
        {
        private:
            unordered_map<string, double> m_numeric;
            unordered_map<string, string> m_textual;
        public:
            const unordered_map<string, double>& numeric() const { return m_numeric; }
            const unordered_map<string, string>& textual() const { return m_textual; }
            unordered_map<string, double>& mutableNumeric() { return m_numeric; }
            unordered_map<string, string>& mutableTextual() { return m_textual; }
        };
    private:
        Type m_type;
        string m_id;
        shared_ptr<Maneuver> m_firstChild;
        shared_ptr<Maneuver> m_nextSibling;
        ParameterDictionary m_parameters;
    protected:
        chrono::microseconds m_startTimestamp;
        chrono::microseconds m_finishTimestamp;
        State m_state;
    protected:
        Maneuver(Type _type, const string& _id, const vector<shared_ptr<Maneuver>>& children);
    public:
        Type type() const { return m_type; }
        const string& id() const { return m_id; }
        shared_ptr<Maneuver> firstChild() const { return m_firstChild; }
        shared_ptr<Maneuver> nextSibling() const { return m_nextSibling; }
        State state() const { return m_state; }
        chrono::microseconds startTimestamp() const { return m_startTimestamp; }
        chrono::microseconds finishTimestamp() const { return m_finishTimestamp; }
        const ParameterDictionary& parameters() const { return m_parameters; }
        ParameterDictionary& mutableParameters() { return m_parameters; }
        virtual bool isProxy() const { return false; }
        //virtual chrono::microseconds expectedDuration() const = 0;
    public:
        virtual void progressTo(chrono::microseconds timestamp) = 0;
        virtual string getStatusString() const;
    private:
        virtual shared_ptr<Maneuver> unProxy() const { return nullptr; }
        void insertChildren(const vector<shared_ptr<Maneuver>>& children);
    public:
        static shared_ptr<Maneuver> unProxy(shared_ptr<Maneuver> source);
        static const char* getStateAcronym(State value);
    };

    class Aircraft
    {
    public:
        enum class Category
        {
            None = 0x0,
            Heavy = 0x01,
            Jet = 0x02,
            Turboprop = 0x04,
            Prop = 0x08,
            LightProp = 0x10,
            Helicopter = 0x20,
            Fighter = 0x40,
            All = 0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 | 0x40
        }; 
        enum class OperationType
        {
            None = 0x0,
            GA = 0x01,
            Airline = 0x02,
            Cargo = 0x04,
            Military = 0x08,
        };
        enum class LightBits
        {
            None = 0x0,
            Beacon = 0x1,
            Taxi = 0x2,
            Nav = 0x4,
            Strobe = 0x8,
            Landing = 0x10,
            BeaconTaxi = 0x1 | 0x2,
            BeaconNav = 0x1 | 0x4,
            BeaconTaxiNav = 0x1 | 0x2 | 0x4,
            BeaconTaxiNavStrobe = 0x1 | 0x2 | 0x4 | 0x8,
            BeaconNavStrobe = 0x1 | 0x4 | 0x8,
            BeaconLandingNavStrobe = 0x1 | 0x4 | 0x8 | 0x10
        };
        enum class TrackSyncMode
        {
            SyncToHeading = 0,
            DoNothing = 1
        };
    public:
        static constexpr float MaxAltitudeAGL = 300.0;
    private:
        shared_ptr<HostServices> m_host;
        int m_id;
        Actor::Nature m_nature;
        string m_modelIcao;
        string m_airlineIcao;
        string m_tailNo;
        Category m_category;
        weak_ptr<Flight> m_flight;
        World::OnChangesCallback m_onChanges;
        Frequency::Listener m_onCommTransmission;
        int m_frequencyKhz;
        shared_ptr<Frequency> m_frequency;
        int m_frequencyListenerId;
    protected:
        Aircraft(
            shared_ptr<HostServices> _host,
            int _id,
            Actor::Nature _nature,
            const string& _modelIcao,
            const string& _airlineIcao,
            const string& _tailNo,
            Category _category
        ) : m_host(_host),
            m_id(_id),
            m_nature(_nature),
            m_modelIcao(_modelIcao), 
            m_airlineIcao(_airlineIcao), 
            m_tailNo(_tailNo), 
            m_category(_category),
            m_onChanges(World::onChangesUnassigned),
            m_onCommTransmission(Frequency::noopListener),
            m_frequencyKhz(-1),
            m_frequencyListenerId(-1)
        {
            //setFrequencyKhz(FREQUENCY_UNICOM_1228);
        }
    public:
        int id() const { return m_id; }
        Actor::Nature nature() const { return m_nature; }
        const string& modelIcao () const { return m_modelIcao; }
        const string& airlineIcao () const { return m_airlineIcao; }
        const string& tailNo () const { return m_tailNo; }
        Category category () const { return m_category; }
        shared_ptr<Frequency> frequency() const { return m_frequency; }
        int frequencyKhz() const { return m_frequencyKhz; }
        shared_ptr<Flight> getFlightOrThrow();
    public:
        virtual void setFrequencyKhz(int _frequencyKhz);
        virtual void setFrequency(shared_ptr<Frequency> _frequency);
        virtual void assignFlight(shared_ptr<Flight> flight);
        virtual void progressTo(chrono::microseconds timestamp) { }
    public:
        virtual const GeoPoint& location() const = 0;
        virtual const AircraftAttitude& attitude() const = 0;
        virtual double track() const = 0;
        virtual const Altitude& altitude() const = 0;
        virtual double groundSpeedKt() const = 0;
        virtual double verticalSpeedFpm() const = 0;
        virtual const string& squawk() const = 0;
        virtual LightBits lights() const = 0;
        virtual bool isLightsOn(LightBits bits) const = 0;
        virtual float gearState() const = 0;
        virtual float flapState() const = 0;
        virtual float spoilerState() const = 0;
        virtual bool justTouchedDown(chrono::microseconds timestamp) = 0;
        virtual string getStatusString() { return "N/A"; }
    public:
        virtual void park(shared_ptr<ParkingStand> parkingStand) = 0;
        virtual void setOnFinal(const Runway::End& runwayEnd) = 0;
//        virtual void setLocation(const GeoPoint& _location) = 0;
//        virtual void setAttitude(const AircraftAttitude& _attitude, TrackSyncMode trackSync = TrackSyncMode::SyncToHeading) = 0;
//        virtual void setAltitude(const Altitude& _altitude) = 0;
//        virtual void setTrack(double _track) = 0;
//        virtual void setGroundSpeedKt(double kt) = 0;
//        virtual void setVerticalSpeedFpm(double fpm) = 0;
//        virtual void setGearState(float ratio) = 0;
//        virtual void setFlapState(float ratio) = 0;
//        virtual void setSpoilerState(float ratio) = 0;
//        virtual void setSquawk(const string& _squawk) = 0;
//        virtual void setLights(LightBits _lights) = 0;
    protected:
        shared_ptr<HostServices> host() const { return m_host; }
        weak_ptr<Flight> flight() const { return m_flight; }
        shared_ptr<World::ChangeSet> getWorldChangeSet() const;
        virtual void notifyChanges() = 0;
    public:
        void onChanges(World::OnChangesCallback callback) { m_onChanges = callback; }
        void onCommTransmission(Frequency::Listener callback) { m_onCommTransmission = callback; }
    };

    DECLARE_ENUM_BITWISE_OP(Aircraft::Category, |)
    DECLARE_ENUM_BITWISE_OP(Aircraft::Category, &)
    DECLARE_ENUM_BITWISE_OP(Aircraft::OperationType, |)
    DECLARE_ENUM_BITWISE_OP(Aircraft::OperationType, &)
    DECLARE_ENUM_BITWISE_OP(Aircraft::LightBits, |)
    DECLARE_ENUM_BITWISE_OP(Aircraft::LightBits, &)

    //TODO: move to libpilot
    // class AIAircraft : public Aircraft
    // {
    // private:
    //     LocalPoint m_location;
    //     Attitude m_attitude;
    //     string m_squawk;
    //     shared_ptr<Frequency> m_frequency;
    // public:
    //     void tuneTo(shared_ptr<Frequency> newFrequency)
    //     {
    //         m_frequency = newFrequency;
    //     }
    //     void squawk(const string& newSquawk)
    //     {
    //         m_squawk = newSquawk;
    //     }
    // };

    class FlightPlan
    {
    public:
        enum class LegType
        {
            Unknown = 0,
            TakeOff = 1,
            Sid = 2,
            EnRoute = 3,
            Star = 4,
            Approach = 5,
            Landing = 6,
            GoAround = 7,
            HoldingPattern = 8
        };
        class Leg
        {
        private:
            LegType m_type;
            GeoPolygon m_geometry;
            string m_fromNavaid;
            string m_toNavaid;
            float m_targetAltitude;
            float m_targetSpeed;
        public:
            Leg(
                LegType _type,
                const GeoPolygon& _geometry,
                const string& _fromNavaid,
                const string& _toNavaid,
                float _targetAltitude,
                float _targetSpeed
            ) : m_type(_type),
                m_geometry(_geometry),
                m_fromNavaid(_fromNavaid),
                m_toNavaid(_toNavaid),
                m_targetAltitude(_targetAltitude),
                m_targetSpeed(_targetSpeed)
            {
            }
        public:
            LegType type() const { return m_type; }
            const GeoPolygon& geometry() const { return m_geometry; }
            const string& fromNavaid() const { return m_fromNavaid; }
            const string& toNavaid() const { return m_toNavaid; }
            float targetAltitude() const { return m_targetAltitude; }
            float targetSpeed() const { return m_targetSpeed; }
        };
        
        class Cursor
        {
        private:
            shared_ptr<FlightPlan> m_flightPlan;
            shared_ptr<Leg> m_activeLeg;
        public:
            Cursor(shared_ptr<FlightPlan> _flightPlan) : 
                m_flightPlan(_flightPlan)
            {
            }
        public:
            // void activateNextLeg();
            // void directTo(const string& navaid);
        };
    private:
        time_t m_departureTime;
        time_t m_arrivalTime;
        string m_departureAirportIcao;
        string m_departureGate;
        string m_departureRunway;
        string m_arrivalAirportIcao;
        string m_arrivalRunway;
        string m_arrivalGate;
        string m_sidName;
        string m_sidTransition;
        string m_starName;
        string m_starTransition;
        string m_approachName;
        vector<shared_ptr<Leg>> m_legs;
        string m_airlineIcao;
        string m_flightNo;
        string m_callsign;
        //GeoPoint m_topOfClimb;
        //GeoPoint m_topOfDescent;
    public:
        FlightPlan(
            time_t _departureTime,
            time_t _arrivalTime,
            const string& _departureAirportIcao,
            const string& _arrivalAirportIcao
        ) :
            m_departureTime(_departureTime),
            m_arrivalTime(_arrivalTime),
            m_departureAirportIcao(_departureAirportIcao),
            m_arrivalAirportIcao(_arrivalAirportIcao)
        {
        }
    public:
        time_t departureTime() const { return m_departureTime; }
        time_t arrivalTime() const { return m_arrivalTime; }
        const string& departureAirportIcao() const { return m_departureAirportIcao; }
        const string& departureGate() const { return m_departureGate; }
        const string& departureRunway() const { return m_departureRunway; }
        const string& arrivalAirportIcao() const { return m_arrivalAirportIcao; }
        const string& arrivalGate() const { return m_arrivalGate; }
        const string& arrivalRunway() const { return m_arrivalRunway; }
        const string& sidName() const { return m_sidName; }
        const string& sidTransition() const { return m_sidTransition; }
        const string& starName() const { return m_starName; }
        const string& starTransition() const { return m_starTransition; }
        const string& approachName() const { return m_approachName; }
        const string& airlineIcao() const { return m_airlineIcao; }
        const string& flightNo() const { return m_flightNo; }
        const string& callsign() const { return m_callsign; }
        const vector<shared_ptr<Leg>>& legs() const { return m_legs; }
    public:
        void setDepartureAirportIcao(const string& icao) { m_departureAirportIcao = icao; }
        void setDepartureGate(const string& name) { m_departureGate = name; }
        void setDepartureRunway(const string& name) { m_departureRunway = name; }
        void setSid(const string& name) { m_sidName = name; }
        void setSidTransition(const string& name) { m_sidTransition = name; }
        void setStar(const string& name) { m_starName = name; }
        void setStarTransition(const string& name) { m_starTransition = name; }
        void setApproach(const string& name) { m_approachName = name; }
        void setArrivalRunway(const string& name) { m_arrivalRunway = name; }
        void setArrivalGate(const string& name) { m_arrivalGate = name; }
        void setArrivalAirportIcao(const string& icao) { m_arrivalAirportIcao = icao; }
        void setAirlineIcao(const string& icao) { m_airlineIcao = icao; }
        void setFlightNo(const string& value) { m_flightNo = value; }
        void setCallsign(const string& name) { m_callsign = name; }
    };

    class Flight : 
        public enable_shared_from_this<Flight>,
        public HaveKey<int>
    {
    public:
        enum class RulesType
        {
            VFR,
            CVFR,
            IFR
        };
        enum class Phase
        {
            NotAssigned = 0,
            Departure = 1,
            EnRoute = 2,
            Arrival = 3,
            TurnAround = 4
        };
    private:
        shared_ptr<HostServices> m_host;
        int m_id;
        RulesType m_rules;
        string m_airlineIcao;
        string m_flightNo;
        string m_callSign;
        shared_ptr<Pilot> m_pilot;
        shared_ptr<Aircraft> m_aircraft;
        shared_ptr<FlightPlan> m_plan;
        shared_ptr<FlightPlan::Cursor> m_planCursor;
        Phase m_phase;
        float m_landingRunwayElevationFeet;
        vector<shared_ptr<Clearance>> m_clearances;
        World::OnChangesCallback m_onChanges;
    public:
        Flight(
            shared_ptr<HostServices> _host,
            int _id,
            RulesType _rules,
            string _airlineIcao,
            string _flightNo,
            string _callSign,
            shared_ptr<FlightPlan> _plan);
    public:
        int id() const { return m_id; }
        const int& getKey() override { return m_id; }
        RulesType rules() const { return m_rules; }
        const string& airlineIcao() const { return m_airlineIcao; }
        const string& flightNo() const { return m_flightNo; }
        const string& callSign() const { return m_callSign; }
        shared_ptr<Aircraft> aircraft() const { return m_aircraft; }
        shared_ptr<Pilot> pilot() const { return m_pilot; }
        shared_ptr<FlightPlan> plan() const { return m_plan; }
        shared_ptr<FlightPlan::Cursor> planCursor() const { return m_planCursor; }
        Phase phase() const { return m_phase; }
        float landingRunwayElevationFeet();
    public:
        void setAircraft(shared_ptr<Aircraft> _aircraft);
        void setPilot(shared_ptr<Pilot> _pilot);
        void progressTo(chrono::microseconds timestamp);
        void addClearance(shared_ptr<Clearance> clearance);
        void setPlan(shared_ptr<FlightPlan> _plan);
        void setPhase(Phase newPhase) { m_phase = newPhase; }
    public:
        void onChanges(World::OnChangesCallback callback) { m_onChanges = callback; }
    public:
        template<class TClearance>
        shared_ptr<TClearance> tryFindClearance(Clearance::Type type)
        {
            return dynamic_pointer_cast<TClearance>(tryFindClearanceUncast(type));
        }
        template<class TClearance>
        shared_ptr<TClearance> findClearanceOrThrow(Clearance::Type type)
        {
            return dynamic_pointer_cast<TClearance>(findClearanceUncastOrThrow(type));
        }
    private:
        shared_ptr<Clearance> tryFindClearanceUncast(Clearance::Type type);
        shared_ptr<Clearance> findClearanceUncastOrThrow(Clearance::Type type);
    };

    class Pilot : public Actor
    {
    private:
        shared_ptr<Flight> m_flight;
        shared_ptr<Aircraft> m_aircraft;
    protected:
        Pilot(
            shared_ptr<HostServices> _host,
            int _id,
            const string& _name,
            Nature _nature,
            Gender _gender,
            shared_ptr<Flight> _flight,
            const Actor::SpeechStyle _speechStyle
        ) : Actor(_host, _id, _name, _nature, Actor::Role::Pilot, _gender, _speechStyle),
            m_flight(_flight),
            m_aircraft(_flight->aircraft())
        {
        }
        Pilot(
            shared_ptr<HostServices> _host,
            int _id,
            Gender _gender,
            shared_ptr<Flight> _flight
        ) : Actor(_host, _id, Actor::Role::Pilot, _gender),
            m_flight(_flight),
            m_aircraft(_flight->aircraft())
        {
        }
    public:
        shared_ptr<Flight> flight() const { return m_flight; }
        shared_ptr<Aircraft> aircraft() const { return m_aircraft; }
        virtual string getStatusString() const { return ""; }
    public:
        virtual shared_ptr<Maneuver> getFlightCycle() = 0;
        virtual shared_ptr<Maneuver> getFinalToGate(const Runway::End& landingRunway) = 0;
        virtual void progressTo(chrono::microseconds timestamp) = 0;
    };

    class Airport
    {
    private:
        friend class WorldBuilder;
    public:
        class Header
        {
        private:
            string m_icao;
            string m_name;
            GeoPoint m_datum;
            float m_elevation;
        public:
            Header(const string& _icao, const string& _name, const GeoPoint& _datum, float _elevation) : 
                m_icao(_icao), 
                m_name(_name),
                m_datum(_datum),
                m_elevation(_elevation)
            {
            }
        public:
            const string& icao() const { return m_icao; }
            const string& name() const { return m_name; }
            const GeoPoint& datum() const { return m_datum; }
            float elevation() const { return m_elevation; }
        };
    private:
        struct MutableState
        {
            vector<string> activeDepartureRunways;
            vector<string> activeArrivalRunways;
        };
    private:
        Header m_header;
        vector<shared_ptr<Runway>> m_runways;
        unordered_map<string, shared_ptr<Runway>> m_runwayByName;
        vector<shared_ptr<ParkingStand>> m_parkingStands;
        unordered_map<string, shared_ptr<ParkingStand>> m_parkingStandByName;
        shared_ptr<TaxiNet> m_taxiNet;
        shared_ptr<ControlFacility> m_tower;
        vector<vector<shared_ptr<Runway>>> m_parallelRunwayGroups;
        shared_ptr<MutableState> m_mutableState;
    public:
        Airport(const Header& _header) : 
            m_header(_header),
            m_mutableState(make_shared<MutableState>())
        {
        }
    public:
        const Header& header() const { return m_header; }
        const vector<shared_ptr<Runway>>& runways() const { return m_runways; }
        const vector<shared_ptr<ParkingStand>>& parkingStands() const { return m_parkingStands; }
        shared_ptr<TaxiNet> taxiNet() const { return m_taxiNet; }
        shared_ptr<ControlFacility> tower() const { return m_tower; }
        bool hasParallelRunways() const { return m_parallelRunwayGroups.size() > 0; }
        int parallelRunwayGroupCount() const { return m_parallelRunwayGroups.size(); }
        const vector<string>& activeDepartureRunways() const { return m_mutableState->activeDepartureRunways; }
        const vector<string>& activeArrivalRunways() const { return m_mutableState->activeArrivalRunways; }
        bool isRunwayActive(const string& runwayName) const;
    public:
        shared_ptr<Runway> getRunwayOrThrow(const string& name) const;
        const Runway::End& getRunwayEndOrThrow(const string& name) const;
        shared_ptr<Runway> tryFindRunway(const string& name) const;
        shared_ptr<ParkingStand> getParkingStandOrThrow(const string& name) const;
        shared_ptr<ParkingStand> tryFindParkingStand(const string& name) const;
        const vector<shared_ptr<Runway>>& getParallelRunwayGroup(int index) const { return m_parallelRunwayGroups.at(index); }
        shared_ptr<Runway> findLongestRunway() const;
        const vector<shared_ptr<Runway>>& findLongestParallelRunwayGroup() const;
        shared_ptr<ParkingStand> findClosestParkingStand(const GeoPoint& location);
    public:
        shared_ptr<ControllerPosition> getControllerPositionOrThrow(ControllerPosition::Type type, const GeoPoint& location) const {
            return m_tower->findPositionOrThrow(type, location);
        }
        shared_ptr<ControllerPosition> clearanceDeliveryAt(const GeoPoint& location) const {
            return m_tower->findPositionOrThrow(ControllerPosition::Type::ClearanceDelivery, location);
        }
        shared_ptr<ControllerPosition> groundAt(const GeoPoint& location) const {
            return m_tower->findPositionOrThrow(ControllerPosition::Type::Ground, location);
        }
        shared_ptr<ControllerPosition> localAt(const GeoPoint& location) const {
            return m_tower->findPositionOrThrow(ControllerPosition::Type::Local, location);
        }
        shared_ptr<ControllerPosition> departureAt(const GeoPoint& location) const {
            return m_tower->findPositionOrThrow(ControllerPosition::Type::Departure, location);
        }
        shared_ptr<ControllerPosition> approachAt(const GeoPoint& location) const {
            return m_tower->findPositionOrThrow(ControllerPosition::Type::Approach, location);
        }
    public:
        void selectActiveRunways();
        void selectArrivalAndDepartureTaxiways();
    private:
        void calculateActiveRunwaysBounds();
    };

    class ParkingStand
    {
    private:
        friend class WorldBuilder;
    public:
        enum class Type
        {
            Unknown = 0,
            Gate = 1,
            Remote = 2,
            Hangar = 3
        };
    private:
        int m_id;
        string m_name;
        Type m_type;
        UniPoint m_location;
        float m_heading;
        string m_widthCode;
        Aircraft::Category m_aircraftCategories;
        Aircraft::OperationType m_operationTypes;
        vector<string> m_airlines;
    public:
        ParkingStand(
            int _id,
            const string& _name, 
            ParkingStand::Type _type,
            const UniPoint& _location,
            float _heading,
            const string& _widthCode,
            Aircraft::Category _aircraftCategories = Aircraft::Category::None,
            Aircraft::OperationType _operationTypes = Aircraft::OperationType::None,
            const vector<string>& _airlines = {}) :
                m_id(_id),
                m_name(_name),
                m_type(_type),
                m_location(_location),
                m_heading(_heading),
                m_widthCode(_widthCode),
                m_aircraftCategories(_aircraftCategories),
                m_operationTypes(_operationTypes),
                m_airlines(_airlines)
        {
        }
    public:
        const int id() const { return m_id; }
        const string& name() const { return m_name; }
        const ParkingStand::Type type() const { return m_type; }
        const UniPoint& location() const { return m_location; }
        float heading() const { return m_heading; }
        const string& widthCode() const { return m_widthCode; }
        Aircraft::Category aircraftCategories() const { return m_aircraftCategories; }
        Aircraft::OperationType operationTypes() const { return m_operationTypes; }
        const vector<string>& airlines() const { return m_airlines; }
        bool hasAircraftCategory(Aircraft::Category category) const { 
            return ((m_aircraftCategories & category) == category);
        }
        bool hasOperationType(Aircraft::OperationType operation) const { 
            return ((m_operationTypes & operation) == operation);
        }
    };

    template<class T>
    class ClosestItemFinder
    {
    private:
        GeoPoint m_location;
        shared_ptr<T> m_closest;
        double m_minDistanceMetric = -1;
    public:
        ClosestItemFinder(const GeoPoint& _location) :
            m_location(_location)
        {
        }
    public:
        void next(const shared_ptr<T>& item)
        {
            const double distanceMetric =
                abs(m_location.latitude - item->location().latitude()) +
                abs(m_location.longitude - item->location().longitude());

            if (m_minDistanceMetric < 0 || distanceMetric < m_minDistanceMetric)
            {
                m_minDistanceMetric = distanceMetric;
                m_closest = item;
            }
        }
    public:
        const shared_ptr<T>& getClosest() const { return m_closest; }
    };

    class TaxiNet : public enable_shared_from_this<TaxiNet>
    {
    private:
        friend class WorldBuilder;
    public:
        vector<shared_ptr<TaxiNode>> m_nodes;
        vector<shared_ptr<TaxiEdge>> m_edges;
        unordered_map<int, shared_ptr<TaxiNode>> m_nodeById;
    public:
        TaxiNet(
            const vector<shared_ptr<TaxiNode>>& _nodes,
            const vector<shared_ptr<TaxiEdge>>& _edges
        ) : m_nodes(_nodes),
            m_edges(_edges)
        {
        }
    public:
        const vector<shared_ptr<TaxiNode>>& nodes() const { return m_nodes; }
        const vector<shared_ptr<TaxiEdge>>& edges() const { return m_edges; }
    public:
        shared_ptr<TaxiNode> getNodeById(int nodeId) const { return getValueOrThrow(m_nodeById, nodeId); };
        shared_ptr<TaxiNode> findClosestNode(const GeoPoint& location, function<bool(shared_ptr<TaxiNode>)> predicate) const;
        shared_ptr<TaxiNode> findClosestNode(
            const GeoPoint& location,
            const vector<shared_ptr<TaxiNode>>& possibleNodes) const;

//      void findNodesAheadOnRunway(
//            const GeoPoint& location,
//            const shared_ptr<Runway>& runway,
//            const Runway::End& runwayEnd,
//            vector<shared_ptr<TaxiNode>>& nodesAhead) const;

        shared_ptr<TaxiNode> findClosestNodeOnRunway(
            const GeoPoint& location,
            const shared_ptr<Runway>& runway,
            const Runway::End& runwayEnd) const;

//        shared_ptr<TaxiPath> tryFindArrivalPathRunwayToGate(
//            shared_ptr<HostServices> host,
//            shared_ptr<Runway> runway,
//            const Runway::End& runwayEnd,
//            shared_ptr<ParkingStand> gate,
//            const GeoPoint &fromPoint);

        shared_ptr<TaxiPath> tryFindDepartureTaxiPathToRunway(
            const GeoPoint& fromPoint,
            const Runway::End& toRunwayEnd);

        shared_ptr<TaxiPath> tryFindExitPathFromRunway(
            shared_ptr<HostServices> host,
            shared_ptr<Runway> runway,
            const Runway::End& runwayEnd,
            shared_ptr<ParkingStand> gate,
            const GeoPoint &fromPoint);

        shared_ptr<TaxiPath> tryFindTaxiPathToGate(
            shared_ptr<ParkingStand> gate,
            const GeoPoint &fromPoint);

        void assignFlightPhaseAllocation(shared_ptr<TaxiPath> path, Flight::Phase allocation);
    private:

        shared_ptr<TaxiEdge> tryFindExitFromRunway(
            shared_ptr<HostServices> host,
            shared_ptr<Runway> runway,
            const Runway::End& runwayEnd,
            const GeoPoint &fromPoint,
            float turnToGateDegrees) const;
    };

    struct ActiveZoneMask
    {
    private:
        friend class WorldBuilder;
    private:
        vector<string> m_runwayNames;
        Runway::Bitmask m_runwaysMask = 0;
    public:
        ActiveZoneMask() : 
            m_runwaysMask(0)
        {
        }
        bool hasAny() const {
            return (m_runwaysMask != 0);
        }
        bool has(shared_ptr<Runway> runway) const { 
            return (m_runwaysMask & runway->maskBit()) != 0; 
        }
        void add(const string& runwayName) {
            m_runwayNames.push_back(runwayName);
        }
        Runway::Bitmask runwaysMask() const {
            return m_runwaysMask;
        }
    };

    struct ActiveZoneMatrix
    {
    private:
        friend class WorldBuilder;
    public:
        ActiveZoneMask departue;
        ActiveZoneMask arrival;
        ActiveZoneMask ils;
    public:
        bool hasAny() const {
            return departue.hasAny() || arrival.hasAny() || ils.hasAny();
        }
    };

    class TaxiEdge
    {
    private:
        friend class WorldBuilder;
    public:
        enum class Type
        {
            Groundway = 0,
            Taxiway = 1,
            Runway = 2,
        };
    private:
        int m_id;
        Type m_type;
        bool m_isOneWay;
        string m_highSpeedExitRunway;
        string m_runwayEndName;
        string m_name;
        float m_lengthMeters;
        float m_heading;
        int m_nodeId1;
        int m_nodeId2;
        shared_ptr<TaxiNode> m_node1;
        shared_ptr<TaxiNode> m_node2;
        shared_ptr<Runway> m_runway;
        shared_ptr<TaxiEdge> m_flipOver;
        Flight::Phase m_flightPhaseAllocation;
        ActiveZoneMatrix m_activeZones;
    public:
        TaxiEdge(
            int _id,
            const string& _name,
            const int _nodeId1,
            const int _nodeId2,
            Type _type = Type::Taxiway,
            bool _isOneWay = false,
            float _lengthMeters = 0
        );
        TaxiEdge(const UniPoint& _fromPoint, const UniPoint& _toPoint);
        TaxiEdge(shared_ptr<TaxiEdge> _source, bool _flippingOver);
    public:
        int id() const { return m_id; }
        Type type() const { return m_type; }
        bool isOneWay() const { return m_isOneWay; }
        bool canFlipOver() const { return !m_isOneWay; }
        //const string& highSpeedExitRunway() const { return m_highSpeedExitRunway; }
        const string& name() const { return m_name; }
        float lengthMeters() const { return m_lengthMeters; }
        float heading() const { return m_heading; }
        int nodeId1() const { return m_nodeId1; }
        int nodeId2() const { return m_nodeId2; }
        shared_ptr<TaxiNode> node1() const { return m_node1; }
        shared_ptr<TaxiNode> node2() const { return m_node2; }
        shared_ptr<Runway> runway() const { return m_runway; }
        const ActiveZoneMatrix& activeZones() const { return m_activeZones; }
        Flight::Phase flightPhaseAllocation() const { return m_flightPhaseAllocation; }
    public:
        bool isRunway(const string& runwayEndName) const { return m_runwayEndName == runwayEndName; }
        bool isHighSpeedExitRunway(const string& runwayName) const { return m_highSpeedExitRunway == runwayName; }
        void setFlightPhaseAllocation(Flight::Phase allocation);
    public:
        static shared_ptr<TaxiEdge> flipOver(shared_ptr<TaxiEdge> source);
        static float calculateTaxiDistance(
            const shared_ptr<TaxiNode>& from, 
            const shared_ptr<TaxiNode>& to);
        static float calculateTaxiDistance(
            const UniPoint& from, 
            const UniPoint& to);
    };

    class TaxiNode
    {
    private:
        friend class WorldBuilder;
    public:
        int m_id;
        UniPoint m_location;
        bool m_isJunction;
        bool m_hasTaxiway;
        bool m_hasRunway;
        vector<shared_ptr<TaxiEdge>> m_edges;
        Flight::Phase m_flightPhaseAllocation;
    public:
        TaxiNode(int _id, const UniPoint& _location) :
            m_id(_id),
            m_location(_location),
            m_isJunction(false),
            m_hasTaxiway(false),
            m_hasRunway(false),
            m_flightPhaseAllocation(Flight::Phase::NotAssigned)
        {
        }
    public:
        int id() const { return m_id; }
        const UniPoint& location() const { return m_location; }
        bool isJunction() const { return m_isJunction; }
        bool hasTaxiway() const { return m_hasTaxiway; }
        bool hasRunway() const { return m_hasRunway; }
        Flight::Phase flightPhaseAllocation() const { return m_flightPhaseAllocation; }
        const vector<shared_ptr<TaxiEdge>>& edges() const { return m_edges; }
    public:
        shared_ptr<TaxiEdge> getEdgeTo(shared_ptr<TaxiNode> node);
        shared_ptr<TaxiEdge> tryFindEdge(function<bool(shared_ptr<TaxiEdge> edge)> predicate);
    public:
        void setFlightPhaseAllocation(Flight::Phase allocation) { m_flightPhaseAllocation = allocation; }
    };

    class TaxiPath
    {
    public:
        typedef function<float(shared_ptr<TaxiEdge> edge)> CostFunction;
    public:
        shared_ptr<TaxiNode> fromNode;
        shared_ptr<TaxiNode> toNode;
        vector<shared_ptr<TaxiEdge>> edges;
    public:
        TaxiPath(
            const shared_ptr<TaxiNode> _fromNode,
            const shared_ptr<TaxiNode> _toNode,
            const vector<shared_ptr<TaxiEdge>>& _edges);
    public:
        vector<string> toHumanFriendlySteps();
        string toHumanFriendlyString();
        void appendEdge(shared_ptr<TaxiEdge> edge);
        void appendEdgeTo(const UniPoint& destination);
    public:
        static shared_ptr<TaxiPath> find(
            shared_ptr<TaxiNet> net,
            shared_ptr<TaxiNode> from,
            shared_ptr<TaxiNode> to,
            CostFunction costFunction = lengthCostFunction);
        static shared_ptr<TaxiPath> tryFind(
            shared_ptr<TaxiNet> taxiNet,
            const GeoPoint& fromPoint,
            const GeoPoint& toPoint,
            CostFunction costFunction = lengthCostFunction);

        static float lengthCostFunction(shared_ptr<TaxiEdge> edge)
        {
            return edge->lengthMeters();
        }
    };

    class WorldBuilder
    {
    public:
        static shared_ptr<World> assembleSampleWorld(
            shared_ptr<HostServices> host, 
            const vector<shared_ptr<Airport>>& airports);

        static shared_ptr<Airport> assembleAirport(
            shared_ptr<HostServices> host,
            const Airport::Header& header,
            const vector<shared_ptr<Runway>>& runways,
            const vector<shared_ptr<ParkingStand>>& parkingStands,
            const vector<shared_ptr<TaxiNode>>& taxiNodes,
            const vector<shared_ptr<TaxiEdge>>& taxiEdges,
            shared_ptr<ControlFacility> tower = nullptr,
            shared_ptr<ControlledAirspace> airspace = nullptr);

        static shared_ptr<ControlFacility> assembleAirportTower(
            shared_ptr<HostServices> host, 
            const Airport::Header& header,
            shared_ptr<ControlledAirspace> airspace,
            const vector<ControllerPosition::Structure>& positions);

        static shared_ptr<ControlledAirspace> assembleSimpleAirspace(
            const AirspaceClass& classification,
            ControlledAirspace::Type type,
            const GeoPoint& centerPoint, 
            float radiusNm, 
            float lowerLimitFeet,
            float upperLimitFeet,
            const string& areaCode,
            const string& icaoCode,
            const string& centerName,
            const string& name);

        static shared_ptr<ControlledAirspace> assembleSampleAirportControlZone(const Airport::Header& header);

        static void addActiveZone(
            shared_ptr<TaxiEdge> edge, 
            const string& runwayName,
            bool departure,
            bool arrival,
            bool ils);

        static void tidyAirportElevations(
            shared_ptr<HostServices> host,
            shared_ptr<Airport> airport);

        static shared_ptr<TaxiNet> assembleTaxiNet(
            shared_ptr<HostServices> host,
            const vector<shared_ptr<Runway>>& runways,
            const vector<shared_ptr<TaxiNode>>& nodes,
            const vector<shared_ptr<TaxiEdge>>& edges);
    private:
        static void fixUpEdgesAndRunways(
            shared_ptr<HostServices> host,
            shared_ptr<Airport> airport);
        static void linkAirportTowerAirspace(
            shared_ptr<HostServices> host,
            shared_ptr<Airport> airport,
            shared_ptr<ControlFacility> tower,
            shared_ptr<ControlledAirspace> airspace);
        static int countLeadingDigits(const string& s);
    };

    class AIControllerFactory
    {
    public:
        virtual shared_ptr<Controller> createController(shared_ptr<ControllerPosition> position) = 0;
    };

    class AIPilotFactory
    {
    public:
        virtual shared_ptr<Pilot> createPilot(shared_ptr<Flight> flight) = 0;
    };

    class AIAircraftFactory
    {
    public:
        virtual shared_ptr<Aircraft> createAircraft(
            const string& modelIcao,
            const string& operatorIcao,
            const string& tailNo,
            Aircraft::Category category) = 0;
    };

    class HostServices
    {
    public:
        typedef chrono::time_point<chrono::high_resolution_clock, chrono::milliseconds> LogTimePoint;
    public:
        class ServicePtr
        {
        private:
            shared_ptr<void> m_ptr;
        public:
            ServicePtr(shared_ptr<void> _ptr) : m_ptr(_ptr) { } 
            template<class T> shared_ptr<T> getAs() { 
                return static_pointer_cast<T>(m_ptr);
            }
        };
        class ServiceContainer
        {
        private:
            unordered_map<string, ServicePtr> m_serviceByTypeKey;
        public:
            template<class TService>
            shared_ptr<TService> get()
            {
                string typeKey(typeid(TService).name());    
                ServicePtr ptr = getValueOrThrow(m_serviceByTypeKey, typeKey);
                shared_ptr<TService> typedPtr = ptr.getAs<TService>();
                return typedPtr;

                // {
                //     return servicePtr.getAs<TService>();
                // }
                // throw runtime_error("Service not found in container: " + typeKey);
            }
            template<class TService>
            void use(shared_ptr<TService> service)
            {
                string typeKey(typeid(TService).name());
                m_serviceByTypeKey.insert({ 
                    typeKey,
                    ServicePtr(service)
                });
            }
        };
    private:
        ServiceContainer m_services;
    public:
        ServiceContainer& services() { return m_services; }
    public:
        virtual shared_ptr<World> getWorld() = 0;
        virtual LocalPoint geoToLocal(const GeoPoint& geo) = 0;
        virtual GeoPoint localToGeo(const LocalPoint& local) = 0;
        virtual int getNextRandom(int maxValue) = 0;
        virtual float queryTerrainElevationAt(const GeoPoint& location) = 0;
        virtual shared_ptr<Controller> createAIController(shared_ptr<ControllerPosition> position) = 0;
        virtual shared_ptr<Pilot> createAIPilot(shared_ptr<Flight> flight) = 0;
        virtual shared_ptr<Aircraft> createAIAircraft(
            const string& modelIcao,
            const string& operatorIcao,
            const string& tailNo,
            Aircraft::Category category) = 0;
        virtual string getResourceFilePath(const vector<string>& relativePathParts) = 0;
        virtual string getHostFilePath(const vector<string>& relativePathParts) = 0;
        virtual vector<string> findFilesInHostDirectory(const vector<string>& relativePathParts) = 0;
        virtual shared_ptr<istream> openFileForRead(const string& filePath) = 0;
        virtual void showMessageBox(const string& title, const char *format, ...) = 0;
        virtual void writeLog(const char* format, ...) = 0;
    public:
        static LogTimePoint logStartTime;
        static void initLogString();
        static chrono::milliseconds getLogTimestamp();
        static void formatLogString(chrono::milliseconds timestamp, char logString[512], const char* format, va_list args);
    };
}

int libWorldFunc();
