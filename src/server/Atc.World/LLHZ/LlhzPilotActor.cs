using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Atc.World.Traffic.Maneuvers;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzPilotActor : 
        AIRadioOperatingActor<LlhzPilotActor.PilotState>
    {
        public const string TypeString = "llhz/ai/pilot";

        public record PilotState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent,
            ImmutableStateMachine StateMachine, 
            ActorRef<Traffic.AircraftActor> Aircraft,
            DepartureIntentType DepartureType,
            int RemainingCircuitCount,
            GeoPoint InitialTaxiwayPoint
        ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);
        
        public record ActivationEvent(
            string UniqueId,
            ActorRef<Traffic.AircraftActor> Aircraft,
            DepartureIntentType DepartureType,
            int? CircuitCount,
            GeoPoint InitialTaxiwayPoint
        ) : RadioOperatorActivationEvent(UniqueId, Aircraft.Get().Com1Radio), IActivationStateEvent<LlhzPilotActor>;

        public record DecrementRemainingCircuitCountEvent : IStateEvent;

        public LlhzPilotActor(
            ActivationEvent activation, 
            IStateStore store, 
            IWorldContext world, 
            AIRadioOperatingActor.ILogger logger,
            IVerbalizationService verbaliazionService) 
            : base(
                TypeString, 
                store, 
                verbaliazionService, 
                world, 
                logger,
                CreateParty(activation), 
                activation, 
                CreateInitialState(activation))
        {
            World.Defer($"power-on-radio|{UniqueId}", () => {
                State.Radio.Get().PowerOn();
            });
        }

        protected override ImmutableStateMachine CreateStateMachine()
        {
            return CreatePatternFlightWorkflow();
        }

        protected override PilotState Reduce(PilotState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case DecrementRemainingCircuitCountEvent:
                    return stateBefore with {
                        RemainingCircuitCount = stateBefore.RemainingCircuitCount > 0 
                            ? stateBefore.RemainingCircuitCount - 1
                            : 0
                    };
                default:
                    return base.Reduce(stateBefore, @event);
            }
        }

        private ImmutableStateMachine CreateWorkflow()
        {
            switch (State.DepartureType)
            {
                case DepartureIntentType.ToStayInPattern:
                    return CreatePatternFlightWorkflow();
                case DepartureIntentType.ToTrainingZones:
                    return CreateTrainingZoneFlightWorkflow();
                default:
                    throw new NotSupportedException($"Departure type '{State.DepartureType}' is not supported.");
            }
        }

        private ImmutableStateMachine CreatePatternFlightWorkflow()
        {
            var builder = new ImmutableStateMachine.Builder(
                initialStateName: "PREFLIGHT_INSPECTION",
                dispatchEvent: DispatchStateMachineEvent,
                scheduleDelay: ScheduleStateMachineDelay);
            
            builder.AddState("PREFLIGHT_INSPECTION", state => state
                .OnTimeout(TimeSpan.FromMinutes(1), transitionTo: "CONTACT_CLEARANCE")
            );

            builder.AddConversationState(this, "CONTACT_CLEARANCE", state => state 
                .Monitor(Frequency.FromKhz(130850)) //TODO use frequency from AIP
                .Transmit(() => new GreetingIntent(this))
                .Receive<GoAheadIntent>(transitionTo: "REQUEST_STARTUP"));

            builder.AddConversationState(this, "REQUEST_STARTUP", state => state
                .Transmit(() => new StartupRequestIntent(
                    this,
                    DepartureIntentType.ToStayInPattern,
                    destinationIcao: null))
                .Receive<StartupApprovalIntent>(
                    memorizeIntent: true,
                    readback: () => new StartupApprovalReadbackIntent(
                        this,
                        originalIntent: GetMemorizedIntent<StartupApprovalIntent>()),
                    transitionTo: "HANDOFF_TO_TOWER")
                );
                    
            builder.AddConversationState(this, "HANDOFF_TO_TOWER", state => state
                .Receive<MonitorFrequencyIntent>(
                    memorizeIntent: true,
                    readback: () => new MonitorFrequencyReadbackIntent(
                        this,
                        originalIntent: GetMemorizedIntent<MonitorFrequencyIntent>()),
                    transitionTo: "START_UP")
            );

            builder.AddState("START_UP", state => state
                .OnTimeout(TimeSpan.FromSeconds(10), transitionTo: "DEPARTURE_REQUEST_TAXI")
            );
            
            builder.AddConversationState(this, "DEPARTURE_REQUEST_TAXI", state => state
                .Monitor(Frequency.FromKhz(122200)) //TODO use frequency from AIP
                .Transmit(() => new DepartureTaxiRequestIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.DepartureTaxiRequest), 
                    IntentOptions.Default))
                .Receive<DepartureTaxiClearanceIntent>(
                    memorizeIntent: true,
                    readback: () => new DepartureTaxiClearanceReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.DepartureTaxiClearanceReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<DepartureTaxiClearanceIntent>()),
                    transitionTo: "DEPARTURE_TAXI")
            );
            
            builder.AddState("DEPARTURE_TAXI", state => state
                .OnEnter(machine => BeginDepartureTaxi())
                .OnTrigger("DEPARTURE_TAXI_REACHED_HOLD_PT", transitionTo: "BEFORE_TAKEOFF_RUNUP")
                //.OnTimeout(TimeSpan.FromSeconds(3), transitionTo: "BEFORE_TAKEOFF_RUNUP")
            );
            builder.AddState("BEFORE_TAKEOFF_RUNUP", state => state
                .OnTimeout(TimeSpan.FromSeconds(3), transitionTo: "BEFORE_TAKEOFF_CHECKLIST")
            );
            builder.AddState("BEFORE_TAKEOFF_CHECKLIST", state => state
                .OnTimeout(TimeSpan.FromSeconds(4), transitionTo: "REPORT_READY_FOR_DEPARTURE")
            );

            builder.AddConversationState(this, "REPORT_READY_FOR_DEPARTURE", state => state
                .Transmit(() => new ReportReadyForDepartureIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.ReadyForDepartureReport), 
                    IntentOptions.Default),
                    transitionTo: "AWAIT_TAKEOFF_CLEARANCE"
                )
            );
            
            builder.AddConversationState(this, "AWAIT_TAKEOFF_CLEARANCE", state => state
                .Receive<TakeoffClearanceIntent>(
                    memorizeIntent: true,
                    readback: () => new TakeoffClearanceReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.TakeoffClearanceReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<TakeoffClearanceIntent>()),
                    transitionTo: "TAKEOFF")
                .Receive<LineUpAndWaitIntent>(
                    memorizeIntent: true,
                    readback: () => new LineUpAndWaitReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.LineUpAndWaitInstructionReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<LineUpAndWaitIntent>()))
                .Receive<HoldShortRunwayIntent>(
                    memorizeIntent: true,
                    readback: () => new HoldShortRunwayReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.HoldShortRunwayInstructionReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<HoldShortRunwayIntent>()))
            );

            builder.AddState("TAKEOFF", state => state
                .OnTimeout(TimeSpan.FromSeconds(10), transitionTo: "REPORT_DOWNWIND")
            );

            builder.AddConversationState(this, "REPORT_DOWNWIND", state => state
                // .OnEnter(machine => {
                // })
                .Transmit(() => new ReportDownwindIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.DownwindPositionReport), 
                    IntentOptions.Default))
                .Receive<LandingSequenceAssignmentIntent>(
                    memorizeIntent: true,
                    readback: () => new LandingSequenceAssignmentReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.LandingSequenceAssignmentReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<LandingSequenceAssignmentIntent>()),
                    transitionTo: "IF_REPORT_REMAINING_CIRCUIT_COUNT")
            );

            builder.AddState("IF_REPORT_REMAINING_CIRCUIT_COUNT", state => state.OnEnter(machine => {
                var nextStateName = State.RemainingCircuitCount == 2 
                    ? "REPORT_REMAINING_CIRCUIT_COUNT" 
                    : "PROCEED_TO_FINAL"; 
                Store.Dispatch(this, new DecrementRemainingCircuitCountEvent());
                machine.TransitionTo(nextStateName);
            }));

            builder.AddConversationState(this, "REPORT_REMAINING_CIRCUIT_COUNT", state => state
                .Transmit(() => new ReportRemainingCircuitCountIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.RemainingCircuitCountReport), 
                    IntentOptions.Default,
                    RemainingCircuitCount: State.RemainingCircuitCount))
                .Receive<ReadbackRemainingCircuitCountIntent>(
                    memorizeIntent: false,
                    transitionTo: "PROCEED_TO_FINAL")
            );
            
            builder.AddState("PROCEED_TO_FINAL", state => state
                .OnTimeout(TimeSpan.FromSeconds(10), transitionTo: "REPORT_FINAL")
            );

            builder.AddConversationState(this, "REPORT_FINAL", state => state
                .Transmit(() => new FinalApproachReportIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.FinalApproachReport), 
                    IntentOptions.Default,
                    Runway: GetMemorizedIntent<LandingSequenceAssignmentIntent>().Runway))
                .Receive<LandingClearanceIntent>(
                    memorizeIntent: true,
                    readback: () => new LandingClearanceReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.LandingClearanceReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<LandingClearanceIntent>()),
                    transitionTo: "LAND_OR_TOUCH_AND_GO")
                .Receive<ContinueApproachIntent>(
                    memorizeIntent: false,
                    readback: () => new ContinueApproachReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.ContinueApproachInstructionReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<ContinueApproachIntent>()))
                .Receive<GoAroundInstructionIntent>(
                    memorizeIntent: true,
                    readback: () => new GoAroundInstructionReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.GoAroundInstructionInstructionReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<GoAroundInstructionIntent>()),
                    transitionTo: "GO_AROUND")
            );

            builder.AddState("LAND_OR_TOUCH_AND_GO", state => state.OnEnter(machine => {
                machine.TransitionTo(State.RemainingCircuitCount < 1
                    ? "LAND"
                    : "TOUCH_AND_GO");
            }));

            builder.AddState("LAND", state => state
                .OnTimeout(TimeSpan.FromSeconds(10), transitionTo: "END_OF_FLIGHT")
            );

            builder.AddState("TOUCH_AND_GO", state => state
                .OnTimeout(TimeSpan.FromSeconds(10), transitionTo: "REPORT_DOWNWIND")
            );
            
            builder.AddState("GO_AROUND", state => state
                .OnTimeout(TimeSpan.FromSeconds(10), transitionTo: "REPORT_DOWNWIND")
            );

            builder.AddConversationState(this, "END_OF_FLIGHT", state => state
                .Transmit(() => new FarewellIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.Farewell), 
                    new IntentOptions(Flags: IntentOptionFlags.HasThanks)))
            );

            return builder.Build();
        }

        private void BeginDepartureTaxi()
        {
            var info = State.StateMachine.GetMemorizedIntent<StartupApprovalIntent>().Information!;
            var is29 = info.ActiveRunwaysDeparture[0] == "29";
            var taxi = new ManeuverBuilder(
                "departure-taxi", 
                State.Aircraft.Get().GetCurrentSituation().Location, 
                World.UtcNow());
            taxi.MoveTo("pull-out", State.InitialTaxiwayPoint, Speed.FromKnots(5));

            var holdPoint = is29
                ? new GeoPoint(32.181513, 34.829653)  // S
                : new GeoPoint(32.181468, 34.829773); // hold east to P  
            
            taxi.MoveTo("pull-out", State.InitialTaxiwayPoint, Speed.FromKnots(3));
            taxi.MoveTo("taxi-to-hold-pt", holdPoint, Speed.FromKnots(5));

            var reachHoldingPointUtc = taxi.FinishUtc;
            
            taxi.Wait("hold-short", TimeSpan.FromDays(1));
            
            var maneuver = taxi.GetManeuver();
            State.Aircraft.Get().ReplaceManeuver(maneuver);
            World.DeferUntil(
                "end-of-departure-taxi", 
                reachHoldingPointUtc,
                () => State.StateMachine.ReceiveTrigger("DEPARTURE_TAXI_REACHED_HOLD_PT"));
        }

        private T GetMemorizedIntent<T>() where T : Intent
        {
            return State.StateMachine.GetMemorizedIntent<T>();
        }

        private ImmutableStateMachine CreateTrainingZoneFlightWorkflow()
        {
            throw new NotImplementedException();
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<LlhzPilotActor, ActivationEvent>(
                TypeString,
                (activation, dependencies) => new LlhzPilotActor(
                    activation,
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(), 
                    dependencies.Resolve<AIRadioOperatingActor.ILogger>(),
                    dependencies.Resolve<IVerbalizationService>() 
                )
            );
        }
        
        private static PartyDescription CreateParty(ActivationEvent activation)
        {
            return new PersonDescription(
                activation.UniqueId, 
                callsign: activation.Aircraft.Get().TailNo, 
                NatureType.AI, 
                VoiceDescription.Default with {
                    Language = "he-IL"
                }, 
                GenderType.Male, 
                AgeType.Senior,
                firstName: null);
        }

        private static PilotState CreateInitialState(ActivationEvent activation)
        {
            return new PilotState(
                activation.Radio,
                null,
                ImmutableStateMachine.Empty,
                activation.Aircraft,
                activation.DepartureType,
                RemainingCircuitCount: activation.CircuitCount ?? 0,
                InitialTaxiwayPoint: activation.InitialTaxiwayPoint);
        }
    }
}
