using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using Atc.World.Tests.Comms;
using Zero.Loss.Actors;

namespace Atc.World.Tests.AI
{
    public class DummyPingPongActor : AIRadioOperatingActor<DummyPingPongActor.PingPongState>
    {
        public static readonly string TypeString = "dummy-ping-pong";

        public enum PingPongRole
        {
            Ping = 1,
            Pong = 2
        }
        
        public record PingPongState(
            ActorRef<RadioStationActor> Radio,
            Intent? PendingTransmissionIntent,
            ImmutableStateMachine StateMachine,
            PingPongRole Role,
            ActorRef<DummyPingPongActor>? Counterparty,
            int RepeatCount
        ) : AIRadioOperatorState(Radio, PendingTransmissionIntent, StateMachine);

        public record DummyActivationEvent(
            string UniqueId, 
            ActorRef<RadioStationActor> Radio,
            PingPongRole Role
        ) : RadioOperatorActivationEvent(UniqueId, Radio), IActivationStateEvent<DummyPingPongActor>;

        public record IncrementRepeatCountEvent : IStateEvent;

        public record SetCounterpartyEvent(ActorRef<DummyPingPongActor> Counterparty) : IStateEvent;

        [NotEventSourced]
        private readonly List<string> _intentLog = new(); 

        public DummyPingPongActor(
            IStateStore store, 
            IVerbalizationService verbalizationService, 
            IWorldContext world, 
            AIRadioOperatingActor.ILogger logger,
            DummyActivationEvent activation) 
            : base(
                TypeString, store, verbalizationService, world, logger, CreateParty(activation), activation, CreateInitialState(activation))
        {
            World.Defer("ignite_ping_pong_cycle", () => {
                DispatchStateMachineEvent(new ImmutableStateMachine.TriggerEvent(
                    Age: State.StateMachine.Age,
                    State.Role == PingPongRole.Ping
                        ? "START_PING_ROLE"
                        : "START_PONG_ROLE"));
            });
        }

        public void SetCounterparty(ActorRef<DummyPingPongActor> counterparty)
        {
            Store.Dispatch(this, new SetCounterpartyEvent(counterparty));
        }
        
        public IReadOnlyList<string> IntentLog => _intentLog;

        public string Callsign => Radio.Callsign;
        
        protected override PingPongState Reduce(PingPongState stateBefore, IStateEvent @event)
        {
            switch (@event)
            {
                case IncrementRepeatCountEvent:
                    return stateBefore with {
                        RepeatCount = stateBefore.RepeatCount + 1
                    };
                case SetCounterpartyEvent setCounterparty:
                    return stateBefore with {
                        Counterparty = setCounterparty.Counterparty
                    };
                default:
                    return base.Reduce(stateBefore, @event);
            }
        }

        protected override void ReceiveIntent(Intent intent)
        {
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
                state => state.OnTimeout(TimeSpan.FromSeconds(5), transitionTo: "PING")
            );
            
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
                    dependencies.Resolve<AIRadioOperatingActor.ILogger>(), 
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
                StateMachine: ImmutableStateMachine.Empty,
                Role: activation.Role, 
                Counterparty: null,
                RepeatCount: 0);
        }
    }
}
