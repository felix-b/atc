using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors.Tests.Impl
{
    [TestFixture]
    public class StateStoreTests
    {
        [Test]
        public void CanDispatchEventToTargetActor()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var actor = new TestActor("test");

            //TODO: move this to StatefulActorTests
            actor.ValueA.Should().Be("INIT-A");
            actor.ValueB.Should().Be("INIT-B");

            store.Dispatch(actor, new TestActor.ChangeValueBEvent("NEW-B"));
            
            actor.ValueA.Should().Be("INIT-A");
            actor.ValueB.Should().Be("NEW-B");
        }

        [Test]
        public void CanInvokeTargetActorObserver()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var actor = new TestActor("test");
            var log = new List<string>();
            
            actor.Observer += (state0, state1) => {
                log.Add($"{state0}->{state1}");
            };

            store.Dispatch(actor, new TestActor.ChangeValueBEvent("NEW-B"));

            log.Should().BeEquivalentTo(new[] {"A[INIT-A],B[INIT-B]->A[INIT-A],B[NEW-B]"});
            log.Clear();
            
            store.Dispatch(actor, new TestActor.ChangeValueAEvent("NEW-A"));
            
            log.Should().BeEquivalentTo(new[] {"A[INIT-A],B[NEW-B]->A[NEW-A],B[NEW-B]"});
        }

        [Test]
        public void CanInvokeListeners()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var actor = new TestActor("test#1");
            var log = new List<string>();
            
            store.AddEventListener(
                (in StateEventEnvelope e) => log.Add($"L1#{e.SequenceNo}|{e.TargetUniqueId}|{e.Event}"), 
                out var listenerId1);
            store.AddEventListener(
                (in StateEventEnvelope e) => log.Add($"L2#{e.SequenceNo}|{e.TargetUniqueId}|{e.Event}"), 
                out var listenerId2);

            store.Dispatch(actor, new TestActor.ChangeValueAEvent("NEW-A"));
            
            log.Should().BeEquivalentTo(new[] {"L1#1|test#1|A:NEW-A", "L2#1|test#1|A:NEW-A"});
            actor.ValueA.Should().Be("NEW-A");
            actor.ValueB.Should().Be("INIT-B");
            
            log.Clear();
            store.Dispatch(actor, new TestActor.ChangeValueBEvent("NEW-B"));

            log.Should().BeEquivalentTo(new[] {"L1#2|test#1|B:NEW-B", "L2#2|test#1|B:NEW-B"});
            actor.ValueA.Should().Be("NEW-A");
            actor.ValueB.Should().Be("NEW-B");
        }

        [Test]
        public void CanSkipFailingListeners()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var actor = new TestActor("test#1");
            var log = new List<string>();
            
            store.AddEventListener(
                (in StateEventEnvelope e) => throw new Exception("TEST-ERROR"), 
                out var listenerId1);
            store.AddEventListener(
                (in StateEventEnvelope e) => log.Add($"L2#{e.SequenceNo}|{e.Event}"), 
                out var listenerId2);

            store.Dispatch(actor, new TestActor.ChangeValueAEvent("NEW-A"));
            
            log.Should().BeEquivalentTo(new[] {"L2#1|A:NEW-A"});
            actor.ValueA.Should().Be("NEW-A");
            actor.ValueB.Should().Be("INIT-B");
            
            log.Clear();
            store.Dispatch(actor, new TestActor.ChangeValueBEvent("NEW-B"));

            log.Should().BeEquivalentTo(new[] {"L2#2|B:NEW-B"});
            actor.ValueA.Should().Be("NEW-A");
            actor.ValueB.Should().Be("NEW-B");
        }

        public class TestActor : StatefulActor<TestActor.MyState>
        {
            public static readonly string TypeString = "test-actor";
            
            public record MyState(string ValueA, string ValueB) : IStateEvent
            {
                public override string ToString() => $"A[{ValueA}],B[{ValueB}]";
            }

            public record ChangeValueAEvent(string NewValueA) : IStateEvent
            {
                public override string ToString() => $"A:{NewValueA}";
            }

            public record ChangeValueBEvent(string NewValueB) : IStateEvent
            {
                public override string ToString() => $"B:{NewValueB}";
            }

            public TestActor(string uniqueId) 
                : base(TypeString, uniqueId, new MyState(ValueA: "INIT-A", ValueB: "INIT-B"))
            {
            }

            public string ValueA => State.ValueA;
            public string ValueB => State.ValueB;
            
            public Action<MyState, MyState> Observer { get; set; }

            protected override MyState Reduce(MyState stateBefore, IStateEvent @event)
            {
                switch (@event)
                {
                    case ChangeValueAEvent changeA:
                        return stateBefore with {ValueA = changeA.NewValueA};
                    case ChangeValueBEvent changeB:
                        return stateBefore with {ValueB = changeB.NewValueB};
                    default:
                        return stateBefore;
                }
            }

            protected override void ObserveChanges(MyState oldState, MyState newState)
            {
                Observer?.Invoke(oldState, newState);
            }
        }
    }
}