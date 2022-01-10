using System.Linq;
using Atc.Data.Control;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
using ProtoBuf;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzClearanceControllerBrain : ILlhzControllerBrain
    {
        private ActorRef<LlhzControllerActor> _towerController;
        
        public void Initialize(ILlhzControllerBrainState state)
        {
            _towerController = state.Airport
                .GetChildrenOfType<LlhzControllerActor>()
                .Single(actor => actor.Get().GetControllerPosition().Type == ControllerPositionType.Local);
        }

        public void ObserveSituation(ILlhzControllerBrainState state, ILlhzControllerBrainActions output)
        {
            // nothing
        }

        public void HandleIncomingTransmission(Intent intent, ILlhzControllerBrainState state, ILlhzControllerBrainActions output)
        {
            switch (intent)
            {
                case GreetingIntent greeting:
                    HandleGreeting(greeting);
                    break;
                case StartupRequestIntent startupRequest:
                    HandleStartupRequest(startupRequest);
                    break;
                case StartupApprovalReadbackIntent approvalReadback:
                    HandleStartupApprovalReadback(approvalReadback);
                    break;
            }

            void HandleGreeting(GreetingIntent greeting)
            {
                var aircraft = state.Airport.GetAircraftByCallsign(greeting.CallsignCalling);
                var flightStrip = new LlhzFlightStrip(
                    DepartureIntentType.Unspecified,
                    aircraft,
                    LlhzFlightStripLane.Unspecified);
                var goAhead = output.CreateIntent(
                    aircraft,
                    WellKnownIntentType.GoAheadInstruction,
                    header => new GoAheadIntent(header, IntentOptions.Default));
                
                output.AddFlightStrip(flightStrip);
                output.Transmit(goAhead);
            }

            void HandleStartupRequest(StartupRequestIntent request)
            {
                var flightStrip = state.StripBoard[request.CallsignCalling];
                var isInitialPointBatzra800 = (
                    request.DepartureType == DepartureIntentType.ToTrainingZones ||
                    request.DepartureType == DepartureIntentType.ToDestination);
                var vfrClearance = new VfrClearance(
                    request.DepartureType, 
                    request.DestinationIcao, 
                    InitialHeading: null, 
                    InitialNavaid: isInitialPointBatzra800 
                        ? "בצרה" 
                        : null, 
                    InitialAltitude: isInitialPointBatzra800 
                        ? Altitude.FromFeetMsl(800) 
                        : null);
                var approval = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.StartupApproval,
                    header => new StartupApprovalIntent(
                        header, 
                        IntentOptions.Default, 
                        state.Airport.Atis,
                        vfrClearance));
                
                output.UpdateFlightStrip(flightStrip with {
                    Lane = LlhzFlightStripLane.StartupApproved
                });
                output.Transmit(approval);
            }

            void HandleStartupApprovalReadback(StartupApprovalReadbackIntent readback)
            {
                var flightStrip = state.StripBoard[readback.CallsignCalling];
                var instruction = output.CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.MonitorFrequencyInstruction,
                    header => new MonitorFrequencyIntent(
                        header, 
                        new IntentOptions(new IntentCondition(ConditionSubjectType.Startup, ConditionTimingType.After)),
                        Frequency.FromKhz(122200),//TODO: get this from the airport? 
                        ControllerPositionType.Local,
                        ControllerCallsign: null));

                output.UpdateFlightStrip(flightStrip with {
                    Lane = LlhzFlightStripLane.StartupApproved
                });
                output.Transmit(instruction);
            }
        }

        public ControllerPositionType PositionType => ControllerPositionType.ClearanceDelivery;
    }
}
