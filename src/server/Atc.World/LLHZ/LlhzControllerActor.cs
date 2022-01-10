using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Atc.Data.Control;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Microsoft.AspNetCore.Http;
using Zero.Loss.Actors;

namespace Atc.World.LLHZ
{
    public class LlhzControllerActor : 
        AIRadioOperatingActor<LlhzControllerActor.ControllerState>
    {
        public const string TypeString = "llhz-atc-controller";

        public record ControllerState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent,
            ImmutableStateMachine StateMachine,
            ControllerPositionData ControllerPosition,
            ActorRef<LlhzAirportActor> Airport,
            ImmutableDictionary<string, LlhzFlightStrip> StripBoard,
            ImmutableQueue<Intent> IncomingIntents,
            ImmutableQueue<Intent> OutgoingIntents
        ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);

        public record ActivationEvent(
            string UniqueId,
            ControllerPositionType PositionType,
            ActorRef<RadioStationActor> Radio,
            ActorRef<LlhzAirportActor> Airport
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<LlhzControllerActor>;

        
        public record DequeueIncomingIntent : IStateEvent;
        public record EnqueueOutgoingIntentEvent(Intent Intent) : IStateEvent;
        public record DequeueOutgoingIntent : IStateEvent;

        public record ApplyBrainOutputEvent(
            bool ConsumedIncomingIntent,
            ImmutableList<Intent>? TransmittedIntents,
            ImmutableList<LlhzFlightStrip>? AddedFlightStrips,
            ImmutableList<LlhzFlightStrip>? RemovedFlightStrips,
            ImmutableList<LlhzFlightStrip>? UpdatedFlightStrips,
            ImmutableList<LlhzFlightStripHandoff>? HandedOffFlightStrips
        ) : IStateEvent;

        public record LlhzFlightStripHandoff(
            LlhzFlightStrip FlightStrip,
            ActorRef<LlhzControllerActor> ToController
        );

        private readonly ILlhzControllerBrain _brain;
        
        public LlhzControllerActor(
            ActivationEvent activation, 
            ILlhzControllerBrain brain,
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
            _brain = brain;
            
            World.Defer($"power-on-radio|{UniqueId}", () => {
                State.Radio.Get().PowerOn();
                State.Radio.Get().TuneTo(Frequency.FromKhz(130850));
            });
        }

        public ControllerPositionData GetControllerPosition()
        {
            return State.ControllerPosition;
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

        protected override ControllerState Reduce(ControllerState stateBefore, IStateEvent @event)
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
                case ApplyBrainOutputEvent outputEvent:
                    return HandleApplyBrainOutputEvent(outputEvent);
                default:
                    return HandleBaseEvent();
            }

            ControllerState HandleBaseEvent()
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

            ControllerState HandleApplyBrainOutputEvent(ApplyBrainOutputEvent outputEvent)
            {
                var nextStripBoard = stateBefore.StripBoard;
                var nextIncomingIntents = outputEvent.ConsumedIncomingIntent
                    ? stateBefore.IncomingIntents.Dequeue()
                    : stateBefore.IncomingIntents;
                var nextOutgoingIntents = stateBefore.OutgoingIntents;
                outputEvent.TransmittedIntents?.ForEach(intent => {
                    nextOutgoingIntents = nextOutgoingIntents.Enqueue(intent);
                });

                if (outputEvent.AddedFlightStrips != null)
                {
                    nextStripBoard = nextStripBoard.AddRange(
                        outputEvent.AddedFlightStrips.Select(
                            strip => new KeyValuePair<string, LlhzFlightStrip>(strip.Callsign, strip)));
                }
                
                if (outputEvent.UpdatedFlightStrips != null)
                {
                    nextStripBoard = nextStripBoard.SetItems(
                        outputEvent.UpdatedFlightStrips.Select(
                            strip => new KeyValuePair<string, LlhzFlightStrip>(strip.Callsign, strip)));
                }

                if (outputEvent.RemovedFlightStrips != null)
                {
                    nextStripBoard = nextStripBoard.RemoveRange(
                        outputEvent.RemovedFlightStrips.Select(
                            strip => strip.Callsign));
                }

                if (outputEvent.HandedOffFlightStrips != null)
                {
                    nextStripBoard = nextStripBoard.RemoveRange(
                        outputEvent.HandedOffFlightStrips.Select(
                            handoff => handoff.FlightStrip.Callsign));
                }
                
                return stateBefore with {
                    StripBoard = nextStripBoard,
                    IncomingIntents = nextIncomingIntents,
                    OutgoingIntents = nextOutgoingIntents
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
            LlhzControllerBrainContext brainContext = new(
                State.ControllerPosition, 
                State.StripBoard, 
                State.Radio, 
                State.Airport, 
                World);

            ObserveSituation();
            HandleIncomingIntents();
            TransitionToNextState();

            void ObserveSituation()
            {
                _brain.ObserveSituation(brainContext.AsState, brainContext.AsActions);
                if (brainContext.HasOutputs())
                {
                    var observationOutputEvent = brainContext.TakeOutputEventAndClear(consumedIncomingIntent: false);
                    Store.Dispatch(this, observationOutputEvent);
                }
            }

            void HandleIncomingIntents()
            {
                while (!State.IncomingIntents.IsEmpty)
                {
                    var nextIncomingIntent = State.IncomingIntents.Peek();
                
                    _brain.HandleIncomingTransmission(nextIncomingIntent, brainContext.AsState, brainContext.AsActions);
                    // always dispatch output event because at least we have to dequeue the consumed intent
                    var intentOutputEvent = brainContext.TakeOutputEventAndClear(consumedIncomingIntent: true);
                    Store.Dispatch(this, intentOutputEvent);
                }
            }

            void TransitionToNextState()
            {
                if (!State.OutgoingIntents.IsEmpty)
                {
                    State.StateMachine.TransitionTo("ACT", resetLastReceivedIntent: true);
                }
                else
                {
                    State.StateMachine.TransitionTo("SLEEP", resetLastReceivedIntent: true);
                }
            }
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<LlhzControllerActor, ActivationEvent>(
                TypeString,
                (activation, dependencies) => new LlhzControllerActor(
                    activation,
                    CreateBrain(activation.PositionType),
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(), 
                    dependencies.Resolve<AIRadioOperatingActor.ILogger>(),
                    dependencies.Resolve<IVerbalizationService>() 
                )
            );
        }

        private static ILlhzControllerBrain CreateBrain(ControllerPositionType positionType)
        {
            switch (positionType)
            {
                case ControllerPositionType.ClearanceDelivery:
                    return new LlhzClearanceControllerBrain();
                case ControllerPositionType.Local:
                    return new LlhzTowerControllerBrain();
                default:
                    throw new NotSupportedException($"AI controller position type '{positionType}' is not supported");
            }
        }

        private static PartyDescription CreateParty()
        {
            return new PersonDescription(
                "#1", 
                "Hertzlia Clearance", 
                NatureType.AI, 
                VoiceDescription.Default with {
                    Language = "he-IL",
                    Gender = VoiceGender.Female
                }, 
                GenderType.Male, 
                AgeType.Senior, 
                "Bob");
        }
        
        private static ControllerState CreateInitialState(ActivationEvent activation)
        {
            return new ControllerState(
                Radio: activation.Radio, 
                PendingTransmissionIntent: null,
                StateMachine: ImmutableStateMachine.Empty,
                ControllerPosition: new ControllerPositionData(),
                Airport: activation.Airport,
                StripBoard: ImmutableDictionary<string, LlhzFlightStrip>.Empty,
                IncomingIntents: ImmutableQueue<Intent>.Empty, 
                OutgoingIntents: ImmutableQueue<Intent>.Empty);
        }
    }
}
