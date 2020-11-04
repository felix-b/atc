// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include "libworld.h"

using namespace std;

namespace world
{
    enum class DeclineReason
    {
        None = 0,
        PlanNotFiled = 10,
        TimeSlot = 20,
        WaitInLine = 25,
        WakeTurbulence = 30,
        TrafficDeparting = 40,
        TrafficLanding = 50,
        TrafficCrossing = 60,
        TrafficDisabled = 70,
        TrafficTaxiing = 80,
        TaxiwaysBusy = 90,
        ApronBusy = 100,
        GateNotVacated = 110,
        GateNotAllocated = 120,
        RunwayNotVacated = 130,
        AirportBusy = 140,
        WeatherUnsafe = 150,
        Maintenance = 160,
        Emergency = 170,
        NotFirstInLine = 180,
    };

    enum class TrafficAdvisoryType
    {
        NotSpecified = 0,
        CrossingRunway = 10,
        HoldingInPosition = 20,
        LandingAhead = 30,
        DepartingAhead = 40,
        TrafficOnFinal = 50,
        LandedOnRunway = 60
    };

    struct TrafficAdvisory
    {
    public:
        TrafficAdvisoryType type;
        string aircraftTypeIcao;
        int miles;
    public:
        static TrafficAdvisory crossingRunway() { return TrafficAdvisory { TrafficAdvisoryType::CrossingRunway }; }
        static TrafficAdvisory holdingInPosition() { return TrafficAdvisory { TrafficAdvisoryType::HoldingInPosition }; }
        static TrafficAdvisory holdingInPosition(const string& typeIcao)
        {
            return TrafficAdvisory { TrafficAdvisoryType::HoldingInPosition, typeIcao };
        }
        static TrafficAdvisory landing()
        {
            return TrafficAdvisory { TrafficAdvisoryType::LandingAhead };
        }
        static TrafficAdvisory landingAhead(const string& typeIcao, int miles = 0)
        {
            return TrafficAdvisory { TrafficAdvisoryType::LandingAhead, typeIcao, miles };
        }
        static TrafficAdvisory landedOnRunway(const string& typeIcao)
        {
            return TrafficAdvisory { TrafficAdvisoryType::LandedOnRunway, typeIcao };
        }
        static TrafficAdvisory departingAhead(const string& typeIcao)
        {
            return TrafficAdvisory { TrafficAdvisoryType::DepartingAhead, typeIcao };
        }
        static TrafficAdvisory onFinal(const string& typeIcao, int miles)
        {
            return TrafficAdvisory { TrafficAdvisoryType::TrafficOnFinal, typeIcao, miles };
        }
    };

    class IfrClearance : public Clearance
    {
    private:
        string m_limit;
        string m_sid;
        string m_transition;
        float m_initialAltitudeFeet;
        float m_cruizeAltitudeFeet;
        int m_furtherClearanceInMinutes;
        int m_departureKhz;
        string m_squawk;
        bool m_readbackCorrect;
    public:
        IfrClearance(
            const Header& _header,
            string _limit,
            string _sid,
            string _transition,
            float _initialAltitudeFeet,
            float _cruizeAltitudeFeet,
            int _furtherClearanceInMinutes,
            int _departureKhz,
            string _squawk
        ) : Clearance(_header),
            m_limit(_limit),
            m_sid(_sid),
            m_transition(_transition),
            m_initialAltitudeFeet(_initialAltitudeFeet),
            m_cruizeAltitudeFeet(_cruizeAltitudeFeet),
            m_furtherClearanceInMinutes(_furtherClearanceInMinutes),
            m_departureKhz(_departureKhz),
            m_squawk(_squawk),
            m_readbackCorrect(false)
        {
        }
    public:
        const string& limit() const { return m_limit; }
        const string& sid() const { return m_sid; }
        const string& transition() const { return m_transition; }
        float initialAltitudeFeet() const { return m_initialAltitudeFeet; }
        float cruizeAltitudeFeet() const { return m_cruizeAltitudeFeet; }
        int furtherClearanceInMinutes() const { return m_furtherClearanceInMinutes; }
        int departureKhz() const { return m_departureKhz; }
        const string& squawk() const { return m_squawk; }
        bool readbackCorrect() const { return m_readbackCorrect; }
    public:
        void setReadbackCorrect() { m_readbackCorrect = true; }
    };

    class IfrClearanceReadbackConfirm : public Clearance
    {
        
    };

    class PushAndStartApproval : public Clearance
    {
    private:
        string m_departureRunway;
        vector<GeoPoint> m_pushbackPath;
        shared_ptr<TaxiPath> m_taxiPath;
    public:
        PushAndStartApproval(
            const Header& _header,
            const string& _departureRunway,
            const vector<GeoPoint>& _pushbackPath,
            shared_ptr<TaxiPath> _taxiPath
        ) : Clearance(_header),
            m_departureRunway(_departureRunway),
            m_pushbackPath(_pushbackPath),
            m_taxiPath(_taxiPath)
        {
        }
    public:
        const string& departureRunway() const { return m_departureRunway; }
        const vector<GeoPoint>& pushbackPath() const { return m_pushbackPath; }
        shared_ptr<TaxiPath> taxiPath() const { return m_taxiPath; }
    };

    class DepartureTaxiClearance : public Clearance
    {
    private:
        string m_departureRunway;
        shared_ptr<TaxiPath> m_taxiPath;
    public:
        DepartureTaxiClearance(
            const Header& _header,
            const string& _departureRunway,
            shared_ptr<TaxiPath> _taxiPath
        ) : Clearance(_header),
            m_departureRunway(_departureRunway),
            m_taxiPath(_taxiPath)
        {
        }
    public:
        const string& departureRunway() const { return m_departureRunway; }
        shared_ptr<TaxiPath> taxiPath() const { return m_taxiPath; }
    };

    class ArrivalTaxiClearance : public Clearance
    {
    private:
        string m_parkingStand;
        shared_ptr<TaxiPath> m_taxiPath;
    public:
        ArrivalTaxiClearance(
            const Header& _header,
            const string& _parkingStand,
            shared_ptr<TaxiPath> _taxiPath
        ) : Clearance(_header),
            m_parkingStand(_parkingStand),
            m_taxiPath(_taxiPath)
        {
        }
    public:
        const string& parkingStand() const { return m_parkingStand; }
        shared_ptr<TaxiPath> taxiPath() const { return m_taxiPath; }
    };

    class RunwayCrossClearance : public Clearance
    {
    private:
        string m_runwayName;
    public:
        RunwayCrossClearance(
            const Header& _header,
            const string& _runwayName
        ) : Clearance(_header),
            m_runwayName(_runwayName)
        {
        }
    public:
        const string& runwayName() const { return m_runwayName; }
    };

    class LineUpAndWaitApproval : public Clearance
    {
    private:
        string m_departureRunway;
        DeclineReason m_waitReason;
    public:
        LineUpAndWaitApproval(
            const Header& _header,
            const string& _departureRunway,
            DeclineReason _waitReason
        ) : Clearance(_header),
            m_departureRunway(_departureRunway),
            m_waitReason(_waitReason)
        {
        }
    public:
        const string& departureRunway() const { return m_departureRunway; }
        DeclineReason waitReason() { return m_waitReason; }
    };

    class TakeoffClearance : public Clearance
    {
    private:
        string m_departureRunway;
        bool m_immediate;
        float m_initialHeading;
        int m_departureKhz;
    public:
        TakeoffClearance(
            const Header& _header,
            const string& _departureRunway,
            bool _immediate,
            float _initialHeading,
            int _departureKhz
        ) : Clearance(_header),
            m_departureRunway(_departureRunway),
            m_immediate(_immediate),
            m_initialHeading(_initialHeading),
            m_departureKhz(_departureKhz)
        {
        }
    public:
        const string& departureRunway() const { return m_departureRunway; }
        bool immediate() const { return m_immediate; }
        float initialHeading() const { return m_initialHeading; }
        int departureKhz() const { return m_departureKhz; }
    };

    class GoAroundRequest : public Clearance
    {
    private:
        string m_runway;
        DeclineReason m_reason;
    public:
        GoAroundRequest(
            const Header& _header,
            const string& _runway,
            DeclineReason _reason
        ) : Clearance(_header),
            m_runway(_runway),
            m_reason(_reason)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        DeclineReason reason() const { return m_reason; }
    };

    class LandingClearance : public Clearance
    {
    private:
        string m_runway;
        int m_groundKhz;
    public:
        LandingClearance(
            const Header& _header,
            const string& _runway,
            int _groundKhz
        ) : Clearance(_header),
            m_runway(_runway),
            m_groundKhz(_groundKhz)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        int groundKhz() const { return m_groundKhz; }
    };
}
