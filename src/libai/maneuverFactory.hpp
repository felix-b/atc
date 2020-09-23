// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once
#include "libworld.h"

using namespace std;
using namespace world;

namespace ai
{
    class ManeuverFactory
    {
    public:
        typedef function<shared_ptr<Maneuver>(shared_ptr<TaxiEdge> atEdge)> HoldingShortCallback;
    private:
        shared_ptr<HostServices> m_host;
    public:
        ManeuverFactory(shared_ptr<HostServices> _host) :
            m_host(_host)
        {
        }
    public:
        // shared_ptr<Maneuver> entireFlight(shared_ptr<Flight> flight);
        // shared_ptr<Maneuver> departureAwaitIfrClearance(shared_ptr<Flight> flight);
        // shared_ptr<Maneuver> departureAwaitPushback(shared_ptr<Flight> flight);
        // shared_ptr<Maneuver> departurePushbackAndStart(shared_ptr<Flight> flight);
        // shared_ptr<Maneuver> departureAwaitTaxi(shared_ptr<Flight> flight);
        // shared_ptr<Maneuver> departureTaxi(shared_ptr<Flight> flight);
        // shared_ptr<Maneuver> departureAwaitTakeoff(shared_ptr<Flight> flight);
        //shared_ptr<Maneuver> departureLineUpAndWait(shared_ptr<Flight> flight);
        shared_ptr<Maneuver> taxiByPath(
            shared_ptr<Flight> flight, 
            shared_ptr<TaxiPath> path,
            bool isPushback,
            HoldingShortCallback onHoldingShort = noopOnHoldingShort);
        //shared_ptr<Maneuver> taxiByPath2(shared_ptr<Flight> flight, const vector<GeoPoint>& path, bool isPushback);
        shared_ptr<Maneuver> taxiStraight(
            shared_ptr<Flight> flight, 
            const GeoPoint& from, 
            const GeoPoint& to, 
            bool isPushback);
        shared_ptr<Maneuver> taxiTurn(
            shared_ptr<Flight> flight,
            const GeoMath::TurnArc& arc,
            chrono::microseconds duration,
            bool isPushback);
        shared_ptr<Maneuver> taxiStop(shared_ptr<Flight> flight);
        shared_ptr<Maneuver> instantAction(function<void()> action);
        shared_ptr<Maneuver> delay(chrono::microseconds duration);
        shared_ptr<Maneuver> deferred(
            Maneuver::Type type, 
            const string& id,
            function<shared_ptr<Maneuver>()> factory);
        shared_ptr<Maneuver> deferred(function<shared_ptr<Maneuver>()> factory);
        shared_ptr<Maneuver> await(Maneuver::Type type, const string& id, function<bool()> isReady);
        shared_ptr<Maneuver> awaitClearance(
            shared_ptr<Flight> flight, 
            Clearance::Type clearanceType, 
            Maneuver::Type type = Maneuver::Type::Unspecified, 
            const string& id = "");
        shared_ptr<Maneuver> sequence(Maneuver::Type type, const string& id, const vector<shared_ptr<Maneuver>>& steps);
        shared_ptr<Maneuver> parallel(Maneuver::Type type, const string& id, const vector<shared_ptr<Maneuver>>& steps);
        shared_ptr<Maneuver> switchLights(shared_ptr<Flight> flight, Aircraft::LightBits lights);
        shared_ptr<Maneuver> tuneComRadio(shared_ptr<Flight> flight, int frequencyKhz);
        shared_ptr<Maneuver> tuneComRadio(shared_ptr<Flight> flight, shared_ptr<Frequency> frequency);
        shared_ptr<Maneuver> transmitIntent(shared_ptr<Flight> flight, shared_ptr<Intent> intent);
        shared_ptr<Maneuver> airborneTurn(shared_ptr<Flight> flight, float fromHeading, float toHeading);
    public:
        static shared_ptr<Maneuver> noopOnHoldingShort(shared_ptr<TaxiEdge> atEdge);
    };
}