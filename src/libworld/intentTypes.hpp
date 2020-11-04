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
                ConversationState::Continue,
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
                ConversationState::Continue,
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
                ConversationState::Continue,
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
                ConversationState::Continue,
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
                ConversationState::Continue,
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
                ConversationState::Continue,
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
                ConversationState::End,
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
                ConversationState::End,
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
                ConversationState::Continue,
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
                ConversationState::Continue,
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
                ConversationState::End,
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
                ConversationState::Continue,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway),
            m_holdingPoint(_holdingPoint)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        const string& holdingPoint() const { return m_holdingPoint; } //TODO: rename; here this is not a holding point
        bool isCritical() const override { return true; }
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
                ConversationState::Continue,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance)
        {
        }
    public:
        const shared_ptr<RunwayCrossClearance> clearance() const { return m_clearance; }
    };

    class GroundHoldShortRunwayIntent : public Intent
    {
    public:
        static const int IntentCode = 1106;
    private:
        string m_runway;
        DeclineReason m_reason;
    public:
        GroundHoldShortRunwayIntent(
            uint64_t _id,
            uint64_t _replyToId,
            const string& _runway,
            DeclineReason _reason,
            shared_ptr<ControllerPosition> _subjectControl,
            shared_ptr<Flight> _subjectFlight
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::RequestRejection,
                IntentCode,
                ConversationState::Continue,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway),
            m_reason(_reason)
        {
        }
    public:
        const DeclineReason reason() const { return m_reason; }
        const string& runway() const { return m_runway; }
        bool isCritical() const override { return true; }
    };

    class PilotRunwayCrossReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1107;
    private:
        shared_ptr<RunwayCrossClearance> m_clearance;
    public:
        PilotRunwayCrossReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<RunwayCrossClearance> _clearance
        ) : Intent(
                _id,
                _replyToId,
                Direction::PilotToController,
                Type::ClearanceReadback,
                IntentCode,
                ConversationState::End,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance)
        {
        }
    public:
        shared_ptr<RunwayCrossClearance> clearance() const { return m_clearance; }
    };

    class PilotRunwayHoldShortReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1108;
    private:
        DeclineReason m_reason;
        string m_runway;
    public:
        PilotRunwayHoldShortReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            const string& _runway,
            DeclineReason _reason
        ) : Intent(
                _id,
                _replyToId,
                Direction::PilotToController,
                Type::Affirmation,
                IntentCode,
                ConversationState::End,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway),
            m_reason(_reason)
        {
        }
    public:
        DeclineReason reason() const { return m_reason; }
        const string& runway() const { return m_runway; }
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
                ConversationState::Continue,
                _subjectControl,
                _subjectFlight
            ),
            m_towerKhz(_towerKhz)
        {
        }
    public:
        int towerKhz() const { return m_towerKhz; }
        bool isCritical() const override { return true; }
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
                ConversationState::Continue,
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

    class TowerDepartureCheckInReplyIntent : public Intent
    {
    public:
        static const int IntentCode = 1125;
    private:
        string m_runway;
        int m_numberInLine;
        bool m_prepareForImmediateTakeoff;
    public:
        TowerDepartureCheckInReplyIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _subjectControl,
            const string& _runway,
            int _numberInLine,
            bool _prepareForImmediateTakeoff
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::Information,
                IntentCode,
                ConversationState::Continue,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway),
            m_numberInLine(_numberInLine),
            m_prepareForImmediateTakeoff(_prepareForImmediateTakeoff)
        {
        }
    public:
        const string& runway() const { return m_runway; }
        int numberInLine() const { return m_numberInLine; }
        bool prepareForImmediateTakeoff() const { return m_prepareForImmediateTakeoff; }
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
                ConversationState::Continue,
                _subjectControl,
                _subjectFlight
            )
        {
        }
    public:
    };

    class TowerDepartureHoldShortIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1165;
    private:
        string m_runwayName;
        DeclineReason m_reason;
    public:
        TowerDepartureHoldShortIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight,
            const string& _runwayName,
            DeclineReason _reason
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::RequestRejection,
                IntentCode,
                ConversationState::Continue,
                tower,
                flight
            ),
            m_runwayName(_runwayName),
            m_reason(_reason)
        {
        }
    public:
        const string& runwayName() const { return m_runwayName; }
        DeclineReason reason() const { return m_reason; }
    };

    class PilotDepartureHoldShortReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1166;
    private:
        string m_runwayName;
    public:
        PilotDepartureHoldShortReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _tower,
            const string& _runwayName
        ) : Intent(
                _id,
                _replyToId,
                Direction::PilotToController,
                Type::Affirmation,
                IntentCode,
                ConversationState::End,
                _tower,
                _subjectFlight
            ),
            m_runwayName(_runwayName)
        {
        }
    public:
        const string& runwayName() const { return m_runwayName; }
    };

    class TowerLineUpAndWaitIntent : public Intent
    {
    public:
        static const int IntentCode = 1140;
    private:
        shared_ptr<LineUpAndWaitApproval> m_approval;
        vector<TrafficAdvisory> m_traffic;
    public:
        TowerLineUpAndWaitIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<LineUpAndWaitApproval> _approval,
            vector<TrafficAdvisory> _traffic
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot, 
                Type::Report, 
                IntentCode,
                ConversationState::Continue,
                _approval->header().issuedBy,
                _approval->header().issuedTo
            ),
            m_approval(_approval),
            m_traffic(_traffic)
        {
        }
    public:
        const string& runway() const { return m_approval->departureRunway(); }
        DeclineReason waitReason() const { return m_approval->waitReason(); }
        shared_ptr<LineUpAndWaitApproval> approval() const { return m_approval; }
        const vector<TrafficAdvisory>& traffic() const { return m_traffic; }
        bool isCritical() const override { return true; }
    };

    class PilotLineUpAndWaitReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1150;
    private:
        shared_ptr<LineUpAndWaitApproval> m_approval;
    public:
        PilotLineUpAndWaitReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<LineUpAndWaitApproval> _approval
        ) : Intent(
                _id,
                _replyToId,
                Direction::PilotToController, 
                Type::Report, 
                IntentCode,
                ConversationState::End,
                _approval->header().issuedBy,
                _approval->header().issuedTo
            ),
            m_approval(_approval)
        {
        }
    public:
        const string& runway() const { return m_approval->departureRunway(); }
        shared_ptr<LineUpAndWaitApproval> approval() const { return m_approval; }
    };

    class TowerClearedForTakeoffIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1160;
    private:
        bool m_cleared;
        shared_ptr<TakeoffClearance> m_clearance;
        vector<TrafficAdvisory> m_traffic;
        int m_departureKhz;
    public:
        TowerClearedForTakeoffIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<TakeoffClearance> _clearance,
            const vector<TrafficAdvisory>& _traffic,
            int _departureKhz
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::Clearance, 
                IntentCode,
                ConversationState::Continue,
                tower,
                flight
            ),
            m_cleared(_cleared),
            m_clearance(_clearance),
            m_traffic(_traffic),
            m_departureKhz(_departureKhz)
        {
        }
    public:
        bool cleared() const { return m_cleared; }
        shared_ptr<TakeoffClearance> clearance() const { return m_clearance; }
        const vector<TrafficAdvisory> traffic() const { return m_traffic; }
        int departureKhz() const { return m_departureKhz; }
        bool isCritical() const override { return true; }
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
                ConversationState::End,
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
                ConversationState::Continue,
                _subjectControl,
                _subjectFlight
            ),
            m_runway(_runway)
        {
        }
    public:
        const string& runway() const { return m_runway; }
    };

    class TowerContinueApproachIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1185;
    private:
        string m_runwayName;
        int m_numberInLine;
        vector<TrafficAdvisory> m_traffic;
    public:
        TowerContinueApproachIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight,
            const string _runwayName,
            int _numberInLine,
            const vector<TrafficAdvisory>& _traffic
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::Information,
                IntentCode,
                ConversationState::Continue,
                tower,
                flight
            ),
            m_runwayName(_runwayName),
            m_numberInLine(_numberInLine),
            m_traffic(_traffic)
        {
        }
    public:
        const string runwayName() const { return m_runwayName; }
        const int numberInLine() const { return m_numberInLine; }
        const vector<TrafficAdvisory>& traffic() const { return m_traffic; }
    };

    class PilotContinueApproachReadbackIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1186;
    private:
        string m_runwayName;
    public:
        PilotContinueApproachReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight,
            const string _runwayName
        ) : Intent(
                _id,
                _replyToId,
                Direction::PilotToController,
                Type::Affirmation,
                IntentCode,
                ConversationState::End,
                tower,
                flight
            ),
            m_runwayName(_runwayName)
        {
        }
    public:
        const string runwayName() const { return m_runwayName; }
    };

    class TowerClearedForLandingIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1190;
    private:
        bool m_cleared;
        shared_ptr<LandingClearance> m_clearance;
        vector<TrafficAdvisory> m_traffic;
        int m_groundKhz;
    public:
        TowerClearedForLandingIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<ControllerPosition> tower,
            shared_ptr<Flight> flight, 
            bool _cleared, 
            shared_ptr<LandingClearance> _clearance,
            const vector<TrafficAdvisory>& _traffic,
            int _groundKhz
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot, 
                Type::Clearance, 
                IntentCode,
                ConversationState::Continue,
                tower,
                flight
            ),
            m_cleared(_cleared),
            m_clearance(_clearance),
            m_traffic(_traffic),
            m_groundKhz(_groundKhz)
        {
        }
    public:
        bool cleared() const { return m_cleared; }
        shared_ptr<LandingClearance> clearance() const { return m_clearance; }
        const vector<TrafficAdvisory>& traffic() const { return m_traffic; }
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
                ConversationState::End,
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
            ConversationState::Continue,
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
        bool isCritical() const override { return true; }
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
                ConversationState::Continue,
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
                ConversationState::End,
                _clearance->header().issuedBy,
                _clearance->header().issuedTo
            ),
            m_clearance(_clearance)
        {
        }
    public:
        shared_ptr<ArrivalTaxiClearance> clearance() const { return m_clearance; }
    };

    class TowerGoAroundIntent : public Intent
    {
    public:
        static constexpr int IntentCode = 1240;
    private:
        shared_ptr<GoAroundRequest> m_request;
        vector<TrafficAdvisory> m_traffic;
    public:
        TowerGoAroundIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<GoAroundRequest> _request,
            const vector<TrafficAdvisory>& _traffic
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToPilot,
                Type::Clearance,
                IntentCode,
                ConversationState::Continue,
                _request->header().issuedBy,
                _request->header().issuedTo
            ),
            m_request(_request),
            m_traffic(_traffic)
        {
        }
    public:
        shared_ptr<GoAroundRequest> request() const { return m_request; }
        const vector<TrafficAdvisory>& traffic() const { return m_traffic; }
        bool isCritical() const override { return true; }
    };

    class PilotGoAroundReadbackIntent : public Intent
    {
    public:
        static const int IntentCode = 1250;
    private:
        shared_ptr<GoAroundRequest> m_request;
    public:
        PilotGoAroundReadbackIntent(
            uint64_t _id,
            uint64_t _replyToId,
            shared_ptr<GoAroundRequest> _request
        ) : Intent(
                _id,
                _replyToId,
                Direction::PilotToController,
                Type::ClearanceReadback,
                IntentCode,
                ConversationState::End,
                _request->header().issuedBy,
                _request->header().issuedTo
            ),
            m_request(_request)
        {
        }
    public:
        shared_ptr<GoAroundRequest> request() const { return m_request; }
    };

    class GroundCrossRunwayRequestFromTowerIntent : public Intent
    {
    public:
        static const int IntentCode = 2010;
    private:
        string m_runwayName;
        uint64_t m_pilotRequestId;
    public:
        GroundCrossRunwayRequestFromTowerIntent(
            uint64_t _id,
            const string& _runwayName,
            shared_ptr<Flight> _subjectFlight,
            shared_ptr<ControllerPosition> _ground,
            shared_ptr<ControllerPosition> _tower,
            uint64_t _pilotRequestId
        ) : Intent(
                _id,
                0,
                Direction::ControllerToController,
                Type::Request,
                IntentCode,
                ConversationState::Continue,
                _ground,
                _subjectFlight,
                _tower
            ),
            m_runwayName(_runwayName),
            m_pilotRequestId(_pilotRequestId)
        {
        }
    public:
        uint64_t pilotRequestId() const { return m_pilotRequestId; }
        const string& runwayName() const { return m_runwayName; }
    };

    class TowerCrossRunwayReplyToGroundIntent : public Intent
    {
    public:
        static const int IntentCode = 2020;
    private:
        string m_runwayName;
        shared_ptr<RunwayCrossClearance> m_clearance;
        DeclineReason m_declineReason;
        uint64_t m_pilotRequestId;
    public:
        TowerCrossRunwayReplyToGroundIntent(
            uint64_t _id,
            uint64_t _replyToId,
            uint64_t _pilotRequestId,
            const string& _runwayName,
            shared_ptr<RunwayCrossClearance> _clearance,
            DeclineReason _declineReason,
            shared_ptr<Flight> _flight,
            shared_ptr<ControllerPosition> _tower,
            shared_ptr<ControllerPosition> _ground
        ) : Intent(
                _id,
                _replyToId,
                Direction::ControllerToController,
                _clearance ? Type::Clearance : Type::RequestRejection,
                IntentCode,
                ConversationState::End,
                _tower,
                _flight,
                _ground
            ),
            m_runwayName(_runwayName),
            m_clearance(_clearance),
            m_declineReason(_declineReason),
            m_pilotRequestId(_pilotRequestId)
        {
        }
    public:
        const string& runwayName() const { return m_runwayName; }
        bool cleared() const { return !!m_clearance; }
        shared_ptr<RunwayCrossClearance> clearance() const { return m_clearance; }
        DeclineReason declineReason() const { return m_declineReason; }
        uint64_t pilotRequestId() const { return m_pilotRequestId; }
    };
}
