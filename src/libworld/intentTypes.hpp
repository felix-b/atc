// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#pragma once

#include "libworld.h"
#include "clearanceTypes.hpp"

using namespace std;


namespace world
{
    class PilotIfrClearanceRequestIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1010;
    private:
        string m_atisLetter; 
    public:
        PilotIfrClearanceRequestIntent(
            const string& _atisLetter,
            shared_ptr<Flight> flight, 
            shared_ptr<ControllerPosition> clearanceDelivery
        ) : Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                clearanceDelivery,
                flight
            ),
            m_atisLetter(_atisLetter)
        {
        }
    public:
        const string& atisLetter() const { return m_atisLetter; }
    };

    class DeliveryIfrClearanceReplyIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1020;
    private:
        bool m_cleared;
        shared_ptr<IfrClearance> m_clearance;
    public:
        DeliveryIfrClearanceReplyIntent(
            shared_ptr<ControllerPosition> clearanceDelivery,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<IfrClearance> _clearance
        ) : Intent(
                Direction::ControllerToPilot, 
                Type::Clearance, 
                IntentCode,
                clearanceDelivery,
                flight
            ),
            m_cleared(_cleared),
            m_clearance(_clearance)
        {
        }
    public:
        bool cleared() const { return m_cleared; }
        shared_ptr<IfrClearance> clearance() const { return m_clearance; }
    };

    class PilotIfrClearanceReadbackIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1030;
    private:
        shared_ptr<IfrClearance> m_clearance;
    public:
        PilotIfrClearanceReadbackIntent(shared_ptr<IfrClearance> _clearance) : 
            Intent(
                Direction::PilotToController, 
                Type::ClearanceReadback, 
                IntentCode,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance)
        {
        }
    public:
        shared_ptr<IfrClearance> clearance() const { return m_clearance; }
    };

    class DeliveryIfrClearanceReadbackCorrectIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1035;
    private:
        shared_ptr<IfrClearance> m_clearance;
        bool m_correct;
        int m_groundKhz;
    public:
        DeliveryIfrClearanceReadbackCorrectIntent(shared_ptr<IfrClearance> _clearance, bool _correct, int _groundKhz) : 
            Intent(
                Direction::ControllerToPilot, 
                Type::Information, 
                IntentCode,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance),
            m_correct(_correct),
            m_groundKhz(_groundKhz)
        {
        }
    public:
        shared_ptr<IfrClearance> clearance() const { return m_clearance; }
        bool correct() const { return m_correct; }
        int groundKhz() const { return m_groundKhz; }        
    };

    class PilotPushAndStartRequestIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1040;
    public:
        PilotPushAndStartRequestIntent(
            shared_ptr<Flight> flight, 
            shared_ptr<ControllerPosition> ground
        ) : Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                ground,
                flight
            )
        {
        }
    };

    class GroundPushAndStartReplyIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1050;
    private:
        bool m_approved;
        shared_ptr<PushAndStartApproval> m_approval;
    public:
        GroundPushAndStartReplyIntent(
            shared_ptr<ControllerPosition> ground,
            shared_ptr<Flight> flight, 
            bool _approved, 
            shared_ptr<PushAndStartApproval> _approval
        ) : Intent(
                Direction::ControllerToPilot, 
                Type::Clearance, 
                IntentCode,
                ground,
                flight
            ),
            m_approved(_approved),
            m_approval(_approval)
        {
        }
    public:
        bool approved() const { return m_approved; }
        shared_ptr<PushAndStartApproval> approval() const { return m_approval; }
    };

    class PilotAffirmationIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1060;
    public:
        PilotAffirmationIntent(shared_ptr<Flight> _subjectFlight, shared_ptr<ControllerPosition> _subjectControl) : 
            Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            )
        {
        }
    };

    class PilotHandoffReadbackIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1065;
    private:
        int m_newFrequencyKhz;
    public:
        PilotHandoffReadbackIntent(shared_ptr<Flight> _subjectFlight, shared_ptr<ControllerPosition> _subjectControl, int _newFrequencyKhz) : 
            Intent(
                Direction::PilotToController, 
                Type::Affirmation, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            ),
            m_newFrequencyKhz(_newFrequencyKhz)
        {
        }
    public:
        int newFrequencyKhz() const { return m_newFrequencyKhz; }
    };

    class PilotDepartureTaxiRequestIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1070;
    public:
        PilotDepartureTaxiRequestIntent(
            shared_ptr<Flight> flight, 
            shared_ptr<ControllerPosition> ground
        ) : Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                ground,
                flight
            )
        {
        }
    };

    class GroundDepartureTaxiReplyIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1080;
    private:
        bool m_cleared;
        shared_ptr<DepartureTaxiClearance> m_clearance;
    public:
        GroundDepartureTaxiReplyIntent(
            shared_ptr<ControllerPosition> ground,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<DepartureTaxiClearance> _clearance
        ) : Intent(
                Direction::ControllerToPilot, 
                Type::Clearance, 
                IntentCode,
                ground,
                flight
            ),
            m_cleared(_cleared),
            m_clearance(_clearance)
        {
        }
    public:
        bool cleared() const { return m_cleared; }
        shared_ptr<DepartureTaxiClearance> clearance() const { return m_clearance; }
    };

    class PilotDepartureTaxiReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1090;
    private:
        shared_ptr<DepartureTaxiClearance> m_clearance;
    public:
        PilotDepartureTaxiReadbackIntent(shared_ptr<DepartureTaxiClearance> _clearance) : 
            Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance)
        {
        }
    public:
        shared_ptr<DepartureTaxiClearance> clearance() const { return m_clearance; }
    };

    class PilotReportHoldingShortIntent : public Intent
    {
    public:
        static const int IntentCode = 1100;
    private:
        string m_runway;
        string m_holdingPoint;
    public:
        PilotReportHoldingShortIntent(shared_ptr<Flight> _subjectFlight, shared_ptr<ControllerPosition> _subjectControl, const string& _runway, const string& _holdingPoint) : 
            Intent(
                Direction::PilotToController, 
                Type::Report, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway),
            m_holdingPoint(_holdingPoint)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        const string& holdingPoint() const { return m_holdingPoint; }
    };

    class GroundSwitchToTowerIntent : public Intent
    {
    public:
        static const int IntentCode = 1110;
    private:
        int m_towerKhz;
    public:
        GroundSwitchToTowerIntent(shared_ptr<Flight> _subjectFlight, shared_ptr<ControllerPosition> _subjectControl, int _towerKhz) : 
            Intent(
                Direction::ControllerToPilot, 
                Type::Information, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            ),
            m_towerKhz(_towerKhz)
        {
        }
    public:
        int towerKhz() const { return m_towerKhz; }
    };

    class PilotCheckInWithTowerIntent : public Intent
    {
    public:
        static const int IntentCode = 1120;
    private:
        string m_runway;
        string m_holdingPoint;
        bool m_haveNumbers;
    public:
        PilotCheckInWithTowerIntent(
            shared_ptr<Flight> _subjectFlight, 
            shared_ptr<ControllerPosition> _subjectControl, 
            const string& _runway, 
            const string& _holdingPoint, 
            bool _haveNumbers
        ) : Intent(
                Direction::PilotToController, 
                Type::Report, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway),
            m_holdingPoint(_holdingPoint),
            m_haveNumbers(_haveNumbers)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        const string& holdingPoint() const { return m_holdingPoint; }
        bool haveNumbers() const { return m_haveNumbers; }
    };

    class ControlStandbyIntent : public Intent
    {
    public:
        static const int IntentCode = 1130;
    public:
        ControlStandbyIntent(shared_ptr<Flight> _subjectFlight, shared_ptr<ControllerPosition> _subjectControl) : 
            Intent(
                Direction::ControllerToPilot, 
                Type::Report, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            )
        {
        }
    public:
    };

    class TowerLineUpIntent : public Intent
    {
    public:
        static const int IntentCode = 1140;
    private:
        shared_ptr<LineupApproval> m_approval;
    public:
        TowerLineUpIntent(shared_ptr<LineupApproval> _approval) : 
            Intent(
                Direction::ControllerToPilot, 
                Type::Report, 
                IntentCode,
                _approval->header().issuedBy, 
                _approval->header().issuedTo
            ),
            m_approval(_approval)
        {
        }
    public:
        const string& runway() const { return m_approval->departureRunway(); }
        bool wait() const { return m_approval->wait(); }
        shared_ptr<LineupApproval> approval() const { return m_approval; }
    };

    class PilotLineUpReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1150;
    private:
        shared_ptr<LineupApproval> m_approval;
    public:
        PilotLineUpReadbackIntent(shared_ptr<LineupApproval> _approval) : 
            Intent(
                Direction::PilotToController, 
                Type::Report, 
                IntentCode,
                _approval->header().issuedBy, 
                _approval->header().issuedTo
            ),
            m_approval(_approval)
        {
        }
    public:
        const string& runway() const { return m_approval->departureRunway(); }
        bool wait() const { return m_approval->wait(); }
        shared_ptr<LineupApproval> approval() const { return m_approval; }
    };

    class TowerClearedForTakeoffIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1160;
    private:
        bool m_cleared;
        shared_ptr<TakeoffClearance> m_clearance;
        int m_departureKhz;
    public:
        TowerClearedForTakeoffIntent(
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<TakeoffClearance> _clearance,
            int _departureKhz
        ) : Intent(
                Direction::ControllerToPilot, 
                Type::Clearance, 
                IntentCode,
                tower,
                flight
            ),
            m_cleared(_cleared),
            m_clearance(_clearance),
            m_departureKhz(_departureKhz)
        {
        }
    public:
        bool cleared() const { return m_cleared; }
        shared_ptr<TakeoffClearance> clearance() const { return m_clearance; }
        int departureKhz() const { return m_departureKhz; }
    };

    class PilotTakeoffClearanceReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1170;
    private:
        shared_ptr<TakeoffClearance> m_clearance;
        int m_departureKhz;
    public:
        PilotTakeoffClearanceReadbackIntent(shared_ptr<TakeoffClearance> _clearance, int _departureKhz) : 
            Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance),
            m_departureKhz(_departureKhz)
        {
        }
    public:
        shared_ptr<TakeoffClearance> clearance() const { return m_clearance; }
        int departureKhz() const { return m_departureKhz; }
    };

    class PilotReportFinalIntent : public Intent
    {
    public:
        static const int IntentCode = 1180;
    private:
        string m_runway;
    public:
        PilotReportFinalIntent(shared_ptr<Flight> _subjectFlight, shared_ptr<ControllerPosition> _subjectControl, const string& _runway) : 
            Intent(
                Direction::PilotToController, 
                Type::Report, 
                IntentCode,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway)
        {
        }
    public:
        const string& runway() const { return m_runway; }
    };

    class TowerClearedForLandingIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1190;
    private:
        bool m_cleared;
        shared_ptr<LandingClearance> m_clearance;
        int m_groundKhz;
    public:
        TowerClearedForLandingIntent(
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<LandingClearance> _clearance,
            int _groundKhz
        ) : Intent(
                Direction::ControllerToPilot, 
                Type::Clearance, 
                IntentCode,
                tower,
                flight
            ),
            m_cleared(_cleared),
            m_clearance(_clearance),
            m_groundKhz(_groundKhz)
        {
        }
    public:
        bool cleared() const { return m_cleared; }
        shared_ptr<LandingClearance> clearance() const { return m_clearance; }
        int groundKhz() const { return m_groundKhz; }
    };

    class PilotLandingClearanceReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1200;
    private:
        shared_ptr<LandingClearance> m_clearance;
        int m_groundKhz;
    public:
        PilotLandingClearanceReadbackIntent(shared_ptr<LandingClearance> _clearance, int _groundKhz) : 
            Intent(
                Direction::PilotToController, 
                Type::Request, 
                IntentCode,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance),
            m_groundKhz(_groundKhz)
        {
        }
    public:
        shared_ptr<LandingClearance> clearance() const { return m_clearance; }
        int groundKhz() const { return m_groundKhz; }
    };
}
