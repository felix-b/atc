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

using namespace std;
using namespace world;

namespace ai
{

    
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
        bool isPushback,
        HoldingShortCallback onHoldingShort)
    {
        const auto getTurnRadius = [isPushback](float fromHeading, float toHeading) {
            bool areSameSign = (fromHeading * toHeading >= 0);
            float angle = areSameSign
                ? abs(fromHeading - toHeading)
                : min(abs(fromHeading) + abs(toHeading), 360 - abs(fromHeading) - abs(toHeading));
            if (angle < 1)
            {
                return -1.0;
            }
            if (isPushback)
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
                isPushback
            ));

            if (enterRoundTurn)
            {
                auto turnDuration = chrono::milliseconds((int)(1000 * turnArc.arcLengthMeters / (isPushback ? 1.0 : 6.0)));
                steps.push_back(taxiTurn(flight, turnArc, turnDuration, isPushback));
            }
        };

        const auto& edges = path->edges;
        vector<shared_ptr<Maneuver>> steps;

        if (!isPushback && flight->aircraft()->location() != edges[0]->node1()->location().geo())
        {
            steps.push_back(taxiStraight(
                flight, 
                flight->aircraft()->location(),
                edges[0]->node1()->location().geo(),
                isPushback
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
        bool isPushback)
    {
        stringstream logstr;
        logstr << setprecision(11) << endl;

        logstr << "---taxiStraight---" << endl;
        logstr << "from=" << from.latitude << "," << from.longitude << endl;
        logstr << "to=" << to.latitude << "," << to.longitude << endl;

        float heading = isPushback
            ? GeoMath::getHeadingFromPoints(to, from)
            : GeoMath::getHeadingFromPoints(from, to);

        float distanceMeters = GeoMath::getDistanceMeters(from, to);

        logstr << "heading=" << heading << endl;
        m_host->writeLog(logstr.str().c_str());

        auto result = shared_ptr<Maneuver>(new AnimationManeuver<GeoPoint>(
            "", 
            from,
            to,
            chrono::milliseconds((int)(1000 * distanceMeters / (isPushback ? 3.0 : 6.0))),
            [](const GeoPoint& from, const GeoPoint& to, double progress, GeoPoint& value) {
                value.latitude = from.latitude + progress * (to.latitude - from.latitude);
                value.longitude = from.longitude + progress * (to.longitude - from.longitude);
                value.altitude = 0;
            },
            [flight, heading](const GeoPoint& value, double progress) {
                auto aircraft = flight->aircraft();
                aircraft->setLocation(value);
                aircraft->setAttitude(aircraft->attitude().withHeading(heading));
            }
        ));

        return result;
    }
    
    shared_ptr<Maneuver> ManeuverFactory::taxiTurn(
        shared_ptr<Flight> flight,
        const GeoMath::TurnArc& arc,
        chrono::microseconds duration,
        bool isPushback)
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
            
        auto result = shared_ptr<Maneuver>(new AnimationManeuver<double>(
            "", 
            arc.arcStartAngle,
            arc.arcEndAngle,
            duration,
            [deltaAngle](const double& from, const double& to, double progress, double& value) {
                value = from + progress * deltaAngle; 
            },
            [flight, arc, isPushback, deltaHeading](const double& value, double progress) {
                auto aircraft = flight->aircraft();
                GeoPoint newLocation(
                    arc.arcCenter.latitude + arc.arcRadius * sin(value),
                    arc.arcCenter.longitude + arc.arcRadius * cos(value));
                int direction = 
                    (arc.arcClockwise ? -1 : 1) *
                    (isPushback ? -1 : 1);
                //double headingProgress = progress + sin(M_PI * progress) / 4;
                double newHeading = arc.heading0 + progress * deltaHeading;
                if (isPushback)
                {
                    newHeading = GeoMath::flipHeading(newHeading);
                }
                // double newHeading = GeoMath::radiansToHeading(
                //     value + 
                //     direction * GeoMath::pi() / 2);
                aircraft->setLocation(newLocation);
                aircraft->setAttitude(aircraft->attitude().withHeading(newHeading));
            }
        ));

        return result;
    }
    
    shared_ptr<Maneuver> ManeuverFactory::taxiStop(shared_ptr<Flight> flight)
    {
        throw runtime_error("not implemented");
    }

    shared_ptr<Maneuver> ManeuverFactory::instantAction(function<void()> action)
    {
        return shared_ptr<Maneuver>(new InstantActionManeuver(
            Maneuver::Type::Unspecified,
            "",
            action
        ));
    }

    shared_ptr<Maneuver> ManeuverFactory::delay(chrono::microseconds duration)
    {
        return deferred([=](){
            chrono::microseconds targetTimestamp = 
                m_host->getWorld()->timestamp() + 
                chrono::microseconds(duration.count());
        
            return await(Maneuver::Type::Unspecified, "", [=]() {
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
        return shared_ptr<AwaitManeuver>(new AwaitManeuver(type, id, isReady));
    }

    shared_ptr<Maneuver> ManeuverFactory::awaitClearance(shared_ptr<Flight> flight, Clearance::Type clearanceType, Maneuver::Type type, const string& id)
    {
        return await(type, id, [=]() {
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
            flight->aircraft()->setLights(lights);
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
    };

    shared_ptr<Maneuver> ManeuverFactory::transmitIntent(shared_ptr<Flight> flight, shared_ptr<Intent> intent)
    {
        auto boxedTransmission = shared_ptr<BoxedTransmission>(new BoxedTransmission());

        return sequence(Maneuver::Type::Unspecified, "", {
            instantAction([=](){ 
                auto frequency = flight->aircraft()->frequency();
                
                if (frequency)
                {
                    boxedTransmission->ptr = frequency->enqueueTransmission(intent);
                }
                else
                {
                    m_host->writeLog(
                        "TRANSMISSION ERROR from pilot[%s] on frequency [%d]: no ATC on this frequency",
                        flight->callSign().c_str(), 
                        flight->aircraft()->frequencyKhz());
                }
            }),
            await(Maneuver::Type::Unspecified, "", [=](){
                return (!boxedTransmission->ptr || boxedTransmission->ptr->state() == Transmission::State::Completed);
            }),
            instantAction([=]() {
                m_host->writeLog(
                    "TRANSMISSION COMPLETED: [%s]->[%s]: %s", 
                    intent->subjectFlight()->callSign().c_str(), 
                    intent->subjectControl()->callSign().c_str(), 
                    boxedTransmission->ptr->verbalizedUtterance()->plainText().c_str());
            })
        });
    }

    shared_ptr<Maneuver> ManeuverFactory::airborneTurn(shared_ptr<Flight> flight, float fromHeading, float toHeading)
    {
        if (abs(fromHeading - toHeading) < 0.1)
        {
            return instantAction([](){});
        }

        auto aircraft = flight->aircraft();
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
}

