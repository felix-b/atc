using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
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
            World.Defer(() => {
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
                .OnEnterStartSequence(sequence => {
                    sequence.AddDelayStep("CHECKLIST", TimeSpan.FromMinutes(1), inheritTriggers: false);
                    sequence.AddTriggerStep("DONE", "PREFLIGHT_INSPECTION_OK");
                })
                .OnTrigger("PREFLIGHT_INSPECTION_OK", transitionTo: "CONTACT_CLEARANCE")
            );

            builder.AddConversationState(this, "CONTACT_CLEARANCE", state => state 
                .Monitor(Frequency.FromKhz(130850)) 
                .Transmit(() => new GreetingIntent(this))
                .Receive<GoAheadInstructionIntent>(transitionTo: "REQUEST_STARTUP"));

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

        //     
        //     builder.AddState("CONTACT_CLEARANCE", state => state
        //         .OnEnter(sequence => {
        //             sequence.AddStep(() => State.Radio.Get().TuneTo(Frequency.FromKhz(130850)));
        //             sequence.AddStep(() => State.Radio.Get().PowerOn());
        //             sequence.AddStep(() => Transmit(new GreetingIntent(CreateIntentHeader(WellKnownIntentType.Greeting))));
        //         })
        //         .OnIntent<GoAheadInstructionIntent>(transitionTo: "READBACK_STARTUP_APPROVAL")
        //     );
        //
        //     builder.AddState("READBACK_STARTUP_APPROVAL", state => state
        //         .OnEnter(sequence => {
        //             sequence.AddStep(() => State.Radio.Get().TuneTo(Frequency.FromKhz(130850)));
        //             sequence.AddStep(() => State.Radio.Get().PowerOn());
        //             sequence.AddStep(() => Transmit(new GreetingIntent(CreateIntentHeader(WellKnownIntentType.Greeting))));
        //         })
        //         .OnIntent<GoAheadInstructionIntent>(transitionTo: "READBACK_STARTUP_APPROVAL")
        //     );
        //
        //     builder.AddState("REQUEST_STARTUP", state => state
        //         .OnEnter(sequence => {
        //             sequence.AddStep(() => Transmit(new StartupRequestIntent(
        //                 CreateIntentHeader(WellKnownIntentType.StartupRequest), 
        //                 DepartureIntentType.ToStayInPattern, 
        //                 DestinationIcao: null)));
        //         })
        //         .OnIntent<StartupApprovalIntent>(transitionTo: "READBACK_STARTUP_APPROVAL")
        //     );
        //
        //     builder.AddState("READBACK_STARTUP_APPROVAL", state => state
        //         .OnEnter(sequence => {
        //             sequence.AddStep(() => Transmit(new StartupRequestIntent(
        //                 CreateIntentHeader(WellKnownIntentType.StartupRequest), 
        //                 DepartureIntentType.ToStayInPattern, 
        //                 DestinationIcao: null)));
        //         })
        //         .OnIntent<GoAheadInstructionIntent>(transitionTo: "REQUEST_STARTUP")
        //     );
            
            return builder.Build();
        }
        
        private T GetMemorizedIntent<T>() where T : Intent
        {
            throw new NotImplementedException();
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
                VoiceDescription.Default, 
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
