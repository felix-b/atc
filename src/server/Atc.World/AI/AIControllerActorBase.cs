#if false

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Atc.World.Control;
using Zero.Loss.Actors;

namespace Atc.World.AI
{
    public abstract class AIControllerActorBase : AIRadioOperatingActor<AIControllerActorBase.AIControllerState>
    {
        public record AIControllerState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent,
            ImmutableList<IncomingPilotsIntent> IncomingIntents 
        ) : RadioOperatorState(Radio, PendingTransmissionIntent);

        public record IncomingPilotsIntent(
            FlightStrip From,
            Intent Intent
        );

        protected AIControllerActorBase(
            string typeString,
            IStateStore store, 
            IVerbalizationService verbalizationService, 
            IWorldContext world, 
            RadioOperatorActivationEvent activation,
            PartyDescription party,
            AIControllerState initialState) 
            : base(typeString, store, verbalizationService, world, party, activation, initialState)
        {
            World.Defer(() => {
                DispatchStateMachineEvent(new ImmutableStateMachine.TriggerEvent(
                    State.Role == PingPongRole.Ping
                        ? "START_PING_ROLE"
                        : "START_PONG_ROLE"));
            });
        }


        protected override void ReceiveIntent(Intent intent)
        {
            intent.Header.OriginatorCallsign
            
            var time = World.UtcNow().TimeOfDay;
            _intentLog.Add($"{time}:{intent.Header.OriginatorCallsign}->{intent.Header.RecipientCallsign}:{intent}");

            base.ReceiveIntent(intent);
        }

        protected override ImmutableStateMachine CreateStateMachine()
        {
            var builder = CreateStateMachineBuilder(initialStateName: "START");
            
            builder.AddConversationState(this, "START", state => state
                .Monitor(Frequency.FromKhz(118000))
                .OnTrigger("START_PING_ROLE", transitionTo: "DELAY_NEXT_PING")
                .OnTrigger("START_PONG_ROLE", transitionTo: "AWAIT_PONG")
            );

            builder.AddState(
                "DELAY_NEXT_PING",
                state => state.OnEnterStartSequence(sequence => sequence
                    .AddDelayStep("DELAY", TimeSpan.FromSeconds(5))
                    .AddTransitionStep("TRANSITION", targetStateName: "PING")
                ));
            
            builder.AddConversationState(this, "PING", state => state
                .OnEnter(machine => Store.Dispatch(this, new IncrementRepeatCountEvent()))
                .Transmit(() => new TestPingIntent(World, State.RepeatCount, this, State.Counterparty))
                .Receive<TestPongIntent>(transitionTo: "DELAY_NEXT_PING")
            );
            
            builder.AddConversationState(this, "AWAIT_PONG", state => state
                .OnEnter(machine => Store.Dispatch(this, new IncrementRepeatCountEvent()))
                .Receive<TestPingIntent>(
                    readback: () => new TestPongIntent(World, State.RepeatCount, this, State.Counterparty),
                    transitionTo: "AWAIT_PONG")
            );

            return builder.Build();
        }
        
        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<DummyPingPongActor, DummyActivationEvent>(
                TypeString,
                (activation, dependencies) => new DummyPingPongActor(
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IVerbalizationService>(), 
                    dependencies.Resolve<IWorldContext>(), 
                    activation
                )
            );
        }

        private static PartyDescription CreateParty(DummyActivationEvent activation)
        {
            return new PersonDescription(
                activation.UniqueId, 
                callsign: activation.Radio.Get().Callsign, 
                NatureType.AI, 
                VoiceDescription.Default, 
                GenderType.Male, 
                AgeType.Senior,
                firstName: "Bob");
        }
        
        private static PingPongState CreateInitialState(DummyActivationEvent activation)
        {
            return new PingPongState(
                Radio: activation.Radio, 
                PendingTransmissionIntent: null, 
                Role: activation.Role, 
                Counterparty: null,
                RepeatCount: 0);
        }
    }
}

#endif