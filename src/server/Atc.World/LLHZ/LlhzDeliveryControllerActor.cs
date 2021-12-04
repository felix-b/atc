using System;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzDeliveryControllerActor : 
        AIRadioOperatingActor<LlhzDeliveryControllerActor.DeliveryControllerState>
    {
        public const string TypeString = "llhz-atc-clrdel";

        public record DeliveryControllerState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent,
            ImmutableStateMachine StateMachine,
            ActorRef<LlhzAirportActor> Airport,
            ImmutableDictionary<string, LlhzFlightStrip> StripBoard,
            Intent? IntentToTransmitNext
        ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);

        public record ActivationEvent(
            string UniqueId,
            ActorRef<RadioStationActor> Radio,
            ActorRef<LlhzAirportActor> Airport
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<LlhzDeliveryControllerActor>;

        public record AddFlightStripEvent(
            LlhzFlightStrip FlightStrip,
            Intent? Transmit
        ) : IStateEvent;

        public record UpdateFlightStripEvent(
            LlhzFlightStrip FlightStrip,
            Intent? Transmit
        ) : IStateEvent;

        public LlhzDeliveryControllerActor(
            ActivationEvent activation, 
            IStateStore store, 
            IWorldContext world,
            AIRadioOperatingActor.ILogger logger,
            IVerbalizationService verbalizationService) 
            : base(
                TypeString, 
                store, 
                verbalizationService, 
                world,
                logger,
                CreateParty(), 
                activation, 
                CreateInitialState(activation))
        {
            World.Defer(() => {
                State.Radio.Get().PowerOn();
                State.Radio.Get().TuneTo(Frequency.FromKhz(130850));
            });
        }

        protected override ImmutableStateMachine CreateStateMachine()
        {
            var builder = new ImmutableStateMachine.Builder(
                initialStateName: "SLEEP",
                dispatchEvent: DispatchStateMachineEvent,
                scheduleDelay: ScheduleStateMachineDelay);

            builder.AddState("SLEEP", state => state
                .OnEnterStartSequence(sequence => {
                    sequence.AddDelayStep("TIMER", TimeSpan.FromSeconds(5), inheritTriggers: true);
                    sequence.AddTransitionStep("TICK", targetStateName: "THINK");
                })
                .OnAnyIntent(transitionTo: "THINK", memorizeIntent: false)
            );

            builder.AddState("THINK", state => state
                .OnEnterStartSequence(sequence => {
                    sequence.AddStep("RUN_LOGIC", Think);
                })
            );

            builder.AddConversationState(this, "ACT", state => state
                .Transmit(GetIntentToTransmitNextOrThrow, transitionTo: "THINK")
                //TODO: add OnAnyIntent to AWAIT_SILENCE, for the case another intent comes in that's more urgent to handle
            );

            return builder.Build();
        }

        protected override DeliveryControllerState Reduce(DeliveryControllerState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case AddFlightStripEvent add:
                    return HandleAddFlightStrp(add);
                case UpdateFlightStripEvent update:
                    return HandleUpdateFlightStrp(update);
                default:
                    return base.Reduce(stateBefore, @event);
            }

            DeliveryControllerState HandleAddFlightStrp(AddFlightStripEvent @event)
            {
                var callsign = @event.FlightStrip.Callsign;
                if (stateBefore.StripBoard.ContainsKey(callsign))
                {
                    throw new InvalidActorEventException($"Flight strip for callsign '{callsign}' was already added");
                }
                return stateBefore with {
                    StripBoard = stateBefore.StripBoard.Add(callsign, @event.FlightStrip),
                    IntentToTransmitNext = @event.Transmit
                };
            }

            DeliveryControllerState HandleUpdateFlightStrp(UpdateFlightStripEvent @event)
            {
                var callsign = @event.FlightStrip.Callsign;
                if (!stateBefore.StripBoard.ContainsKey(callsign))
                {
                    throw new InvalidActorEventException($"No flight strip for callsign '{callsign}'");
                }
                return stateBefore with {
                    StripBoard = stateBefore.StripBoard.SetItem(callsign, @event.FlightStrip),
                    IntentToTransmitNext = @event.Transmit
                };
            }
        }

        private Intent GetIntentToTransmitNextOrThrow()
        {
            return State.IntentToTransmitNext ?? throw new InvalidOperationException("IntentToTransmitNext is null");
        }
        
        private void Think(IStateMachineContext context)
        {
            switch (context.LastReceivedIntent)
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
                default:
                    context.TransitionTo("SLEEP");
                    break;
            }

            if (State.IntentToTransmitNext != null)
            {
                context.TransitionTo("ACT"); //???, resetLastReceivedIntent: false);
            }

            void HandleGreeting(GreetingIntent greeting)
            {
                var aircraft = State.Airport.Get().GetAircraftByCallsign(greeting.CallsignCalling);
                var flightStrip = new LlhzFlightStrip(
                    DepartureIntentType.Unspecified,
                    aircraft,
                    LlhzFlightStripLane.Unspecified);
                var goAhead = CreateIntent(
                    aircraft,
                    WellKnownIntentType.GoAheadInstruction,
                    header => new GoAheadInstructionIntent(header, IntentOptions.Default));
                Store.Dispatch(this, new AddFlightStripEvent(
                    flightStrip,
                    Transmit: goAhead));
            }

            void HandleStartupRequest(StartupRequestIntent request)
            {
                var flightStrip = State.StripBoard[request.CallsignCalling];
                var vfrClearance = new VfrClearance(null, null, null);
                var approval = CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.StartupApproval,
                    header => new StartupApprovalIntent(header, IntentOptions.Default, vfrClearance));
                Store.Dispatch(this, new UpdateFlightStripEvent(
                    flightStrip with {
                        Lane = LlhzFlightStripLane.StartupApproved
                    },
                    Transmit: approval));
            }

            void HandleStartupApprovalReadback(StartupApprovalReadbackIntent readback)
            {
                
            }
        }

        private T CreateIntent<T>(
            ActorRef<AircraftActor> recipient,
            WellKnownIntentType type,  
            Func<IntentHeader, T> factory) 
            where T : Intent
        {
            var header = new IntentHeader(
                type,
                CustomCode: 0,
                OriginatorUniqueId: Radio.UniqueId,
                OriginatorCallsign: Radio.Callsign,
                RecipientUniqueId: recipient.UniqueId,
                RecipientCallsign: recipient.Get().Callsign,
                CreatedAtUtc: World.UtcNow());

            T intent = factory(header);
            return intent;
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<LlhzDeliveryControllerActor, ActivationEvent>(
                TypeString,
                (activation, dependencies) => new LlhzDeliveryControllerActor(
                    activation,
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(), 
                    dependencies.Resolve<AIRadioOperatingActor.ILogger>(),
                    dependencies.Resolve<IVerbalizationService>() 
                )
            );
        }

        private static PartyDescription CreateParty()
        {
            return new PersonDescription(
                "#1", 
                "Herzlia Clearance", 
                NatureType.AI, 
                VoiceDescription.Default, 
                GenderType.Male, 
                AgeType.Senior, 
                "Bob");
        }
        
        private static DeliveryControllerState CreateInitialState(ActivationEvent activation)
        {
            return new DeliveryControllerState(
                Radio: activation.Radio, 
                PendingTransmissionIntent: null,
                StateMachine: ImmutableStateMachine.Empty,
                Airport: activation.Airport,
                StripBoard: ImmutableDictionary<string, LlhzFlightStrip>.Empty,
                IntentToTransmitNext: null);
        }
    }
}
