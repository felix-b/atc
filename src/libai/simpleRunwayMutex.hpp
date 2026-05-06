//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <functional>
#include <memory>
#include <string>
#include <queue>
#include <utility>
#include <vector>
#include <unordered_set>
#include <chrono>
#include <sstream>
#include <algorithm>
#include <cctype>

#include "libworld.h"
#include "clearanceFactory.hpp"
#include "intentTypes.hpp"
#include "intentFactory.hpp"
#include "clearanceFactory.hpp"
#include "aiControllerBase.hpp"
#include "libai.hpp"
#include "resourceAllocator.hpp"

using namespace std;
using namespace world;

namespace ai
{
    class SimpleRunwayMutex;

    struct FlightStrip
    {
    public:
        struct Event
        {
        public:
            enum class Type {
                NotSet = 0,
                Continue = 10,
                HoldShort = 20,
                GoAround = 30,
                ClearedForTakeoff = 40,
                ClearedToLand = 50,
                AuthorizedLineUpAndWait = 60,
                ClearedToCross = 70
            };
            typedef function<void(const Event &event)> Listener;
        public:
            Event()
            {
            }
            Event(
                Type _type,
                shared_ptr<Flight>& _subject,
                int _numberInLine = 0,
                bool _immediate = false,
                DeclineReason _reason = DeclineReason::None,
                const vector<TrafficAdvisory>& _traffic = {}
            ) : type(_type),
                subject(_subject),
                numberInLine(_numberInLine),
                immediate(_immediate),
                reason(_reason),
                traffic(_traffic)
            {
            }
        public:
            Type type = Type::NotSet;
            shared_ptr<Flight> subject;
            int numberInLine = 0;
            bool immediate = false;
            DeclineReason reason = DeclineReason::None;
            vector<TrafficAdvisory> traffic;
        public:
            static Event holdShort(shared_ptr<Flight>& _subject, DeclineReason reason, const vector<TrafficAdvisory>& _traffic = {})
            {
                return Event(Type::HoldShort, _subject, 0, false, reason, _traffic);
            }
            static Event holdShort(shared_ptr<Flight>& _subject, DeclineReason reason, bool prepareForImmediate, const vector<TrafficAdvisory>& _traffic = {})
            {
                return Event(Type::HoldShort, _subject, 0, prepareForImmediate, reason, _traffic);
            }
            static Event Continue(shared_ptr<Flight>& _subject, int _numberInLine, const vector<TrafficAdvisory>& _traffic = {}, bool prepareForImmediate = false)
            {
                return Event(Type::Continue, _subject, _numberInLine, prepareForImmediate, DeclineReason::None, _traffic);
            }
            static Event clearedToLand(shared_ptr<Flight>& _subject, const vector<TrafficAdvisory>& _traffic = {})
            {
                return Event(Type::ClearedToLand, _subject, 1, false, DeclineReason::None, _traffic);
            }
            static Event clearedToCross(shared_ptr<Flight>& _subject, bool _withoutDelay = false, const vector<TrafficAdvisory>& _traffic = {})
            {
                return Event(Type::ClearedToCross, _subject, 0, _withoutDelay, DeclineReason::None, _traffic);
            }
            static Event clearedForTakeoff(shared_ptr<Flight>& _subject, bool _immediate = false, const vector<TrafficAdvisory>& _traffic = {})
            {
                return Event(Type::ClearedForTakeoff, _subject, 0, _immediate, DeclineReason::None, _traffic);
            }
            static Event authorizedLuaw(shared_ptr<Flight>& _subject, const vector<TrafficAdvisory>& _traffic = {})
            {
                return Event(Type::AuthorizedLineUpAndWait, _subject, 0, false, DeclineReason::None, _traffic);
            }
            static Event goAround(shared_ptr<Flight>& _subject, DeclineReason reason)
            {
                return Event(Type::GoAround, _subject, 0, false, reason);
            }
        };
    public:
        FlightStrip(shared_ptr<Flight> _flight, Event::Listener _listener) :
            flight(_flight),
            listener(std::move(_listener))
        {
        }
    public:
        shared_ptr<Flight> flight;
        Event::Listener listener;
    };

    typedef uint32_t RunwayStateFlagsType;
    enum RunwayStateFlags
    {
        RWY_STATE_VACATED = 0,
        RWY_STATE_CLEARED_LANDING = 0x01,
        RWY_STATE_CLEARED_TAKEOFF = 0x02,
        RWY_STATE_CLEARED_CROSSING = 0x04,
        RWY_STATE_AUTHORIZED_LUAW = 0x10,
    };

    struct RunwayStripBoard
    {
        RunwayStateFlagsType flags = RWY_STATE_VACATED;
        vector<shared_ptr<FlightStrip>> arrivalsLine;
        vector<shared_ptr<FlightStrip>> departuresLine;
        vector<shared_ptr<FlightStrip>> crossingsLine;
        shared_ptr<FlightStrip> clearedToLand;
        shared_ptr<FlightStrip> clearedToTakeoff;
        unordered_set<shared_ptr<FlightStrip>> clearedToCross;
        unordered_set<shared_ptr<FlightStrip>> crossing;
        shared_ptr<FlightStrip> authorizedLuaw;
    };

    class SimpleRunwayMutex
    {
    public:
        struct TimingThresholds
        {
            // arrival no factor
            int RWY_TIME_INFINITY = 360;
            // minimal time-to-threshold arrival must be cleared to land and the runway must be vacated - if not, arrival goes around
            int RWY_TIME_VACATED_BEFORE_LANDING_MIN = 15; //TODO -> RWY_TIME_CLEAR_BEFORE_LANDING_CRITICAL_MIN
            // time-to-threshold at which the arrival should normally be cleared for landing, when possible
            int RWY_TIME_CLEARED_BEFORE_LANDING_MIN = 90; //TODO -> RWY_TIME_CLEARED_BEFORE_LANDING_NORMAL
            // maximal time-to-threshold the arrival can be cleared for landing immediately when checking in with TWR
            int RWY_TIME_CLEARED_BEFORE_LANDING_MAX = 110;
            // maximal arrival time-to-threshold at which takeoff clearances must be "immediate"
            int RWY_TIME_IMMEDIATE_TAKEOFF_BEFORE_LANDING_MAX = 180;
            // minimal arrival time-to-threshold at which LUAW authorization is allowed
            int RWY_TIME_LUAW_AUTHORIZATION_BEFORE_LANDING_MIN = 120;
            // minimal arrival time-to-threshold at which takeoff clearance is allowed
            int RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN = 100;
            // minimal arrival time-to-threshold at which crossing clearance is allowed
            int RWY_TIME_CROSS_BEFORE_LANDING_MIN = 90;
            // maximal arrival time-to-threshold at which departing aircraft get traffic advisory on arrival
            int RWY_TIME_DEPARTURE_TRAFFIC_ADVISORY_MAX = 180;
            // maximal arrival time-to-threshold at which crossing aircraft get traffic advisory on arrival
            int RWY_TIME_CROSS_TRAFFIC_ADVISORY_MAX = 360;
            // minimal arrival time-to-threshold at which crossing clearance is prioritized over takeoff clearance for current LUAW
            int RWY_TIME_CROSS_OVER_LUAW_BEFORE_LANDING_MIN = 120;
        };
    private:
        shared_ptr<HostServices> m_host;
        shared_ptr<Runway> m_activeRunway;
        const Runway::End& m_activeRunwayEnd;
        const Runway::Bounds& m_activeRunwayBounds;
        TimingThresholds m_timing;
        RunwayStripBoard m_board;
        unordered_set<shared_ptr<Flight>> m_occupants;
        chrono::microseconds m_lastCheckTimestamp;
        shared_ptr<Flight> m_recentDepartureFlight;
        chrono::microseconds m_recentDepartureTimestamp;
    public:
        SimpleRunwayMutex(
            shared_ptr<HostServices> _host,
            shared_ptr<Runway> _activeRunway,
            const Runway::End& _activeRunwayEnd,
            const TimingThresholds& _timing,
            const RunwayStripBoard& _board
        ) : m_host(_host),
            m_activeRunway(_activeRunway),
            m_activeRunwayEnd(_activeRunwayEnd),
            m_activeRunwayBounds(_activeRunway->bounds()),
            m_timing(_timing),
            m_board(_board),
            m_lastCheckTimestamp(0),
            m_recentDepartureTimestamp(chrono::seconds(-1))
        {
            _activeRunway->calculateBounds();
        }

        void checkInArrival(shared_ptr<Flight> flight, FlightStrip::Event::Listener listener)
        {
            auto newEntry = checkIn(flight, listener, m_board.arrivalsLine, "arrival");
            onArrivalChecksIn(newEntry);
        }

        void checkInDeparture(shared_ptr<Flight> flight, FlightStrip::Event::Listener listener)
        {
            auto newEntry = checkIn(flight, listener, m_board.departuresLine, "departure");
            onDepartureChecksIn(newEntry);
        }

        void checkInCrossing(shared_ptr<Flight> flight, FlightStrip::Event::Listener listener)
        {
            auto newEntry = checkIn(flight, listener, m_board.crossingsLine, "crossing");
            onCrossingChecksIn(newEntry);
        }

        void progressTo(chrono::microseconds timestamp)
        {
            if ((timestamp - m_lastCheckTimestamp).count() >= 1300000)
            {
                m_lastCheckTimestamp = timestamp;
                performPeriodicCheck();
            }
        }

        void clearFlights()
        {
            m_occupants.clear();
            m_board.flags = RWY_STATE_VACATED;
            m_board.clearedToLand.reset();
            m_board.clearedToCross.clear();
            m_board.crossing.clear();
            m_board.clearedToTakeoff.reset();
            m_board.authorizedLuaw.reset();
            m_board.arrivalsLine.clear();
            m_board.departuresLine.clear();
            m_board.crossingsLine.clear();
            m_recentDepartureFlight.reset();
            m_recentDepartureTimestamp = chrono::seconds(-1);
        }

        shared_ptr<HostServices> host() const
        {
            return m_host;
        }

        const RunwayStripBoard& board() const
        {
            return m_board;
        }

        bool hasActiveOperations() const
        {
            // Return true when any traffic is checked in/queued
            // clearedToLand always counts as active operation
            if (m_board.clearedToLand)
            {
                return true;
            }
            // Any non-empty queue counts as active operation
            if (!m_board.arrivalsLine.empty())
            {
                return true;
            }
            if (!m_board.departuresLine.empty())
            {
                return true;
            }
            if (!m_board.crossingsLine.empty())
            {
                return true;
            }
            // Also check if runway is actively occupied
            if (!m_occupants.empty())
            {
                return true;
            }
            // Check if any aircraft are cleared for takeoff, crossing, or LUAW
            if (m_board.clearedToTakeoff || !m_board.clearedToCross.empty() || m_board.authorizedLuaw)
            {
                return true;
            }
            return false;
        }

    private:

        enum class WakeClass
        {
            Light = 0,
            Medium = 1,
            Heavy = 2,
            Super = 3
        };

        struct SeparationProfile
        {
            WakeClass wakeClass = WakeClass::Medium;
            float referenceArrivalSpeedKt = 145.0f;
            float departureRollSeconds = 35.0f;
            float lineupSeconds = 8.0f;
            float crossingSeconds = 18.0f;
        };

        static string uppercaseCopy(string value)
        {
            transform(value.begin(), value.end(), value.begin(), [](unsigned char c) {
                return static_cast<char>(toupper(c));
            });
            return value;
        }

        WakeClass inferWakeClass(shared_ptr<Flight> flight) const
        {
            if (!flight || !flight->aircraft())
            {
                return WakeClass::Medium;
            }

            const string modelIcao = uppercaseCopy(flight->aircraft()->modelIcao());
            if (modelIcao.rfind("A388", 0) == 0)
            {
                return WakeClass::Super;
            }

            static const vector<string> heavyPrefixes = {
                "A30", "A33", "A34", "A35", "A38", "B74", "B76", "B77", "B78", "C5", "DC1", "IL9", "MD1"
            };

            for (const auto& prefix : heavyPrefixes)
            {
                if (modelIcao.rfind(prefix, 0) == 0)
                {
                    return WakeClass::Heavy;
                }
            }

            const auto category = flight->aircraft()->category();
            if (category == Aircraft::Category::Heavy)
            {
                return WakeClass::Heavy;
            }
            if (category == Aircraft::Category::LightProp ||
                category == Aircraft::Category::Prop ||
                category == Aircraft::Category::Helicopter)
            {
                return WakeClass::Light;
            }

            return WakeClass::Medium;
        }

        SeparationProfile getSeparationProfile(shared_ptr<Flight> flight) const
        {
            SeparationProfile profile;
            profile.wakeClass = inferWakeClass(flight);

            switch (profile.wakeClass)
            {
            case WakeClass::Super:
                profile.referenceArrivalSpeedKt = 165.0f;
                profile.departureRollSeconds = 55.0f;
                profile.lineupSeconds = 14.0f;
                profile.crossingSeconds = 26.0f;
                break;
            case WakeClass::Heavy:
                profile.referenceArrivalSpeedKt = 155.0f;
                profile.departureRollSeconds = 45.0f;
                profile.lineupSeconds = 12.0f;
                profile.crossingSeconds = 24.0f;
                break;
            case WakeClass::Light:
                profile.referenceArrivalSpeedKt = 100.0f;
                profile.departureRollSeconds = 22.0f;
                profile.lineupSeconds = 6.0f;
                profile.crossingSeconds = 14.0f;
                break;
            case WakeClass::Medium:
            default:
                profile.referenceArrivalSpeedKt = 145.0f;
                profile.departureRollSeconds = 32.0f;
                profile.lineupSeconds = 8.0f;
                profile.crossingSeconds = 18.0f;
                break;
            }

            if (flight && flight->aircraft())
            {
                const auto actualGroundSpeed = static_cast<float>(flight->aircraft()->groundSpeedKt());
                if (actualGroundSpeed > 60.0f && !flight->aircraft()->altitude().isGround())
                {
                    profile.referenceArrivalSpeedKt = actualGroundSpeed;
                }
            }

            return profile;
        }

        float arrivalTimingScale(shared_ptr<FlightStrip> arrival) const
        {
            if (!arrival)
            {
                return 1.0f;
            }

            const auto profile = getSeparationProfile(arrival->flight);
            return min(1.35f, max(0.65f, profile.referenceArrivalSpeedKt / 145.0f));
        }

        float scaleArrivalTiming(float baseSeconds, shared_ptr<FlightStrip> arrival) const
        {
            return baseSeconds * arrivalTimingScale(arrival);
        }

        float wakeSeparationNm(WakeClass leader, WakeClass follower) const
        {
            if (leader == WakeClass::Super)
            {
                return follower >= WakeClass::Heavy ? 6.0f : 8.0f;
            }
            if (leader == WakeClass::Heavy)
            {
                return follower == WakeClass::Light ? 6.0f : 5.0f;
            }
            if (leader == WakeClass::Medium && follower == WakeClass::Light)
            {
                return 4.0f;
            }

            return 2.5f;
        }

        float wakeSeparationSeconds(WakeClass leader, WakeClass follower, float arrivalSpeedKt) const
        {
            const float speedKt = max(80.0f, arrivalSpeedKt);
            return wakeSeparationNm(leader, follower) * 3600.0f / speedKt;
        }

        float requiredTakeoffGapSeconds(shared_ptr<FlightStrip> departure, shared_ptr<FlightStrip> arrival) const
        {
            if (!departure || !arrival)
            {
                return 0.0f;
            }

            const auto departureProfile = getSeparationProfile(departure->flight);
            const auto arrivalProfile = getSeparationProfile(arrival->flight);
            return departureProfile.departureRollSeconds +
                wakeSeparationSeconds(
                    departureProfile.wakeClass,
                    arrivalProfile.wakeClass,
                    arrivalProfile.referenceArrivalSpeedKt) +
                5.0f;
        }

        float requiredLuawGapSeconds(shared_ptr<FlightStrip> departure, shared_ptr<FlightStrip> arrival) const
        {
            if (!departure)
            {
                return 0.0f;
            }

            const auto departureProfile = getSeparationProfile(departure->flight);
            return departureProfile.lineupSeconds + requiredTakeoffGapSeconds(departure, arrival);
        }

        float requiredCrossingGapSeconds(shared_ptr<FlightStrip> crossing, shared_ptr<FlightStrip> arrival) const
        {
            if (!crossing)
            {
                return 0.0f;
            }

            const auto crossingProfile = getSeparationProfile(crossing->flight);
            return crossingProfile.crossingSeconds + 8.0f;
        }

        bool hasRecentDepartureWakeConflict(
            shared_ptr<FlightStrip> arrival,
            DeclineReason& reason,
            vector<TrafficAdvisory>* traffic = nullptr) const
        {
            if (!arrival || !m_recentDepartureFlight || m_recentDepartureTimestamp.count() < 0)
            {
                return false;
            }

            const float elapsedSeconds = static_cast<float>((m_lastCheckTimestamp - m_recentDepartureTimestamp).count()) / 1000000.0f;
            if (elapsedSeconds < 0.0f)
            {
                return false;
            }

            const auto recentDepartureStrip = make_shared<FlightStrip>(m_recentDepartureFlight, FlightStrip::Event::Listener());
            const float requiredGapSeconds = requiredTakeoffGapSeconds(recentDepartureStrip, arrival);
            if (elapsedSeconds + getSecondsToTouchdown(arrival) >= requiredGapSeconds)
            {
                return false;
            }

            if (traffic)
            {
                traffic->push_back(TrafficAdvisory::departingAhead(m_recentDepartureFlight->aircraft()->modelIcao()));
            }
            reason = DeclineReason::WakeTurbulence;
            return true;
        }

        void rememberDepartureRoll(const shared_ptr<FlightStrip>& departure, bool approximateStart = false)
        {
            if (!departure || !departure->flight)
            {
                return;
            }

            if (m_recentDepartureFlight == departure->flight && m_recentDepartureTimestamp.count() >= 0)
            {
                return;
            }

            m_recentDepartureFlight = departure->flight;
            m_recentDepartureTimestamp = m_lastCheckTimestamp;

            if (approximateStart)
            {
                const auto profile = getSeparationProfile(departure->flight);
                const auto rollDuration = chrono::microseconds(static_cast<long long>(profile.departureRollSeconds * 1000000.0f));
                m_recentDepartureTimestamp = (m_lastCheckTimestamp > rollDuration)
                    ? (m_lastCheckTimestamp - rollDuration)
                    : chrono::microseconds(0);
            }
        }

        void performPeriodicCheck()
        {
            checkRunwayVacation();

            if (m_board.clearedToTakeoff && isDepartureStartedTakeoffRoll())
            {
                rememberDepartureRoll(m_board.clearedToTakeoff);
            }

            // detect cleared departure vacated
            if (m_board.clearedToTakeoff && isFeetAboveGround(m_board.clearedToTakeoff, 100))
            {
                auto vacated = m_board.clearedToTakeoff;
                rememberDepartureRoll(vacated, true);
                m_board.clearedToTakeoff.reset();
                m_board.flags &= ~RWY_STATE_CLEARED_TAKEOFF;

                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] departure [%s] vacated, now state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    vacated->flight->callSign().c_str(),
                    m_board.flags);
            }

            // detect cleared landing vacated
            if (m_board.clearedToLand && m_board.clearedToLand->flight->aircraft()->altitude().isGround())
            {
                if (!hasKey(m_occupants, m_board.clearedToLand->flight))
                {
                    auto vacated = m_board.clearedToLand;
                    m_board.clearedToLand.reset();
                    m_board.flags &= ~RWY_STATE_CLEARED_LANDING;

                    m_host->writeLog(
                        "AICONT|TWR-RWY-MUTEX[%s] arrival [%s] vacated, now state[0x%X]",
                        m_activeRunwayEnd.name().c_str(),
                        vacated->flight->callSign().c_str(),
                        m_board.flags);
                }
            }

            // detect all cleared crossing vacated
            if (!m_board.clearedToCross.empty())
            {
                // Collect stale entries first to avoid modifying sets while iterating.
                vector<shared_ptr<FlightStrip>> vacatedCrossings;
                for (const auto& crossingSubject : m_board.crossing)
                {
                    if (!hasKey(m_occupants, crossingSubject->flight))
                    {
                        vacatedCrossings.push_back(crossingSubject);
                    }
                }
                for (const auto& vacated : vacatedCrossings)
                {
                    m_board.clearedToCross.erase(vacated);
                    m_board.crossing.erase(vacated);

                    m_host->writeLog(
                        "AICONT|TWR-RWY-MUTEX[%s] crossing [%s] vacated, now state[0x%X]",
                        m_activeRunwayEnd.name().c_str(),
                        vacated->flight->callSign().c_str(),
                        m_board.flags);
                }

                for (const auto& crossingSubject : m_board.clearedToCross)
                {
                    if (hasKey(m_occupants, crossingSubject->flight))
                    {
                        m_board.crossing.insert(crossingSubject);
                    }
                }

                if (m_board.clearedToCross.empty())
                {
                    m_board.flags &= ~RWY_STATE_CLEARED_CROSSING;
                    m_board.crossing.clear();
                }
            }

            bool immediate = false;
            DeclineReason reason = DeclineReason::None;
            vector<TrafficAdvisory> traffic;

            shared_ptr<FlightStrip> numberOneForLanding = m_board.clearedToLand
                ? m_board.clearedToLand
                : !m_board.arrivalsLine.empty()
                    ? m_board.arrivalsLine.at(0)
                    : nullptr;
            float secondsToTouchdown = numberOneForLanding
                ? getSecondsToTouchdown(numberOneForLanding)
                : m_timing.RWY_TIME_INFINITY;
            const float crossingPriorityThreshold = (numberOneForLanding && !m_board.crossingsLine.empty())
                ? max(
                    scaleArrivalTiming((float)m_timing.RWY_TIME_CROSS_OVER_LUAW_BEFORE_LANDING_MIN, numberOneForLanding),
                    requiredCrossingGapSeconds(m_board.crossingsLine.at(0), numberOneForLanding))
                : (float)m_timing.RWY_TIME_INFINITY;
            const float landingClearThreshold = numberOneForLanding
                ? scaleArrivalTiming((float)m_timing.RWY_TIME_CLEARED_BEFORE_LANDING_MIN, numberOneForLanding)
                : (float)m_timing.RWY_TIME_INFINITY;
            const float landingVacatedThreshold = numberOneForLanding
                ? scaleArrivalTiming((float)m_timing.RWY_TIME_VACATED_BEFORE_LANDING_MIN, numberOneForLanding)
                : (float)m_timing.RWY_TIME_INFINITY;

            // if LUAW clear it for takeoff, unless someone wants to cross
            if (isLuawTheOnlyOccupant() &&
                (!numberOneForLanding || m_board.crossingsLine.empty() || secondsToTouchdown < crossingPriorityThreshold))
            {
                if (tryClearForTakeoff(m_board.authorizedLuaw, immediate, reason, traffic))
                {
                    m_board.authorizedLuaw->listener(FlightStrip::Event::clearedForTakeoff(m_board.authorizedLuaw->flight, immediate, traffic));
                    m_board.flags &= ~RWY_STATE_AUTHORIZED_LUAW;
                    m_board.authorizedLuaw.reset();
                }
                return;
            }

            // clear next arrival to land if close enough; if too close and cannot be cleared, go around
            if (numberOneForLanding && secondsToTouchdown <= landingClearThreshold)
            {
                bool numberOneWentAround = false;

                if (!m_board.clearedToLand && tryClearToLand(numberOneForLanding, reason, traffic))
                {
                    numberOneForLanding->listener(FlightStrip::Event::clearedToLand(numberOneForLanding->flight, traffic));
                }

                bool safe = isSafeToLand();
                const auto goAroundFlight = numberOneForLanding->flight;
                //TODO: ping occupants to start moving their tails
                // if (!safe) { ... }

                if (secondsToTouchdown < landingVacatedThreshold && !safe)
                {
                    if (reason == DeclineReason::None)
                    {
                        reason = DeclineReason::RunwayNotVacated;
                    }
                    m_host->writeLog(
                        "AICONT|TWR-RWY-MUTEX[%s] arrival [%s] stt[%d] GO AROUND reason[%d], state[0x%X]",
                        m_activeRunwayEnd.name().c_str(),
                        numberOneForLanding->flight->callSign().c_str(),
                        (int)secondsToTouchdown,
                        reason,
                        m_board.flags);

                    m_board.flags &= ~RWY_STATE_CLEARED_LANDING;
                    m_board.clearedToLand.reset();
                    if (!m_board.arrivalsLine.empty() &&
                        (numberOneForLanding == m_board.arrivalsLine.at(0) ||
                         m_board.arrivalsLine.at(0)->flight == goAroundFlight))
                    {
                        m_board.arrivalsLine.erase(m_board.arrivalsLine.begin());
                    }

                    numberOneWentAround = true;

                    numberOneForLanding->listener(FlightStrip::Event::goAround(
                        numberOneForLanding->flight,
                        reason));
                }

                // reject arrivals that got too close and weren't cleared to land
                while (!m_board.arrivalsLine.empty())
                {
                    auto arrival = m_board.arrivalsLine.at(0);
                    if (numberOneWentAround && arrival->flight == goAroundFlight)
                    {
                        m_board.arrivalsLine.erase(m_board.arrivalsLine.begin());
                        continue;
                    }

                    secondsToTouchdown = getSecondsToTouchdown(arrival);
                    if (secondsToTouchdown > scaleArrivalTiming((float)m_timing.RWY_TIME_VACATED_BEFORE_LANDING_MIN, arrival))
                    {
                        break;
                    }

                    reason = DeclineReason::RunwayNotVacated;
                    m_host->writeLog(
                        "AICONT|TWR-RWY-MUTEX[%s] arrival [%s] stt[%d] GO AROUND reason[%d], state[0x%X]",
                        m_activeRunwayEnd.name().c_str(),
                        arrival->flight->callSign().c_str(),
                        (int)secondsToTouchdown,
                        reason,
                        m_board.flags);
                    m_board.arrivalsLine.erase(m_board.arrivalsLine.begin());
                    arrival->listener(FlightStrip::Event::goAround(arrival->flight, reason));
                }
            }

            if (m_board.flags == RWY_STATE_VACATED || m_board.flags == (RWY_STATE_VACATED | RWY_STATE_AUTHORIZED_LUAW))
            {
                // vacated - who's next
                // clear to cross
                while (!m_board.crossingsLine.empty() && m_board.clearedToCross.size() < 5)
                {
                    auto nextToCross = m_board.crossingsLine.at(0);
                    if (tryClearToCross(nextToCross, immediate, reason, traffic))
                    {
                        nextToCross->listener(FlightStrip::Event::clearedToCross(nextToCross->flight, immediate, traffic));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // clear for takeoff or LUAW
            if (!m_board.departuresLine.empty())
            {
                auto numberOneForDeparture = m_board.departuresLine.at(0);

                if (tryClearForTakeoff(numberOneForDeparture, immediate, reason, traffic))
                {
                    numberOneForDeparture->listener(FlightStrip::Event::clearedForTakeoff(numberOneForDeparture->flight, immediate, traffic));
                }
                else if (tryAuthorizeLuaw(numberOneForDeparture, reason, traffic))
                {
                    numberOneForDeparture->listener(FlightStrip::Event::authorizedLuaw(numberOneForDeparture->flight, traffic));
                }
            }
        }

        bool checkRunwayVacation()
        {
            m_occupants.clear();

            for (const auto& flight : m_host->getWorld()->flights())
            {
                if (m_activeRunwayBounds.contains(flight->aircraft()->location()))
                {
                    if (!isFeetAboveGround(flight, 100))
                    {
                        m_occupants.insert(flight);
                    }
                }
            }

            return m_occupants.empty();
        }

        bool isFeetAboveGround(shared_ptr<FlightStrip> subject, float minFeet)
        {
            return isFeetAboveGround(subject->flight, minFeet);
        }

        bool isFeetAboveGround(shared_ptr<Flight> flight, float minFeet)
        {
            Altitude altitude = flight->aircraft()->altitude();
            float feetAgl = (altitude.isGroundBased()
                ? altitude.feet()
                : altitude.feet() - m_activeRunwayEnd.elevationFeet());
            return feetAgl >= minFeet;
        }

        bool isLuawTheOnlyOccupant()
        {
            if (m_occupants.size() != 1)
            {
                return false;
            }

            return m_board.authorizedLuaw && hasKey(m_occupants, m_board.authorizedLuaw->flight);
        }

        bool isDepartureStartedTakeoffRoll()
        {
            return m_board.clearedToTakeoff && m_board.clearedToTakeoff->flight->aircraft()->groundSpeedKt() > 45;
        }

        shared_ptr<FlightStrip> checkIn(
            const shared_ptr<Flight>& flight,
            const FlightStrip::Event::Listener& listener,
            vector<shared_ptr<FlightStrip>>& line,
            const string& lineName)
        {
            for (size_t i = 0; i < line.size(); ++i)
            {
                const auto& existing = line.at(i);
                if (existing && existing->flight == flight)
                {
                    existing->listener = listener;

                    m_host->writeLog(
                        "AICONT|TWR-RWY-MUTEX[%s] refreshed %s flight[%s] number-in-line[%d] rwy-state[0x%X]",
                        m_activeRunwayEnd.name().c_str(),
                        lineName.c_str(),
                        flight->callSign().c_str(),
                        static_cast<int>(i + 1),
                        m_board.flags);

                    return existing;
                }
            }

            line.push_back(make_shared<FlightStrip>(flight, listener));

            m_host->writeLog(
                "AICONT|TWR-RWY-MUTEX[%s] added %s flight[%s] number-in-line[%d] rwy-state[0x%X]",
                m_activeRunwayEnd.name().c_str(),
                lineName.c_str(),
                flight->callSign().c_str(),
                line.size(),
                m_board.flags);

            return line.at(line.size() - 1);
        }

        bool tryClearToLand(shared_ptr<FlightStrip> subject, DeclineReason& reason, vector<TrafficAdvisory>& traffic)
        {
            if (!isFirstInLine(subject, m_board.arrivalsLine))
            {
                reason = DeclineReason::NotFirstInLine;
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] WARNING: attempt to clear [%s] for landing: NOT FIRST IN LINE, state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    m_board.flags);
                return false;
            }

            bool isRunwayStateCompatible = (
                !m_board.clearedToLand &&
                m_board.clearedToCross.empty() &&
                (m_board.flags & (RWY_STATE_CLEARED_LANDING | RWY_STATE_CLEARED_CROSSING)) == 0);
            if (!isRunwayStateCompatible)
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to land: state[0x%X] INCOMPATIBLE WITH LANDING CLEARANCE",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    m_board.flags);
                reason = DeclineReason::RunwayNotVacated;
                return false;
            }

            const float secondsToTouchdown = getSecondsToTouchdown(subject);

            if (!checkRunwayVacation() || m_board.clearedToTakeoff || m_board.authorizedLuaw)
            {
                const bool departureRolling = m_board.clearedToTakeoff && isDepartureStartedTakeoffRoll();
                const bool runwayOnlyHasRollingDeparture =
                    departureRolling &&
                    m_occupants.size() <= 1 &&
                    (m_occupants.empty() || hasKey(m_occupants, m_board.clearedToTakeoff->flight));

                if (!runwayOnlyHasRollingDeparture || m_board.authorizedLuaw)
                {
                    if (m_occupants.size() > 0)
                    {
                        m_host->writeLog(
                            "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to land: runway occupied by [%s], state[0x%X]",
                            m_activeRunwayEnd.name().c_str(),
                            subject->flight->callSign().c_str(),
                            (*m_occupants.begin())->callSign().c_str(),
                            m_board.flags);
                    }
                    else
                    {
                        shared_ptr<FlightStrip> occupantToBe = m_board.clearedToTakeoff
                            ? m_board.clearedToTakeoff
                            : m_board.authorizedLuaw;
                        if (occupantToBe)
                        {
                            m_host->writeLog(
                                "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to land: [%s] cleared for takeoff/LUAW, state[0x%X]",
                                m_activeRunwayEnd.name().c_str(),
                                subject->flight->callSign().c_str(),
                                occupantToBe->flight->callSign().c_str(),
                                m_board.flags);
                        }
                    }

                    reason = DeclineReason::RunwayNotVacated;
                    return false;
                }

                const float requiredDepartureGap = max(
                    scaleArrivalTiming((float)m_timing.RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN, subject),
                    requiredTakeoffGapSeconds(m_board.clearedToTakeoff, subject));
                if (secondsToTouchdown < requiredDepartureGap || hasRecentDepartureWakeConflict(subject, reason, &traffic))
                {
                    if (reason == DeclineReason::None)
                    {
                        reason = requiredTakeoffGapSeconds(m_board.clearedToTakeoff, subject) >
                            scaleArrivalTiming((float)m_timing.RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN, subject)
                            ? DeclineReason::WakeTurbulence
                            : DeclineReason::TrafficDeparting;
                    }

                    if (traffic.empty())
                    {
                        traffic.push_back(TrafficAdvisory::departingAhead(m_board.clearedToTakeoff->flight->aircraft()->modelIcao()));
                    }

                    m_host->writeLog(
                        "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to land: [%s] departing ahead, stt[%f] need[%f], state[0x%X]",
                        m_activeRunwayEnd.name().c_str(),
                        subject->flight->callSign().c_str(),
                        m_board.clearedToTakeoff->flight->callSign().c_str(),
                        secondsToTouchdown,
                        requiredDepartureGap,
                        m_board.flags);
                    return false;
                }

                traffic.push_back(TrafficAdvisory::departingAhead(m_board.clearedToTakeoff->flight->aircraft()->modelIcao()));
            }
            else if (hasRecentDepartureWakeConflict(subject, reason, &traffic))
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to land: recent departure wake conflict, stt[%f], state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    secondsToTouchdown,
                    m_board.flags);
                return false;
            }

            m_board.flags |= RWY_STATE_CLEARED_LANDING;
            m_board.clearedToLand = subject;
            m_board.arrivalsLine.erase(m_board.arrivalsLine.begin());

            m_host->writeLog(
                "AICONT|TWR-RWY-MUTEX[%s] clearing [%s] to land",
                m_activeRunwayEnd.name().c_str(),
                subject->flight->callSign().c_str());

            reason = DeclineReason::None;
            return true;
        }

        bool tryClearToCross(shared_ptr<FlightStrip> subject, bool& withoutDelay, DeclineReason& reason, vector<TrafficAdvisory>& traffic)
        {
            RunwayStateFlagsType incompatibleFlags = RWY_STATE_CLEARED_TAKEOFF | RWY_STATE_CLEARED_LANDING | RWY_STATE_AUTHORIZED_LUAW;
            if ((m_board.flags & incompatibleFlags) != 0)
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to cross: state[0x%X] INCOMPATIBLE WITH CROSSING CLEARANCE",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    m_board.flags);
                reason = getDeclineReasonForCurrentState();
                return false;
            }

//            if (!isFirstInLine(subject, m_board.crossingsLine))
//            {
//                reason = DeclineReason::NotFirstInLine;
//                m_host->writeLog(
//                    "AICONT|TWR-RWY-MUTEX[%s] WARNING: attempt to clear [%s] for crossing: NOT FIRST IN LINE, state[0x%X]",
//                    m_activeRunwayEnd.name().c_str(),
//                    subject->flight->callSign().c_str(),
//                    m_board.flags);
//                return false;
//            }

            float secondsToTouchdown = m_timing.RWY_TIME_INFINITY;
            shared_ptr<FlightStrip> numberOneForLanding;

            if (!m_board.arrivalsLine.empty())
            {
                numberOneForLanding = m_board.arrivalsLine.at(0);
                secondsToTouchdown = getSecondsToTouchdown(numberOneForLanding);
            }

            const float requiredGap = numberOneForLanding
                ? max(
                    scaleArrivalTiming((float)m_timing.RWY_TIME_CROSS_BEFORE_LANDING_MIN, numberOneForLanding),
                    requiredCrossingGapSeconds(subject, numberOneForLanding))
                : 0.0f;

            if (numberOneForLanding && secondsToTouchdown <= requiredGap)
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] to cross: [%s] landing in [%f] sec need[%f], state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    numberOneForLanding->flight->callSign().c_str(),
                    secondsToTouchdown,
                    requiredGap,
                    m_board.flags);
                reason = DeclineReason::TrafficLanding;
                return false;
            }

            m_board.flags |= RWY_STATE_CLEARED_CROSSING;
            m_board.clearedToCross.insert(subject);
            {
                auto it = find(m_board.crossingsLine.begin(), m_board.crossingsLine.end(), subject);
                if (it != m_board.crossingsLine.end())
                {
                    m_board.crossingsLine.erase(it);
                }
            }
            withoutDelay = (
                (numberOneForLanding &&
                    secondsToTouchdown < scaleArrivalTiming((float)m_timing.RWY_TIME_CROSS_TRAFFIC_ADVISORY_MAX, numberOneForLanding)) ||
                m_board.authorizedLuaw);

            if (withoutDelay && numberOneForLanding)
            {
                traffic.push_back(TrafficAdvisory::onFinal(
                    numberOneForLanding->flight->aircraft()->modelIcao(),
                    getMilesOnFinal(numberOneForLanding)));
            }

            m_host->writeLog(
                "AICONT|TWR-RWY-MUTEX[%s] clearing [%s] to cross",
                m_activeRunwayEnd.name().c_str(),
                subject->flight->callSign().c_str());

            return true;
        }

        bool tryClearForTakeoff(shared_ptr<FlightStrip> subject, bool& immediate, DeclineReason& reason, vector<TrafficAdvisory>& traffic)
        {
            if (m_board.flags != RWY_STATE_VACATED && m_board.flags != RWY_STATE_AUTHORIZED_LUAW)
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] for takeoff: state[0x%X] INCOMPATIBLE WITH TAKEOFF CLEARANCE",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    m_board.flags);
                reason = getDeclineReasonForCurrentState();
                return false;
            }

            if (subject != m_board.authorizedLuaw && !isFirstInLine(subject, m_board.departuresLine))
            {
                reason = DeclineReason::NotFirstInLine;
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] WARNING: attempt to clear [%s] for takeoff: NOT FIRST IN LINE, state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    m_board.flags);
                return false;
            }

            float secondsToTouchdown = m_timing.RWY_TIME_INFINITY;
            shared_ptr<FlightStrip> numberOneForLanding;

            if (!m_board.arrivalsLine.empty())
            {
                numberOneForLanding = m_board.arrivalsLine.at(0);
                secondsToTouchdown = getSecondsToTouchdown(numberOneForLanding);
            }

            const float requiredWakeGap = numberOneForLanding
                ? requiredTakeoffGapSeconds(subject, numberOneForLanding)
                : 0.0f;
            const float requiredGap = numberOneForLanding
                ? max(
                    scaleArrivalTiming((float)m_timing.RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN, numberOneForLanding),
                    requiredWakeGap)
                : 0.0f;

            if (numberOneForLanding && secondsToTouchdown <= requiredGap)
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT CLEAR [%s] for takeoff: [%s] landing in [%f] sec need[%f], state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    numberOneForLanding->flight->callSign().c_str(),
                    secondsToTouchdown,
                    requiredGap,
                    m_board.flags);
                reason = requiredWakeGap > scaleArrivalTiming((float)m_timing.RWY_TIME_TAKEOFF_BEFORE_LANDING_MIN, numberOneForLanding)
                    ? DeclineReason::WakeTurbulence
                    : DeclineReason::TrafficLanding;
                return false;
            }

            m_board.flags |= RWY_STATE_CLEARED_TAKEOFF;
            m_board.clearedToTakeoff = subject;
            if (subject != m_board.authorizedLuaw)
            {
                m_board.departuresLine.erase(m_board.departuresLine.begin());
            }

            immediate = numberOneForLanding &&
                secondsToTouchdown < scaleArrivalTiming((float)m_timing.RWY_TIME_IMMEDIATE_TAKEOFF_BEFORE_LANDING_MAX, numberOneForLanding);

            if (numberOneForLanding && secondsToTouchdown < m_timing.RWY_TIME_INFINITY)
            {
                traffic.push_back(TrafficAdvisory::onFinal(
                    numberOneForLanding->flight->aircraft()->modelIcao(),
                    getMilesOnFinal(numberOneForLanding)));
            }

            m_host->writeLog(
                "AICONT|TWR-RWY-MUTEX[%s] clearing [%s] for takeoff",
                m_activeRunwayEnd.name().c_str(),
                subject->flight->callSign().c_str());

            return true;
        }

        bool tryAuthorizeLuaw(shared_ptr<FlightStrip> subject, DeclineReason& reason, vector<TrafficAdvisory>& traffic)
        {
            if (!isFirstInLine(subject, m_board.departuresLine))
            {
                reason = DeclineReason::NotFirstInLine;
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] WARNING: attempt to authorize [%s] for LUAW: NOT FIRST IN LINE, state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    m_board.flags);
                return false;
            }

            if (m_board.clearedToLand && !m_board.clearedToLand->flight->aircraft()->altitude().isGround())
            {
                reason = DeclineReason::TrafficLanding;
                traffic.push_back(TrafficAdvisory::onFinal(
                    m_board.clearedToLand->flight->aircraft()->modelIcao(),
                    getMilesOnFinal(m_board.clearedToLand)));
                return false;
            }

            if (m_board.authorizedLuaw)
            {
                reason = DeclineReason::WaitInLine;
                return false;
            }

            if (m_board.clearedToTakeoff && !isDepartureStartedTakeoffRoll())
            {
                reason = DeclineReason::WaitInLine;
                return false;
            }

            float secondsToTouchdown = m_timing.RWY_TIME_INFINITY;
            shared_ptr<FlightStrip> numberOneForLanding;

            if (!m_board.arrivalsLine.empty())
            {
                numberOneForLanding = m_board.arrivalsLine.at(0);
                secondsToTouchdown = getSecondsToTouchdown(numberOneForLanding);
            }

            const float requiredWakeGap = numberOneForLanding
                ? requiredLuawGapSeconds(subject, numberOneForLanding)
                : 0.0f;
            const float requiredGap = numberOneForLanding
                ? max(
                    scaleArrivalTiming((float)m_timing.RWY_TIME_LUAW_AUTHORIZATION_BEFORE_LANDING_MIN, numberOneForLanding),
                    requiredWakeGap)
                : 0.0f;

            if (numberOneForLanding && secondsToTouchdown < requiredGap)
            {
                m_host->writeLog(
                    "AICONT|TWR-RWY-MUTEX[%s] CONFLICT CANNOT AUTHORIZE [%s] for LUAW: [%s] landing in [%f] sec need[%f], state[0x%X]",
                    m_activeRunwayEnd.name().c_str(),
                    subject->flight->callSign().c_str(),
                    numberOneForLanding->flight->callSign().c_str(),
                    secondsToTouchdown,
                    requiredGap,
                    m_board.flags);
                reason = requiredWakeGap > scaleArrivalTiming((float)m_timing.RWY_TIME_LUAW_AUTHORIZATION_BEFORE_LANDING_MIN, numberOneForLanding)
                    ? DeclineReason::WakeTurbulence
                    : DeclineReason::TrafficLanding;
                return false;
            }

            m_board.flags |= RWY_STATE_AUTHORIZED_LUAW;
            m_board.authorizedLuaw = subject;
            m_board.departuresLine.erase(m_board.departuresLine.begin());

            if (!m_board.clearedToCross.empty())
            {
                traffic.push_back(TrafficAdvisory::crossingRunway());
            }

            if (numberOneForLanding)
            {
                traffic.push_back(TrafficAdvisory::onFinal(
                    numberOneForLanding->flight->aircraft()->modelIcao(),
                    getMilesOnFinal(numberOneForLanding)));
            }

            m_host->writeLog(
                "AICONT|TWR-RWY-MUTEX[%s] authorizing [%s] for LUAW",
                m_activeRunwayEnd.name().c_str(),
                subject->flight->callSign().c_str());

            return true;
        }

        void onArrivalChecksIn(shared_ptr<FlightStrip> subject)
        {
            vector<TrafficAdvisory> traffic;
            DeclineReason reason;
            float secondsToTouchdown = getSecondsToTouchdown(subject);
            const float clearanceWindow = scaleArrivalTiming((float)m_timing.RWY_TIME_CLEARED_BEFORE_LANDING_MAX, subject);

            // Always try to clear for landing immediately if within the max clearance window
            // and this is the first arrival in line. The periodic check will handle retries.
            if (secondsToTouchdown < clearanceWindow && isFirstInLine(subject, m_board.arrivalsLine))
            {
                if (tryClearToLand(subject, reason, traffic))
                {
                    subject->listener(FlightStrip::Event::clearedToLand(subject->flight, traffic));
                    return;
                }
            }

            if (!isFirstInLine(subject, m_board.arrivalsLine, m_board.clearedToLand.get()))
            {
                const auto& predecessor = m_board.arrivalsLine.size() > 1
                    ? m_board.arrivalsLine.at(m_board.arrivalsLine.size() - 2)
                    : m_board.clearedToLand;
                if (predecessor)
                {
                    double distanceMeters = GeoMath::getDistanceMeters(
                        subject->flight->aircraft()->location(),
                        predecessor->flight->aircraft()->location());

                    bool isLanded = predecessor->flight->aircraft()->altitude().isGround();
                    if (!isLanded)
                    {
                        traffic.push_back(TrafficAdvisory::landingAhead(
                            predecessor->flight->aircraft()->modelIcao(),
                            distanceMeters / METERS_IN_1_NAUTICAL_MILE));
                    }
                    else if (!m_board.authorizedLuaw)
                    {
                        traffic.push_back(TrafficAdvisory::landedOnRunway(
                            predecessor->flight->aircraft()->modelIcao()));
                    }
                }
            }

            if (m_board.authorizedLuaw)
            {
                traffic.push_back(TrafficAdvisory::holdingInPosition(m_board.authorizedLuaw->flight->aircraft()->modelIcao()));
            }

            if (traffic.empty() && m_board.clearedToTakeoff)
            {
                traffic.push_back(TrafficAdvisory::departingAhead(m_board.clearedToTakeoff->flight->aircraft()->modelIcao()));
            }

            if (traffic.empty() && !m_board.clearedToCross.empty())
            {
                traffic.push_back(TrafficAdvisory::crossingRunway());
            }

            int numberInLine = m_board.arrivalsLine.size();
            if (m_board.clearedToLand && !m_board.clearedToLand->flight->aircraft()->altitude().isGround())
            {
                numberInLine++;
            }
            subject->listener(FlightStrip::Event::Continue(subject->flight, numberInLine, traffic));
        }

        void onDepartureChecksIn(shared_ptr<FlightStrip> subject)
        {
            vector<TrafficAdvisory> traffic;
            DeclineReason reason = DeclineReason::None;
            bool immediate = false;

            if (!isFirstInLine(subject, m_board.departuresLine))
            {
                // assuming last in line
                subject->listener(FlightStrip::Event::Continue(
                    subject->flight, m_board.departuresLine.size(), {}, true));
            }
            else if (tryClearForTakeoff(subject, immediate, reason, traffic))
            {
                subject->listener(FlightStrip::Event::clearedForTakeoff(subject->flight, immediate, traffic));
            }
            else if (tryAuthorizeLuaw(subject, reason, traffic))
            {
                subject->listener(FlightStrip::Event::authorizedLuaw(subject->flight, traffic));
            }
            else
            {
                subject->listener(FlightStrip::Event::holdShort(subject->flight, reason, true));
            }
        }

        void onCrossingChecksIn(shared_ptr<FlightStrip> subject)
        {
            vector<TrafficAdvisory> traffic;
            DeclineReason reason = DeclineReason::None;
            bool withoutDelay = false;

            if (!isFirstInLine(subject, m_board.crossingsLine))
            {
                // Not first in line; hold short and wait for the periodic check to sequence clearances.
                subject->listener(FlightStrip::Event::Continue(
                    subject->flight, static_cast<int>(m_board.crossingsLine.size()), {}));
                return;
            }

            if (tryClearToCross(subject, withoutDelay, reason, traffic))
            {
                subject->listener(FlightStrip::Event::clearedToCross(subject->flight, withoutDelay, traffic));
            }
            else
            {
                subject->listener(FlightStrip::Event::holdShort(subject->flight, reason, traffic));
            }
        }

        void onMinClearanceTimeBeforeTouchdown(shared_ptr<FlightStrip> subject)
        {

        }

        void onMinVacatedTimeBeforeTouchdown(shared_ptr<FlightStrip> subject)
        {

        }

        void onDepartureVacates(shared_ptr<FlightStrip> subject)
        {

        }

        void onArrivalVacates(shared_ptr<FlightStrip> subject)
        {

        }

        void onCrossingVacates(shared_ptr<FlightStrip> subject)
        {

        }

        void onDepartureBeginsRoll(shared_ptr<FlightStrip> subject)
        {
            int x = 10;
        }

        bool isSafeToLand()
        {
            if (m_board.flags != RWY_STATE_CLEARED_LANDING || !m_board.clearedToLand)
            {
                return false;
            }

            if (m_occupants.empty())
            {
                return true;
            }

            if (m_occupants.size() > 1)
            {
                return false;
            }

            return hasKey(m_occupants, m_board.clearedToLand->flight);
        }

        DeclineReason getDeclineReasonForCurrentState()
        {
            if ((m_board.flags & RWY_STATE_CLEARED_LANDING) != 0)
            {
                return DeclineReason::TrafficLanding;
            }
            if ((m_board.flags & RWY_STATE_CLEARED_TAKEOFF) != 0)
            {
                return DeclineReason::TrafficDeparting;
            }
            if ((m_board.flags & RWY_STATE_AUTHORIZED_LUAW) != 0)
            {
                return DeclineReason::TrafficDeparting;
            }
            if ((m_board.flags & RWY_STATE_CLEARED_CROSSING) != 0)
            {
                return DeclineReason::TrafficCrossing;
            }
            return DeclineReason::None;
        }

//        void addTrafficForCurrentState(vector<TrafficAdvisory>& traffic)
//        {
//            if (m_board.clearedToLand)
//            {
//                float miles = getMilesOnFinal(m_board.clearedToLand);
//                if (miles > 0.5)
//                {
//                    traffic.push_back(TrafficAdvisory::onFinal(
//                        m_board.clearedToLand->flight->aircraft()->modelIcao(),
//                        miles));
//                }
//                else
//                {
//                    traffic.push_back(TrafficAdvisory::landing());
//                }
//            }
//            if ((m_board.flags & RWY_STATE_CLEARED_TAKEOFF) != 0)
//            {
//                return DeclineReason::TrafficDeparting;
//            }
//            if ((m_board.flags & RWY_STATE_CLEARED_CROSSING) != 0)
//            {
//                return DeclineReason::TrafficCrossing;
//            }
//            return DeclineReason::None;
//        }

        float getSecondsToTouchdown(shared_ptr<FlightStrip> strip) const
        {
            Altitude altitude = strip->flight->aircraft()->altitude();
            float verticalSpeedFpm = strip->flight->aircraft()->verticalSpeedFpm();

            // Aircraft already on ground have 0 seconds to touchdown
            if (altitude.isGround())
            {
                return 0.0f;
            }

            // Only descending aircraft can have a finite arrival time.
            // A climbing or level aircraft returns infinity so separation logic is not skewed.
            if (verticalSpeedFpm >= -0.001f)
            {
                return m_timing.RWY_TIME_INFINITY;
            }

            float feetAgl = altitude.isGroundBased()
                ? altitude.feet()
                : altitude.feet() - m_activeRunwayEnd.elevationFeet();

            // Don't allow negative AGL (below ground)
            if (feetAgl <= 0.0f)
            {
                return 0.0f;
            }

            float result = min(feetAgl * 60.0f / (-verticalSpeedFpm), (float)m_timing.RWY_TIME_INFINITY);
            return result < 0.001f ? m_timing.RWY_TIME_INFINITY : result;
        }

        float getMilesOnFinal(shared_ptr<FlightStrip> subject) const
        {
            double distanceMeters = GeoMath::getDistanceMeters(
                subject->flight->aircraft()->location(),
                m_activeRunwayEnd.centerlinePoint().geo());
            return min(distanceMeters / METERS_IN_1_NAUTICAL_MILE, 10.0);
        }

        bool isFirstInLine(
            const shared_ptr<FlightStrip>& subject,
            const vector<shared_ptr<FlightStrip>>& line,
            FlightStrip *cleared = nullptr)
        {
            return subject.get() == cleared || (!cleared && !line.empty() && line.at(0) == subject);
        }
    };
}
