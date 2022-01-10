using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Control;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
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
            switch (intent)
            {
                case DepartureTaxiRequestIntent taxiRequest:
                    HandleDepartureTaxiRequest(taxiRequest);
                    break;
            }

            void HandleDepartureTaxiRequest(DepartureTaxiRequestIntent request)
            {
                var flightStrip = state.StripBoard[request.CallsignCalling];
                var atis = state.Airport.Atis;

                var clearance = new DepartureTaxiClearance(
                    ActiveRunway: atis.ActiveRunwaysDeparture[0],
                    HoldingPoint: null,
                    TaxiPath: ImmutableList<string>.Empty,
                    HoldShortRunways: ImmutableList<string>.Empty); 

                var clearanceIntent = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.DepartureTaxiClearance,
                    header => new DepartureTaxiClearanceIntent(
                        header, 
                        new IntentOptions(Condition: null, Flags: IntentOptionFlags.HasGreeting), 
                        OriginalRequest: request,
                        Cleared: true,
                        Clearance: clearance));
                
                output.UpdateFlightStrip(flightStrip with {
                    Lane = LlhzFlightStripLane.DepartureTaxiApproved
                });
                output.Transmit(clearanceIntent);
            }

            void HandleDepartureTaxiClearanceReadback(DepartureTaxiRequestIntent request)
            {
                //TODO how do we handle (missing) readbacks?
            }
        }

        public ControllerPositionType PositionType => ControllerPositionType.ClearanceDelivery;
    }
}
