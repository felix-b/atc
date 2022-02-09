using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Control;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Geo.Linq;
using ProtoBuf;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzTowerControllerBrain : ILlhzControllerBrain
    {
        public void Initialize(ILlhzControllerBrainState state)
        {
        }

        public void ObserveSituation(ILlhzControllerBrainState state, ILlhzControllerBrainActions output)
        {
            // nothing
        }

        public void HandleIncomingTransmission(Intent intent, ILlhzControllerBrainState state, ILlhzControllerBrainActions output)
        {
            var flightStrip = state.StripBoard[intent.CallsignCalling];
            var info = state.Airport.Information;

            switch (intent)
            {
                case DepartureTaxiRequestIntent taxiRequest:
                    HandleDepartureTaxiRequest(taxiRequest);
                    break;
                case ReportReadyForDepartureIntent readyForDeparture:
                    HandleReadyForDepartureReport(readyForDeparture);
                    break;
                case ReportDownwindIntent downwind:
                    HandleDownwindReport(downwind);
                    break;
                case ReportRemainingCircuitCountIntent remaining:
                    HandleRemainingCircuitCountReport(remaining);
                    break;
                case FinalApproachReportIntent final:
                    HandleFinalApproachReport(final);
                    break;
                case FarewellIntent farewell:
                    HandleFarewell(farewell);
                    break;
            }

            void HandleDepartureTaxiRequest(DepartureTaxiRequestIntent request)
            {
                var clearanceIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.DepartureTaxiClearance,
                    header => new DepartureTaxiClearanceIntent(
                        header, 
                        new IntentOptions(Condition: null, Flags: IntentOptionFlags.HasGreeting), 
                        OriginalRequest: request,
                        ActiveRunway: info.ActiveRunwaysDeparture[0],
                        HoldingPoint: null,
                        TaxiPath: ImmutableList<string>.Empty,
                        HoldShortRunways: ImmutableList<string>.Empty));
                
                output.UpdateFlightStrip(flightStrip with {
                    Lane = LlhzFlightStripLane.DepartureTaxiApproved,
                    LaneSinceUtc = state.UtcNow,
                });
                output.Transmit(clearanceIntent);
            }

            void HandleReadyForDepartureReport(ReportReadyForDepartureIntent report)
            {
                var clearanceIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.TakeoffClearance,
                    header => new TakeoffClearanceIntent(
                        header,
                        IntentOptions.Default,
                        Runway: info.ActiveRunwaysDeparture[0],
                        Wind: info.Wind));
                
                output.UpdateFlightStrip(flightStrip with {
                    Lane = LlhzFlightStripLane.ClearedForTakeoff,
                    LaneSinceUtc = state.UtcNow,
                    RemainingCircuitCount = flightStrip.DepartureType == DepartureIntentType.ToStayInPattern
                        ? 1000 // until updated with ReportRemainingCircuitCountIntent
                        : null
                });
                output.Transmit(clearanceIntent);
            }

            void HandleDownwindReport(ReportDownwindIntent report)
            {
                //TODO
                FindTrafficInFrontOfDownwind(state, out var trafficCountInFront, out var closestTraffic); 
                var number = trafficCountInFront + 1;
                
                var assignmentIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.LandingSequenceAssignment,
                    header => new LandingSequenceAssignmentIntent(
                        header,
                        IntentOptions.Default with {
                            Traffic = closestTraffic 
                        },
                        Runway: info.ActiveRunwaysDeparture[0],
                        LandingSequenceNumber: number));
                
                output.UpdateFlightStrip(flightStrip with {
                    Lane = LlhzFlightStripLane.ReportedDownwind,
                    LaneSinceUtc = state.UtcNow,
                });
                output.Transmit(assignmentIntent);
            }

            void HandleRemainingCircuitCountReport(ReportRemainingCircuitCountIntent report)
            {
                var readbackIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.RemainingCircuitCountReadback,
                    header => new ReadbackRemainingCircuitCountIntent(
                        header,
                        IntentOptions.Default,
                        OriginalIntent: report));
                
                output.UpdateFlightStrip(flightStrip with {
                    RemainingCircuitCount = report.RemainingCircuitCount
                });
                output.Transmit(readbackIntent);
            }
                
            void HandleFinalApproachReport(FinalApproachReportIntent report)
            {
                var landingType = flightStrip.RemainingCircuitCount.HasValue && flightStrip.RemainingCircuitCount.Value > 0
                    ? LandingType.TouchAndGo
                    : LandingType.FullStop;
                
                var clearanceIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.LandingSequenceAssignment,
                    header => new LandingClearanceIntent(
                        header,
                        IntentOptions.Default, 
                        Runway: info.ActiveRunwaysDeparture[0],
                        Wind: info.Wind,
                        landingType));
                
                output.UpdateFlightStrip(flightStrip with {
                    Lane = landingType == LandingType.FullStop 
                        ? LlhzFlightStripLane.ClearedToLand
                        : LlhzFlightStripLane.ClearedForTouchAndGo,
                    LaneSinceUtc = state.UtcNow,
                    RemainingCircuitCount = landingType == LandingType.FullStop 
                        ? null
                        : flightStrip.RemainingCircuitCount!.Value - 1
                });
                output.Transmit(clearanceIntent);
            }

            void HandleFarewell(FarewellIntent farewell)
            {
                var replyIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.Farewell,
                    header => new FarewellIntent(
                        header,
                        new IntentOptions(Flags: IntentOptionFlags.HasFarewell)));
                
                output.RemoveFlightStrip(flightStrip);
                output.Transmit(replyIntent);
            }
        }

        public ControllerPositionType PositionType => ControllerPositionType.Local;

        private void FindTrafficInFrontOfDownwind(
            ILlhzControllerBrainState state, 
            out int count, 
            out TrafficAdvisory? closest)
        {
            var flightStripsInFront = state.StripBoard.Values
                .Where(IsInFrontOfDownwind)
                .OrderBy(strip => (int) strip.Lane)
                .ToArray();

            count = flightStripsInFront.Length;
            closest = count > 0
                ? CreateAdvisory(flightStripsInFront[0])
                : null;
            
            bool IsInFrontOfDownwind(LlhzFlightStrip strip)
            {
                var lane = strip.Lane;

                if (lane == LlhzFlightStripLane.ClearedForTouchAndGo ||
                    lane == LlhzFlightStripLane.ClearedToLand)
                {
                    return state.UtcNow.Subtract(strip.LaneSinceUtc).TotalSeconds <= 10;
                }
                else
                {
                    return (
                        lane == LlhzFlightStripLane.ReportedDownwind ||
                        lane == LlhzFlightStripLane.ReportedFinal);
                }
            }

            TrafficAdvisory? CreateAdvisory(LlhzFlightStrip strip)
            {
                var timeInLane = state.UtcNow.Subtract(strip.LaneSinceUtc);
                
                switch (strip.Lane)
                {
                    case LlhzFlightStripLane.ReportedDownwind:
                        return new TrafficAdvisory(Location: new TrafficAdvisoryLocation(
                            RelativeOrdering: TrafficAdvisoryLocationOrdering.InFront,
                            Pattern: timeInLane.TotalSeconds < 5 ? TrafficAdvisoryLocationPattern.Downwind : TrafficAdvisoryLocationPattern.Base,
                            Refinement: timeInLane.TotalSeconds < 5 ? TrafficAdvisoryLocationRefinement.EndOf : null));
                    case LlhzFlightStripLane.ReportedFinal:
                        return new TrafficAdvisory(Location: new TrafficAdvisoryLocation(
                            RelativeOrdering: TrafficAdvisoryLocationOrdering.InFront,
                            Pattern: TrafficAdvisoryLocationPattern.Final));
                    case LlhzFlightStripLane.ClearedToLand:
                    case LlhzFlightStripLane.ClearedForTouchAndGo:
                        return new TrafficAdvisory(Location: new TrafficAdvisoryLocation(
                            RelativeOrdering: TrafficAdvisoryLocationOrdering.InFront,
                            Pattern: TrafficAdvisoryLocationPattern.Final,
                            Refinement: TrafficAdvisoryLocationRefinement.EndOf));
                }

                return null;
            }
        }
    }
}
