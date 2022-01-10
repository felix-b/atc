using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
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
            ActorRef<AircraftActor> Aircraft,
            DepartureIntentType DepartureType
        ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);

        public record ActivationEvent(
            string UniqueId,
            ActorRef<AircraftActor> Aircraft,
            DepartureIntentType DepartureType
        ) : RadioOperatorActivationEvent(UniqueId, Aircraft.Get().Com1Radio), IActivationStateEvent<LlhzPilotActor>;

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
                .OnTimeout(TimeSpan.FromMinutes(1), transitionTo: "DEPARTURE_REQUEST_TAXI")
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
                .OnTimeout(TimeSpan.FromMinutes(30), transitionTo: "BEFORE_TAKEOFF_RUNUP")
            );
            builder.AddState("BEFORE_TAKEOFF_RUNUP", state => state
                .OnTimeout(TimeSpan.FromSeconds(30), transitionTo: "BEFORE_TAKEOFF_CHECKLIST")
            );
            builder.AddState("BEFORE_TAKEOFF_CHECKLIST", state => state
                .OnTimeout(TimeSpan.FromSeconds(30), transitionTo: "REPORT_READY_FOR_DEPARTURE")
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
                .OnTimeout(TimeSpan.FromMinutes(30), transitionTo: "REPORT_DOWNWIND")
            );

            builder.AddConversationState(this, "REPORT_DOWNWIND", state => state
                .Transmit(() => new DepartureTaxiRequestIntent(
                    CreatePilotToAtcIntentHeader(WellKnownIntentType.DownwindPositionReport), 
                    IntentOptions.Default))
                .Receive<LandingSequenceAssignmentIntent>(
                    memorizeIntent: true,
                    readback: () => new LandingSequenceAssignmentReadbackIntent(
                        CreatePilotToAtcIntentHeader(WellKnownIntentType.LandingSequenceAssignmentReadback), 
                        IntentOptions.Default,
                        OriginalIntent: GetMemorizedIntent<LandingSequenceAssignmentIntent>()),
                    transitionTo: "PROCEED_TO_FINAL")
            );

            builder.AddState("PROCEED_TO_FINAL", state => state
                .OnTimeout(TimeSpan.FromMinutes(50), transitionTo: "REPORT_DOWNWIND")
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
                    transitionTo: "LAND")
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

            builder.AddState("LAND", state => state
                .OnTimeout(TimeSpan.FromMinutes(40), transitionTo: "END_OF_FLIGHT")
            );

            builder.AddState("GO_AROUND", state => state
                .OnTimeout(TimeSpan.FromMinutes(40), transitionTo: "REPORT_DOWNWIND")
            );

            builder.AddState("END_OF_FLIGHT", state => {});

            return builder.Build();
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
                activation.DepartureType);
        }
    }
}
