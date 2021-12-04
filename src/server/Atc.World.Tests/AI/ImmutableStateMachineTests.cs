using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.AI;
using Atc.World.Comms;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using NUnit.Framework;
using Zero.Loss.Actors;

namespace Atc.World.Tests.AI
{
    [TestFixture]
    public class ImmutableStateMachineTests
    {
        private static ImmutableStateMachine BuildTestStateMachine(
            Action<ImmutableStateMachine.RegularStateBuilder>? onAddStateBegin = null,
            Action<ImmutableStateMachine.RegularStateBuilder>? onAddStateMiddle = null,
            Action<ImmutableStateMachine.RegularStateBuilder>? onAddStateEnd = null, 
            ImmutableStateMachine.DispatchEventCallback? onDispatchEvent = null,
            ImmutableStateMachine.ScheduleDelayCallback onScheduleDelay = null)
        {
            var effectiveDispatchEvent =
                onDispatchEvent ?? new ImmutableStateMachine.DispatchEventCallback(e => { });

            var effectiveScheduleDelay =
                onScheduleDelay ?? new ImmutableStateMachine.ScheduleDelayCallback((t, onDue) => IDeferHandle.Noop);

            var builder = new ImmutableStateMachine.Builder("BEGIN", effectiveDispatchEvent, effectiveScheduleDelay);

            builder.AddState("BEGIN", s => {
                s.OnTrigger("A", transitionTo: "MIDDLE");
                onAddStateBegin?.Invoke(s);
            });
            builder.AddState("MIDDLE", s => {
                s.OnTrigger("B", transitionTo: "END");
                s.OnTrigger("C", transitionTo: "BEGIN");
                onAddStateMiddle?.Invoke(s);
            });
            builder.AddState("END", s => {
                onAddStateEnd?.Invoke(s);
            });
            
            return builder.Build();
        }
        
        [Test]
        public void CanBuildStateMachine()
        {
            var stateMachine = BuildTestStateMachine();

            stateMachine.Should().NotBeNull();
            stateMachine.StateByName.Keys.Should().BeEquivalentTo(new[] {"BEGIN", "MIDDLE", "END"});

            var stateBegin = stateMachine.StateByName["BEGIN"];
            var stateMiddle = stateMachine.StateByName["MIDDLE"];
            var stateEnd = stateMachine.StateByName["END"];

            stateMachine.State.Should().BeSameAs(stateBegin);
            
            stateBegin.Name.Should().Be("BEGIN");
            stateBegin.TransitionByTriggerId.Keys.Should().BeEquivalentTo(new[] {"A"});
            stateBegin.TransitionByTriggerId["A"].TriggerId.Should().Be("A");
            stateBegin.TransitionByTriggerId["A"].TargetStateName.Should().Be("MIDDLE");

            stateMiddle.Name.Should().Be("MIDDLE");
            stateMiddle.TransitionByTriggerId.Keys.Should().BeEquivalentTo(new[] {"B", "C"});
            stateMiddle.TransitionByTriggerId["B"].TargetStateName.Should().Be("END");
            stateMiddle.TransitionByTriggerId["C"].TargetStateName.Should().Be("BEGIN");

            stateEnd.Name.Should().Be("END");
            stateEnd.TransitionByTriggerId.Keys.Should().BeEmpty();
        }

        private class TestMachineWithEventsContainer
        {
            private readonly ImmutableStateMachine _machine0;
            private ImmutableStateMachine _machineN;

            public TestMachineWithEventsContainer(MachineFactoryCallback machineFactory)
            {
                _machine0 = machineFactory(DispatchEvent);
                _machineN = _machine0;
                _machine0.Start();
            }

            public ImmutableStateMachine Machine0 => _machine0;
            public ImmutableStateMachine MachineN => _machineN;

            private void DispatchEvent(IStateEvent @event)
            {
                var machineEvent = (IImmutableStateMachineEvent) @event;  
                _machineN = ImmutableStateMachine.Reduce(_machineN, machineEvent);
                _machineN.Start();
            }

            public delegate ImmutableStateMachine MachineFactoryCallback(ImmutableStateMachine.DispatchEventCallback dispatchEvent);
        }

        [Test]
        public void CanReceiveTriggerAndTransition()
        {
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(onDispatchEvent: dispatchEvent));

            machines.MachineN.State.Name.Should().Be("BEGIN");
            machines.MachineN.ReceiveTrigger("A");
            machines.MachineN.State.Name.Should().Be("MIDDLE");
            machines.MachineN.ReceiveTrigger("B");
            machines.MachineN.State.Name.Should().Be("END");
            machines.MachineN.Should().NotBeSameAs(machines.Machine0);
        }


        [Test]
        public void CanInvokeOnEnterCallback()
        {
            var log = new List<string>();
            ImmutableStateMachine.StateEnterCallback callback = machine => {
                log.Add("callback");
            };
            
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateMiddle: state => state.OnEnter(callback),
                    onDispatchEvent: dispatchEvent
                )
            );
            
            machines.MachineN.ReceiveTrigger("A");
            
            log.Should().BeStrictlyEquivalentTo("callback");
            log.Clear();

            machines.MachineN.ReceiveTrigger("B");
            
            log.Should().BeEmpty();
        }
        
        [Test]
        public void CanBuildSequenceWithSimpleSteps()
        {
            ImmutableStateMachine.StateEnterCallback step1 = m => { };
            ImmutableStateMachine.StateEnterCallback step2 = m => { };

            var machine = BuildTestStateMachine(
                onAddStateMiddle: state => state.OnEnterStartSequence(sequence => {
                    sequence.AddStep("S1", step1).AddStep("S2", step2);
                })
            );

            machine.StateByName.Keys.Should().BeEquivalentTo(new[] {
                "BEGIN", "MIDDLE", "MIDDLE/S1", "MIDDLE/S2", "MIDDLE/$READY", "END"
            });
        }

        [Test]
        public void CanRunSequenceWithSimpleSteps()
        {
            var log = new List<string>();
            ImmutableStateMachine.StateEnterCallback step1 = m => log.Add("step1");
            ImmutableStateMachine.StateEnterCallback step2 = m => log.Add("step2");
            
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateMiddle: state => state.OnEnterStartSequence(sequence => {
                        sequence.AddStep("S1", step1).AddStep("S2", step2);
                    }),
                    onDispatchEvent: dispatchEvent
                )
            );
            
            machines.MachineN.ReceiveTrigger("A");
            log.Should().BeStrictlyEquivalentTo("step1", "step2");
        }

        [Test] 
        public void CanTriggerTransitionByIntent()
        {
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateBegin: state => state.OnIntent<TestIntentOne>(
                        transitionTo: "END"    
                    ),
                    onDispatchEvent: dispatchEvent
                )
            );
            
            machines.MachineN.ReceiveIntent(new TestIntentOne());

            machines.MachineN.State.Name.Should().Be("END");
        }

        [Test] 
        public void CanMemorizeReceivedIntent()
        {
            var intent1 = new TestIntentOne();
            var intent2 = new TestIntentTwo();
            
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateBegin: state => state.OnIntent<TestIntentOne>(
                        transitionTo: "MIDDLE",
                        memorizeIntent: true
                    ),
                    onAddStateMiddle: state => state.OnIntent<TestIntentTwo>(
                        transitionTo: "END",
                        memorizeIntent: true
                    ),
                    onDispatchEvent: dispatchEvent
                )
            );
            
            machines.MachineN.State.Name.Should().Be("BEGIN");
            machines.MachineN.ReceiveIntent(intent1);
            machines.MachineN.State.Name.Should().Be("MIDDLE");
            machines.MachineN.MemorizedIntentByType.Keys.Should().BeEquivalentTo(typeof(TestIntentOne));
            machines.MachineN.ReceiveIntent(intent2);
            machines.MachineN.State.Name.Should().Be("END");
            machines.MachineN.MemorizedIntentByType.Keys.Should().BeEquivalentTo(
                typeof(TestIntentOne), typeof(TestIntentTwo)
            );

            machines.MachineN.GetMemorizedIntent<TestIntentOne>().Should().BeSameAs(intent1);
            machines.MachineN.GetMemorizedIntent<TestIntentTwo>().Should().BeSameAs(intent2);
        }

        [Test] 
        public void CanRunSequenceWithDelayStep()
        {
            var log = new List<string>();
            Action onDelayDue = () => { };
            
            ImmutableStateMachine.ScheduleDelayCallback scheduleDelay = (time, onDue) => {
                log.Add($"schedule-delay({time})");
                onDelayDue = onDue;
                return IDeferHandle.Noop;
            };
            
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateBegin: state => {
                        state.OnEnterStartSequence(
                            sequence => sequence
                                .AddDelayStep("S1", TimeSpan.FromMinutes(1), inheritTriggers: false)
                                .AddStep("S2", m => log.Add("step-S2"))
                        );
                    },
                    onDispatchEvent: dispatchEvent,
                    onScheduleDelay: scheduleDelay 
                )
            );
            
            machines.MachineN.State.Name.Should().Be("BEGIN/S1");

            log.Should().BeEquivalentTo("schedule-delay(00:01:00)");

            onDelayDue();
            
            log.Should().BeStrictlyEquivalentTo(
                "schedule-delay(00:01:00)",
                "step-S2"
            );
        }

        [Test] 
        public void CanRunSequenceWithTriggerStep()
        {
            var log = new List<string>();
            
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateBegin: state => state
                        .OnEnterStartSequence(sequence => sequence
                            .AddTriggerStep("S1", "TEST-TRIGGER")
                            .AddStep("S2", m => log.Add("step-S2")))
                        .OnTrigger(
                            "TEST-TRIGGER", 
                            transitionTo: "END"),
                    onDispatchEvent: dispatchEvent
                )
            );
            
            machines.MachineN.State.Name.Should().Be("END");
            log.Should().BeEmpty();
        }

        [Test]
        public void CanRunSequenceWithTransitionStep()
        {
            var machines = new TestMachineWithEventsContainer(
                dispatchEvent => BuildTestStateMachine(
                    onAddStateBegin: state => state.OnTrigger("A", transitionTo: "MIDDLE"),
                    onAddStateMiddle: state => state.OnEnterStartSequence(sequence => {
                        sequence.AddTransitionStep("S1", "END");
                    }),
                    onDispatchEvent: dispatchEvent
                )
            );
            machines.MachineN.State.Name.Should().Be("BEGIN");
            machines.MachineN.ReceiveTrigger("A");
            machines.MachineN.State.Name.Should().Be("END");
        }

        [Test]
        public void CanBuildConversationState()
        {
            var builder = new ImmutableStateMachine.Builder(
                initialStateName: "BEGIN", 
                dispatchEvent: e => { }, 
                scheduleDelay: (e, t) => IDeferHandle.Noop);

            var actor = Mock.Of<IRadioOperatingActor>();
            var intentToTransmit = new TestIntentOne();
            //var intentToReceive = new TestIntentTwo();
            var intentToReadBack = new TestIntentThree();

            builder.AddConversationState(actor, "BEGIN", state => state
                .Monitor(Frequency.FromKhz(123450))
                .Transmit(() => intentToTransmit)
                .Receive<TestIntentTwo>(memorizeIntent: true, readback: () => intentToReadBack, transitionTo: "END")
            );
            builder.AddState("END", s => { });

            var machine = builder.Build();

            machine.StateByName.Keys.Should().BeEquivalentTo(new[] {
                "BEGIN",
                "BEGIN/TRANSMIT/AWAIT_SILENCE",
                "BEGIN/TRANSMIT/TRANSMIT",                
                "BEGIN/AWAIT_RECEIVE",
                "BEGIN/READBACK_OF_TESTINTENTTWO/AWAIT_SILENCE",
                "BEGIN/READBACK_OF_TESTINTENTTWO/TRANSMIT",
                "END"
            }, options => options.WithoutStrictOrdering());
        }

        [Test]
        public void RunConversationState_MonitorFrequencyAndInitiateTransmission()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var intentToTransmit = new TestIntentOne();
            var intentToReceive = new TestIntentTwo();
            var intentToReadBack = new TestIntentThree();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Monitor(Frequency.FromKhz(123450))
                    .Transmit(() => intentToTransmit)
                    .Receive<TestIntentTwo>(
                        memorizeIntent: true, 
                        readback: () => intentToReadBack, 
                        transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.State.Name.Should().Be("BEGIN/TRANSMIT/AWAIT_SILENCE");
            actorMock.Verify(x => x.MonitorFrequency(Frequency.FromKhz(123450)), Times.Once);
            actorMock.Verify(x => x.InitiateTransmission(intentToTransmit), Times.Once);
        }

        [Test]
        public void RunConversationState_TransmitInitialIntent()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var intentToTransmit = new TestIntentOne();
            var intentToReceive = new TestIntentTwo();
            var intentToReadBack = new TestIntentThree();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Monitor(Frequency.FromKhz(123450))
                    .Transmit(() => intentToTransmit)
                    .Receive<TestIntentTwo>(
                        memorizeIntent: true, 
                        readback: () => intentToReadBack, 
                        transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
            machines.MachineN.State.Name.Should().Be("BEGIN/TRANSMIT/TRANSMIT");
            
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
            machines.MachineN.State.Name.Should().Be("BEGIN/AWAIT_RECEIVE");
        }

        [Test]
        public void RunConversationState_ReceiveIntent()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var intentToTransmit = new TestIntentOne();
            var intentToReceive = new TestIntentTwo();
            var intentToReadBack = new TestIntentThree();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Monitor(Frequency.FromKhz(123450))
                    .Transmit(() => intentToTransmit)
                    .Receive<TestIntentTwo>(
                        memorizeIntent: true, 
                        readback: () => intentToReadBack, 
                        transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
            machines.MachineN.ReceiveIntent(intentToReceive);

            machines.MachineN.State.Name.Should().Be("BEGIN/READBACK_OF_TESTINTENTTWO/AWAIT_SILENCE");
        }

        [Test]
        public void RunConversationState_MemorizeReceivedIntent()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var intentToTransmit = new TestIntentOne();
            var intentToReceive = new TestIntentTwo();
            var intentToReadBack = new TestIntentThree();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Monitor(Frequency.FromKhz(123450))
                    .Transmit(() => intentToTransmit)
                    .Receive<TestIntentTwo>(
                        memorizeIntent: true, 
                        readback: () => intentToReadBack, 
                        transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
            machines.MachineN.ReceiveIntent(intentToReceive);
            machines.MachineN.GetMemorizedIntent<TestIntentTwo>().Should().BeSameAs(intentToReceive);
        }

        [Test]
        public void RunConversationState_TransmitReadbackIntent()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var intentToTransmit = new TestIntentOne();
            var intentToReceive = new TestIntentTwo();
            var intentToReadBack = new TestIntentThree();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Monitor(Frequency.FromKhz(123450))
                    .Transmit(() => intentToTransmit)
                    .Receive<TestIntentTwo>(
                        memorizeIntent: true, 
                        readback: () => intentToReadBack, 
                        transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
            
            machines.MachineN.ReceiveIntent(intentToReceive);

            machines.MachineN.State.Name.Should().Be("BEGIN/READBACK_OF_TESTINTENTTWO/AWAIT_SILENCE");
            actorMock.Verify(x => x.InitiateTransmission(intentToReadBack), Times.Once);
            actorMock.Verify(x => x.InitiateTransmission(It.IsAny<Intent>()), Times.Exactly(2));
            
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionStartedTriggerId);
            machines.MachineN.ReceiveTrigger(AIRadioOperatingActor.TransmissionFinishedTriggerId);
            
            machines.MachineN.State.Name.Should().Be("END");
            actorMock.Verify(x => x.InitiateTransmission(It.IsAny<Intent>()), Times.Exactly(2));
        }

        [Test]
        public void RunConversationState_MonitorOnly()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Monitor(Frequency.FromKhz(123450))
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.State.Name.Should().Be("BEGIN/$READY");
            actorMock.Verify(x => x.MonitorFrequency(Frequency.FromKhz(123450)), Times.Once);
        }

        [Test]
        public void RunConversationState_TransmitOnly()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();
            var intentToTransmit = new TestIntentOne();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop; 
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Transmit(() => intentToTransmit, transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.State.Name.Should().Be("BEGIN/TRANSMIT/AWAIT_SILENCE");
            actorMock.Verify(x => x.MonitorFrequency(It.IsAny<Frequency>()), Times.Never);
            actorMock.Verify(x => x.InitiateTransmission(intentToTransmit), Times.Once);
        }
        
        [Test]
        public void RunConversationState_ReceiveOnly()
        {
            var log = new List<string>();
            var actorMock = new Mock<IRadioOperatingActor>();

            var intentToReceive = new TestIntentTwo();

            var machines = new TestMachineWithEventsContainer(dispatchEvent => {
                var builder = new ImmutableStateMachine.Builder(
                    initialStateName: "BEGIN", 
                    dispatchEvent, 
                    scheduleDelay: (t, f) => {
                        log.Add($"scheudle-delay({t})");
                        return IDeferHandle.Noop;
                    });

                builder.AddConversationState(actorMock.Object, "BEGIN", state => state
                    .Receive<TestIntentTwo>(
                        memorizeIntent: true, 
                        transitionTo: "END")
                );
                builder.AddState("END", s => { });
                return builder.Build();
            });
            
            machines.MachineN.State.Name.Should().Be("BEGIN/AWAIT_RECEIVE");
            machines.MachineN.ReceiveIntent(intentToReceive);
            machines.MachineN.GetMemorizedIntent<TestIntentTwo>().Should().BeSameAs(intentToReceive);
            machines.MachineN.State.Name.Should().Be("END");
        }
        

        private record TestIntentOne() : Intent(
            CreateHeader(),
            IntentOptions.Default
        ) {
            private static IntentHeader CreateHeader() => new IntentHeader(
                WellKnownIntentType.Custom,
                123,
                "#1",
                "#1",
                null,
                null,
                DateTime.UtcNow);
        }

        private record TestIntentTwo() : Intent(
            CreateHeader(),
            IntentOptions.Default
        ) {
            private static IntentHeader CreateHeader() => new IntentHeader(
                WellKnownIntentType.Custom,
                456,
                "#2",
                "#2",
                null,
                null,
                DateTime.UtcNow);
        }

        private record TestIntentThree() : Intent(
            CreateHeader(),
            IntentOptions.Default
        ) {
            private static IntentHeader CreateHeader() => new IntentHeader(
                WellKnownIntentType.Custom,
                789,
                "#3",
                "#3",
                null,
                null,
                DateTime.UtcNow);
        }
    }
}
