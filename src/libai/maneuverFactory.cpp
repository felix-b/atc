// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#define _USE_MATH_DEFINES
#include <cmath>
#include <iostream>
#include <sstream>
#include <iomanip>
#include "libworld.h"
#include "basicManeuverTypes.hpp"
#include "maneuverFactory.hpp"
#include "clearanceFactory.hpp"
#include "intentFactory.hpp"
#include "aiAircraft.hpp"

using namespace std;
using namespace world;

namespace ai
{
    static shared_ptr<AIAircraft> getAIAircraft(shared_ptr<Flight> flight)
    {
        auto aircraft = dynamic_pointer_cast<AIAircraft>(flight->aircraft());
        if (aircraft)
        {
            return aircraft;
        }
        throw runtime_error("Flight [" + flight->callSign() + "]: not an AI aircraft");
    }
    
    // shared_ptr<Maneuver> ManeuverFactory::departureLineUpAndWait(shared_ptr<Flight> flight)
    // {
    //     auto airport = m_host->getWorld()->getAirport(flight->plan()->departureAirportIcao());
    //     auto gate = airport->getParkingStandOrThrow(flight->plan()->departureGate());
    //     auto runway = airport->getRunwayOrThrow(flight->plan()->departureRunway());
    //     auto runwayEnd = runway->getEndOrThrow(flight->plan()->departureRunway());

    // }

    shared_ptr<Maneuver> ManeuverFactory::taxiByPath(
        shared_ptr<Flight> flight, 
        shared_ptr<TaxiPath> path,
        TaxiType typeOfTaxi,
        HoldingShortCallback onHoldingShort)
    {
        const auto getTurnRadius = [typeOfTaxi](float fromHeading, float toHeading) {
            bool areSameSign = (fromHeading * toHeading >= 0);
            float angle = areSameSign
                ? abs(fromHeading - toHeading)
                : min(abs(fromHeading) + abs(toHeading), 360 - abs(fromHeading) - abs(toHeading));
            if (angle < 1)
            {
                return -1.0;
            }
            if (typeOfTaxi == TaxiType::Pushback)
            {
                return 0.00015;
            }
            if (angle < 15)
            {
                return 0.0004;
            }
            if (angle < 30)
            {
                return 0.0003;
            }
            return 0.0002;
            //return (angle > 1 ? 0.0002 /*  * (1 + angle/180)  */ : -1);
        };

        const auto calcRoundTurn = [=](
            shared_ptr<TaxiEdge> from, 
            shared_ptr<TaxiEdge> to,
            GeoMath::TurnData& turnData,
            GeoMath::TurnArc& turnArc
        ) {
            float turnRadius;
            if (!to || (turnRadius = getTurnRadius(from->heading(), to->heading())) <= 0)
            {
                return false;
            }
            turnData = {  
                from->node1()->location().geo(),
                from->node2()->location().geo(),
                GeoMath::headingToAngleRadians(from->heading()),
                to->node1()->location().geo(),
                to->node2()->location().geo(),
                GeoMath::headingToAngleRadians(to->heading()),
                turnRadius
            };
            m_host->writeLog("before calculateTurn");
            GeoMath::calculateTurn(turnData, turnArc, m_host);
            m_host->writeLog("after calculateTurn");
            return true;
        };

        const auto patchArcEndPoint = [](GeoMath::TurnArc& arc){
            GeoPoint patched(
                arc.arcCenter.latitude + arc.arcRadius * sin(arc.arcEndAngle),
                arc.arcCenter.longitude + arc.arcRadius * cos(arc.arcEndAngle),
                7777);
            return patched;
        };

        const auto addTaxiStep = [=](
            vector<shared_ptr<Maneuver>>& steps,
            GeoMath::TurnData& turnData,
            GeoMath::TurnArc& turnArc,
            shared_ptr<TaxiEdge> prevEdge,
            shared_ptr<TaxiEdge> edge,
            shared_ptr<TaxiEdge> nextEdge,
            bool exitRoundTurn,
            bool& enterRoundTurn
        ){
            stringstream logstr;
            logstr << setprecision(11) << endl;

            logstr << "---addTaxiStep(" << edge->node1()->id() << "->" << edge->node2()->id() << ")" << endl;
            logstr << edge->node1()->location().geo().latitude << ","
                 << edge->node1()->location().geo().longitude << "->"
                 << edge->node2()->location().geo().latitude << ","
                 << edge->node2()->location().geo().longitude
                 << (edge->activeZones().hasAny() ? " [AZ!]" : "") << endl;
            m_host->writeLog(logstr.str().c_str());

            if (edge->activeZones().hasAny() && (!prevEdge || !prevEdge->activeZones().hasAny()))
            {
                auto holdShortManeuver = onHoldingShort(edge);
                if (holdShortManeuver)
                {
                    steps.push_back(holdShortManeuver);
                }
            }

            const GeoPoint straightFromPoint = exitRoundTurn
                ? patchArcEndPoint(turnArc)
                : edge->node1()->location().geo();

            enterRoundTurn = calcRoundTurn(edge, nextEdge, turnData, turnArc);
            const GeoPoint straightToPoint = enterRoundTurn
                ? turnArc.p0
                : edge->node2()->location().geo();
            
            steps.push_back(taxiStraight(
                flight, 
                straightFromPoint,
                straightToPoint,
                typeOfTaxi
            ));

            if (enterRoundTurn)
            {
                float speedFactor = (typeOfTaxi == TaxiType::HighSpeed ? 10.0f : (typeOfTaxi == TaxiType::Pushback ? 1.0f : 6.0f));
                auto turnDuration = chrono::milliseconds((int)(1000 * turnArc.arcLengthMeters / speedFactor));
                steps.push_back(taxiTurn(flight, turnArc, turnDuration, typeOfTaxi));
            }
        };

        const auto& edges = path->edges;
        vector<shared_ptr<Maneuver>> steps;

        if (typeOfTaxi != TaxiType::Pushback && flight->aircraft()->location() != edges[0]->node1()->location().geo())
        {
            steps.push_back(taxiStraight(
                flight, 
                flight->aircraft()->location(),
                edges[0]->node1()->location().geo(),
                typeOfTaxi
            ));
        }

        GeoMath::TurnData turnData;
        GeoMath::TurnArc turnArc;
        bool exitingRoundTurn = false;
        bool enteredRoundTurn = false;

        for (int i = 0 ; i < edges.size() ; i++ )
        {
            exitingRoundTurn = enteredRoundTurn;
            enteredRoundTurn = false;
            addTaxiStep(
                steps, 
                turnData,
                turnArc,
                i > 0 ? edges[i-1] : nullptr,
                edges[i],
                i < edges.size() - 1 ? edges[i+1] : nullptr,
                exitingRoundTurn,
                enteredRoundTurn);
        }

        return shared_ptr<Maneuver>(new SequentialManeuver(
            Maneuver::Type::TaxiByPath,
            "",
            steps
        ));
    }

    shared_ptr<Maneuver> ManeuverFactory::taxiStraight(
        shared_ptr<Flight> flight, 
        const GeoPoint& from, 
        const GeoPoint& to, 
        TaxiType typeOfTaxi)
    {
        stringstream logstr;
        logstr << setprecision(11) << endl;

        logstr << "---taxiStraight---" << endl;
        logstr << "from=" << from.latitude << "," << from.longitude << endl;
        logstr << "to=" << to.latitude << "," << to.longitude << endl;

        float heading = (typeOfTaxi == TaxiType::Pushback
            ? GeoMath::getHeadingFromPoints(to, from)
            : GeoMath::getHeadingFromPoints(from, to));

        float distanceMeters = GeoMath::getDistanceMeters(from, to);

        logstr << "heading=" << heading << endl;
        m_host->writeLog(logstr.str().c_str());

        auto world = m_host->getWorld();
        float speedFactor = (typeOfTaxi == TaxiType::HighSpeed ? 12.0f : (typeOfTaxi == TaxiType::Pushback ? 1.0f : 6.0f));
        auto result = shared_ptr<Maneuver>(new AnimationManeuver<GeoPoint>(
            "", 
            from,
            to,
            chrono::milliseconds((int)(1000 * distanceMeters / speedFactor)),
            [=](const GeoPoint& from, const GeoPoint& to, double progress, GeoPoint& value) {
                value.latitude = from.latitude + progress * (to.latitude - from.latitude);
                value.longitude = from.longitude + progress * (to.longitude - from.longitude);
                value.altitude = 0;
            },
            [flight, heading, typeOfTaxi](const GeoPoint& value, double progress) {
                auto aircraft = getAIAircraft(flight);
                aircraft->setLocation(value);
                aircraft->setAttitude(aircraft->attitude().withHeading(heading));
            },
            [=](Maneuver::SemaphoreState previousState, chrono::microseconds totalWaitDuration) {
                return obstacleScanSemaphore(
                    world, flight, typeOfTaxi == TaxiType::Pushback, previousState, totalWaitDuration);
            }
        ));

        return result;
    }
    
    shared_ptr<Maneuver> ManeuverFactory::taxiTurn(
        shared_ptr<Flight> flight,
        const GeoMath::TurnArc& arc,
        chrono::microseconds duration,
        TaxiType typeOfTaxi)
    {
        stringstream logstr;
        logstr << setprecision(11) << endl;
        logstr << "---taxiTurn---" << endl;
        logstr << "from=" << arc.p0.latitude << "," << arc.p0.longitude << endl;
        logstr << "to=" << arc.p1.latitude << "," << arc.p1.longitude << endl;
        logstr << "arcCenter=" << arc.arcCenter.latitude << "," << arc.arcCenter.longitude << endl;
        logstr << "heading0=" << arc.heading0 << endl;
        logstr << "heading1=" << arc.heading1 << endl;
        logstr << "deltaAngle=" << arc.arcDeltaAngle << endl;

        double deltaAngle = arc.arcDeltaAngle;
        // double deltaAngle = (
        //     arc.arcStartAngle > M_PI_2 && arc.arcStartAngle <= M_PI && arc.arcEndAngle < -M_PI_2 && arc.arcEndAngle >= -M_PI
        //     ? M_PI - arc.arcStartAngle + arc.arcEndAngle + M_PI
        //     : arc.arcEndAngle - arc.arcStartAngle);
        
        float deltaHeading = arc.heading1 - arc.heading0;
        if (deltaHeading < 180)
        {
            deltaHeading += 360;
        }
        if (deltaHeading > 180)
        {
            deltaHeading -= 360;
        }
        //m_host->writeLog("taxiTurn-DELTA-HEADING> arc.heading0=%f, arc.heading1=%f, deltaHeading=%f", arc.heading0, arc.heading1, deltaHeading);
        logstr << "deltaHeading=" << deltaHeading << endl;
        m_host->writeLog(logstr.str().c_str());

        auto world = m_host->getWorld();
        auto result = shared_ptr<Maneuver>(new AnimationManeuver<double>(
            "", 
            arc.arcStartAngle,
            arc.arcEndAngle,
            duration,
            [deltaAngle](const double& from, const double& to, double progress, double& value) {
                value = from + progress * deltaAngle; 
            },
            [flight, arc, typeOfTaxi, deltaHeading](const double& value, double progress) {
                auto aircraft = getAIAircraft(flight);
                GeoPoint newLocation(
                    arc.arcCenter.latitude + arc.arcRadius * sin(value),
                    arc.arcCenter.longitude + arc.arcRadius * cos(value));
                int direction = 
                    (arc.arcClockwise ? -1 : 1) *
                    (typeOfTaxi == TaxiType::Pushback ? -1 : 1);
                //double headingProgress = progress + sin(M_PI * progress) / 4;
                double newHeading = arc.heading0 + progress * deltaHeading;
                if (typeOfTaxi == TaxiType::Pushback)
                {
                    newHeading = GeoMath::flipHeading(newHeading);
                }
                // double newHeading = GeoMath::radiansToHeading(
                //     value + 
                //     direction * GeoMath::pi() / 2);
                aircraft->setLocation(newLocation);
                aircraft->setAttitude(aircraft->attitude().withHeading(newHeading));
            },
            [=](Maneuver::SemaphoreState previousState, chrono::microseconds totalWaitDuration) {
                return obstacleScanSemaphore(
                    world, flight, typeOfTaxi == TaxiType::Pushback, previousState, totalWaitDuration);
            }
        ));

        return result;
    }
    
    shared_ptr<Maneuver> ManeuverFactory::taxiStop(shared_ptr<Flight> flight)
    {
        throw runtime_error("not implemented");
    }

    shared_ptr<Maneuver> ManeuverFactory::instantAction(function<void()> action, const string& id)
    {
        return shared_ptr<Maneuver>(new InstantActionManeuver(
            Maneuver::Type::Unspecified,
            id,
            action
        ));
    }

    shared_ptr<Maneuver> ManeuverFactory::delay(chrono::microseconds duration)
    {
        return deferred([=](){
            chrono::microseconds targetTimestamp = 
                m_host->getWorld()->timestamp() + 
                chrono::microseconds(duration.count());
        
            return await(Maneuver::Type::Unspecified, "delay", [=]() {
                return m_host->getWorld()->timestamp() >= targetTimestamp;
            });
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::deferred(
        Maneuver::Type type, 
        const string& id,
        DeferredManeuver::Factory factory)
    {
        return DeferredManeuver::create(type, id, factory);
    }

    shared_ptr<Maneuver> ManeuverFactory::deferred(DeferredManeuver::Factory factory)
    {
        return DeferredManeuver::create(Maneuver::Type::Unspecified, "", factory);
    }

    shared_ptr<Maneuver> ManeuverFactory::await(Maneuver::Type type, const string& id, function<bool()> isReady)
    {
        return shared_ptr<AwaitManeuver>(new AwaitManeuver(m_host, type, id, isReady));
    }

    shared_ptr<Maneuver> ManeuverFactory::awaitClearance(shared_ptr<Flight> flight, Clearance::Type clearanceType, const string& id)
    {
        return await(Maneuver::Type::AwaitClearance, id, [=]() {
            return !!flight->tryFindClearance<Clearance>(clearanceType);
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::sequence(Maneuver::Type type, const string& id, const vector<shared_ptr<Maneuver>>& steps)
    {
        return shared_ptr<SequentialManeuver>(new SequentialManeuver(
            type,
            id,
            steps
        ));
    }
    
    shared_ptr<Maneuver> ManeuverFactory::parallel(Maneuver::Type type, const string& id, const vector<shared_ptr<Maneuver>>& steps)
    {
        return shared_ptr<ParallelManeuver>(new ParallelManeuver(
            type,
            id,
            steps
        ));
    }

    shared_ptr<Maneuver> ManeuverFactory::switchLights(shared_ptr<Flight> flight, Aircraft::LightBits lights)
    {
        return instantAction([=](){
            getAIAircraft(flight)->setLights(lights);
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::tuneComRadio(shared_ptr<Flight> flight, int frequencyKhz)
    {
        return instantAction([=](){ 
            flight->aircraft()->setFrequencyKhz(frequencyKhz);
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::tuneComRadio(shared_ptr<Flight> flight, shared_ptr<Frequency> frequency)
    {
        return instantAction([=](){ 
            flight->aircraft()->setFrequency(frequency);
        });
    }

    struct BoxedTransmission
    {
        shared_ptr<Transmission> ptr;
        bool enqueuedPushToTalk = false;
    };

    static chrono::milliseconds getSilenceDurationBeforePushToTalk(
        const shared_ptr<Flight>& flight,
        const shared_ptr<Intent>& intent,
        int millisecondsOverride)
    {
        if (millisecondsOverride >= 0)
        {
            return chrono::milliseconds(millisecondsOverride);
        }

        if (intent->isReply())
        {
            return chrono::milliseconds(150);
        }

        if (intent->isCritical())
        {
            return chrono::milliseconds(1500);
        }

        return chrono::milliseconds(flight->phase() == Flight::Phase::Arrival ? 3000 : 4000);
    }

    shared_ptr<Maneuver> ManeuverFactory::transmitIntent(
        shared_ptr<Flight> flight,
        shared_ptr<Intent> intent,
        const string& id,
        int millisecondsSilence,
        Frequency::CancellationQueryCallback onQueryCancel)
    {
        auto boxedTransmission = shared_ptr<BoxedTransmission>(new BoxedTransmission());
        chrono::milliseconds silenceDuration = getSilenceDurationBeforePushToTalk(flight, intent, millisecondsSilence);
        string silenceAwaitId =
            flight->callSign() + "/" + to_string(silenceDuration.count()) + "-silence/" +
            to_string(flight->aircraft()->frequencyKhz());
        auto waitStartedAt = m_host->getWorld()->timestamp();

        return sequence(Maneuver::Type::Unspecified, id, {
            await(Maneuver::Type::AwaitSilenceOnFrequency, silenceAwaitId, [=](){
                auto frequency = flight->aircraft()->frequency();
                if (!frequency)
                {
                    return true;
                }
                if (onQueryCancel())
                {
                    m_host->writeLog(
                        "AIPILO|TRANSMISSION CANCELLED [%s]->[%s] intent code[%d]",
                        intent->subjectFlight()->callSign().c_str(),
                        intent->subjectControl()->callSign().c_str(),
                        intent->code());
                    return true;
                }
                if (!boxedTransmission->enqueuedPushToTalk)
                {
                    frequency->enqueuePushToTalk(silenceDuration, intent, [=](shared_ptr<Transmission> transmission) {
                        boxedTransmission->ptr = transmission;
                    }, onQueryCancel);
                    boxedTransmission->enqueuedPushToTalk = true;
                }
                bool result = !!boxedTransmission->ptr;
                return result;

//                auto elapsed = m_host->getWorld()->timestamp() - waitStartedAt;
//                int effectiveWaitMilliseconds = (elapsed.count() > 30000000 ? awaitSilenceMilliseconds / 2 : awaitSilenceMilliseconds);
//                auto frequency = flight->aircraft()->frequency();
//                bool result = !frequency || frequency->wasSilentFor(chrono::milliseconds(effectiveWaitMilliseconds));
//                return result;
            }),
            instantAction([=](){
                if (!flight->aircraft()->frequency())
                {
                    m_host->writeLog(
                        "AIPILO|TRANSMISSION ERROR from [%s] on [%d]: no ATC on this frequency",
                        flight->callSign().c_str(), 
                        flight->aircraft()->frequencyKhz());
                }
            }),
            await(Maneuver::Type::Unspecified, "await-transmit-end", [=](){
                return (!boxedTransmission->ptr ||
                    boxedTransmission->ptr->state() == Transmission::State::Completed ||
                    boxedTransmission->ptr->state() == Transmission::State::Cancelled);
            }),
            instantAction([=]() {
                if (!boxedTransmission->ptr)
                {
                    return;
                }
                bool completed = boxedTransmission->ptr->state() == Transmission::State::Completed;
                m_host->writeLog(
                    "AIPILO|TRANSMISSION %s [%s]->[%s] intent code[%d]",
                    completed ? "COMPLETED" : "CANCELLED",
                    intent->subjectFlight()->callSign().c_str(),
                    intent->subjectControl()->callSign().c_str(),
                    intent->code());
            })
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::airborneTurn(shared_ptr<Flight> flight, float fromHeading, float toHeading)
    {
        if (abs(fromHeading - toHeading) < 0.1)
        {
            return instantAction([](){});
        }

        auto aircraft = getAIAircraft(flight);
        float turnDegrees = GeoMath::getTurnDegrees(fromHeading, toHeading);
        int bankAngle = (abs(turnDegrees) > 15 ? 20 : 10) * (turnDegrees < 0 ? -1 : 1);
        int turnRate = abs(bankAngle) == 20 ? 2 : 1;
        auto turnDuration = chrono::milliseconds((int)(1000 * abs(turnDegrees) / turnRate));
        auto rollDuration = chrono::milliseconds(abs(bankAngle) == 20 ? 3000 : 2000);

        m_host->writeLog(
            "MANEUVER airborneTurn: from %f to %f = %f deg, bank %d, rate %d, Tturn=%lld ms, Tbank=%lld ms",
            fromHeading, toHeading, turnDegrees, bankAngle, turnRate, turnDuration.count(), rollDuration.count());

        auto rollIn = shared_ptr<Maneuver>(new AnimationManeuver<double>(
            "", 
            0,
            bankAngle,
            rollDuration,
            [](const double& from, const double& to, double progress, double& value) {
                value = from + (to - from) * progress; 
            },
            [=](const double& value, double progress) {
                aircraft->setAttitude(aircraft->attitude().withRoll(value));
            }
        ));
        auto rollOut = shared_ptr<Maneuver>(new AnimationManeuver<double>(
            "", 
            bankAngle,
            0,
            rollDuration,
            [](const double& from, const double& to, double progress, double& value) {
                value = from + (to - from) * progress; 
            },
            [=](const double& value, double progress) {
                aircraft->setAttitude(aircraft->attitude().withRoll(value));
            }
        ));
        auto turn = shared_ptr<Maneuver>(new AnimationManeuver<double>(
            "", 
            0,
            turnDegrees,
            turnDuration,
            [](const double& from, const double& to, double progress, double& value) {
                value = from + (to - from) * progress; 
            },
            [=](const double& value, double progress) {
                auto newHeading = GeoMath::addTurnToHeading(fromHeading, value);
                aircraft->setAttitude(aircraft->attitude().withHeading(newHeading));
            }
        ));

        return parallel(Maneuver::Type::Unspecified, "", {
            sequence(Maneuver::Type::Unspecified, "", {
                rollIn,
                delay(turnDuration - 2 * rollDuration),
                rollOut
            }),
            sequence(Maneuver::Type::Unspecified, "", {
                turn, // TODO: vary turn rate by bank angle
                instantAction([=]() {
                    aircraft->setAttitude(aircraft->attitude().withHeading(toHeading));
                })
            }),
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::noopOnHoldingShort(shared_ptr<TaxiEdge> atEdge)
    {
        return nullptr;
    }

    void ManeuverFactory::calculateObstacleScanRect(
        const GeoPoint& location,
        float heading,
        GeoPoint& topLeft,
        GeoPoint& bottomRight,
        float radiusMeters)
    {
        float radius = 0.00001 * radiusMeters;
        float halfRadius = radius / 2;
        int sectorIndex = getScanSectorIndex(heading);

        switch (sectorIndex)
        {
        case 0: // -22.5..+22.5
            topLeft = {location.latitude + radius, location.longitude - halfRadius };
            bottomRight = { location.latitude, location.longitude + halfRadius };
            break;
        case 1: // +22.5..+67.5
            topLeft = {location.latitude + radius, location.longitude };
            bottomRight = { location.latitude, location.longitude + radius };
            break;
        case 2:
            topLeft = {location.latitude + halfRadius, location.longitude };
            bottomRight = {location.latitude - halfRadius, location.longitude + radius };
            break;
        case 3:
            topLeft = { location.latitude, location.longitude };
            bottomRight = { location.latitude - radius, location.longitude + radius };
            break;
        case 4:
            topLeft = {location.latitude, location.longitude - halfRadius };
            bottomRight = { location.latitude - radius, location.longitude + halfRadius };
            break;
        case 5:
            topLeft = { location.latitude, location.longitude - radius };
            bottomRight = { location.latitude - radius, location.longitude };
            break;
        case 6:
            topLeft = {location.latitude + halfRadius, location.longitude - radius };
            bottomRight = { location.latitude - halfRadius, location.longitude };
            break;
        case 7:
            topLeft = {location.latitude + radius, location.longitude - radius };
            bottomRight = { location.latitude, location.longitude };
            break;
        }
    }

    Maneuver::SemaphoreState ManeuverFactory::obstacleScanSemaphore(
        shared_ptr<World> world,
        shared_ptr<Flight> ourFlight,
        bool isPushback,
        Maneuver::SemaphoreState previousState,
        chrono::microseconds closedStateTotalDuration)
    {
        auto ourAircraft = ourFlight->aircraft();
        auto ourPhase = ourFlight->phase();

        float scanRadiusMeters = previousState == Maneuver::SemaphoreState::Open ? 60 : 75;
        float ourHeading = isPushback
            ? GeoMath::flipHeading(ourAircraft->attitude().heading())
            : ourAircraft->attitude().heading();

        GeoPoint scanTopLeft;
        GeoPoint scanBottomRight;
        calculateObstacleScanRect(ourAircraft->location(), ourHeading, scanTopLeft, scanBottomRight, scanRadiusMeters);

        const auto isAircraftAnObstacle = [&](shared_ptr<Aircraft> otherAircraft) {
            if (otherAircraft == ourAircraft)
            {
                return false;
            }
            if (otherAircraft->nature() == Actor::Nature::Human)
            {
                return true;
            }

            auto otherPhase = otherAircraft->getFlightOrThrow()->phase();
            if (ourPhase != otherPhase)
            {
                return (ourPhase == Flight::Phase::Departure && otherPhase != Flight::Phase::TurnAround);
            }

            float headingToOther = GeoMath::getHeadingFromPoints(ourAircraft->location(), otherAircraft->location());
            float turnToOther = GeoMath::getTurnDegrees(ourHeading, headingToOther);
            float deltaHeading = GeoMath::getTurnDegrees(ourHeading, otherAircraft->attitude().heading());
            bool isSameDirection = abs(deltaHeading) < 60;
            bool isInFront = abs(turnToOther) < 60;
            //bool isBehind = abs(turnToOther) > 120;
            bool isHeadOn = abs(deltaHeading) > 165;

            if (isHeadOn)
            {
                return false;
            }
            if (isInFront && isSameDirection)
            {
                return true;
            }

            return (turnToOther > 0); //right-hand rule
        };

        bool obstaclesDetected = world->detectAircraftInRect(scanTopLeft, scanBottomRight, isAircraftAnObstacle);
        return obstaclesDetected
            ? Maneuver::SemaphoreState::Closed
            : Maneuver::SemaphoreState::Open;
    }

    int ManeuverFactory::getScanSectorIndex(float heading)
    {
        int sector = (int)((heading + 22.5) / 45.0) % 8;
        return sector;
    }
}
