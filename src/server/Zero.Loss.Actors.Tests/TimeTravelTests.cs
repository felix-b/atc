using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using System.Text.Json;
using NUnit.Framework;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors.Tests
{
    [TestFixture]
    public class TimeTravelTests
    {
        [Test]
        public void CanTakeSnapshot()
        {
            var store = new StateStore(new NoopStateStoreLogger());
            var dependencies = SimpleDependencyContext.NewWithStore(store);
            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var initialSnapshot1 = supervisor.TimeTravel.TakeSnapshot();
            var initialSnapshot2 = supervisor.TimeTravel.TakeSnapshot();
            AssertSnapshotsMatch(initialSnapshot1, initialSnapshot2);

            var parent1 = ParentActor.Create(supervisor, "ONE");
            var parent2 = ParentActor.Create(supervisor, "TWO");

            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            var finalSnapshot1 = supervisor.TimeTravel.TakeSnapshot();
            var finalSnapshot2 = supervisor.TimeTravel.TakeSnapshot();
            AssertSnapshotsMatch(finalSnapshot1, finalSnapshot2);
            AssertSnapshotsDoNotMatch(initialSnapshot1, finalSnapshot1);
        }

        [Test]
        public void CanRewindAndReplayAll_TrivialGraph()
        {
            var eventLog = new List<StateEventEnvelope>();
            var store = new StateStore(new NoopStateStoreLogger());
            store.AddEventListener((in StateEventEnvelope e) => eventLog.Add(e), out _);
            
            var dependencies = SimpleDependencyContext.NewWithStore(store);
            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var initialSnapshot = supervisor.TimeTravel.TakeSnapshot();

            var parent1 = ParentActor.Create(supervisor, "ONE");
            var parent2 = ParentActor.Create(supervisor, "TWO");

            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            var eventCountBeforeRewind = eventLog.Count;
            eventCountBeforeRewind.Should().BeGreaterOrEqualTo(2);
            
            supervisor.TimeTravel.RestoreSnapshot(initialSnapshot);

            Assert.Throws<ActorNotFoundException>(() => parent1.Get());
            Assert.Throws<ActorNotFoundException>(() => parent2.Get());

            var copyOfEventLog = eventLog.ToArray();
            eventLog.Clear();
            
            supervisor.TimeTravel.ReplayEvents(copyOfEventLog);
            
            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            eventLog.Count.Should().Be(eventCountBeforeRewind);
        }

        [Test]
        public void CanRewindBackAndForth_TrivialGraph()
        {
            var eventLog = new List<StateEventEnvelope>();
            var store = new StateStore(new NoopStateStoreLogger());
            store.AddEventListener((in StateEventEnvelope e) => eventLog.Add(e), out _);
            
            var dependencies = SimpleDependencyContext.NewWithStore(store);
            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var initialSnapshot = supervisor.TimeTravel.TakeSnapshot();

            var parent1 = ParentActor.Create(supervisor, "ONE");
            var parent2 = ParentActor.Create(supervisor, "TWO");

            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            var eventCountBeforeRewind = eventLog.Count;
            var finalSnapshot = supervisor.TimeTravel.TakeSnapshot();

            supervisor.TimeTravel.RestoreSnapshot(initialSnapshot);

            Assert.Throws<ActorNotFoundException>(() => parent1.Get());
            Assert.Throws<ActorNotFoundException>(() => parent2.Get());

            supervisor.TimeTravel.RestoreSnapshot(finalSnapshot);
            
            parent1.Get().Str.Should().Be("ONE");
            parent2.Get().Str.Should().Be("TWO");

            eventLog.Count.Should().Be(eventCountBeforeRewind);
        }

        [Test]
        public void CanTimeTravel_GraphWithRefs()
        {
            var eventLogBuilder = new List<StateEventEnvelope>();
            var store = new StateStore(new NoopStateStoreLogger());
            store.AddEventListener((in StateEventEnvelope e) => eventLogBuilder.Add(e), out _);
            
            var dependencies = SimpleDependencyContext.NewWithStore(store);
            var supervisor = new SupervisorActor(store, dependencies);
            ParentActor.RegisterType(supervisor);
            ChildActor.RegisterType(supervisor);

            var initialSnapshot = supervisor.TimeTravel.TakeSnapshot();
            var beginningSnapshot = supervisor.TimeTravel.TakeSnapshot();
            var middle1Snapshot = supervisor.TimeTravel.TakeSnapshot();
            var middle2Snapshot = supervisor.TimeTravel.TakeSnapshot();
            var finalSnapshot = supervisor.TimeTravel.TakeSnapshot();

            PerformInputs();

            var eventLog = eventLogBuilder.ToImmutableArray();
            eventLog[^1].SequenceNo.Should().Be(store.NextSequenceNo - 1);
            
            supervisor.TimeTravel.RestoreSnapshot(middle1Snapshot);
            AssertSnapshotsMatch(middle1Snapshot, supervisor.TimeTravel.TakeSnapshot());

            supervisor.TimeTravel.ReplayEvents(GetEventSubSequence(middle1Snapshot.NextSequenceNo, middle2Snapshot.NextSequenceNo));
            AssertSnapshotsMatch(middle2Snapshot, supervisor.TimeTravel.TakeSnapshot());

            supervisor.TimeTravel.ReplayEvents(GetEventSubSequence(middle2Snapshot.NextSequenceNo, eventLog[^1].SequenceNo + 1));
            AssertSnapshotsMatch(finalSnapshot, supervisor.TimeTravel.TakeSnapshot());

            Console.WriteLine(JsonSerializer.Serialize(finalSnapshot));
            
            void PerformInputs()
            {
                var parent1 = ParentActor.Create(supervisor, "ONE");
                var child1 = ChildActor.Create(supervisor, 123);
                parent1.Get().AddChild(child1);

                beginningSnapshot = supervisor.TimeTravel.TakeSnapshot();

                child1.Get().UpdateNum(456);
                var child2 = ChildActor.Create(supervisor, 987);
                parent1.Get().AddChild(child2);

                middle1Snapshot = supervisor.TimeTravel.TakeSnapshot();

                parent1.Get().RemoveChild(child1);
                supervisor.DeleteActor(child1);
                parent1.Get().UpdateStr("ONE-tag");

                middle2Snapshot = supervisor.TimeTravel.TakeSnapshot();

                var parent2 = ParentActor.Create(supervisor, "TWO");

                finalSnapshot = supervisor.TimeTravel.TakeSnapshot();
            }

            IEnumerable<StateEventEnvelope> GetEventSubSequence(ulong sinceSequenceNo, ulong beforeSequenceNo)
            {
                return eventLog
                    .SkipWhile(e => e.SequenceNo < sinceSequenceNo)
                    .TakeWhile(e => e.SequenceNo < beforeSequenceNo);
            }
        }

        private void AssertSnapshotsMatch(ActorStateSnapshot expected, ActorStateSnapshot actual)
        {
            var expectedJson =  JsonSerializer.Serialize(expected);
            var actualJson =  JsonSerializer.Serialize(actual);
            Assert.That(actualJson, Is.EqualTo(expectedJson));
        }
        
        private void AssertSnapshotsDoNotMatch(ActorStateSnapshot expected, ActorStateSnapshot actual)
        {
            var expectedJson =  JsonSerializer.Serialize(expected);
            var actualJson =  JsonSerializer.Serialize(actual);
            Assert.That(actualJson, Is.Not.EqualTo(expectedJson));
        }
    }
}
