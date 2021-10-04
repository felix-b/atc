﻿using System;

namespace Atc.World.Abstractions
{
    public abstract record Intent(
        IntentHeader Header
    );

    public record IntentHeader(
        WellKnownIntentType Type,
        int CustomCode,
        Party Originator,
        Party? Recipient,
        DateTime CreatedAtUtc
    );

    public enum WellKnownIntentType
    {
        Unspecified = 0,
        Custom = 0xFFFFFF,
        RadioCheckRequest = 0x010,
        RadioCheckReply = 0x011,
        Greeting = 0x020,
        GoAheadInstruction = 0x021,
        StartupRequest = 0x030,
        StartupApproval = 0x031,
        StartupApprovalReadback = 0x032,
        TaxiRequest = 0x040,
        TaxiClearance = 0x041,
        TaxiClearanceReadback = 0x042,
        HoldShortRunwayReport = 0x050,
        CrossRunwayInstruction = 0x051,
        CrossRunwayInstructionReadback = 0x052,
        HoldShortRunwayInstruction = 0x053,
        HoldShortRunwayInstructionReadback = 0x054,
        ReadyForTakeoffReport = 0x060,
        TakeoffClearance = 0x061,
        TakeoffClearanceReadback = 0x062,
        LineUpAndWaitInstruction = 0x063,
        LineUpAndWaitInstructionReadback = 0x064,
        PilotPositionReport = 0x070,
        DirectToNavaidClearance = 0x080,
        DirectToNavaidClearanceReadback = 0x081,
        JoinPatternInstruction = 0x090,
        JoinPatternInstructionReadback = 0x091,
        LandingNumberAssignment = 0x0A0,
        LandingNumberAssignmentReadback = 0x0A1,
        EnterTrainingZoneInstruction = 0x0B1,
        EnterTrainingZoneInstructionReadback = 0x0B2,
        LeaveTrainingZoneRequest = 0x0C0,
        LeaveTrainingZoneStandByInstruction = 0x0C1, 
        LeaveTrainingZoneStandByInstructionReadback = 0x0C2, 
        FinalReport = 0x0D0,
        LandingClearance = 0x0D1,
        LandingClearanceReadback = 0x0D2,
        //TBD.............
    }
}