using System;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
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
            ImmutableQueue<Intent> IncomingIntents,
            ImmutableQueue<Intent> OutgoingIntents
        ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);

        public record ActivationEvent(
            string UniqueId,
            ActorRef<RadioStationActor> Radio,
            ActorRef<LlhzAirportActor> Airport
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<LlhzDeliveryControllerActor>;

        public record DequeueIncomingIntent : IStateEvent;

        public record DequeueOutgoingIntent : IStateEvent;

        public record AddFlightStripEvent(
            LlhzFlightStrip FlightStrip,
            bool ConsumeIncomingIntent,
            Intent? Transmit
        ) : IStateEvent;

        public record UpdateFlightStripEvent(
            LlhzFlightStrip FlightStrip,
            bool ConsumeIncomingIntent,
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
            World.Defer($"power-on-radio|{UniqueId}", () => {
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
                .OnAnyIntent(transitionTo: "THINK", memorizeIntent: false)
                .OnTimeout(TimeSpan.FromSeconds(5), transitionTo: "THINK")
            );

            builder.AddState("THINK", state => state.OnEnter(Think));

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
                case DequeueIncomingIntent:
                    return stateBefore with {
                        IncomingIntents = stateBefore.IncomingIntents.Dequeue()
                    };
                case DequeueOutgoingIntent:
                    return stateBefore with {
                        OutgoingIntents = stateBefore.OutgoingIntents.Dequeue()
                    };
                case AddFlightStripEvent add:
                    return HandleAddFlightStrp(add);
                case UpdateFlightStripEvent update:
                    return HandleUpdateFlightStrp(update);
                default:
                    return HandleBaseEvent();
            }

            DeliveryControllerState HandleBaseEvent()
            {
                var stateAfter = base.Reduce(stateBefore, @event);
                if (stateAfter != stateBefore && stateAfter.StateMachine.LastReceivedIntent != null)
                {
                    return stateAfter with {
                        IncomingIntents = stateAfter.IncomingIntents.Enqueue(stateAfter.StateMachine.LastReceivedIntent)
                    };
                }
                return stateAfter;
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
                    IncomingIntents = @event.ConsumeIncomingIntent
                        ? stateBefore.IncomingIntents.Dequeue()
                        : stateBefore.IncomingIntents,
                    OutgoingIntents = @event.Transmit != null  
                        ? stateBefore.OutgoingIntents.Enqueue(@event.Transmit)
                        : stateBefore.OutgoingIntents
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
                    IncomingIntents = @event.ConsumeIncomingIntent
                        ? stateBefore.IncomingIntents.Dequeue()
                        : stateBefore.IncomingIntents,
                    OutgoingIntents = @event.Transmit != null  
                        ? stateBefore.OutgoingIntents.Enqueue(@event.Transmit)
                        : stateBefore.OutgoingIntents
                };
            }
        }

        private Intent GetIntentToTransmitNextOrThrow()
        {
            if (State.OutgoingIntents.IsEmpty)
            {
                throw new InvalidOperationException("IntentToTransmitNext is null");
            }

            var intent = State.OutgoingIntents.Peek();
            //TODO: defer dequeue until begin of transmission??? -> avoid loosing outgoing intent on failover
            Store.Dispatch(this, new DequeueOutgoingIntent());
            return intent;
        }
        
        private void Think(IStateMachineContext context)
        {
            while (!State.IncomingIntents.IsEmpty)
            {
                var nextIncomingIntent = State.IncomingIntents.Peek();

                switch (nextIncomingIntent)
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
                        Store.Dispatch(this, new DequeueIncomingIntent());
                        break;
                }
            }

            if (!State.OutgoingIntents.IsEmpty)
            {
                State.StateMachine.TransitionTo("ACT", resetLastReceivedIntent: true);
            }
            else
            {
                State.StateMachine.TransitionTo("SLEEP", resetLastReceivedIntent: true);
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
                    header => new GoAheadIntent(header, IntentOptions.Default));
                Store.Dispatch(this, new AddFlightStripEvent(
                    flightStrip,
                    ConsumeIncomingIntent: true,
                    Transmit: goAhead));
            }

            void HandleStartupRequest(StartupRequestIntent request)
            {
                var flightStrip = State.StripBoard[request.CallsignCalling];
                var vfrClearance = new VfrClearance(
                    request.DepartureType, 
                    request.DestinationIcao, 
                    InitialHeading: null, 
                    InitialNavaid: request.DepartureType == DepartureIntentType.ToTrainingZones 
                        ? "בצרה" 
                        : null, 
                    InitialAltitude: request.DepartureType == DepartureIntentType.ToTrainingZones 
                        ? Altitude.FromFeetMsl(800) 
                        : null);
                var approval = CreateIntent(
                    flightStrip.Aircraft,
                    WellKnownIntentType.StartupApproval,
                    header => new StartupApprovalIntent(
                        header, 
                        IntentOptions.Default, 
                        State.Airport.Get().Atis,
                        vfrClearance));
                Store.Dispatch(this, new UpdateFlightStripEvent(
                    flightStrip with {
                        Lane = LlhzFlightStripLane.StartupApproved
                    },
                    ConsumeIncomingIntent: true,
                    Transmit: approval));
            }

            void HandleStartupApprovalReadback(StartupApprovalReadbackIntent readback)
            {
                Store.Dispatch(this, new DequeueIncomingIntent());
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
                OriginatorPosition: Radio.Location,
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
                IncomingIntents: ImmutableQueue<Intent>.Empty, 
                OutgoingIntents: ImmutableQueue<Intent>.Empty);
        }
    }
}
