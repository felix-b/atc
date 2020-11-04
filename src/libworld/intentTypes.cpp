// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "libworld.h"
#include "intentTypes.hpp"

using namespace std;

namespace world
{
    constexpr int PilotAffirmationIntent::IntentCode;
    constexpr int PilotHandoffReadbackIntent::IntentCode;
    constexpr int PilotIfrClearanceRequestIntent::IntentCode;
    constexpr int DeliveryIfrClearanceReplyIntent::IntentCode;
    constexpr int PilotIfrClearanceReadbackIntent::IntentCode;
    constexpr int DeliveryIfrClearanceReadbackCorrectIntent::IntentCode;
    constexpr int PilotPushAndStartRequestIntent::IntentCode;
    constexpr int GroundPushAndStartReplyIntent::IntentCode;
    constexpr int PilotDepartureTaxiRequestIntent::IntentCode;
    constexpr int GroundDepartureTaxiReplyIntent::IntentCode;
    constexpr int PilotDepartureTaxiReadbackIntent::IntentCode;
    constexpr int PilotReportHoldingShortIntent::IntentCode;
    constexpr int GroundRunwayCrossClearanceIntent::IntentCode;
    constexpr int GroundSwitchToTowerIntent::IntentCode;
    constexpr int PilotCheckInWithTowerIntent::IntentCode;
    constexpr int ControlStandbyIntent::IntentCode;
    constexpr int TowerLineUpAndWaitIntent::IntentCode;
    constexpr int PilotLineUpAndWaitReadbackIntent::IntentCode;
    constexpr int TowerClearedForTakeoffIntent::IntentCode;
    constexpr int PilotTakeoffClearanceReadbackIntent::IntentCode;
    constexpr int PilotReportFinalIntent::IntentCode;
    constexpr int TowerClearedForLandingIntent::IntentCode;
    constexpr int PilotLandingClearanceReadbackIntent::IntentCode;
    constexpr int PilotArrivalCheckInWithGroundIntent::IntentCode;
    constexpr int GroundArrivalTaxiReplyIntent::IntentCode;
    constexpr int PilotArrivalTaxiReadbackIntent::IntentCode;
    constexpr int GroundHoldShortRunwayIntent::IntentCode;
    constexpr int TowerDepartureCheckInReplyIntent::IntentCode;
    constexpr int TowerDepartureHoldShortIntent::IntentCode;
    constexpr int PilotDepartureHoldShortReadbackIntent::IntentCode;
    constexpr int PilotRunwayCrossReadbackIntent::IntentCode;
    constexpr int PilotRunwayHoldShortReadbackIntent::IntentCode;
    constexpr int TowerContinueApproachIntent::IntentCode;
    constexpr int PilotContinueApproachReadbackIntent::IntentCode;
    constexpr int TowerGoAroundIntent::IntentCode;
    constexpr int PilotGoAroundReadbackIntent::IntentCode;
}
