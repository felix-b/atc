// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
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
            uint64_t _id,
            const string& _atisLetter,
            shared_ptr<Flight> flight, 
            shared_ptr<ControllerPosition> clearanceDelivery
        ) : Intent(
                _id,
                0,
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
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> clearanceDelivery,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<IfrClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
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
        PilotIfrClearanceReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<IfrClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
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
        DeliveryIfrClearanceReadbackCorrectIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<IfrClearance> _clearance,
            bool _correct,
            int _groundKhz
        ) : Intent(
                _id,
                _replyToId,
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
            uint64_t _id,
            shared_ptr<Flight> flight,
            shared_ptr<ControllerPosition> ground
        ) : Intent(
                _id,
                0,
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
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> ground,
            shared_ptr<Flight> flight, 
            bool _approved, 
            shared_ptr<PushAndStartApproval> _approval
        ) : Intent(
                _id,
                _replyToId,
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
        PilotAffirmationIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl
        ) : Intent(
                _id,
                _replyToId,
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
        PilotHandoffReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            int _newFrequencyKhz
        ) : Intent(
                _id,
                _replyToId,
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
            uint64_t _id,
            shared_ptr<Flight> flight,
            shared_ptr<ControllerPosition> ground
        ) : Intent(
                _id,
                0,
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
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> ground,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<DepartureTaxiClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
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
        PilotDepartureTaxiReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<DepartureTaxiClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
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
        PilotReportHoldingShortIntent(
            uint64_t _id,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            const string& _runway,
            const string& _holdingPoint
        ) : Intent(
                _id,
                0,
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

    class GroundRunwayCrossClearanceIntent : public Intent
    {
    public:
        static const int IntentCode = 1105;
    private:
        shared_ptr<RunwayCrossClearance> m_clearance;
    public:
        GroundRunwayCrossClearanceIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<RunwayCrossClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::Clearance,
                IntentCode,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance)
        {
        }
    public:
        const shared_ptr<RunwayCrossClearance> clearance() const { return m_clearance; }
    };

    class GroundSwitchToTowerIntent : public Intent
    {
    public:
        static const int IntentCode = 1110;
    private:
        int m_towerKhz;
    public:
        GroundSwitchToTowerIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            int _towerKhz
        ) : Intent(
                _id,
                _replyToId,
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
            uint64_t _id,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl, 
            const string& _runway, 
            const string& _holdingPoint, 
            bool _haveNumbers
        ) : Intent(
                _id,
                0,
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
        ControlStandbyIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl
        ) : Intent(
                _id,
                _replyToId,
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
        TowerLineUpIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<LineupApproval> _approval
        ) : Intent(
                _id,
                _replyToId,
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
        PilotLineUpReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<LineupApproval> _approval
        ) : Intent(
                _id,
                _replyToId,
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
            uint64_t _id,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<TakeoffClearance> _clearance,
            int _departureKhz
        ) : Intent(
                _id,
                0,
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
        PilotTakeoffClearanceReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<TakeoffClearance> _clearance,
            int _departureKhz
        ) : Intent(
                _id,
                _replyToId,
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
        PilotReportFinalIntent(
            uint64_t _id,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            const string& _runway
        ) : Intent(
                _id,
                0,
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
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<LandingClearance> _clearance,
            int _groundKhz
        ) : Intent(
                _id,
                _replyToId,
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
        PilotLandingClearanceReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<LandingClearance> _clearance, int _groundKhz
        ) : Intent(
                _id,
                _replyToId,
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

    class PilotArrivalCheckInWithGroundIntent : public Intent
    {
    public:
        static const int IntentCode = 1210;
    private:
        string m_runway;
        string m_exitName;
        shared_ptr<TaxiEdge> m_exitEdge;  //TODO: handle human pilot
    public:
        PilotArrivalCheckInWithGroundIntent(
            uint64_t _id,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            const string& _runway,
            const string& _exitName,
            shared_ptr<TaxiEdge> _exitEdge
        ) : Intent(
            _id,
            0,
            Direction::PilotToController,
            Type::Report,
            IntentCode,
            _subjectControl,
            _subjectFlight
        ),
            m_runway(_runway),
            m_exitName(_exitName),
            m_exitEdge(_exitEdge)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        const string& exitName() const { return m_exitName; }
        shared_ptr<TaxiEdge> exitEdge() const { return m_exitEdge; }
    };

    class GroundArrivalTaxiReplyIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1220;
    private:
        bool m_cleared;
        shared_ptr<ArrivalTaxiClearance> m_clearance;
    public:
        GroundArrivalTaxiReplyIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> ground,
            shared_ptr<Flight> flight,
            bool _cleared,
            shared_ptr<ArrivalTaxiClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
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
        shared_ptr<ArrivalTaxiClearance> clearance() const { return m_clearance; }
    };

    class PilotArrivalTaxiReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1230;
    private:
        shared_ptr<ArrivalTaxiClearance> m_clearance;
    public:
        PilotArrivalTaxiReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ArrivalTaxiClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
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
        shared_ptr<ArrivalTaxiClearance> clearance() const { return m_clearance; }
    };
}
